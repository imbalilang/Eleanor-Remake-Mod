using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EleanorRomance
{
    public static partial class EndingPageUI
    {
        private static Button BuildStoryOverlay(Transform parent, Sprite cg, EndingPages.Page page)
        {
            // Size the image and its controls against the same game area, including when
            // the native letterbox manager reparents the root canvas to its UI camera.
            Image art = NewImage(parent, "cg", Color.white);
            art.raycastTarget = false;
            art.sprite = cg;
            art.preserveAspect = true;
            Stretch(art.rectTransform);
            AspectRatioFitter fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectRatio = 16f / 9f;
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            TMP_FontAsset font = ResolvePageFont();
            BuildStoryCaption(art.transform, page, font);
            return BuildStoryContinue(art.transform, font);
        }

        private static void BuildStoryCaption(Transform parent, EndingPages.Page page, TMP_FontAsset font)
        {
            const float paddingX = 24f, paddingY = 16f, gap = 5f, maxWidth = 740f;
            Image plate = NewImage(parent, "caption", new Color(0.06f, 0.05f, 0.09f, 0.83f));
            plate.raycastTarget = false;
            var title = NewText(plate.transform, "title", page.LiteralTitle ?? L.Get(page.TitleKey),
                font, 22f, TextAlignmentOptions.TopLeft, new Color(1f, 0.85f, 0.58f));
            var body = NewText(plate.transform, "body", page.Text, font, 30f, TextAlignmentOptions.TopLeft, Color.white);

            float textWidth = Mathf.Clamp(Mathf.Ceil(Mathf.Max(title.preferredWidth, body.preferredWidth)) + 2f, 1f, maxWidth);
            float titleHeight = Mathf.Ceil(title.GetPreferredValues(title.text, textWidth, Mathf.Infinity).y);
            float bodyHeight = Mathf.Ceil(body.GetPreferredValues(body.text, textWidth, Mathf.Infinity).y);
            RectTransform box = plate.rectTransform;
            // Tabletop gestures, the pot and the countersigned paper occupy the lower edge in these shots.
            bool top = page.CgKey switch
            {
                "er_event_harem_dice_cg" or "er_event_harem_first_kiss_cg" or "er_event_harem_nod_cg" or
                "er_event_harem_lydia_date_cg" or "er_event_love_water_cut_cg" or "er_event_maid_signature_cg" or
                "er_event_revolution_wrist_cg" or "er_event_revolution_ride_cg" => true,
                _ => false
            };
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0f, top ? 1f : 0f);
            box.anchoredPosition = new Vector2(48f, top ? -40f : 40f);
            box.sizeDelta = new Vector2(textWidth + 2f * paddingX, titleHeight + gap + bodyHeight + 2f * paddingY);
            SetCaptionLine(title.rectTransform, paddingX, paddingY, textWidth, titleHeight);
            SetCaptionLine(body.rectTransform, paddingX, paddingY + titleHeight + gap, textWidth, bodyHeight);
        }

        private static void SetCaptionLine(RectTransform line, float x, float y, float width, float height)
        {
            line.anchorMin = line.anchorMax = line.pivot = new Vector2(0f, 1f);
            line.anchoredPosition = new Vector2(x, -y);
            line.sizeDelta = new Vector2(width, height);
        }

        private static Button BuildStoryContinue(Transform parent, TMP_FontAsset font)
        {
            Image rim = NewImage(parent, "continue", new Color32(55, 36, 23, 255));
            RectTransform hit = rim.rectTransform;
            hit.anchorMin = hit.anchorMax = hit.pivot = new Vector2(1f, 0f);
            hit.anchoredPosition = new Vector2(-48f, 40f);
            hit.sizeDelta = new Vector2(248f, 88f);

            Image face = NewImage(rim.transform, "face", Color.white);
            face.raycastTarget = false;
            Stretch(face.rectTransform);
            face.rectTransform.offsetMin = new Vector2(4f, 4f);
            face.rectTransform.offsetMax = new Vector2(-4f, -4f);
            Button button = rim.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color32(240, 191, 98, 255);
            colors.highlightedColor = new Color32(255, 225, 154, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color32(207, 149, 64, 255);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = NewText(face.transform, "label", L.Get("er_story_cg_continue"), font,
                32f, TextAlignmentOptions.Center, new Color32(43, 29, 18, 255));
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform);
            return button;
        }
    }
}
