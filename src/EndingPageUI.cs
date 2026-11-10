using UnityEngine;
using UnityEngine.UI;

namespace EleanorRomance
{
    /// <summary>
    /// Adds a single full-screen CG behind the native GameOver panel.
    /// Placeholder art can be replaced without touching this code.
    /// </summary>
    public static class EndingPageUI
    {
        private const string ObjectName = "er_ending_page_cg";
        private static GameObject cgObject;

        public static void SetPage(EndingPages.Page page)
        {
            try
            {
                if (page == null) { Hide(); return; }
                GameOverUIManager manager = GameOverUIManager.Instance;
                if (manager == null || manager.darkScreen == null) { Hide(); return; }

                Sprite sprite = PortraitCompat.GetLocalSprite(page.CgKey);
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
                    image.preserveAspect = false;
                }

                cgObject.GetComponent<Image>().sprite = sprite;
                cgObject.SetActive(true);
            }
            catch (System.Exception ex)
            {
                ModEntry.LogError("Ending page CG failed: " + ex);
                Hide();
            }
        }

        public static void Hide()
        {
            if (cgObject != null)
            {
                Object.Destroy(cgObject);
                cgObject = null;
            }
        }
    }
}
