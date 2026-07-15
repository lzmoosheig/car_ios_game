using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// The one compact building/NPC panel (Doc 09 §3.3): title, a few live status lines,
    /// up to three action buttons. Builds its own uGUI hierarchy at runtime so the scene
    /// only needs an anchored, empty rect under the HUD canvas.
    ///
    /// Panels configure and inform; they never move items. All logistics stay physical.
    /// </summary>
    public sealed class InfoPanelView : MonoBehaviour
    {
        private const int MaxLines = 6;
        private const int MaxActions = 3;
        private const float RefreshInterval = 0.25f;

        private IInteractable _target;
        private Text _title;
        private readonly Text[] _lines = new Text[MaxLines];
        private readonly Button[] _buttons = new Button[MaxActions];
        private readonly Text[] _buttonLabels = new Text[MaxActions];
        private readonly List<string> _lineBuf = new();
        private readonly List<InteractableAction> _actionBuf = new();
        private float _timer;
        private bool _built;

        public bool IsOpen => _target != null;

        public void Open(IInteractable target)
        {
            EnsureBuilt();
            _target = target;
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            _target = null;
            if (_built) gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_target == null) return;
            _timer += Time.unscaledDeltaTime;
            if (_timer < RefreshInterval) return;
            _timer = 0f;
            Refresh();
        }

        private void Refresh()
        {
            if (_target == null || (_target as Component) == null) { Close(); return; }

            _title.text = _target.Title;

            _lineBuf.Clear();
            _target.GetInfoLines(_lineBuf);
            for (int i = 0; i < MaxLines; i++)
            {
                bool used = i < _lineBuf.Count;
                _lines[i].gameObject.SetActive(used);
                if (used) _lines[i].text = _lineBuf[i];
            }

            _actionBuf.Clear();
            _target.GetActions(_actionBuf);
            for (int i = 0; i < MaxActions; i++)
            {
                bool used = i < _actionBuf.Count;
                _buttons[i].gameObject.SetActive(used);
                if (!used) continue;
                var action = _actionBuf[i];
                _buttonLabels[i].text = action.Label;
                _buttons[i].interactable = action.Enabled;
                _buttons[i].onClick.RemoveAllListeners();
                var invoke = action.Invoke;
                _buttons[i].onClick.AddListener(() => { invoke?.Invoke(); Refresh(); });
            }
        }

        // ------------------------------------------------------------- runtime uGUI build

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.09f, 0.14f, 0.92f);

            var layout = gameObject.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 12);
            layout.spacing = 4f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            var fitter = gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _title = MakeText(font, "Title", 22, FontStyle.Bold, new Color(1f, 0.86f, 0.4f));
            for (int i = 0; i < MaxLines; i++)
                _lines[i] = MakeText(font, $"Line{i}", 17, FontStyle.Normal, new Color(0.88f, 0.92f, 1f));

            for (int i = 0; i < MaxActions; i++)
            {
                var btnGo = new GameObject($"Action{i}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                btnGo.transform.SetParent(transform, false);
                btnGo.GetComponent<LayoutElement>().minHeight = 44f; // comfortable tap target
                var img = btnGo.GetComponent<Image>();
                img.color = new Color(0.16f, 0.45f, 0.85f, 0.95f);
                _buttons[i] = btnGo.GetComponent<Button>();
                _buttons[i].targetGraphic = img;

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(btnGo.transform, false);
                var rect = (RectTransform)labelGo.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                var label = labelGo.GetComponent<Text>();
                label.font = font;
                label.fontSize = 18;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                _buttonLabels[i] = label;
            }

            gameObject.SetActive(false);
        }

        private Text MakeText(Font font, string name, int size, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }
    }
}
