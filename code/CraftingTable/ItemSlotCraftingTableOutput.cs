using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace CraftingTable
{
    public class ItemSlotCraftingTableOutput : ItemSlotOutput
    {
        public bool hasLeftOvers;

        public InventoryCraftingTable Inv
        {
            get
            {
                return (InventoryCraftingTable)this.inventory;
            }
        }
        public ItemSlotCraftingTableOutput(InventoryBase inventory) : base(inventory)
        {            
        }

        protected override void FlipWith(ItemSlot withSlot)
        {
            ItemStackMoveOperation itemStackMoveOperation = new ItemStackMoveOperation(this.Inv.Api.World, EnumMouseButton.Button1, (EnumModifierKey)0, EnumMergePriority.AutoMerge, base.StackSize);
            this.CraftSingle(withSlot, ref itemStackMoveOperation);
        }

        public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            if (this.Empty) return 0;

            op.RequestedQuantity = base.StackSize;
            ItemStack craftedStack = this.itemstack.Clone();

            if (this.hasLeftOvers)
            {
                int moved = base.TryPutInto(sinkSlot, ref op);
                if (!this.Empty)
                {
                    TriggerEvent(craftedStack, moved, op.ActingPlayer);
                    return moved;
                }
                this.hasLeftOvers = false;
                this.Inv.ConsumeIngredients(sinkSlot);
                if (this.Inv.CanStillCraftCurrent())
                {
                    this.itemstack = this.prevStack.Clone();
                }
            }
            if (op.ShiftDown)
            {
                this.CraftMany(sinkSlot, ref op);
            }
            else
            {
                this.CraftSingle(sinkSlot, ref op);
            }
            if (op.ActingPlayer != null)
            {
                TriggerEvent(craftedStack, op.MovedQuantity, op.ActingPlayer);
            }
            return op.MovedQuantity;
        }

        private void TriggerEvent(ItemStack craftedStack, int moved, IPlayer actingPlayer)
        {
            TreeAttribute tree = new TreeAttribute();
            craftedStack.StackSize = moved;
            tree["itemstack"] = new ItemstackAttribute(craftedStack);
            tree["byentityid"] = new LongAttribute(actingPlayer.Entity.EntityId);
            actingPlayer.Entity.World.Api.Event.PushEvent("onitemcrafted", tree);
        }

        public void CraftSingle(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            int stacksize = base.StackSize;
            int moveditems = TryPutIntoNoEvent(sinkSlot, ref op);
            if (moveditems == stacksize)
            {
                Inv.ConsumeIngredients(sinkSlot);
            }
            if (moveditems > 0)
            {
                //hasLeftOvers = true;
                sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
                OnItemSlotModified(sinkSlot.Itemstack);
            }            
        }

        public void CraftMany(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            if (this.itemstack == null)
            { return; }
            int totalgiven = 0; // num
            int given; // num2
            for (; ;)
            {
                this.prevStack = this.itemstack.Clone();
                int stackSize = base.StackSize;
                op.RequestedQuantity = base.StackSize;
                op.MovedQuantity = 0;
                given = this.TryPutIntoNoEvent(sinkSlot, ref op);
                totalgiven += given;
                if (stackSize > given)
                {
                    break;
                }
                this.Inv.ConsumeIngredients(sinkSlot);
                if (!this.Inv.CanStillCraftCurrent())
                {
                    goto Bounce;
                }
                this.itemstack = this.prevStack;
            }
            this.hasLeftOvers = (given > 0);
            Bounce:
            if (totalgiven > 0)
            {
                sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
                this.OnItemSlotModified(sinkSlot.Itemstack);
            }
        }
        public int TryPutIntoNoEvent(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            if (!sinkSlot.CanTakeFrom(this, EnumMergePriority.AutoMerge) || !this.CanTake() || this.itemstack == null)
            {
                return 0;
            }
            if (sinkSlot.Itemstack == null)
            {
                int num = Math.Min(sinkSlot.GetRemainingSlotSpace(base.Itemstack), op.RequestedQuantity);
                if (num > 0)
                {
                    sinkSlot.Itemstack = this.TakeOut(num);
                    op.MovedQuantity = (op.MovableQuantity = Math.Min(sinkSlot.StackSize, num));
                }
                return op.MovedQuantity;
            }
            ItemStackMergeOperation itemStackMergeOperation = op.ToMergeOperation(sinkSlot, this);
            op = itemStackMergeOperation;
            int requestedQuantity = op.RequestedQuantity;
            op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(this.itemstack), op.RequestedQuantity);
            sinkSlot.Itemstack.Collectible.TryMergeStacks(itemStackMergeOperation);
            op.RequestedQuantity = requestedQuantity;
            return itemStackMergeOperation.MovedQuantity;
        }

        private ItemStack prevStack;
    }
}
