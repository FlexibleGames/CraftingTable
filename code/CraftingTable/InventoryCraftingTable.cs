using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;
using Vintagestory.API.MathTools;

namespace CraftingTable
{
    public class InventoryCraftingTable : InventoryBase, ISlotProvider          
    {
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        
        public InventoryCraftingTable(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            this.slots = base.GenEmptySlots(this.GridSizeSq);
            this.outputSlot = new ItemSlotCraftingTableOutput(this);
            //this.InvNetworkUtil = new InventoryNetworkUtil(this, api);
            this.InvNetworkUtil = new CraftingInventoryNetworkUtil(this, api);
        }

        public InventoryCraftingTable(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api)
        {
            this.slots = base.GenEmptySlots(this.GridSizeSq);
            this.outputSlot = new ItemSlotCraftingTableOutput(this);
            //this.InvNetworkUtil = new InventoryNetworkUtil(this, api);
            this.InvNetworkUtil = new CraftingInventoryNetworkUtil(this, api);
        }

        public void SetPlayer(IPlayer player)
        {
            this.tableuser = player;
        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            (InvNetworkUtil as CraftingInventoryNetworkUtil).Api = api;            
            if (api.Side == EnumAppSide.Server)
            {
                sapi = api as ICoreServerAPI;
            }
            else
            {
                capi = api as ICoreClientAPI;
            }

        }

        internal void BeginCraft()
        {
            this.isCrafting = true;
        }

        internal void EndCraft()
        {
            this.isCrafting = false;
            this.FindMatchingRecipe();
        }

        public bool CanStillCraftCurrent()
        {            
            if (tableuser != null)
            {
                return this.MatchingRecipe != null && this.MatchingRecipe.Matches(tableuser, slots, GridSize);
            }
            return this.MatchingRecipe != null;
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            if (stack == this.outputSlot.Itemstack)
            {
                return 0f;
            }    
            return base.GetTransitionSpeedMul(transType, stack);
        }

        public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            object result;
            if (slotId == this.GridSizeSq)
            {
                this.BeginCraft();
                result = base.ActivateSlot(slotId, sourceSlot, ref op);
                if (!this.outputSlot.Empty && op.ShiftDown)
                {
                    if (this.Api.Side == EnumAppSide.Client)
                    {
                        this.outputSlot.Itemstack = null;
                    }
                    else
                    {                        
                        Api.World.SpawnItemEntity(outputSlot.Itemstack, this.Pos.ToVec3d());                                              
                    }
                }
                this.EndCraft();
            }
            else
            {
                result = base.ActivateSlot(slotId, sourceSlot, ref op);
            }
            return result;
        }

        public override void OnItemSlotModified(ItemSlot slot)
        {
            if (this.isCrafting)
            {
                return;
            }
            if (slot is ItemSlotCraftingTableOutput)
            {
                return;
            }
            this.FindMatchingRecipe();
        }

        public override void OnOwningEntityDeath(Vec3d pos)
        {
            foreach (ItemSlot itemSlot in this)
            {
                if (!(itemSlot is ItemSlotCraftingTableOutput) && !itemSlot.Empty)
                {
                    this.Api.World.SpawnItemEntity(itemSlot.Itemstack, Pos.ToVec3d(), null);
                    itemSlot.Itemstack = null;
                    itemSlot.MarkDirty();
                }
            }
        }

        public override void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
        {
            base.DidModifyItemSlot(slot, extractedStack);
        }

        public void FindMatchingRecipe()
        {
            this.MatchingRecipe = null;
            this.outputSlot.Itemstack = null;            
            List<GridRecipe> gridRecipes = this.Api.World.GridRecipes;            

            IPlayer forPlayer = tableuser;
            if (tableuser == null && Api.Side == EnumAppSide.Client)
            {
                forPlayer = capi.World.Player;
                tableuser = capi.World.Player;
            }

            for (int i = 0; i < gridRecipes.Count; i++)
            {
                GridRecipe gridRecipe = gridRecipes[i];
                if (gridRecipe.Enabled && gridRecipe.Matches(forPlayer, this.slots, this.GridSize))
                {
                    this.MatchingRecipe = gridRecipes[i];
                    this.outputSlot.Itemstack = this.MatchingRecipe.Output.ResolvedItemstack.Clone();
                    if (gridRecipe.CopyAttributesFrom != null)
                    {
                        ItemStack inputStackForPatternCode = gridRecipe.GetInputStackForPatternCode(gridRecipe.CopyAttributesFrom, this.slots);
                        if (inputStackForPatternCode != null)
                        {
                            ITreeAttribute treeAttribute = inputStackForPatternCode.Attributes.Clone();
                            treeAttribute.MergeTree(this.outputSlot.Itemstack.Attributes);
                            this.outputSlot.Itemstack.Attributes = treeAttribute;
                        }
                    }
                    this.outputSlot.Itemstack.Collectible.OnCreatedByCrafting(this.slots, this.outputSlot, gridRecipes[i]);
                    this.dirtySlots.Add(this.GridSizeSq);
                    return;
                }
            }
            this.dirtySlots.Add(this.GridSizeSq);
        }
        
        public void ConsumeIngredients()
        {
            if (this.MatchingRecipe == null)
            {
                return;
            }
            this.MatchingRecipe.ConsumeInput(tableuser, slots, GridSize);
            for (int i = 0; i < this.GridSizeSq + 1; i++)
            {
                this.dirtySlots.Add(i);
            }
        } 

        public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, List<ItemSlot> skipSlots = null)
        {
            return base.GetBestSuitedSlot(sourceSlot, skipSlots);
        }        

        public ItemSlot[] Slots
        {
            get
            {
                return this.slots;
            }
        }

        /// <summary>
        /// Access Crafting Table Slots
        /// </summary>
        /// <param name="slotId">0-8 is grid, 9 is output</param>
        /// <returns></returns>
        public override ItemSlot this[int slotId] 
        { 
            get
            {
                if (slotId < 0 || slotId >= this.Count)
                {
                    return null;
                }
                if (slotId == this.GridSizeSq)
                {
                    return this.outputSlot;
                }
                return this.slots[slotId];
            }
            set
            {
                if (slotId < 0 || slotId >= this.Count)
                {
                    throw new ArgumentOutOfRangeException("slotid");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (slotId == this.GridSizeSq)
                {
                    this.outputSlot = (ItemSlotCraftingTableOutput)value;
                    return;
                }
                this.slots[slotId] = value;
            }
        }

        public override int Count { get { return this.GridSizeSq + 1; } }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            ItemSlot[] array = this.SlotsFromTreeAttributes(tree, null, null);
            int? num = (array != null) ? new int?(array.Length) : null;
            int num2 = this.slots.Length;
            if (num.GetValueOrDefault() == num2 & num != null)
            {
                this.slots = array;                
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.SlotsToTreeAttributes(slots, tree);
            this.ResolveBlocksOrItems();
        }

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (!isMerge)
            {
                return this.baseWeight + 1f;
            }
            return this.baseWeight + 4f;
        }

        public override bool RemoveOnClose { get { return true; } }

        public override bool HasOpened(IPlayer player)
        {
            return (tableuser != null && tableuser.PlayerUID == player.PlayerUID);
        }        
        
        private int GridSize = 3; // possibly configurable?
        public int GridSizeSq = 9; // expandable? 5x5 -> 9x9 maybe?
        private ItemSlot[] slots;
        private ItemSlotCraftingTableOutput outputSlot;
        public GridRecipe MatchingRecipe;
        private bool isCrafting;
        public IPlayer tableuser;
    }
}
