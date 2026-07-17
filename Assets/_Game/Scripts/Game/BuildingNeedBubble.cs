using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    [RequireComponent(typeof(BuildingView))]
    public sealed class BuildingNeedBubble : MonoBehaviour
    {
        [SerializeField] private float heightPadding = 1.25f;
        [SerializeField] private float maxVisibleDistance = 72f;

        private BuildingView _building;
        private PlayerViewController _playerView;
        private VehicleInteractor _vehicleInteractor;
        private Camera _camera;
        private GameObject _bubble;
        private Image _background;
        private Image _accent;
        private Text _title;
        private Text _detail;
        private float _refreshTimer;

        private void Awake()
        {
            _building = GetComponent<BuildingView>();
            _playerView = FindAnyObjectByType<PlayerViewController>();
            _vehicleInteractor = FindAnyObjectByType<VehicleInteractor>();
            _camera = Camera.main;
            BuildBubble();
            RefreshText();
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.25f;
                RefreshText();
            }
        }

        private void LateUpdate()
        {
            if (_bubble == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 toBubble = _bubble.transform.position - _camera.transform.position;
            bool thirdPerson = _playerView == null || !_playerView.IsFirstPerson;
            bool driving = _vehicleInteractor != null && _vehicleInteractor.IsDriving;
            bool visible = thirdPerson && !driving && toBubble.sqrMagnitude <= maxVisibleDistance * maxVisibleDistance
                && Vector3.Dot(_camera.transform.forward, toBubble) > 0f;
            if (_bubble.activeSelf != visible) _bubble.SetActive(visible);
            if (visible)
                _bubble.transform.rotation = Quaternion.LookRotation(toBubble, _camera.transform.up);
        }

        private void BuildBubble()
        {
            _bubble = new GameObject("NeedBubble", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            _bubble.transform.SetParent(transform, true);
            _bubble.transform.position = new Vector3(transform.position.x, FindVisualTop() + heightPadding, transform.position.z);
            _bubble.transform.localScale = Vector3.one * 0.011f;
            var rootRect = (RectTransform)_bubble.transform;
            rootRect.sizeDelta = new Vector2(300f, 88f);
            var canvas = _bubble.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 6;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_bubble.transform, false);
            Stretch(panel.GetComponent<RectTransform>());
            _background = panel.GetComponent<Image>();
            _background.color = new Color(0.035f, 0.05f, 0.065f, 0.94f);
            _background.raycastTarget = false;

            var accentGo = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentGo.transform.SetParent(panel.transform, false);
            var accentRect = accentGo.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(9f, 0f);
            _accent = accentGo.GetComponent<Image>();
            _accent.raycastTarget = false;

            _title = CreateText(panel.transform, "Building", 18, new Vector2(20f, 49f), new Vector2(264f, 27f));
            _title.color = new Color(0.72f, 0.79f, 0.86f);
            _detail = CreateText(panel.transform, "Need", 25, new Vector2(20f, 10f), new Vector2(264f, 38f));
        }

        private void RefreshText()
        {
            if (_building == null || _title == null || _detail == null) return;
            _building.GetWorldCue(out string detail, out BuildingCueTone tone);
            _title.text = _building.BuildingName.ToUpperInvariant();
            _detail.text = detail;
            Color color = tone switch
            {
                BuildingCueTone.Critical => new Color(1f, 0.32f, 0.22f),
                BuildingCueTone.Attention => new Color(1f, 0.70f, 0.18f),
                BuildingCueTone.Positive => new Color(0.25f, 0.82f, 0.48f),
                _ => new Color(0.28f, 0.66f, 1f)
            };
            _accent.color = color;
            _detail.color = color;
        }

        private float FindVisualTop()
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            float top = transform.position.y + 4.2f;
            foreach (var renderer in renderers)
                if (renderer != null) top = Mathf.Max(top, renderer.bounds.max.y);
            return top;
        }

        private static Text CreateText(Transform parent, string name, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
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
