using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    /// <summary>
    /// A compact world-space tag that hovers over the car currently in the bay and spells out
    /// the exact part it needs ("NEEDS 4× TIRES"), tinted with that part's catalog colour so
    /// the requirement is unmistakable at a glance. Billboards to the camera and follows the
    /// car; hidden while driving a vehicle. Built entirely in code — no prefab to wire.
    /// </summary>
    public sealed class CarNeedLabel : MonoBehaviour
    {
        private Camera _camera;
        private PlayerViewController _playerView;
        private VehicleInteractor _vehicleInteractor;
        private Transform _follow;
        private float _height;

        private GameObject _bubble;
        private Image _background;
        private Image _accent;
        private Text _text;

        public static CarNeedLabel Attach(Transform car, float height)
        {
            var go = new GameObject("CarNeedLabel");
            var label = go.AddComponent<CarNeedLabel>();
            label._follow = car;
            label._height = height;
            label.Build();
            return label;
        }

        private void Awake()
        {
            _camera = Camera.main;
            _playerView = FindAnyObjectByType<PlayerViewController>();
            _vehicleInteractor = FindAnyObjectByType<VehicleInteractor>();
        }

        public void Set(string message, Color color)
        {
            if (_text != null) _text.text = message;
            if (_accent != null) _accent.color = color;
        }

        private void Build()
        {
            _bubble = new GameObject("Tag", typeof(RectTransform), typeof(Canvas));
            _bubble.transform.SetParent(transform, false);
            _bubble.transform.localScale = Vector3.one * 0.012f;
            var rect = (RectTransform)_bubble.transform;
            rect.sizeDelta = new Vector2(320f, 70f);
            var canvas = _bubble.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 8;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_bubble.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            _background = panel.GetComponent<Image>();
            _background.color = new Color(0.03f, 0.04f, 0.06f, 0.94f);
            _background.raycastTarget = false;

            var accentGo = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentGo.transform.SetParent(panel.transform, false);
            var accentRect = accentGo.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(1f, 0f);
            accentRect.pivot = new Vector2(0.5f, 0f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 10f);
            _accent = accentGo.GetComponent<Image>();
            _accent.color = new Color(1f, 0.70f, 0.18f);
            _accent.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 12f);
            textRect.offsetMax = new Vector2(-16f, -6f);
            _text = textGo.GetComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 30;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.color = Color.white;
            _text.raycastTarget = false;
        }

        private void LateUpdate()
        {
            if (_follow == null) { Destroy(gameObject); return; }
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            transform.position = _follow.position + Vector3.up * _height;

            bool thirdPerson = _playerView == null || !_playerView.IsFirstPerson;
            bool driving = _vehicleInteractor != null && _vehicleInteractor.IsDriving;
            bool visible = thirdPerson && !driving;
            if (_bubble.activeSelf != visible) _bubble.SetActive(visible);
            if (!visible) return;

            Vector3 toCamera = transform.position - _camera.transform.position;
            transform.rotation = Quaternion.LookRotation(toCamera, _camera.transform.up);
        }
    }
}
