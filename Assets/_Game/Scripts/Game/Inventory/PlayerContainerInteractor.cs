using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// The player's "walk up and press a key" hook for containers. Each frame it finds the
    /// nearest <see cref="InventoryContainer"/> within that container's radius and, while one is
    /// in reach, shows a prompt. Pressing the interact key opens the
    /// <see cref="ContainerTransferScreen"/> for it (and closes it again). Walking out of range
    /// closes an open screen, so the player is never trapped in a menu.
    /// </summary>
    public sealed class PlayerContainerInteractor : MonoBehaviour
    {
        [SerializeField] private InventoryComponent playerInventory;
        [SerializeField] private Key interactKey = Key.E;

        private InventoryContainer _open;
        private Text _prompt;

        private void Awake()
        {
            if (playerInventory == null) playerInventory = GetComponent<InventoryComponent>();
        }

        private void Update()
        {
            var screen = ContainerTransferScreen.Instance;

            // Auto-close if we wandered away from the container we opened.
            if (_open != null && (screen == null || !screen.IsShowing(_open) || OutOfRange(_open)))
            {
                if (screen != null && screen.IsShowing(_open)) screen.Close();
                _open = null;
            }

            var nearest = _open != null ? _open : FindNearest();

            var kb = Keyboard.current;
            if (kb != null && kb[interactKey].wasPressedThisFrame && playerInventory != null)
            {
                if (_open != null) { screen.Close(); _open = null; }
                else if (nearest != null) { screen.Open(nearest, playerInventory); _open = nearest; }
            }

            UpdatePrompt(nearest);
        }

        private bool OutOfRange(InventoryContainer c)
            => Vector3.Distance(transform.position, c.Position) > c.InteractRadius + 0.75f;

        private InventoryContainer FindNearest()
        {
            InventoryContainer best = null;
            float bestDist = float.MaxValue;
            foreach (var c in FindObjectsByType<InventoryContainer>(FindObjectsSortMode.None))
            {
                if (c == null) continue;
                float d = Vector3.Distance(transform.position, c.Position);
                if (d <= c.InteractRadius && d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        private void UpdatePrompt(InventoryContainer near)
        {
            bool show = near != null && (_open == null);
            if (_prompt == null)
            {
                if (!show) return;
                BuildPrompt();
            }
            _prompt.transform.parent.gameObject.SetActive(show);
            if (show)
                _prompt.text = $"[{interactKey}]  Open {near.DisplayTitle}";
        }

        private void BuildPrompt()
        {
            var canvasGo = new GameObject("ContainerPromptCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panel = new GameObject("Prompt", typeof(RectTransform));
            panel.transform.SetParent(canvasGo.transform, false);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 150f);
            rt.sizeDelta = new Vector2(420f, 52f);

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(panel.transform, false);
            var t = txtGo.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            var trt = (RectTransform)txtGo.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            _prompt = t;
        }
    }
}
