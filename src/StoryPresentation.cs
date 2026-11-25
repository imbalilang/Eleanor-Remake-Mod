using System;
using System.Collections;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace EleanorRomance
{
    /// <summary>
    /// Data emitted by the content compiler for a block that needs the two
    /// native backup-client slots behind the main customer.
    /// </summary>
    public sealed class StoryPresentationSpec
    {
        public readonly string Mode;
        public readonly string Backup1Sprite;
        public readonly string Backup1Name;
        public readonly string Backup2Sprite;
        public readonly string Backup2Name;

        public StoryPresentationSpec(
            string mode, string backup1Sprite, string backup1Name,
            string backup2Sprite, string backup2Name)
        {
            Mode = mode ?? "immediate";
            Backup1Sprite = backup1Sprite ?? "";
            Backup1Name = backup1Name ?? "";
            Backup2Sprite = backup2Sprite ?? "";
            Backup2Name = backup2Name ?? "";
        }
    }

    /// <summary>
    /// Reuses the native StoreUIManager.secGameObject/secGameObject2 staging
    /// (the same two GenericClientMono objects used by the security call) for
    /// non-security story presentations.
    ///
    /// The two backups are visual-only: they do not trade, cannot be reported
    /// and are not StoreClient instances. A backup line can use the native SEC
    /// speaker/bubble; the main customer keeps the normal client bubble.
    /// </summary>
    public static class StoryPresentation
    {
        private static bool shown;
        private static StoreClient host;
        private static string backupName;
        private static string savedId1;
        private static string savedId2;
        private static Sprite savedSprite1;
        private static Sprite savedSprite2;
        private static bool usedFirst;
        private static bool usedSecond;
        private static int backupNameVersion;
        private static readonly ConditionalWeakTable<Dialogue, string> companionLines =
            new ConditionalWeakTable<Dialogue, string>();

        public static bool IsShown => shown;

        public static void Apply(StoryPresentationSpec spec)
        {
            if (spec == null) return;
            ShowBackups(spec.Backup1Sprite, spec.Backup2Sprite,
                spec.Mode, spec.Backup1Name, spec.Backup2Name);
        }

        public static string NameFor(Dialogue dialogue)
        {
            if (dialogue?.title != DialogUIManager.SEC) return null;
            if (companionLines.TryGetValue(dialogue, out string name)) return name;
            if (!shown || host == null) return null;
            var current = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
            return ReferenceEquals(host, current) ? backupName : null;
        }

        internal static void MarkCompanion(Dialogue dialogue, string name)
        {
            companionLines.Add(dialogue, name);
        }

        public static bool ShowBackups(
            string backup1Sprite,
            string backup2Sprite = "",
            string mode = "immediate",
            string backup1Name = "",
            string backup2Name = "")
        {
            try
            {
                StoreUIManager ui = StoreUIManager.Instance;
                if (ui == null) return false;
                if (PlayerStore.Instance != null &&
                    (PlayerStore.Instance.isClientBeingArrested ||
                     PlayerStore.Instance.isHandlingShootout))
                    return false;

                GenericClientMono first = ui.secGameObject;
                GenericClientMono second = ui.secGameObject2;
                if (shown) Clear();
                usedFirst = first != null && !string.IsNullOrEmpty(backup1Sprite);
                usedSecond = second != null && !string.IsNullOrEmpty(backup2Sprite);
                if (!usedFirst && !usedSecond) return false;
                host = PlayerStore.Instance?.currentClientInstance?.GetClientBlueprint();
                backupName = backup1Sprite == "eleanor1" ? L.Eleanor
                    : usedFirst ? backup1Name : backup2Name;
                // Prime the native SEC name label as soon as the presentation
                // is staged, so the first SEC line cannot flash the default
                // "security officer" name before its per-line override runs.
                SetBackupNameDeferred(backupName);
                savedId1 = usedFirst ? first.id : null;
                savedId2 = usedSecond ? second.id : null;
                savedSprite1 = usedFirst ? first.image.sprite : null;
                savedSprite2 = usedSecond ? second.image.sprite : null;
                shown = true;
                bool walkIn = string.Equals(mode, "arrive", StringComparison.OrdinalIgnoreCase);
                if (usedFirst) ShowSlot(first, backup1Sprite, walkIn);
                if (usedSecond) ShowSlot(second, backup2Sprite, walkIn);
                return true;
            }
            catch (Exception ex)
            {
                ModEntry.LogError("StoryPresentation.ShowBackups failed: " + ex);
                Clear();
                return false;
            }
        }

        private static void ShowSlot(GenericClientMono slot, string sprite, bool walkIn)
        {
            // Keep arrival callbacks out of the native security sequence.
            slot.id = "PRESENTATION";
            slot.gameObject.SetActive(true);
            if (walkIn) slot.StartArrive();
            else slot.SetImmediatlyArrive();
            slot.image.sprite = SpriteDict.Instance.ShowClientSprite(PortraitCompat.Resolve(sprite));
        }

        internal static void OnCustomerLeaving(StoreClient client)
        {
            if (client != null && ReferenceEquals(host, client)) Clear();
        }

        public static void Clear()
        {
            backupNameVersion++;
            try
            {
                if (!shown) return;
                StoreUIManager ui = StoreUIManager.Instance;
                if (ui != null)
                {
                    if (usedFirst) RestoreSlot(ui.secGameObject, savedId1, savedSprite1);
                    if (usedSecond) RestoreSlot(ui.secGameObject2, savedId2, savedSprite2);
                }
                SetBackupName(LocHelper.GetLocalizedClientName("name_security_officer"));
            }
            catch (Exception ex)
            {
                ModEntry.LogError("StoryPresentation.Clear failed: " + ex);
            }
            finally
            {
                shown = false;
                host = null;
                backupName = null;
                savedSprite1 = savedSprite2 = null;
                usedFirst = usedSecond = false;
            }
        }

        private static void RestoreSlot(GenericClientMono slot, string id, Sprite sprite)
        {
            if (slot == null) return;
            slot.gameObject.SetActive(false);
            slot.id = id;
            if (slot.image != null) slot.image.sprite = sprite;
        }

        /// <summary>Changes the label on the native SEC bubble for a backup speaker.</summary>
        public static void SetBackupName(string name)
        {
            DialogUIManager dialog = DialogUIManager.Instance;
            if (dialog == null || dialog.secNameBubble == null || string.IsNullOrEmpty(name)) return;
            try
            {
                TextMeshProUGUI label = dialog.secNameBubble.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = name;
            }
            catch (Exception ex)
            {
                ModEntry.LogError("StoryPresentation backup name failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Sets the name now and once more on the next frame.  The native SEC
        /// bubble can initialise its label when it is first activated, after the
        /// prefix that primes it; the deferred set survives that first-enable
        /// reset without affecting later lines.
        /// </summary>
        public static void SetBackupNameDeferred(string name)
        {
            int version = ++backupNameVersion;
            if (string.IsNullOrEmpty(name)) return;
            SetBackupName(name);
            try
            {
                ModHelper.StartCoroutine(ModConstants.ModId, ApplyBackupNameAfterFrame(name, version));
            }
            catch (Exception ex)
            {
                ModEntry.LogError("StoryPresentation deferred backup name failed: " + ex.Message);
            }
        }

        private static IEnumerator ApplyBackupNameAfterFrame(string name, int version)
        {
            yield return null;
            if (version != backupNameVersion) yield break;
            SetBackupName(name);
        }
    }
}
