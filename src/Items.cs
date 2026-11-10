using UnityEngine;

namespace EleanorRomance
{
    public static class Items
    {
        // The 64px source art is displayed at half size, using its middle 32px
        // strip. Its visible footprint is 32x16 UI pixels, or two inventory cells.
        private const string BraceletShape = "11";
        private const string BraceletSprite = "eleanor_bracelet_compact";
        private const string LocketSprite = "eleanor_locket_compact";
        private const string LegacyBraceletShape = "0000" + "0110" + "0110" + "0000";
        private const string OversizedBraceletShape = "0000" + "1111" + "1111" + "0000";
        // 0.73 spawns referenced the unregistered legacy id "wrench_old", which
        // creates a dummy item showing the raw identifier. The game has no
        // normal wrench item ("wrench_orange" lives in UnusedDirectory); the
        // real second tool is the native Wire Cutter.
        private const string LegacyWrenchId = "wrench_old";
        private const string WireCutterAtlas = "Items/items_tool";
        private const string WireCutterSprite = "wirecutter";
        private static bool repairOnPlacement;

        public static void Register(ModItemDirectory directory)
        {
            directory.Add(ModConstants.LocketItemId, CreateLocket);
            directory.Add(ModConstants.BraceletItemId, CreateBracelet);
            directory.Add(ModConstants.PlaqueItemId, CreatePlaque);
            directory.Add(ModConstants.PatronItemId, () => CreateOrnament(2));
            directory.Add(ModConstants.GratitudeItemId, () => CreateOrnament(3));
        }

        public static GameItem CreateLocket()
        {
            EnsureLocketSprite();
            GameItem item = ItemDirectory.CreateEmptyItem(ModConstants.LocketItemId)
                .SetName(L.Get("er_item_locket_name"));
            item.useSpriteFromMod=true;
            item.SetSprite(ModConstants.ModId,LocketSprite)
                .SetShape(new GridShapeBuilder().SetData("1111",2).Build());
            item.onSlotAddItemFunc+=OnLocketPlaced;

            item.unitValue = 240;
            item.shortDescription = L.Get("er_item_locket_desc");
            item.flavorText = L.Get("er_item_locket_flavor");
            item.SetGameItemType(TypeHelper.LUXURY_ITEM);
            item.EnableTag(TypeHelper.LUXURY_ITEM);
            item.EnableTag("ER_KEEPSAKE");
            item.EnableTag("IMPORTANT_TAG");
            return item;
        }

        public static GameItem CreatePlaque()=>CreateOrnament(1);
        public static GameItem CreateOrnament(int stage)
        {
            string prefix=stage==1?"er_dual_plaque":stage==2?"er_dual_patron":"er_dual_gratitude";
            EnsureOrnamentSprite();
            GameItem item=ItemDirectory.CreateEmptyItem(DualRewards.ItemIds[stage-1]).SetName(DualRewards.Name(stage));
            item.useSpriteFromMod=true;
            // Deliberate existing-art fallback: no dependency on unverified native sprite IDs.
            item.SetSprite(ModConstants.ModId,"er_lydia_recommendation_plaque_compact")
                .SetShape(new GridShapeBuilder().SetData("1",1).Build());
            item.unitValue=0;item.shortDescription=L.Get(prefix+"_desc");item.flavorText=L.Get(prefix+"_flavor");
            item.EnableTag("IMPORTANT_TAG");item.EnableTag("ER_DUAL_ORNAMENT");
            if(stage==1)item.EnableTag("ER_DUAL_PLAQUE");return item;
        }

        private static void EnsureOrnamentSprite()
        {
            foreach(LoadedMod mod in ModLoader.Mods)
            {
                if(mod?.Manifest==null || mod.Manifest.ID!=ModConstants.ModId)continue;
                const string key="er_lydia_recommendation_plaque_compact";
                if(mod.Sprites.ContainsKey(key))return;
                Sprite source;
                if(!mod.Sprites.TryGetValue("er_lydia_recommendation_plaque",out source))return;
                Sprite padded=CreatePaddedHitSprite(source,4);
                mod.Sprites[key]=padded!=null?padded:Sprite.Create(source.texture,source.rect,
                    new Vector2(0.5f,0.5f),source.pixelsPerUnit*4,0,SpriteMeshType.FullRect);
                return;
            }
        }

