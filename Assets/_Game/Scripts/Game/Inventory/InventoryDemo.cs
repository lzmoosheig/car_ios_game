using UnityEngine;
using UnityEngine.UI;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// A self-contained, play-mode demonstration and debug inspector for the slot-based
    /// inventory system. Drop this on an empty GameObject (or use the Overhaul &gt; Inventory
    /// menu) and press Play: it wires up a catalog, a source building that outputs parts, a
    /// nearly-full destination building, the player, and a Parts-Delivery worker whose own
    /// inventory only accepts <see cref="ItemCategory.Part"/> items. It then builds a canvas
    /// with a live grid for each inventory so you can watch - and interact with - items being
    /// produced, carried by the worker, and deposited (with partial transfers) between buildings.
    ///
    /// Everything here is scaffolding for testing/inspection; the reusable system lives in
    /// <see cref="SlotInventory"/>, <see cref="InventoryComponent"/>, <see cref="InventoryTransfer"/>
    /// and <see cref="InventoryCourier"/>.
    /// </summary>
    public sealed class InventoryDemo : MonoBehaviour
    {
        [SerializeField] private bool autoRunWorker = true;

        private InventoryComponent _player;
        private InventoryComponent _sourceBuilding;
        private InventoryComponent _destBuilding;
        private InventoryComponent _workerInv;
        private InventoryCourier _courier;

        private void Start()
        {
            // Keep the worker hauling even when the editor Game view isn't focused, so the
            // demo animates in the background (and in headless/automation sessions).
            Application.runInBackground = true;
            EnsureCatalog();
            BuildEntities();
            BuildUI();
        }

        private static void EnsureCatalog()
        {
            var catalog = ResourceCatalog.Instance;
            if (catalog == null)
            {
                catalog = new GameObject("ResourceCatalog").AddComponent<ResourceCatalog>();
            }
            catalog.SeedDefaults();
        }

        private void BuildEntities()
        {
            _player = MakeInventory("Player", 12);

            _sourceBuilding = MakeInventory("Source Building (Parts Factory)", 12);
            _sourceBuilding.Add("tire", 24);
            _sourceBuilding.Add("engine", 6);
            _sourceBuilding.Add("oil", 10); // a consumable the parts worker should ignore

            _destBuilding = MakeInventory("Destination Building (Assembly)", 4);
            _destBuilding.Add("tire", 16); // starts nearly full -> shows partial transfer

            var workerGo = new GameObject("Parts Delivery Worker");
            workerGo.transform.SetParent(transform, false);
            _workerInv = workerGo.AddComponent<InventoryComponent>();
            _workerInv.Configure(3, categories: new[] { ItemCategory.Part }, label: "Parts Worker (carries Part items only)");
            _courier = workerGo.AddComponent<InventoryCourier>();
            _courier.Configure(_workerInv, _sourceBuilding, _destBuilding);
        }

        private InventoryComponent MakeInventory(string label, int slots)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var inv = go.AddComponent<InventoryComponent>();
            inv.Configure(slots, label: label);
            return inv;
        }

        private void Update()
        {
            if (autoRunWorker && _courier != null) _courier.Tick(Time.deltaTime);
        }

        // ---------------------------------------------------------------------- UI

        private void BuildUI()
        {
            var canvasGo = new GameObject("InventoryDemoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            EnsureEventSystem();

            // Root vertical layout.
            var root = new GameObject("Root", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasGo.transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(24, 24);
            root.offsetMax = new Vector2(-24, -24);
            var vlg = root.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            AddHeader(root, "Inventory System Demo  -  left-click to pick up/move, right-click to split. Worker auto-hauls Parts.");

            var buttonRow = AddRow(root, 44);
            AddButton(buttonRow, "Restock Source", () => { _sourceBuilding.Add("tire", 12); _sourceBuilding.Add("engine", 4); });
            AddButton(buttonRow, "Give Player Oil", () => _player.Add("oil", 20));
            AddButton(buttonRow, "Worker Collect", () => _courier.CollectStep());
            AddButton(buttonRow, "Worker Deposit", () => _courier.DepositStep());
            AddButton(buttonRow, autoRunWorker ? "Auto: ON" : "Auto: OFF", null, out var autoBtn);
            var autoLabel = autoBtn.GetComponentInChildren<Text>();
            autoBtn.onClick.AddListener(() => { autoRunWorker = !autoRunWorker; autoLabel.text = autoRunWorker ? "Auto: ON" : "Auto: OFF"; });

            var panelsRow = AddRow(root, 460);
            var hlg = panelsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;

            AddPanel(panelsRow, "PLAYER", _player, 4);
            AddPanel(panelsRow, "SOURCE BUILDING", _sourceBuilding, 4);
            AddPanel(panelsRow, "PARTS WORKER", _workerInv, 3);
            AddPanel(panelsRow, "DEST BUILDING", _destBuilding, 4);
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        private static void AddHeader(RectTransform parent, string text)
        {
            var go = new GameObject("Header", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24;
            t.color = Color.white;
            t.text = text;
            go.AddComponent<LayoutElement>().minHeight = 34;
        }

        private static RectTransform AddRow(RectTransform parent, float height)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = height;
            return (RectTransform)go.transform;
        }

        private void AddPanel(RectTransform parent, string title, InventoryComponent inv, int columns)
        {
            var panelGo = new GameObject($"Panel_{title}", typeof(RectTransform));
            panelGo.transform.SetParent(parent, false);
            var panelBg = panelGo.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.35f);
            var pvlg = panelGo.AddComponent<VerticalLayoutGroup>();
            pvlg.padding = new RectOffset(10, 10, 10, 10);
            pvlg.spacing = 8;
            pvlg.childControlWidth = true;
            pvlg.childForceExpandHeight = false;

            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 20;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.text = title;
            titleGo.AddComponent<LayoutElement>().minHeight = 26;

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(panelGo.transform, false);
            var grid = gridGo.AddComponent<InventoryGridView>();
            var le = gridGo.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            grid.SetLayout(columns, 76f, 6f);
            grid.Bind(inv, title);
        }

        private static void AddButton(RectTransform parent, string label, System.Action onClick)
            => AddButton(parent, label, onClick, out _);

        private static void AddButton(RectTransform parent, string label, System.Action onClick, out Button button)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.20f, 0.45f, 0.85f, 0.9f);
            button = go.AddComponent<Button>();
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 150;

            // Lay the buttons out left-to-right inside the row.
            if (parent.GetComponent<HorizontalLayoutGroup>() == null)
            {
                var hlg = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10;
                hlg.childForceExpandWidth = false;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
            }

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(go.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 18;
            txt.text = label;
            var rt = (RectTransform)txtGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
