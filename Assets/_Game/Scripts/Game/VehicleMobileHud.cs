using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Overhaul.Game
{
    public enum VehicleMobileControl { Left, Right, Forward, Reverse }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(VehicleInteractor))]
    public sealed class VehicleMobileHud : MonoBehaviour
    {
        private VehicleInteractor _interactor;
        private GameObject _drivingControls;
        private GameObject _toggleButton;
        private Text _toggleLabel;

        private void Awake()
        {
            _interactor = GetComponent<VehicleInteractor>();
            BuildUi();
        }

        private void Update()
        {
            bool driving = _interactor.IsDriving;
            if (_drivingControls != null && _drivingControls.activeSelf != driving)
                _drivingControls.SetActive(driving);

            bool showToggle = driving || _interactor.CanEnterVehicle;
            if (_toggleButton != null && _toggleButton.activeSelf != showToggle)
                _toggleButton.SetActive(showToggle);
            if (_toggleLabel != null) _toggleLabel.text = driving ? "EXIT" : "DRIVE";
        }

        public void SetHeld(VehicleMobileControl control, bool held) => _interactor.SetMobileControl(control, held);

        private void BuildUi()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("VehicleMobileHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _drivingControls = new GameObject("DrivingControls", typeof(RectTransform));
            _drivingControls.transform.SetParent(canvasGo.transform, false);
            Stretch(_drivingControls.GetComponent<RectTransform>());

            CreateHoldButton("SteerLeft", "<", new Vector2(125f, 125f), new Vector2(150f, 145f), VehicleMobileControl.Left, _drivingControls.transform);
            CreateHoldButton("SteerRight", ">", new Vector2(125f, 125f), new Vector2(305f, 145f), VehicleMobileControl.Right, _drivingControls.transform);
            CreateHoldButton("Throttle", "GO", new Vector2(145f, 145f), new Vector2(-155f, 155f), VehicleMobileControl.Forward, _drivingControls.transform, true);
            CreateHoldButton("Reverse", "BRAKE / R", new Vector2(175f, 92f), new Vector2(-155f, 290f), VehicleMobileControl.Reverse, _drivingControls.transform, true);

            _toggleButton = CreateButton("VehicleToggle", "DRIVE", new Vector2(170f, 72f), new Vector2(-170f, 410f), canvasGo.transform, true);
            _toggleLabel = _toggleButton.GetComponentInChildren<Text>();
            _toggleButton.GetComponent<Button>().onClick.AddListener(_interactor.RequestToggleVehicle);
            _drivingControls.SetActive(false);
            _toggleButton.SetActive(false);
        }

        private void CreateHoldButton(string name, string label, Vector2 size, Vector2 position, VehicleMobileControl control, Transform parent, bool right = false)
        {
            var go = CreateButton(name, label, size, position, parent, right);
            var hold = go.AddComponent<VehicleHoldButton>();
            hold.Configure(this, control);
        }

        private static GameObject CreateButton(string name, string label, Vector2 size, Vector2 position, Transform parent, bool right)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(right ? 1f : 0f, 0f);
            rect.pivot = new Vector2(right ? 1f : 0f, 0f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.78f);
            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.16f, 0.45f, 0.4f, 0.9f);
            colors.pressedColor = new Color(0.08f, 0.62f, 0.48f, 0.95f);
            button.colors = colors;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());
            var text = textGo.GetComponent<Text>();
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = label.Length > 3 ? 25 : 42;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(go);
        }
    }

    public sealed class VehicleHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private VehicleMobileHud _hud;
        private VehicleMobileControl _control;

        public void Configure(VehicleMobileHud hud, VehicleMobileControl control)
        {
            _hud = hud;
            _control = control;
        }

        public void OnPointerDown(PointerEventData eventData) => _hud.SetHeld(_control, true);
        public void OnPointerUp(PointerEventData eventData) => _hud.SetHeld(_control, false);
        public void OnPointerExit(PointerEventData eventData) => _hud.SetHeld(_control, false);
    }
}