        /// <summary>
        /// The game's item click uses per-pixel alpha hit testing.  A 1x1 ornament
        /// whose badge has transparent margins therefore loses most of its cell
        /// to click-through.  Give fully transparent pixels a 1/255 alpha floor:
        /// the whole cell becomes clickable while remaining visually transparent.
        /// The layout is unchanged; this only widens the hit area.
        /// </summary>
        private static Sprite CreatePaddedHitSprite(Sprite source,int scale)
        {
            try
            {
                Rect r=source.rect;
                int w=Mathf.RoundToInt(r.width),h=Mathf.RoundToInt(r.height);
                if(w<=0 || h<=0)return null;
                Color[] pixels=source.texture.GetPixels(Mathf.RoundToInt(r.x),Mathf.RoundToInt(r.y),w,h);
                const float hitAlpha=1f/255f;
                for(int i=0;i<pixels.Length;i++)
                    if(pixels[i].a<hitAlpha)pixels[i].a=hitAlpha;
                var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
                texture.filterMode=source.texture.filterMode;
                texture.wrapMode=source.texture.wrapMode;
                texture.SetPixels(pixels);
                texture.Apply(false,false);
                return Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(0.5f,0.5f),
                    source.pixelsPerUnit*scale,0,SpriteMeshType.FullRect);
            }
            catch(System.Exception ex)
            {
                ModEntry.Log("Ornament hit-area padding unavailable; using the compact sprite: "+ex.Message);
                return null;
            }
        }

        public static GameItem CreateBracelet()
        {
            EnsureBraceletSprite();
            GameItem item = ItemDirectory.CreateEmptyItem(ModConstants.BraceletItemId)
                .SetName(L.Get("er_item_bracelet_name"));
            item.useSpriteFromMod = true;
            item.SetSprite(ModConstants.ModId, BraceletSprite)
                .SetShape(new GridShapeBuilder().SetData(BraceletShape, 2).Build());

            item.onSlotAddItemFunc += OnBraceletPlaced;

            item.unitValue = 140;
            item.shortDescription = L.Get("er_item_bracelet_desc");
            item.flavorText = L.Get("er_item_bracelet_flavor");
            item.SetGameItemType(TypeHelper.LUXURY_ITEM);
            item.EnableTag(TypeHelper.LUXURY_ITEM);
            return item;
        }

        private static void EnsureBraceletSprite()
        {
            foreach (LoadedMod mod in ModLoader.Mods)
            {
                if (mod == null || mod.Manifest == null || mod.Manifest.ID != ModConstants.ModId) continue;
                if (mod.Sprites.ContainsKey(BraceletSprite)) return;

                Sprite source = mod.Sprites["eleanor_bracelet"];
                Rect rect = source.rect;
                // All opaque pixels lie within this strip. Retain the source
                // texture's point filtering and use UI.Image's native PPU scaling.
                Rect strip = new Rect(rect.x, rect.y + 16, 64, 32);
                mod.Sprites[BraceletSprite] = Sprite.Create(source.texture, strip,
                    new Vector2(0.5f, 0.5f), source.pixelsPerUnit * 2, 0, SpriteMeshType.FullRect);
                return;
            }
            throw new System.InvalidOperationException("Eleanor bracelet sprites are not loaded.");
        }

        private static void EnsureLocketSprite()
        {
            foreach(LoadedMod mod in ModLoader.Mods)
            {
                if(mod==null || mod.Manifest==null || mod.Manifest.ID!=ModConstants.ModId)continue;
                if(mod.Sprites.ContainsKey(LocketSprite))return;
                Sprite source=mod.Sprites["eleanor_locket"];
                mod.Sprites[LocketSprite]=Sprite.Create(source.texture,source.rect,
                    new Vector2(0.5f,0.5f),source.pixelsPerUnit*2,0,SpriteMeshType.FullRect);
                return;
            }
            throw new System.InvalidOperationException("Eleanor locket sprites are not loaded.");
        }

