using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

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
            if (this.hasLeftOvers)
            {
                int result = base.TryPutInto(sinkSlot, ref op);
                if (!this.Empty)
                {
                    return result;
                }
                this.hasLeftOvers = false;
                this.Inv.ConsumeIngredients();
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
            return op.MovedQuantity;
        }

        public void CraftSingle(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            int stacksize = base.StackSize;
            int moveditems = base.TryPutInto(sinkSlot, ref op);
            if (moveditems == stacksize)
            {
                Inv.ConsumeIngredients();
            }
            if (moveditems > 0 && !this.Empty)
            {
                hasLeftOvers = true;
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
                this.Inv.ConsumeIngredients();
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
