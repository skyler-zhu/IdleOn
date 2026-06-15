using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IdleOnLike.UI
{
    public static class RuntimeUiFactory
    {
        private static Font runtimeFont;

        public static Canvas CreateCanvas(string name)
        {
            EnsureEventSystem();

            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<RuntimeUiLifetime>();

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
        {
            var panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);

            var rect = panelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = panelObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        public static RectTransform CreateScrollContent(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color backgroundColor)
        {
            var root = CreatePanel(parent, name, anchorMin, anchorMax, Vector2.zero, Vector2.zero, backgroundColor);
            var scrollRect = root.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 28f;

            var viewportObject = new GameObject("Viewport");
            viewportObject.transform.SetParent(root, false);
            var viewport = viewportObject.AddComponent<RectTransform>();
            Stretch(viewport, Vector2.zero, Vector2.zero);
            var viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            var mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewport, false);
            var content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            return content;
        }

        public static void SetScrollContentHeight(RectTransform content, float height)
        {
            if (content == null)
            {
                return;
            }

            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, height));
            content.anchoredPosition = Vector2.zero;
        }

        public static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<Text>();
            text.font = GetRuntimeFont();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color color)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.AddComponent<Image>();
            image.color = color;

            var button = buttonObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = color * 1.15f;
            colors.pressedColor = color * 0.85f;
            colors.selectedColor = color;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(ClearSelectedUi);

            var labelText = CreateText(buttonObject.transform, "Label", label, 22, TextAnchor.MiddleCenter, Color.white);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.zero);
            return button;
        }

        public static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void ClearSelectedUi()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private static Font GetRuntimeFont()
        {
            if (runtimeFont == null)
            {
                runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Liberation Sans", "Noto Sans" }, 16);
            }

            return runtimeFont;
        }
    }
}