        public static void BeginLoad()
        {
            repairOnPlacement = false;
        }

        public static void RepairLoadedShapes()
        {
            repairOnPlacement = true;
            if (!VersionGate.IsSupported || PlayerStore.Instance == null) return;

            int deferred = 0;
            int deferredLockets = 0;
            int repairedWrench = 0;
            try
            {
                foreach (GameItem item in PlayerStore.Instance.FindAllItem(false))
                {
                    if (item != null && item.identifier == ModConstants.LocketItemId
                        && !item.IsTag("IMPORTANT_TAG")) item.EnableTag("IMPORTANT_TAG");
                    if (IsLegacyBracelet(item) && !RepairBraceletShape(item)) deferred++;
                    if(IsLegacyLocket(item) && !RepairLocketShape(item))deferredLockets++;
                    if (RepairLegacyWrench(item)) repairedWrench++;
                }
            }
            catch (System.Exception ex)
            {
                ModEntry.LogError("Keepsake shape update failed: " + ex);
            }

            if (repairedWrench > 0) ModEntry.Log("Repaired " + repairedWrench + " legacy wrench_old item(s) to Wire Cutter.");
            if (deferred > 0) StoryDirector.Notify(L.Get("er_notice_bracelet_space"));
            if(deferredLockets>0)StoryDirector.Notify(L.Get("er_biz_locket_space"));
        }

        private static void OnLocketPlaced(GameItem item,GameInventory inventory,SlotMarker slot)
        {
            if(repairOnPlacement && !SaveManager.IsDecoding && VersionGate.IsSupported)RepairLocketShape(item);
        }
        private static bool RepairLegacyWrench(GameItem item)
        {
            if (item == null || item.identifier != LegacyWrenchId) return false;
            string name = LocHelper.GetLocalizedItem("item_wire_cutter_name");
            if (string.IsNullOrEmpty(name)) name = "Wire Cutter";
            item.name = name;
            item.identifierName = name;
            item.shortDescription = LocHelper.GetLocalizedItem("item_wire_cutter_desc");
            item.flavorText = LocHelper.GetLocalizedItem("item_wire_cutter_flavor");
            item.unitValue = 40;
            item.SetGameItemType("TOOL");
            item.SetSprite(WireCutterAtlas, WireCutterSprite);
            return true;
        }
        private static bool IsLegacyLocket(GameItem item)=>item!=null && item.identifier==ModConstants.LocketItemId &&
            item.shape!=null && item.shape.width==4 && item.shape.height==4;
        private static bool RepairLocketShape(GameItem item)
        {
            if(!IsLegacyLocket(item))return true;
            GridShape shape=item.shape,modified=item.modifiedShape;TagSystem state=item.modifiedState;
            int ox=item.modifiedXOrigin,oy=item.modifiedYOrigin;
            string sprite=item.spritePath,atlas=item.spriteAtlasPath;
            bool applied=false;
            try
            {
                // A filled square can overlap items nested in the old chain's
                // holes. Try only validated positions inside the former bounds.
                for(int pass=0;pass<10;pass++)
                {
                    int ax=pass==0?1:(pass-1)%3,ay=pass==0?1:(pass-1)/3;
                    int x=int.MaxValue,y=int.MaxValue;
                    for(int dx=0;dx<2;dx++)for(int dy=0;dy<2;dy++)
                    {var p=modified.ForwardTransform(ox+ax+dx,oy+ay+dy);x=System.Math.Min(x,p.Item1);y=System.Math.Min(y,p.Item2);}
                    item.shape=new GridShapeBuilder().SetData("1111",2).Build();
                    item.modifiedShape=new GridShapeBuilder(item.shape).SetTransform(x,y,modified.flipped,modified.orientation).Build();
                    item.modifiedXOrigin=0;item.modifiedYOrigin=0;
                    if(!item.CheckValidShapeState())continue;
                    EnsureLocketSprite();item.SetSprite(ModConstants.ModId,LocketSprite);item.ValidateShapeState();
                    applied=true;return true;
                }
                return false;
            }
            catch(System.Exception ex){ModEntry.LogError("Locket shape update deferred: "+ex);return false;}
            finally
            {
                if(!applied)
                {item.shape=shape;item.modifiedShape=modified;item.modifiedState=state;item.modifiedXOrigin=ox;item.modifiedYOrigin=oy;item.SetSprite(atlas,sprite);}
            }
        }

