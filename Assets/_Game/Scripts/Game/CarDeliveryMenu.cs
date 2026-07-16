using UnityEngine;
using UnityEngine.UI;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// Runtime controller for the Car Delivery overlay. The editor tool
    /// (CarDeliverySetup) builds the uGUI hierarchy at edit time; this only wires data
    /// onto widgets and owns open/close/responsive-layout state - the same split
    /// HudView uses for the always-on HUD.
    /// </summary>
    public sealed class CarDeliveryMenu : MonoBehaviour
    {
        /// <summary>Plain reference bundle so Configure doesn't balloon into a dozen params.</summary>
        public sealed class Refs
        {
            public GameObject Root;
            public GameObject Blocker;
            public Button CloseButton;
            public Button BuySlotButton;
            public Text BuySlotCostText;
            public Button StartDeliveryButton;
            public CurrencyDisplay CashDisplay;
            public CurrencyDisplay GoldDisplay;
            public DeliveryPartOverviewSlot[] OverviewSlots;
            public DeliverySlotView[] SlotViews;
            public DeliveryItemRow[] ItemRows;
            public Transform WideRow;
            /// <summary>Outer narrow-mode container whose active state is toggled.</summary>
            public Transform NarrowColumn;
            /// <summary>Where sections get reparented in narrow mode - the scroll view's
            /// Content transform, one level inside NarrowColumn, so overflow scrolls instead
            /// of squeezing every cell down to unreadable heights.</summary>
            public Transform NarrowContent;
            public Transform SlotsSection;
            public Transform BuySection;
            public float NarrowBreakpoint = 900f;
        }

        private const float RefreshInterval = 0.2f;
        private const float LayoutCheckInterval = 0.5f;
        private const int SkipGoldCost = 5;

        // [SerializeField] (despite being set only from editor-time Configure calls, never
        // the inspector): Unity does not persist plain private fields across the domain
        // reload that happens on entering Play Mode / reopening the project, so without
        // this attribute every reference here comes back null and the menu is inert.
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _blockerButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _buySlotButton;
        [SerializeField] private Text _buySlotCostText;
        [SerializeField] private Button _startDeliveryButton;
        [SerializeField] private CurrencyDisplay _cashDisplay;
        [SerializeField] private CurrencyDisplay _goldDisplay;
        [SerializeField] private DeliveryPartOverviewSlot[] _overviewSlots;
        [SerializeField] private DeliverySlotView[] _slotViews;
        [SerializeField] private DeliveryItemRow[] _itemRows;
        [SerializeField] private Transform _wideRow;
        [SerializeField] private Transform _narrowColumn;
        [SerializeField] private Transform _narrowContent;
        [SerializeField] private Transform _slotsSection;
        [SerializeField] private Transform _buySection;
        [SerializeField] private float _narrowBreakpoint = 900f;
        private bool? _isNarrow;

        [SerializeField] private CarDeliverySystem _system;
        [SerializeField] private EconomyManager _economy;
        private float _refreshTimer;
        private float _layoutTimer;

        private void Awake()
        {
            BindInteractions();
        }

        public bool IsOpen => _root != null && _root.activeSelf;

        public void Configure(Refs refs, CarDeliverySystem system, EconomyManager economy)
        {
            _root = refs.Root;
            _blockerButton = refs.Blocker != null ? refs.Blocker.GetComponent<Button>() : null;
            _closeButton = refs.CloseButton;
            _buySlotButton = refs.BuySlotButton;
            _buySlotCostText = refs.BuySlotCostText;
            _startDeliveryButton = refs.StartDeliveryButton;
            _cashDisplay = refs.CashDisplay;
            _goldDisplay = refs.GoldDisplay;
            _overviewSlots = refs.OverviewSlots;
            _slotViews = refs.SlotViews;
            _itemRows = refs.ItemRows;
            _wideRow = refs.WideRow;
            _narrowColumn = refs.NarrowColumn;
            _narrowContent = refs.NarrowContent;
            _slotsSection = refs.SlotsSection;
            _buySection = refs.BuySection;
            _narrowBreakpoint = refs.NarrowBreakpoint;
            _system = system;
            _economy = economy;

            BindInteractions();

            if (_root != null) _root.SetActive(false);
            ApplyResponsiveLayout();
        }

        private void BindInteractions()
        {
            BindButton(_closeButton, Close);
            BindButton(_blockerButton, Close);
            BindButton(_buySlotButton, BuyNextSlot);
            BindButton(_startDeliveryButton, StartAllIdle);

            if (_slotViews != null)
                for (int i = 0; i < _slotViews.Length; i++)
                {
                    if (_slotViews[i] == null) continue;
                    _slotViews[i].Clicked -= OnSlotActionClicked;
                    _slotViews[i].Clicked += OnSlotActionClicked;
                }

            if (_itemRows != null)
                for (int i = 0; i < _itemRows.Length; i++)
                {
                    if (_itemRows[i] == null) continue;
                    _itemRows[i].Clicked -= OnBuyClicked;
                    _itemRows[i].Clicked += OnBuyClicked;
                }
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void BuyNextSlot() => _system?.TryUnlockNextSlot();
        private void StartAllIdle() => _system?.StartAllIdle();

        public void Open()
        {
            if (_root == null) return;
            _root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Update()
        {
            if (!IsOpen) return;

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer >= RefreshInterval)
            {
                _refreshTimer = 0f;
                Refresh();
            }

            _layoutTimer += Time.unscaledDeltaTime;
            if (_layoutTimer >= LayoutCheckInterval)
            {
                _layoutTimer = 0f;
                ApplyResponsiveLayout();
            }
        }

        /// <summary>Toggles between a side-by-side and a stacked content layout so the menu
        /// stays usable on narrow phone widths. Reparents the two sections between a
        /// HorizontalLayoutGroup row and a VerticalLayoutGroup column (Unity only allows one
        /// layout group per GameObject) rather than rebuilding anything, and only runs the
        /// swap when the narrow/wide state actually flips.</summary>
        private void ApplyResponsiveLayout()
        {
            if (_wideRow == null || _narrowColumn == null || _slotsSection == null || _buySection == null) return;

            float width = Screen.safeArea.width > 0f ? Screen.safeArea.width : Screen.width;
            bool narrow = width < _narrowBreakpoint;
            if (_isNarrow.HasValue && _isNarrow.Value == narrow) return;
            _isNarrow = narrow;

            // Narrow mode reparents into the scroll view's Content (one level inside
            // _narrowColumn) so a device too short to fit both sections at full height
            // scrolls instead of squeezing every row down to unreadable heights.
            var target = narrow ? (_narrowContent != null ? _narrowContent : _narrowColumn) : _wideRow;
            _slotsSection.SetParent(target, false);
            _buySection.SetParent(target, false);
            _slotsSection.SetSiblingIndex(0);
            _buySection.SetSiblingIndex(1);
            _wideRow.gameObject.SetActive(!narrow);
            _narrowColumn.gameObject.SetActive(narrow);
        }

        private void OnSlotActionClicked(DeliverySlotView clickedView)
        {
            int index = System.Array.IndexOf(_slotViews, clickedView);
            if (_system == null || index < 0 || index >= _system.Slots.Count) return;
            var slot = _system.Slots[index];
            if (slot == null || !slot.Unlocked) return;

            if (slot.IsComplete) _system.CollectSlot(index);
            else if (slot.Running) _system.TrySkip(index, SkipGoldCost);
            else _system.TryStart(index);
        }

        private void OnBuyClicked(DeliveryItemRow clickedRow)
        {
            int index = System.Array.IndexOf(_itemRows, clickedRow);
            if (index < 0 || index >= CarDeliveryCatalog.Items.Length) return;
            _system?.TryBuy(CarDeliveryCatalog.Items[index].Id);
        }

        private void Refresh()
        {
            if (_system == null) return;

            if (_cashDisplay != null) _cashDisplay.SetValue(_economy != null ? _economy.Wallet : 0);
            if (_goldDisplay != null) _goldDisplay.SetValue(_economy != null ? _economy.Gold : 0);

            var slots = _system.Slots;
            if (_slotViews != null)
                for (int i = 0; i < _slotViews.Length && i < slots.Count; i++)
                    RefreshSlot(_slotViews[i], slots[i]);

            var items = CarDeliveryCatalog.Items;
            RefreshOverview();

            if (_itemRows != null)
                for (int i = 0; i < _itemRows.Length && i < items.Length; i++)
                    RefreshRow(_itemRows[i], items[i]);

            if (_buySlotCostText != null) _buySlotCostText.text = $"${_system.NextSlotUnlockCost():N0}";
            bool hasLockedSlot = false;
            foreach (var s in slots) if (!s.Unlocked) { hasLockedSlot = true; break; }
            if (_buySlotButton != null) _buySlotButton.gameObject.SetActive(hasLockedSlot);
        }

        private void RefreshOverview()
        {
            if (_overviewSlots == null) return;

            string[] ids = { "tire", "battery", "oil", "paint" };
            for (int i = 0; i < _overviewSlots.Length && i < ids.Length; i++)
            {
                var view = _overviewSlots[i];
                if (view == null) continue;
                string id = ids[i];
                view.Set(IconFor(id), DeliveryDisplayName(id), _system.OwnedCountOf(id));
            }
        }

        private void RefreshSlot(DeliverySlotView view, DeliverySlotState slot)
        {
            if (view == null || slot == null) return;

            if (!slot.Unlocked)
            {
                view.ShowLocked($"Requires\nLevel {slot.UnlockRequirementLevel}");
                return;
            }

            string name = ResourceCatalog.DisplayName(slot.ItemId);
            string timer;
            string actionBadge = null;
            if (slot.IsComplete)
            {
                timer = "Ready!";
            }
            else if (slot.Running)
            {
                timer = FormatTime(Mathf.Max(0f, slot.DurationSeconds - slot.ElapsedSeconds));
                actionBadge = SkipGoldCost.ToString();
            }
            else
            {
                timer = "Idle";
            }

            view.ShowActive(IconFor(slot.ItemId), name, slot.Quantity, slot.Progress01, timer, true, actionBadge);
        }

        private void RefreshRow(DeliveryItemRow row, DeliveryItemDef def)
        {
            if (row == null) return;
            bool affordable = def.Currency == DeliveryCurrency.Gold
                ? _economy == null || _economy.Gold >= def.Price
                : _economy == null || _economy.Wallet >= def.Price;
            string priceLabel = def.Currency == DeliveryCurrency.Gold ? def.Price.ToString() : $"${def.Price:N0}";
            row.Set(IconFor(def.Id), def.DisplayName, def.Description, $"x{def.PurchaseQuantity}", priceLabel, affordable);
        }

        private static Sprite IconFor(string itemId)
            => CarDeliveryIcons.Instance != null ? CarDeliveryIcons.Instance.Get(itemId) : null;

        private static string DeliveryDisplayName(string itemId)
            => CarDeliveryCatalog.TryFind(itemId, out var def) ? def.DisplayName : ResourceCatalog.DisplayName(itemId);

        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            int m = total / 60;
            int s = total % 60;
            return $"{m:00}m {s:00}s";
        }
    }
}
