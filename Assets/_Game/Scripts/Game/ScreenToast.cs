using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// Small transient messages ("Need tires first.", "+$24  +1 rep"). One instance under
    /// the HUD canvas; anyone can call <see cref="ScreenToast.Show"/>. Repeated identical
    /// messages just refresh the timer instead of stacking.
    /// </summary>
    public sealed class ScreenToast : MonoBehaviour
    {
        private static ScreenToast _instance;

        [SerializeField] private float holdSeconds = 1.6f;
        [SerializeField] private float fadeSeconds = 0.4f;

        private Text _text;
        private CanvasGroup _group;
        private float _timer = -1f;

        public static void Show(string message)
        {
            if (_instance == null || string.IsNullOrEmpty(message)) return;
            _instance.ShowInternal(message);
        }

        private void Awake()
        {
            _instance = this;

            _group = gameObject.GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.11f, 0.88f);

            var textGo = new GameObject("Message", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(transform, false);
            var rect = (RectTransform)textGo.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 8f); rect.offsetMax = new Vector2(-16f, -8f);

            _text = textGo.GetComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 20;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = new Color(1f, 0.95f, 0.85f);

            _group.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void ShowInternal(string message)
        {
            _text.text = message;
            _timer = holdSeconds + fadeSeconds;
            _group.alpha = 1f;
        }

        private void Update()
        {
            if (_timer < 0f) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= fadeSeconds)
                _group.alpha = Mathf.Clamp01(_timer / fadeSeconds);
            if (_timer <= 0f) _timer = -1f;
        }
    }
}
