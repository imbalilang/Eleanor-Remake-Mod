using UnityEngine;

namespace EleanorRomance
{
    public static class Items
    {
        public static void Register(ModItemDirectory directory)
        {
            directory.Add(ModConstants.LocketItemId, CreateLocket);
            directory.Add(ModConstants.BraceletItemId, CreateBracelet);
        }

        public static GameItem CreateLocket()
        {
            GameItem item = ItemDirectory.CreateEmptyItem(ModConstants.LocketItemId)
                .SetName(L.Get("er_item_locket_name"))
                .SetSpriteAndShapeFromMod(ModConstants.ModId, "eleanor_locket");

            item.unitValue = 240;
            item.shortDescription = L.Get("er_item_locket_desc");
            item.flavorText = L.Get("er_item_locket_flavor");
            item.SetGameItemType(TypeHelper.LUXURY_ITEM);
            item.EnableTag(TypeHelper.LUXURY_ITEM);
            item.EnableTag("ER_KEEPSAKE");
            return item;
        }

        public static GameItem CreateBracelet()
        {
            GameItem item = ItemDirectory.CreateEmptyItem(ModConstants.BraceletItemId)
                .SetName(L.Get("er_item_bracelet_name"))
                .SetSpriteAndShapeFromMod(ModConstants.ModId, "eleanor_bracelet");

            item.unitValue = 140;
            item.shortDescription = L.Get("er_item_bracelet_desc");
            item.flavorText = L.Get("er_item_bracelet_flavor");
            item.SetGameItemType(TypeHelper.LUXURY_ITEM);
            item.EnableTag(TypeHelper.LUXURY_ITEM);
            return item;
        }

        public static void GiveLocket()
        {
            if (RomanceState.LocketGiven) return;
            if (GiveItem(ModConstants.LocketItemId))
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

        private static bool GiveItem(string id)
        {
            try
            {
                GameItem item = ModHelper.SpawnItem(id);
                if (item == null)
                {
                    ModEntry.LogError("SpawnItem returned null for " + id);
                    return false;
                }

                GraphUtils.TryAcceptAll(EmporiumEntry.Instance.invElement, item);
                if (item.parentInventory == null)
                {
                    GraphUtils.TryAcceptAll(EmporiumEntry.Instance.afterhourInventory, item);
                }
                if (item.parentInventory == null)
                {
                    ModEntry.LogError("Could not place item " + id + " (inventory full).");
                    return false;
                }
                return true;
            }
            catch (System.Exception ex)
            {
                ModEntry.LogError("Failed to give item " + id + ": " + ex);
                return false;
            }
        }
    }
}