        private static void OnBraceletPlaced(GameItem item, GameInventory inventory, SlotMarker slot)
        {
            // Loading attaches items one at a time, before all neighbours exist.
            // Defer validation until the entire save has finished loading.
            if (repairOnPlacement && !SaveManager.IsDecoding && VersionGate.IsSupported)
                RepairBraceletShape(item);
        }

        private static bool IsLegacyBracelet(GameItem item)
        {
            if (item == null || item.identifier != ModConstants.BraceletItemId
                || item.shape == null || item.shape.width != 4 || item.shape.height != 4) return false;

            bool legacy = true;
            bool oversized = true;
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    int cell = item.shape.GetLocal(x, y);
                    legacy &= cell == LegacyBraceletShape[y * 4 + x] - '0';
                    oversized &= cell == OversizedBraceletShape[y * 4 + x] - '0';
                }
            return legacy || oversized;
        }

        private static bool RepairBraceletShape(GameItem item)
        {
            if (!IsLegacyBracelet(item)) return true;

            GridShape original = item.shape;
            GridShape originalModified = item.modifiedShape;
            TagSystem originalState = item.modifiedState;
            int originalX = item.modifiedXOrigin;
            int originalY = item.modifiedYOrigin;
            string originalSprite = item.spritePath;
            string originalAtlas = item.spriteAtlasPath;
            bool applied = false;
            try
            {
                // Anchor the smaller bracelet inside two cells occupied in both
                // old masks, including rotated/flipped items at inventory edges.
                var first = originalModified.ForwardTransform(originalX + 1, originalY + 1);
                var second = originalModified.ForwardTransform(originalX + 2, originalY + 1);
                item.shape = new GridShapeBuilder().SetData(BraceletShape, 2).Build();
                item.modifiedShape = new GridShapeBuilder(item.shape).SetTransform(
                    System.Math.Min(first.Item1, second.Item1), System.Math.Min(first.Item2, second.Item2),
                    originalModified.flipped, originalModified.orientation).Build();
                item.modifiedXOrigin = 0;
                item.modifiedYOrigin = 0;
                if (!item.CheckValidShapeState()) return false;
                EnsureBraceletSprite();
                item.SetSprite(ModConstants.ModId, BraceletSprite);
                item.ValidateShapeState();
                applied = true;
                return true;
            }
            catch (System.Exception ex)
            {
                ModEntry.LogError("Bracelet shape update deferred: " + ex);
                return false;
            }
            finally
            {
                if (!applied)
                {
                    item.shape = original;
                    item.modifiedShape = originalModified;
                    item.modifiedState = originalState;
                    item.modifiedXOrigin = originalX;
                    item.modifiedYOrigin = originalY;
                    item.SetSprite(originalAtlas, originalSprite);
                }
            }
        }

        public static void GiveLocket()
        {
            if (RomanceState.LocketGiven) return;
            if (StoryEngine.GiveToPlayer(ModConstants.LocketItemId))
            {
                RomanceState.LocketGiven = true;
                RomanceState.LocketWithEleanor = false;
            }
        }

        public static GameItem SpawnBracelet()
        {
            try
            {
                return ModHelper.SpawnItem(ModConstants.BraceletItemId);
            }
            catch (System.Exception ex)
            {
                ModEntry.LogError("SpawnBracelet failed: " + ex);
                return null;
            }
        }

    }
}
