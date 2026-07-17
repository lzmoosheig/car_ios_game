using UnityEngine;
using UnityEngine.UI;

namespace Overhaul.Game
{
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerSprintHud : MonoBehaviour
    {
        private PlayerController _controller;
        private VehicleInteractor _vehicleInteractor;
        private GameObject _buttonRoot;
        private Image _buttonImage;
        private Text _label;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _vehicleInteractor = GetComponent<VehicleInteractor>();
            BuildUi();
            _controller.SprintChanged += Refresh;
            Refresh(_controller.IsSprinting);
        }

        private void OnDestroy()
        {
            if (_controller != null) _controller.SprintChanged -= Refresh;
        }

        private void Update()
        {
            bool visible = _vehicleInteractor == null || !_vehicleInteractor.IsDriving;
            if (_buttonRoot != null && _buttonRoot.activeSelf != visible) _buttonRoot.SetActive(visible);
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("SprintHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 18;
            canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _buttonRoot = new GameObject("SprintToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            _buttonRoot.transform.SetParent(canvasGo.transform, false);
            var rect = _buttonRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-38f, 38f);
            rect.sizeDelta = new Vector2(154f, 76f);
            _buttonImage = _buttonRoot.GetComponent<Image>();
            _buttonRoot.GetComponent<Button>().onClick.AddListener(_controller.ToggleSprint);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(_buttonRoot.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            _label = labelGo.GetComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 23;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.raycastTarget = false;
        }

        private void Refresh(bool sprinting)
        {
            if (_label != null) _label.text = sprinting ? ">>  SPRINT" : ">  RUN";
            if (_buttonImage != null) _buttonImage.color = sprinting
                ? new Color(0.08f, 0.62f, 0.40f, 0.94f)
                : new Color(0.055f, 0.07f, 0.085f, 0.82f);
        }
    }
}
