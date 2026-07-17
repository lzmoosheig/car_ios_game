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
        private Text _speedText;
        private Text _gearText;
        private GameObject _lapPanel;
        private Text _lapText;
        private Text _lapStatusText;
        private WorkshopTestDriveLoop _testLoop;
        private Canvas _managementCanvas;
        private float _readoutTimer;

        private void Awake()
        {
            _interactor = GetComponent<VehicleInteractor>();
            _testLoop = FindAnyObjectByType<WorkshopTestDriveLoop>();
            var managementHud = GameObject.Find("HUD");
            if (managementHud != null) _managementCanvas = managementHud.GetComponent<Canvas>();
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

            if (_lapPanel != null) _lapPanel.SetActive(driving && _testLoop != null);
            _readoutTimer -= Time.unscaledDeltaTime;
            if (_readoutTimer <= 0f)
            {
                _readoutTimer = 0.1f;
                var vehicle = _interactor.CurrentVehicle;
                if (_speedText != null) _speedText.text = vehicle != null ? Mathf.RoundToInt(vehicle.SpeedKph).ToString("000") : "000";
                if (_gearText != null) _gearText.text = vehicle != null ? vehicle.GearDirection : "N";
                if (driving && _testLoop != null) RefreshLapReadout();
            }

            // Timed test laps get an uncluttered driving view. The management HUD is
            // restored immediately when the lap ends, resets, or the player exits.
            if (_managementCanvas != null)
                _managementCanvas.enabled = !(driving && _testLoop != null && _testLoop.Active);
        }

        public void SetHeld(VehicleMobileControl control, bool held) => _interactor.SetMobileControl(control, held);

        private void OnDisable()
        {
            if (_managementCanvas != null) _managementCanvas.enabled = true;
        }

        private void BuildUi()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("VehicleMobileHUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvas.pixelPerfect = true;
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

            BuildSpeedReadout(_drivingControls.transform);
            BuildLapReadout(_drivingControls.transform);

            var reset = CreateButton("ResetVehicle", "RESET", new Vector2(140f, 62f), new Vector2(-350f, 410f), _drivingControls.transform, true);
            reset.GetComponent<Button>().onClick.AddListener(_interactor.RequestResetVehicle);

            _toggleButton = CreateButton("VehicleToggle", "DRIVE", new Vector2(170f, 72f), new Vector2(-170f, 410f), canvasGo.transform, true);
            _toggleLabel = _toggleButton.GetComponentInChildren<Text>();
            _toggleButton.GetComponent<Button>().onClick.AddListener(_interactor.RequestToggleVehicle);
            _drivingControls.SetActive(false);
            _toggleButton.SetActive(false);
        }

        private void BuildSpeedReadout(Transform parent)
        {
            var panel = new GameObject("SpeedReadout", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(250f, 112f);
            rect.anchoredPosition = new Vector2(0f, 42f);
            panel.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.055f, 0.88f);

            _speedText = CreateText(panel.transform, "Speed", "000", 52, new Vector2(18f, 38f), new Vector2(160f, 64f), TextAnchor.MiddleRight);
            var unit = CreateText(panel.transform, "Unit", "KM/H", 15, new Vector2(108f, 12f), new Vector2(70f, 25f), TextAnchor.MiddleRight);
            unit.color = new Color(0.65f, 0.72f, 0.78f);
            _gearText = CreateText(panel.transform, "Gear", "N", 42, new Vector2(182f, 28f), new Vector2(52f, 68f), TextAnchor.MiddleCenter);
            _gearText.color = new Color(0.35f, 0.92f, 0.68f);
        }

        private void BuildLapReadout(Transform parent)
        {
            _lapPanel = new GameObject("WorkshopLapReadout", typeof(RectTransform), typeof(Image));
            _lapPanel.transform.SetParent(parent, false);
            var rect = _lapPanel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(410f, 96f);
            rect.anchoredPosition = new Vector2(0f, -34f);
            _lapPanel.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.055f, 0.88f);
            _lapStatusText = CreateText(_lapPanel.transform, "Status", "CROSS START TO TEST", 17, new Vector2(16f, 58f), new Vector2(378f, 26f), TextAnchor.MiddleLeft);
            _lapStatusText.color = new Color(0.38f, 0.82f, 1f);
            _lapText = CreateText(_lapPanel.transform, "Times", "BALANCED  00:00.000  BEST --:--.---", 19, new Vector2(16f, 17f), new Vector2(378f, 38f), TextAnchor.MiddleLeft);
        }

        private void RefreshLapReadout()
        {
            _lapStatusText.text = _testLoop.Status;
            string current = FormatTime(_testLoop.LapTime);
            string best = _testLoop.BestLap > 0f ? FormatTime(_testLoop.BestLap) : "--:--.---";
            _lapText.text = $"{_testLoop.SetupName.ToUpperInvariant()}  {current}  BEST {best}";
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            return $"{minutes:00}:{seconds - minutes * 60f:00.000}";
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Vector2 position, Vector2 dimensions, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
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
