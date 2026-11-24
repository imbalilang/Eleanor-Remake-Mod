using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace EleanorRomance
{
    /// <summary>
    /// Two page shapes share this file:
    ///  - SetPage: legacy full-screen CG behind the native GameOver panel;
    ///  - Standalone pages for route endings and illustrated story scenes.
    /// </summary>
    public static class EndingPageUI
    {
        private const string ObjectName = "er_ending_page_cg";
        private const string RouteRootName = "er_route_ending_page";
        private static GameObject cgObject;
        private static GameObject routeRoot;

        public static void SetPage(EndingPages.Page page)
        {
            try
            {
                if (page == null) { Hide(); return; }
                GameOverUIManager manager = GameOverUIManager.Instance;
                if (manager == null || manager.darkScreen == null) { Hide(); return; }

                Sprite sprite = GetCgSprite(page.CgKey);
                if (sprite == null) { Hide(); return; }

                if (cgObject == null || cgObject.transform.parent != manager.darkScreen.transform)
                {
                    Hide();
                    cgObject = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
                    cgObject.transform.SetParent(manager.darkScreen.transform, false);
                    RectTransform rect = cgObject.GetComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    Image image = cgObject.GetComponent<Image>();
                    image.raycastTarget = false;
                    image.preserveAspect = true;
                }

                cgObject.GetComponent<Image>().sprite = sprite;
                cgObject.SetActive(true);
            }
            catch (Exception ex)
            {
                ModEntry.LogError("Ending page CG failed: " + ex);
                Hide();
            }
        }

        public static void Hide()
        {
            if (cgObject != null)
            {
                UnityEngine.Object.Destroy(cgObject);
                cgObject = null;
            }
        }

        public static void ShowMaidCompletion()
        {
            if (MaidState.Flag("ending_card_shown")) return;
            EndingPages.Page page = EndingPages.TryMaidCompletion();
            if (page == null) return;
            if (ShowRoutePage(page)) MaidState.Flag("ending_card_shown", true);
        }

        public static bool ShowDualCompletion(Action onContinue = null)
        {
            if (DualState.Flag("ending_card_shown")) return false;
            EndingPages.Page page = EndingPages.TryDualCompletion();
            return page != null && ShowRoutePage(page, false, () =>
            {
                DualState.Flag("ending_card_shown", true);
                onContinue?.Invoke();
            });
        }

        public static void ShowRevolutionCompletion()
        {
            if (RomanceState.RevEndingShown) return;
            EndingPages.Page page = EndingPages.TryRevolutionCompletion();
            if (page == null) return;
            ShowRoutePage(page, false, () => RomanceState.RevEndingShown = true);
        }

        public static void ShowLoveCompletion()
        {
            if (LoveState.A3EndingShown) return;
            EndingPages.Page page = EndingPages.TryLoveCompletion();
            if (page == null) return;
            if (ShowRoutePage(page)) LoveState.A3EndingShown = true;
        }

        public static void HideRoutePage()
        {
            if (routeRoot != null)
            {
                UnityEngine.Object.Destroy(routeRoot);
                routeRoot = null;
            }
        }

        internal static bool ShowStoryScene(string titleKey, string cgKey, string textKey, Action onContinue = null)
            => ShowRoutePage(new EndingPages.Page(titleKey, cgKey, textKey), true, onContinue);

        // New chapter pages retain their caption while the corresponding illustration is in production.
        internal static bool ShowChapterPage(string titleKey, string cgKey, string textKey, Action onContinue = null)
            => ShowRoutePage(new EndingPages.Page(titleKey, cgKey, textKey), true, onContinue, true);

        internal static bool PageOpen => routeRoot != null;

        private static Sprite GetCgSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            Sprite sprite = PortraitCompat.GetLocalSprite(key);
            if (sprite == null) return null;

            // The game loads every Textures/Items PNG with point filtering.
            // Each of these CGs has its own texture, so filtering can be set per artwork.
            switch (key)
            {
                case "er_event_harem_lydia_date_cg":
                case "er_event_love_water_cut_cg":
                case "er_event_maid_gala_cg":
                case "er_event_revolution_embrace_cg":
                case "er_ending_love_wedding":
                    var texture = sprite.texture;
                    if (texture != null) texture.filterMode = FilterMode.Bilinear;
                    break;
            }
            return sprite;
        }

        internal static bool ShowScript(string title, string cg, string text, Action next,
            string[] choices = null, Action decline = null)
            => ShowRoutePage(new EndingPages.Page(title, cg, text, true), true, next, true, choices, decline);

        private static bool ShowRoutePage(EndingPages.Page page, bool storyScene = false, Action onContinue = null, bool textFallback = false, string[] choices = null, Action decline = null)
        {
            try
            {
                Sprite cg = GetCgSprite(page.CgKey);
                if (storyScene && cg == null && !textFallback)
                {
                    ModEntry.LogError("Story CG missing: " + page.CgKey);
                    return false;
                }
                HideRoutePage();

                GameObject root = new GameObject(RouteRootName,
                    typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                routeRoot = root;
                UnityEngine.Object.DontDestroyOnLoad(root);
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 30000;
                CanvasScaler scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                Image background = NewImage(root.transform, "background", new Color(0.015f, 0.018f, 0.025f, 0.97f));
                Stretch(background.rectTransform);
                background.raycastTarget = true;

                if (cg != null)
                {
                    Image image = NewImage(root.transform, "cg", Color.white);
                    // Keep the whole 16:9 CG above the text, including hands
                    // and gestures near the counter at the bottom of the art.
                    RectTransform imageRect = image.rectTransform;
                    imageRect.anchorMin = new Vector2(0.03f, storyScene ? 0.265f : 0.44f);
                    imageRect.anchorMax = new Vector2(0.97f, 0.98f);
                    imageRect.offsetMin = Vector2.zero;
                    imageRect.offsetMax = Vector2.zero;
                    image.sprite = cg;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                }

                Image panel = NewImage(root.transform, "panel", new Color(0.03f, 0.035f, 0.05f, 0.90f));
                RectTransform panelRect = panel.rectTransform;
                panelRect.anchorMin = new Vector2(0.055f, 0.035f);
                panelRect.anchorMax = new Vector2(0.945f, cg == null ? 0.72f : storyScene ? 0.25f : 0.415f);
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;

                TMP_FontAsset font = ResolvePageFont();
                TextMeshProUGUI title = NewText(panel.transform, "title", page.LiteralTitle ?? L.Get(page.TitleKey), font,
                    34, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.55f));
                RectTransform titleRect = title.rectTransform;
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -18f);
                titleRect.sizeDelta = new Vector2(-40f, 48f);

                TextMeshProUGUI body = NewText(panel.transform, "body", page.Text, font,
                    20, storyScene ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft, Color.white);
                body.enableAutoSizing = true;
                body.fontSizeMin = 16f;
                body.fontSizeMax = storyScene ? 25f : 20f;
                RectTransform bodyRect = body.rectTransform;
                bodyRect.anchorMin = Vector2.zero;
                bodyRect.anchorMax = Vector2.one;
                bodyRect.offsetMin = new Vector2(32f, storyScene ? 78f : 118f);
                bodyRect.offsetMax = new Vector2(-32f, -76f);

                if (!storyScene)
                {
                    TextMeshProUGUI note = NewText(panel.transform, "note", L.Get("er_ending_content_end"), font,
                        17, TextAlignmentOptions.Center, new Color(0.80f, 0.84f, 0.90f));
                    RectTransform noteRect = note.rectTransform;
                    noteRect.anchorMin = new Vector2(0f, 0f);
                    noteRect.anchorMax = new Vector2(1f, 0f);
                    noteRect.pivot = new Vector2(0.5f, 0f);
                    noteRect.anchoredPosition = new Vector2(0f, 82f);
                    noteRect.sizeDelta = new Vector2(-40f, 28f);
                }

                GameObject buttonObject = new GameObject("continue",
                    typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(panel.transform, false);
                RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.5f, 0f);
                buttonRect.anchorMax = new Vector2(0.5f, 0f);
                buttonRect.pivot = new Vector2(0.5f, 0f);
                buttonRect.anchoredPosition = new Vector2(0f, 22f);
                buttonRect.sizeDelta = new Vector2(430f, 50f);
                Image buttonImage = buttonObject.GetComponent<Image>();
                buttonImage.color = new Color(0.72f, 0.58f, 0.32f, 0.96f);
                Button button = buttonObject.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    if (!ReferenceEquals(routeRoot, root)) return;
                    HideRoutePage();
                    onContinue?.Invoke();
                });

                TextMeshProUGUI buttonText = NewText(buttonObject.transform, "label",
                    choices == null ? L.Get(storyScene ? "er_story_cg_continue" : "er_ending_return_button") : choices[0], font, 20, TextAlignmentOptions.Center,
                    new Color(0.10f, 0.08f, 0.04f));
                Stretch(buttonText.rectTransform);
                if (choices != null)
                {
                    buttonRect.anchoredPosition = new Vector2(-230f, 22f);
                    var other = UnityEngine.Object.Instantiate(buttonObject, panel.transform, false);
                    other.GetComponent<RectTransform>().anchoredPosition = new Vector2(230f, 22f);
                    other.GetComponentInChildren<TextMeshProUGUI>().text = choices[1];
                    var otherButton = other.GetComponent<Button>();
                    otherButton.onClick = new Button.ButtonClickedEvent();
                    otherButton.onClick.AddListener(() =>
                    {
                        if (!ReferenceEquals(routeRoot, root)) return;
                        HideRoutePage();
                        decline?.Invoke();
                    });
                }


                return true;
            }
            catch (Exception ex)
            {
                ModEntry.LogError((storyScene ? "Story CG failed: " : "Standalone ending page failed: ") + ex);
                HideRoutePage();
                return false;
            }
        }

        /// <summary>
        /// Page text follows the game's current language.  TMP's has-glyph
        /// probing used to pick a Chinese font for every locale and left
        /// missing-glyph boxes in English/Japanese/Korean.
        /// </summary>
        private static TMP_FontAsset ResolvePageFont()
        {
            try
            {
                TMP_FontAsset localized = TryLocaleFont();
                if (localized != null) return localized;
            }
            catch (Exception ex)
            {
                ModEntry.Log("Locale font unavailable; using TMP default: " + ex.GetType().Name);
            }
            return TMP_Settings.defaultFontAsset;
        }

        // Kept out of line so a game build without the localization package
        // fails here and falls back instead of failing the whole page.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TMP_FontAsset TryLocaleFont()
        {
            FontLoader loader = FontLoader.Instance;
            if (loader == null) return null;
            Locale locale = LocalizationSettings.SelectedLocale;
            if (locale == null) return null;
            return loader.GetFontByRole(FontLoader.FontRole.Noto_Sans, locale);
        }

        private static Image NewImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI NewText(Transform parent, string name, string text,
            TMP_FontAsset font, float size, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.enableWordWrapping = true;
            label.raycastTarget = false;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
