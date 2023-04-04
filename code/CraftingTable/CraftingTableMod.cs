using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

namespace CraftingTable
{
    public class CraftingTableMod : ModSystem
    {
        ICoreClientAPI capi;
        ICoreServerAPI sapi;
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            if (api.Side == EnumAppSide.Server)
            {
                sapi = api as ICoreServerAPI;
            }
            else
            {
                capi = api as ICoreClientAPI;
            }
            api.RegisterBlockClass("BlockCraftingTable", typeof(BlockCraftingTable));
            api.RegisterBlockEntityClass("BECraftingTable", typeof(BECraftingTable));
        }

    }
}
