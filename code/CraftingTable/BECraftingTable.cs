using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace CraftingTable
{
    public class BECraftingTable : BlockEntityOpenableContainer, ITexPositionSource
    {
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Server)
            {
                sapi = api as ICoreServerAPI;
            }
            else
            {
                capi = api as ICoreClientAPI;
            }
            this.inventory.LateInitialize(string.Concat(new string[]
            {
                "fgcraftinggtable-",
                this.Pos.X.ToString(),
                "/",
                this.Pos.Y.ToString(),
                "/",
                this.Pos.Z.ToString()
            }), api);
            inventory.Pos = this.Pos;
            UpdateMeshes();
            MarkDirty(true);
        }        

        public string Material
        {
            get
            {
                return base.Block.LastCodePart(0);
            }
        }

        public BECraftingTable()
        {
            this.inventory = new InventoryCraftingTable(null, null);
            this.inventory.SlotModified += OnSlotModified;
            inventory.Pos = this.Pos;
            meshes = new MeshData[inventory.Count - 1];
        }

        public void OnSlotModified(int slotid)
        {            
            IWorldChunk chunkAtPos = this.Api.World.BlockAccessor.GetChunkAtBlockPos(Pos);
            if (chunkAtPos == null) return;
            chunkAtPos.MarkModified();
            UpdateMesh(slotid);
            MarkDirty(true);
        }

        public void UpdateMesh(int slotid)
        {
            if (this.Api == null || this.Api.Side == EnumAppSide.Server)
            {
                return;
            }
            if (slotid == inventory.GridSizeSq) return;
            if (this.inventory[slotid].Empty)
            {
                this.meshes[slotid] = null;
                return;
            }
            MeshData meshData = this.GenMesh(inventory[slotid].Itemstack);
            if (meshData != null)
            {
                float scaler = 0f;
                if (inventory[slotid].Itemstack.Class == EnumItemClass.Item)
                {
                    scaler = 0.33f;
                }
                else
                {
                    scaler = 0.1875f;
                }
                TranslateMesh(meshData, slotid, scaler);
                meshes[slotid] = meshData;
            }
        }

        public void UpdateMeshes()
        {
            for (int i = 0; i < inventory.Count-1; i++)
            {
                UpdateMesh(i);
            }
            MarkDirty(true);
        }

        public void TranslateMesh(MeshData meshData, int slotId, float scale)
        {
            meshData.Scale(BECraftingTable.center, scale, scale, scale);
            float offsetx = 0f;
            float offsetz = 0f;
            float stdoffset = 0.1875f;            
            switch (slotId)
            {
                case 0:
                    {
                        offsetx = -stdoffset;
                        offsetz = -stdoffset;
                        break;
                    }
                case 1:
                    {
                        offsetx = 0.0f;
                        offsetz = -stdoffset;
                        break;
                    }
                case 2:
                    {
                        offsetx = stdoffset;
                        offsetz = -stdoffset;
                        break;
                    }
                case 3:
                    {
                        offsetx = -stdoffset;
                        offsetz = 0.0f;
                        break;
                    }
                case 4:
                    {
                        offsetx = 0.0f;
                        offsetz = 0.0f;
                        break;
                    }
                case 5:
                    {
                        offsetx = stdoffset;
                        offsetz = 0.0f;
                        break;
                    }
                case 6:
                    {
                        offsetx = -stdoffset;
                        offsetz = stdoffset;
                        break;
                    }
                case 7:
                    {
                        offsetx = 0.0f;
                        offsetz = stdoffset;
                        break;
                    }
                case 8:
                    {
                        offsetx = stdoffset;
                        offsetz = stdoffset;
                        break;
                    }
            }
            meshData.Translate(offsetx, 0.9375f, offsetz);            
        }

        public MeshData GenMesh(ItemStack stack)
        {
            IContainedMeshSource meshsource = stack.Collectible as IContainedMeshSource;
            MeshData meshData;
            if (meshsource != null)
            {
                meshData = meshsource.GenMesh(stack, capi.BlockTextureAtlas, Pos);
                meshData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, base.Block.Shape.rotateY * 0.0174532924f, 0f);
            }
            else
            {
                ICoreClientAPI coreClientAPI = this.Api as ICoreClientAPI;
                if (stack.Class == EnumItemClass.Block)
                {
                    meshData = coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                }
                else
                {
                    this.nowTesselatingObj = stack.Collectible;
                    this.nowTesselatingShape = null;
                    if (stack.Item.Shape != null)
                    {
                        this.nowTesselatingShape = coreClientAPI.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                        
                    }
                    coreClientAPI.Tesselator.TesselateItem(stack.Item, out meshData, this);
                    meshData.RenderPassesAndExtraBits.Fill((short)2);
                }
            }
            return meshData;
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            for (int i = 0; i < meshes.Length; i++)
            {
                if (this.meshes[i] != null)
                {
                    mesher.AddMeshData(meshes[i], 1);
                }
            }
            return false;
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {            
            if (Api.World is IServerWorldAccessor)
            {                
                if (inventory.tableuser != null && inventory.tableuser.PlayerUID == byPlayer.PlayerUID)
                {
                    sapi.Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos.X, Pos.Y, Pos.Z, 1001, null);
                    byPlayer.InventoryManager.CloseInventory(inventory);
                    inventory.SetPlayer(null);
                    base.MarkDirty(false);
                }
                else
                {
                    sapi.Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos.X, Pos.Y, Pos.Z, 1000, null);
                    byPlayer.InventoryManager.OpenInventory(inventory);                    
                    inventory.SetPlayer(byPlayer);
                    inventory.FindMatchingRecipe();
                    base.MarkDirty(false);
                }
            }
            return true;
        }

        public bool OnClearButtonPressed(IPlayer byPlayer)
        {
            if (byPlayer != null && this.inventory != null)
            {
                foreach (ItemSlot itemSlot in this.inventory)
                {
                    if (!(itemSlot is ItemSlotCraftingTableOutput) && !itemSlot.Empty)
                    {
                        if (!byPlayer.InventoryManager.TryGiveItemstack(itemSlot.Itemstack, true))
                        {
                            this.Api.World.SpawnItemEntity(itemSlot.Itemstack, Pos.ToVec3d(), null);
                        }                        
                        itemSlot.Itemstack = null;
                        itemSlot.MarkDirty();
                    }
                }
                if (Api.Side == EnumAppSide.Client) UpdateMeshes();
            }
            return true;
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
            if (this.Api != null)
            {
                this.inventory.AfterBlocksLoaded(Api.World);
                if (Api.Side == EnumAppSide.Client)
                {
                    UpdateMeshes();
                }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute treeAttribute = new TreeAttribute();
            this.inventory.ToTreeAttributes(treeAttribute);
            tree["inventory"] = treeAttribute;
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();            
            if (tabledialog != null)
            {
                tabledialog.TryClose();
            }
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            inventory.SetPlayer(null);
            this.inventory.SlotModified -= OnSlotModified;
            base.OnBlockBroken(byPlayer);
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {            
            if (packetid < 1000)
            {
                this.inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                inventory.SetPlayer(player);
                this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
                return;
            }
            if (packetid == 1001 && player.InventoryManager != null)
            {
                inventory.SetPlayer(null);
                player.InventoryManager.CloseInventory(inventory);
            }            
            if (packetid == 1002 && player.InventoryManager != null)
            {
                OnClearButtonPressed(player);
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == 1000 && (this.tabledialog == null || !this.tabledialog.IsOpened()))
            {
                tabledialog = new GuiDialogBECraftingTable(DialogTitle, inventory, Pos, capi, this);                                
                inventory.SetPlayer(capi.World.Player);
                walkAwayListener = capi.Event.RegisterGameTickListener(On1SecTick, 1000);
                tabledialog.TryOpen();                
                tabledialog.OnClosed += delegate ()
                {                    
                    inventory.SetPlayer(null);
                    if (tabledialog != null) tabledialog.Dispose();
                    tabledialog = null;
                    capi.Event.UnregisterGameTickListener(walkAwayListener);
                    walkAwayListener = 0;
                };                
            }
            if (packetid == 1001)
            {
                ((IClientWorldAccessor)Api.World).Player.InventoryManager.CloseInventory(inventory);
                tabledialog.TryClose();
            }         
        }

        private void On1SecTick(float obj)
        {
            if (tabledialog != null && tabledialog.IsOpened() && capi != null)
            {
                if (this.Pos.DistanceTo(capi.World.Player.Entity.Pos.AsBlockPos) > 5)
                {
                    capi.Network.SendPacketClient(this.capi.World.Player.InventoryManager.CloseInventory(inventory));
                    tabledialog.TryClose();
                }
            }
        }

        public string DialogTitle
        {
            get
            {
                return "Crafting Table";
            }
        }

        public override InventoryBase Inventory
        {
            get
            {
                return this.inventory;
            }
        }

        public override string InventoryClassName
        {
            get
            {
                return "fgcraftingtable";
            }
        }

        public Size2i AtlasSize
        {
            get
            {
                return this.capi.BlockTextureAtlas.Size;
            }
        }

        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                Item item = this.nowTesselatingObj as Item;
                Dictionary<string, CompositeTexture> dictionary = (Dictionary<string, CompositeTexture>)((item != null) ? item.Textures : (this.nowTesselatingObj as Block).Textures);
                AssetLocation assetLocation = null;
                CompositeTexture compositeTexture;
                if (dictionary.TryGetValue(textureCode, out compositeTexture))
                {
                    assetLocation = compositeTexture.Baked.BakedName;
                }
                if (assetLocation == null && dictionary.TryGetValue("all", out compositeTexture))
                {
                    assetLocation = compositeTexture.Baked.BakedName;
                }
                if (assetLocation == null)
                {
                    Shape shape = this.nowTesselatingShape;
                    if (shape != null)
                    {
                        shape.Textures.TryGetValue(textureCode, out assetLocation);
                    }
                }
                if (assetLocation == null)
                {
                    assetLocation = new AssetLocation(textureCode);
                }
                return this.getOrCreateTexPos(assetLocation);
            }
        }

        private TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition textureAtlasPosition = this.capi.BlockTextureAtlas[texturePath];
            if (textureAtlasPosition == null)
            {
                IAsset asset = this.capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"), true);
                if (asset != null)
                {
                    BitmapRef bmp = asset.ToBitmap(this.capi);
                    int num;
                    //this.capi.BlockTextureAtlas.InsertTextureCached(texturePath, bmp, out num, out textureAtlasPosition, 0.005f);
                    this.capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out num, out textureAtlasPosition, null, 0.005f);
                }
                else
                {
                    ILogger logger = this.capi.World.Logger;
                    string str = "For render in block ";
                    AssetLocation code = base.Block.Code;
                    logger.Warning(str + ((code != null) ? code.ToString() : null) + ", item {0} defined texture {1}, not no such texture found.", new object[]
                    {
                        this.nowTesselatingObj.Code,
                        texturePath
                    });
                }
            }
            return textureAtlasPosition;
        }

        protected MeshData[] meshes;
        private static readonly Vec3f center = new Vec3f(0.5f, 0f, 0.5f);
        public InventoryCraftingTable inventory;
        public GuiDialogBECraftingTable tabledialog;
        private long walkAwayListener;
        protected CollectibleObject nowTesselatingObj;
        protected Shape nowTesselatingShape;

    }
}
