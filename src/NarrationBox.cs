using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace EleanorRomance
{
    /// <summary>
    /// Dedicated tailless dark narration banner positioned above the sales window.
    /// Keeps narration visually separate from NPC speech bubbles without mutating
    /// DialogUIManager.clientBubble, while preserving native typewriter, advance,
    /// skip, history and scaling behavior.
    /// </summary>
    public static class NarrationBox
    {
        private const string ObjectName = "er_narration_box";
        private static readonly Color32 PanelFill = new Color32(19, 24, 34, 236);
        private static readonly Color32 PanelBorder = new Color32(110, 127, 150, 175);
        private static readonly Color32 PanelOuter = new Color32(10, 13, 20, 248);
        private static readonly Color32 TextColor = new Color32(224, 232, 242, 255);

        private static DialogUIManager owner;
        private static TextBubble box;
        private static Vector3 baseScale = Vector3.one;
        private static Texture2D panelTexture;
        private static Sprite panelSprite;
        private static float panelSpritePpu = -1f;
        private static bool showing;
        [ThreadStatic] private static Dialogue handledInDisplay;
        private static readonly Vector3[] windowCorners = new Vector3[4];

        public static bool Enabled { get; set; } = true;

        public static bool IsNarration(Dialogue line)
        {
            if (!Enabled || !DialogueSpeakers.Enabled || !VersionGate.IsSupported || line == null) return false;
            if (!DialogueSpeakers.HasOwnLabel(line)) return false;
            string narrator = L.Get("er_name_narrator");
            return !string.IsNullOrEmpty(narrator) && string.Equals(line.title, narrator, StringComparison.Ordinal);
        }

        internal static bool IsShowing(DialogUIManager ui)
            => showing && !ReferenceEquals(ui, null) && ReferenceEquals(owner, ui) && !ReferenceEquals(box, null) && box != null && box.gameObject.activeSelf;

        internal static bool ConsumeHandled(DialogUIManager ui, Dialogue line)
        {
            bool handled = !ReferenceEquals(line, null) && ReferenceEquals(handledInDisplay, line);
            handledInDisplay = null;
            return handled || IsShowing(ui);
        }

        internal static bool IsTyping(DialogUIManager ui)
        {
            if (!IsShowing(ui)) return false;
            var tw = box.typewriter;
            return !ReferenceEquals(tw, null) && tw != null && box.gameObject.activeInHierarchy && tw.isShowingText;
        }

        internal static void SkipTypewriter(DialogUIManager ui)
        {
            if (!IsShowing(ui)) return;
            var tw = box.typewriter;
            if (!ReferenceEquals(tw, null) && tw != null) tw.SkipTypewriter();
        }

        internal static void Hide(DialogUIManager ui = null)
        {
            if (!ReferenceEquals(ui, null) && !ReferenceEquals(owner, null) && !ReferenceEquals(owner, ui)) return;
            showing = false;
            if (ReferenceEquals(box, null)) return;
            try
            {
                if (box == null)
                {
                    box = null;
                    owner = null;
                    return;
                }
                if (box.typewriter != null) box.typewriter.StopShowingText();
                if (box.gameObject.activeSelf) box.gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("NarrationBox.Hide failed: " + ex);
            }
        }

        internal static void Clear()
        {
            showing = false;
            owner = null;
            if (!ReferenceEquals(box, null))
            {
                try
                {
                    if (box != null) UnityEngine.Object.Destroy(box.gameObject);
                }
                catch { }
                box = null;
            }
            if (!ReferenceEquals(panelSprite, null))
            {
                try
                {
                    if (panelSprite != null) UnityEngine.Object.Destroy(panelSprite);
                }
                catch { }
                panelSprite = null;
                panelSpritePpu = -1f;
            }
            if (!ReferenceEquals(panelTexture, null))
            {
                try
                {
                    if (panelTexture != null) UnityEngine.Object.Destroy(panelTexture);
                }
                catch { }
                panelTexture = null;
            }
        }

        internal static bool TryShow(DialogUIManager ui, Dialogue dialogue, ref Dialogue displayingUncompletedDialog)
        {
            if (!IsNarration(dialogue) || ReferenceEquals(ui, null)) return false;
            if (ReferenceEquals(ui.clientBubble, null) || ui.clientBubble == null) return false;
            if (ui.clientBubble.transform.parent == null) return false;
            try
            {
                // Hide active native bubbles before cloning so Instantiate never clones
                // an active clientBubble or fires TypewriterCore.OnEnable on stale NPC text.
                ui.HideBubbles();
                if (ui.clientNameBubble != null) ui.clientNameBubble.SetActive(false);

                TextBubble target = EnsureBox(ui);
                if (target == null || target.typewriter == null) return false;

                displayingUncompletedDialog = dialogue;
                LocGenderHelper.ResolveDialogueLine(dialogue);

                RecordHistory(ui, dialogue);

                target.typewriter.StopShowingText();
                target.typewriter.resetTypingSpeedAtStartup = false;
                SyncSpeed(target);
                ApplyStyle(target);
                ApplyScale(target);
                PositionBox(target);

                target.typewriter.hideAppearancesOnSkip = false;
                showing = true;
                handledInDisplay = dialogue;
                target.gameObject.SetActive(true);
                target.typewriter.ShowText(dialogue.plainText);
                SyncSpeed(target);
                if (EmporiumOption.Instance != null && !EmporiumOption.Instance.enableTypewriterEffect)
                {
                    target.typewriter.hideAppearancesOnSkip = true;
                    target.typewriter.SkipTypewriter();
                }

                // Re-measure and re-clamp after TMP parses the active line so wide narration stays inside the frame.
                if (target.textfield != null) target.textfield.ForceMeshUpdate(true, true);
                var rect = target.transform as RectTransform;
                if (rect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    PositionBox(target);
                }
                return true;
            }
            catch (Exception ex)
            {
                ModEntry.LogError("NarrationBox.TryShow failed; falling back to native bubble: " + ex);
                handledInDisplay = null;
                Hide(ui);
                return false;
            }
        }

        internal static void RefreshLayout(DialogUIManager ui)
        {
            if (ReferenceEquals(ui, null) || !ReferenceEquals(owner, ui) || ReferenceEquals(box, null) || box == null) return;
            try
            {
                ApplyScale(box);
                PositionBox(box);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("NarrationBox.RefreshLayout failed: " + ex);
            }
        }

        internal static void SyncSpeed()
        {
            if (ReferenceEquals(box, null) || box == null) return;
            try { SyncSpeed(box); }
            catch (Exception ex) { ModEntry.LogError("NarrationBox.SyncSpeed failed: " + ex); }
        }

        private static void RecordHistory(DialogUIManager ui, Dialogue dialogue)
        {
            var client = PlayerStore.Instance?.currentClientInstance;
            if (client != null)
            {
                ui.UpdateDialogHistory("<color=#eb4034>" + client.GetClientBlueprint().displayName + ":</color> " + dialogue.plainText);
            }
            else
            {
                ui.UpdateDialogHistory("<color=#eb4034>Customer :</color> " + dialogue.plainText);
            }
        }

        private static TextBubble EnsureBox(DialogUIManager ui)
        {
            if (!ReferenceEquals(box, null) && box != null && ReferenceEquals(owner, ui))
                return box;

            if (!ReferenceEquals(box, null))
            {
                try { if (box != null) UnityEngine.Object.Destroy(box.gameObject); }
                catch { }
                box = null;
            }

            Transform parent = ui.clientBubble.transform.parent;
            if (parent == null) return null;

            float scaleFactor = EmporiumOption.DialogBubbleScale;
            if (scaleFactor <= 0.01f) scaleFactor = 1f;
            baseScale = ui.clientBubble.transform.localScale / scaleFactor;

            TextBubble clone = UnityEngine.Object.Instantiate(ui.clientBubble, parent);
            clone.name = ObjectName;
            clone.gameObject.SetActive(false);
            if (clone.textfield != null) clone.textfield.text = string.Empty;

            int clientIndex = ui.clientBubble.transform.GetSiblingIndex();
            clone.transform.SetSiblingIndex(Mathf.Min(parent.childCount - 1, clientIndex + 1));

            if (clone.typewriter != null)
            {
                clone.typewriter.resetTypingSpeedAtStartup = false;
                clone.typewriter.onTextShowed = new UnityEvent();
                clone.typewriter.onTextShowed.AddListener(ui.OnCurrentTextDisplayed);
            }

            DialogBubbleClickAdvance.Attach(clone.gameObject, () =>
            {
                if (TutorialUIManager.Instance != null)
                    TutorialUIManager.Instance.DismissAdvanceDialogHint();
                if (ui != null)
                    ui.OnAdvancePressed();
            });

            HideExtraTailGraphics(clone);
            NormalizePadding(clone);
            ApplyStyle(clone);

            owner = ui;
            box = clone;
            return clone;
        }

        private static void HideExtraTailGraphics(TextBubble target)
        {
            Transform textRoot = target.textfield != null ? target.textfield.transform : null;
            Image bg = target.backgroundImage;
            foreach (Image img in target.GetComponentsInChildren<Image>(true))
            {
                if (img == null || img == bg) continue;
                if (textRoot != null && (img.transform == textRoot || textRoot.IsChildOf(img.transform) || img.transform.IsChildOf(textRoot)))
                    continue;
                img.gameObject.SetActive(false);
            }
        }

        private static void NormalizePadding(TextBubble target)
        {
            // Keep symmetric padding in the native canvas's own pixel units so the tailless
            // frame matches the game's resolution without inflating low-res pixel-art bubbles.
            LayoutGroup group = target.GetComponent<LayoutGroup>();
            if (group != null)
            {
                RectOffset p = group.padding;
                int h = (p.left > 0 && p.right > 0) ? Math.Min(p.left, p.right) : Math.Max(p.left, p.right);
                int v = (p.top > 0 && p.bottom > 0) ? Math.Min(p.top, p.bottom) : Math.Max(p.top, p.bottom);
                if (h > 0 || v > 0)
                    group.padding = new RectOffset(h, h, v, v);
                return;
            }
            if (target.textfield != null)
            {
                RectTransform tr = target.textfield.rectTransform;
                if (tr != null && tr.parent == target.transform && tr.anchorMin == Vector2.zero && tr.anchorMax == Vector2.one)
                {
                    float left = tr.offsetMin.x;
                    float right = -tr.offsetMax.x;
                    float bottom = tr.offsetMin.y;
                    float top = -tr.offsetMax.y;
                    float h = (left > 0f && right > 0f) ? Mathf.Min(left, right) : Mathf.Max(Mathf.Abs(left), Mathf.Abs(right));
                    float v = (top > 0f && bottom > 0f) ? Mathf.Min(top, bottom) : Mathf.Max(Mathf.Abs(top), Mathf.Abs(bottom));
                    if (h > 0f || v > 0f)
                    {
                        tr.offsetMin = new Vector2(h, v);
                        tr.offsetMax = new Vector2(-h, -v);
                    }
                }
            }
        }

        private static void ApplyStyle(TextBubble target)
        {
            if (target.backgroundImage != null)
            {
                float refPpu = target.backgroundImage.sprite != null && target.backgroundImage.sprite != panelSprite && target.backgroundImage.sprite.pixelsPerUnit > 0f
                    ? target.backgroundImage.sprite.pixelsPerUnit
                    : (panelSpritePpu > 0f ? panelSpritePpu : 100f);
                Sprite sprite = EnsurePanelSprite(refPpu);
                if (sprite != null)
                {
                    target.backgroundImage.sprite = sprite;
                    target.backgroundImage.type = Image.Type.Sliced;
                    target.backgroundImage.color = Color.white;
                    target.backgroundImage.raycastTarget = true;
                }
            }
            if (target.textfield != null)
            {
                target.textfield.color = TextColor;
                target.textfield.alignment = TextAlignmentOptions.Center;
            }
        }

        private static void ApplyScale(TextBubble target)
        {
            float factor = EmporiumOption.DialogBubbleScale;
            if (factor <= 0.01f) factor = 1f;
            target.transform.localScale = baseScale * factor;
        }

        private static void SyncSpeed(TextBubble target)
        {
            if (target.typewriter == null) return;
            float baseSpeed = StoreUIManager.Instance != null ? StoreUIManager.Instance.dialogSpeed : 1f;
            target.typewriter.SetTypewriterSpeed(baseSpeed * GetLocaleSpeedMultiplier());
        }

        private static float GetLocaleSpeedMultiplier()
        {
            string code = LocalizationSettings.SelectedLocale != null
                ? LocalizationSettings.SelectedLocale.Identifier.Code
                : "en";
            return code == "ko" || code == "ja" || code == "zh" || code == "zh-Hans" ? 0.35f : 1f;
        }

        private static void PositionBox(TextBubble target)
        {
            RectTransform rect = target.transform as RectTransform;
            if (rect == null) return;
            RectTransform parent = rect.parent as RectTransform;
            if (parent == null) return;

            rect.pivot = new Vector2(0.5f, 1f);
            Rect pr = parent.rect;

            // Default anchor: top-center above the sales window (horizontally centered in the store).
            float targetX = pr.center.x;
            float targetY = pr.yMax - pr.height * 0.035f;

            StoreUIManager storeUi = StoreUIManager.Instance;
            bool closeup = storeUi != null && (storeUi.isShootingMode || storeUi.isMagnifyingMode);
            if (!closeup && TryGetSalesWindowTopCenter(parent, out Vector2 winTop))
            {
                targetX = winTop.x;
                targetY = Mathf.Min(pr.yMax - pr.height * 0.035f, winTop.y + pr.height * 0.035f);
            }

            float halfW = Mathf.Abs(rect.rect.width * rect.localScale.x) * 0.5f;
            float marginX = pr.width * 0.025f;
            float minX = pr.xMin + halfW + marginX;
            float maxX = pr.xMax - halfW - marginX;
            targetX = minX <= maxX ? Mathf.Clamp(targetX, minX, maxX) : pr.center.x;
            targetY = Mathf.Clamp(targetY, pr.yMax - pr.height * 0.14f, pr.yMax - pr.height * 0.03f);

            rect.localPosition = new Vector3(targetX, targetY, rect.localPosition.z);
        }

        private static bool TryGetSalesWindowTopCenter(RectTransform targetParent, out Vector2 localTopCenter)
        {
            localTopCenter = Vector2.zero;
            Canvas targetCanvas = targetParent.GetComponentInParent<Canvas>();
            Camera targetCam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;

            StoreShutterButton shutter = StoreShutterButton.Instance;
            RectTransform mask = shutter != null && shutter.blockerMaskObject != null
                ? shutter.blockerMaskObject.rectTransform
                : null;
            if (mask != null)
            {
                mask.GetWorldCorners(windowCorners);
                Vector3 worldTopCenter = (windowCorners[1] + windowCorners[2]) * 0.5f;
                Canvas maskCanvas = mask.GetComponentInParent<Canvas>();
                Camera maskCam = maskCanvas != null && maskCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? maskCanvas.worldCamera
                    : null;
                Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(maskCam, worldTopCenter);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParent, screenPt, targetCam, out localTopCenter))
                {
                    Rect pr = targetParent.rect;
                    if (localTopCenter.x >= pr.xMin && localTopCenter.x <= pr.xMax
                        && localTopCenter.y >= pr.yMin && localTopCenter.y <= pr.yMax + pr.height * 0.1f)
                        return true;
                }
            }

            StoreUIManager storeUi = StoreUIManager.Instance;
            Camera worldCam = Camera.main;
            if (storeUi != null && storeUi.clientGameObjectParent != null && worldCam != null)
            {
                Vector3 screenPt3 = worldCam.WorldToScreenPoint(storeUi.clientGameObjectParent.transform.position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(targetParent, screenPt3, targetCam, out Vector2 clientLocal))
                {
                    Rect pr = targetParent.rect;
                    localTopCenter = new Vector2(clientLocal.x, pr.yMax - pr.height * 0.035f);
                    return true;
                }
            }

            return false;
        }

        private static Sprite EnsurePanelSprite(float pixelsPerUnit)
        {
            if (pixelsPerUnit <= 0f) pixelsPerUnit = 100f;
            if (panelSprite != null && Mathf.Approximately(panelSpritePpu, pixelsPerUnit)) return panelSprite;
            if (panelSprite != null)
            {
                try { UnityEngine.Object.Destroy(panelSprite); } catch { }
                panelSprite = null;
            }
            const int size = 16;
            if (panelTexture == null)
            {
                panelTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                panelTexture.filterMode = FilterMode.Point;
                panelTexture.wrapMode = TextureWrapMode.Clamp;

                Color32[] pixels = new Color32[size * size];
                Color32 clear = new Color32(0, 0, 0, 0);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool corner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);
                        if (corner)
                        {
                            pixels[y * size + x] = clear;
                            continue;
                        }
                        bool outer = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                        bool innerBorder = x == 1 || x == size - 2 || y == 1 || y == size - 2;
                        pixels[y * size + x] = outer ? PanelOuter : innerBorder ? PanelBorder : PanelFill;
                    }
                }
                panelTexture.SetPixels32(pixels);
                panelTexture.Apply(false, false);
            }

            panelSprite = Sprite.Create(
                panelTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0u,
                SpriteMeshType.FullRect,
                new Vector4(4f, 4f, 4f, 4f));
            panelSpritePpu = pixelsPerUnit;
            return panelSprite;
        }
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.DisplayClientText))]
    internal static class NarrationBoxDisplayPatch
    {
        // Run after ModDialogueSpeakerPatch.Prefix (default priority) so DialogueSpeakers
        // has already marked the line for history rewriting and expression hooks have run.
        [HarmonyPriority(Priority.Low)]
        private static bool Prefix(DialogUIManager __instance, Dialogue dialogue, ref Dialogue ___displayingUncompletedDialog)
            => !NarrationBox.TryShow(__instance, dialogue, ref ___displayingUncompletedDialog);
    }

    [HarmonyPatch(typeof(DialogUIManager), "IsAnyBubbleTyping")]
    internal static class NarrationBoxTypingPatch
    {
        private static void Postfix(DialogUIManager __instance, ref bool __result)
        {
            if (!__result && NarrationBox.IsTyping(__instance)) __result = true;
        }
    }

    [HarmonyPatch(typeof(DialogUIManager), "SkipTypewriters")]
    internal static class NarrationBoxSkipPatch
    {
        private static void Postfix(DialogUIManager __instance) => NarrationBox.SkipTypewriter(__instance);
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.HideBubbles))]
    internal static class NarrationBoxHidePatch
    {
        private static void Postfix(DialogUIManager __instance) => NarrationBox.Hide(__instance);
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.HideAllDialogUI))]
    internal static class NarrationBoxHideAllPatch
    {
        private static void Postfix(DialogUIManager __instance) => NarrationBox.Hide(__instance);
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.InterruptDialog))]
    internal static class NarrationBoxInterruptPatch
    {
        private static void Postfix(DialogUIManager __instance) => NarrationBox.Hide(__instance);
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.ResumeIfWaitingForAdvance))]
    internal static class NarrationBoxResumePatch
    {
        private static bool Prefix(DialogUIManager __instance) => !NarrationBox.IsTyping(__instance);
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.ApplyBubbleScale))]
    internal static class NarrationBoxScalePatch
    {
        private static void Postfix(DialogUIManager __instance) => NarrationBox.RefreshLayout(__instance);
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.SetNormalMode))]
    internal static class NarrationBoxNormalModePatch
    {
        private static void Postfix(DialogUIManager __instance) => NarrationBox.RefreshLayout(__instance);
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.SetShootingMode))]
    internal static class NarrationBoxShootingModePatch
    {
        private static void Postfix(DialogUIManager __instance) => NarrationBox.RefreshLayout(__instance);
    }

    [HarmonyPatch(typeof(DialogUIManager), nameof(DialogUIManager.PlayClientSingleText))]
    internal static class NarrationBoxSingleTextPatch
    {
        private static void Prefix(DialogUIManager __instance) => NarrationBox.Hide(__instance);
    }

    [HarmonyPatch(typeof(StoreUIManager), nameof(StoreUIManager.SetTypewriterSpeed))]
    internal static class NarrationBoxSpeedPatch
    {
        private static void Postfix() => NarrationBox.SyncSpeed();
    }
}
