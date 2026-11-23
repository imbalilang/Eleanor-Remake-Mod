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
                Sprite padded=CreateHitSprite(source,source.rect,4);
                mod.Sprites[key]=padded!=null?padded:Sprite.Create(source.texture,source.rect,
                    new Vector2(0.5f,0.5f),source.pixelsPerUnit*4,0,SpriteMeshType.FullRect);
                return;
            }
        }

        /// <summary>
        /// The game's item click uses per-pixel alpha hit testing (threshold
        /// 1e-05).  A badge with transparent margins therefore loses most of its
        /// rectangle to click-through.  Give fully transparent pixels a ~1%
        /// white backing (alpha 3/255): the whole item rectangle stays clickable
        /// while remaining visually transparent.  Opaque art and layout are
        /// unchanged; only the empty area gets a hit surface.
        /// </summary>
        private static Sprite CreateHitSprite(Sprite source,Rect rect,float pixelsPerUnitScale)
        {
            try
            {
                int x=Mathf.RoundToInt(rect.x),y=Mathf.RoundToInt(rect.y);
                int w=Mathf.RoundToInt(rect.width),h=Mathf.RoundToInt(rect.height);
                if(w<=0 || h<=0)return null;
                Color[] pixels=source.texture.GetPixels(x,y,w,h);
                Color backing=new Color(1f,1f,1f,3f/255f);
                for(int i=0;i<pixels.Length;i++)
                {
                    if(pixels[i].a<=0f)pixels[i]=backing;
                    else if(pixels[i].a<backing.a)pixels[i].a=backing.a;
                }
                var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
                texture.name=source.texture.name+"_hit";
                texture.filterMode=source.texture.filterMode;
                texture.wrapMode=source.texture.wrapMode;
                texture.SetPixels(pixels);
                texture.Apply(false,false);
                return Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(0.5f,0.5f),
                    source.pixelsPerUnit*pixelsPerUnitScale,0,SpriteMeshType.FullRect);
            }
            catch(System.Exception ex)
            {
                ModEntry.Log("Item hit-area backing unavailable; using the compact sprite: "+ex.Message);
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
                Sprite hit = CreateHitSprite(source, strip, 2);
                mod.Sprites[BraceletSprite] = hit ?? Sprite.Create(source.texture, strip,
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
                Sprite hit=CreateHitSprite(source,source.rect,2);
                mod.Sprites[LocketSprite]=hit!=null?hit:Sprite.Create(source.texture,source.rect,
                    new Vector2(0.5f,0.5f),source.pixelsPerUnit*2,0,SpriteMeshType.FullRect);
                return;
            }
            throw new System.InvalidOperationException("Eleanor locket sprites are not loaded.");
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
