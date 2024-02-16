using System;
using System.Collections.Generic;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;

namespace CraftingTable
{
    public class GuiDialogBECraftingTable : GuiDialogBlockEntity
    {
        private BECraftingTable betable;
        public GuiDialogBECraftingTable(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi, BECraftingTable bentity) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (base.IsDuplicate)
            {
                return;
            }            
            capi.World.Player.InventoryManager.OpenInventory(inventory);
            betable = bentity;
            this.SetupDialog();
        }
        
        private void OnSlotModified(int slotid)
        {
            betable.MarkDirty();
            this.capi.Event.EnqueueMainThreadTask(new Action(this.SetupDialog), "setupcraftingtabledlg");           
        }

        public void SetupDialog()
        {
            ItemSlot hoveredSlot = this.capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null)
            {
                InventoryBase inv1 = hoveredSlot.Inventory;
                string inv1str = (inv1 != null) ? inv1.InventoryID : null;
                InventoryBase inv2 = base.Inventory;
                if (inv1str != ((inv2 != null) ? inv2.InventoryID : null))
                {
                    hoveredSlot = null;
                }
            }
            
            double elementtodialogpadding = GuiStyle.ElementToDialogPadding;
            double unscaledslotpadding = GuiElementItemSlotGridBase.unscaledSlotPadding;

            // Sets up the main grid, output slot, and the overall dialog that presents both
            ElementBounds dialog = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            
            ElementBounds maingrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 40, 3, 3)
                .FixedGrow(unscaledslotpadding);
            ElementBounds output = ElementStdBounds.SlotGrid(EnumDialogArea.None, 60, 90, 1, 1).RightOf(maingrid, 50)
                .FixedGrow(unscaledslotpadding);
            ElementBounds clearbtn = ElementBounds.FixedOffseted(EnumDialogArea.None, 0, 40, 20, 20).RightOf(maingrid, 20);
            dialog.BothSizing = ElementSizing.FitToChildren;
            
            dialog.WithChildren(new ElementBounds[]
            {
                maingrid,
                clearbtn,
                output
            });
            ElementBounds window = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);
            base.ClearComposers();
            if (this.capi.Settings.Bool["immersiveMouseMode"])
            {
                window.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-12, 0);
            }
            else
            {
                window.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(20, 0);
            }
            BlockPos bepos = base.BlockEntityPosition;
            this.SingleComposer = this.capi.Gui.CreateCompo("fgtabledlg" + (bepos?.ToString()), window)
                .AddShadedDialogBG(dialog, true, 5)
                .AddDialogTitleBar("Crafting Table", new Action(OnTitleBarClose), null, null)
                .BeginChildElements(dialog)
                .AddInset(maingrid, 3, 0.85f).AddItemSlotGrid(Inventory, new Action<object>(SendInvPacket) /*base.DoSendPacket*/, 3, new int[]
                { 0,1,2,3,4,5,6,7,8 }, maingrid, "craftinggrid")
                .AddSmallButton("X", OnClearButton, clearbtn, EnumButtonStyle.Small, "clearbtn")
                .AddItemSlotGrid(Inventory, new Action<object>(SendInvPacket) /*base.DoSendPacket*/, 1, new int[]
                { 9 }, output, "outputslot")
                .EndChildElements().Compose(true);

            if (hoveredSlot != null)
            {
                base.SingleComposer.OnMouseMove(new MouseEvent(this.capi.Input.MouseX, capi.Input.MouseY));
            }
            //SingleComposer.UnfocusOwnElements();
        }

        private void SendInvPacket(object p)
        {
            this.capi.Network.SendBlockEntityPacket(base.BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);            
            //this.capi.Network.SendPacketClient(p);
        }

        private bool OnClearButton()
        {
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, 1002, null);
            return true;
        }

        private void OnTitleBarClose()
        {
            this.TryClose();
        }

        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            this.Inventory.SlotModified += OnSlotModified;
        }        

        public override void OnGuiClosed()
        {
            base.Inventory.SlotModified -= this.OnSlotModified;
            this.capi.Network.SendPacketClient(this.capi.World.Player.InventoryManager.CloseInventory(this.Inventory));
            base.SingleComposer.GetSlotGrid("craftinggrid").OnGuiClosed(capi);
            base.SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(capi);
            base.OnGuiClosed();            
        }
    }
}
