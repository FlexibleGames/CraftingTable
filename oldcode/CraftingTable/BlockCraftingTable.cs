using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;

namespace CraftingTable
{
    public class BlockCraftingTable : Block
    {
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (api.Side == EnumAppSide.Server)
            {
                sapi = api as ICoreServerAPI;
            }
            else
            {
                capi = api as ICoreClientAPI;
            }
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {            
            bool success = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            return success;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECraftingTable tableentity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECraftingTable;
            if (tableentity != null)
            {
                tableentity.OnPlayerRightClick(byPlayer, blockSel);
                return true;
            }
            else
            { 
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }
            
        }
    }
}
