using IdleOnLike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IdleOnLike.UI
{
    public sealed class ExitConfirmationPanel
    {
        private readonly GameRuntime runtime;
        private readonly RectTransform root;

        public ExitConfirmationPanel(GameRuntime runtime)
        {
            this.runtime = runtime;
            var canvas = RuntimeUiFactory.CreateCanvas("Exit Confirmation UI");
            root = RuntimeUiFactory.CreatePanel(canvas.transform, "Exit Confirmation Panel", new Vector2(0.36f, 0.36f), new Vector2(0.64f, 0.62f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.06f, 0.08f, 0.96f));

            var title = RuntimeUiFactory.CreateText(root, "Title", "Exit Game?", 28, TextAnchor.MiddleCenter, Color.white);
            RuntimeUiFactory.SetRect(title.rectTransform, new Vector2(0.08f, 0.60f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

            var yesButton = RuntimeUiFactory.CreateButton(root, "Yes Button", "Yes", new Color(0.45f, 0.18f, 0.18f, 1f));
            RuntimeUiFactory.SetRect(yesButton.GetComponent<RectTransform>(), new Vector2(0.12f, 0.18f), new Vector2(0.46f, 0.42f), Vector2.zero, Vector2.zero);
            yesButton.onClick.AddListener(ExitGame);

            var noButton = RuntimeUiFactory.CreateButton(root, "No Button", "No", new Color(0.26f, 0.32f, 0.42f, 1f));
            RuntimeUiFactory.SetRect(noButton.GetComponent<RectTransform>(), new Vector2(0.54f, 0.18f), new Vector2(0.88f, 0.42f), Vector2.zero, Vector2.zero);
            noButton.onClick.AddListener(Hide);

            root.gameObject.SetActive(false);
            RuntimeUiOverlayRegistry.Register(root);
        }

        public void Show()
        {
            RuntimeUiOverlayRegistry.Show(root);
        }

        private void Hide()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        private void ExitGame()
        {
            runtime?.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
