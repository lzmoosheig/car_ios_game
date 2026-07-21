using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>Turns the Basic Change Bay staff character into a tire-only supply inventory.</summary>
    public static class BasicChangeBayStaffInventorySetup
    {
        private const string ScenePath = "Assets/_Game/Scenes/CityGarage.unity";
        private const string StationName = "Station_BASIC_CHANGE_BAY";
        private const string StaffName = "StaffCharacter_BASIC_CHANGE_BAY";

        [MenuItem("Overhaul/Inventory/Setup Basic Change Bay Staff")]
        public static void Setup()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);

            if (!ApplyToOpenScene())
            {
                EditorUtility.DisplayDialog("Basic Change Bay Staff",
                    $"Couldn't wire '{StaffName}'. Make sure the station staff exists in the scene.", "OK");
                return;
            }

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Basic Change Bay Staff",
                "Done. The Basic Change Bay staff now accepts only Tires. Walk up and press E to stock him.", "OK");
        }

        public static bool ApplyToOpenScene()
        {
            EnsureCatalog();

            var staff = FindTransform(StaffName);
            var station = GameObject.Find(StationName);
            var bay = ResolveBay(station);

            if (staff == null || bay == null) return false;

            var itemId = string.IsNullOrEmpty(bay.InputResourceId) ? "tire" : bay.InputResourceId;
            var inv = staff.GetComponent<InventoryComponent>() ?? Undo.AddComponent<InventoryComponent>(staff.gameObject);
            inv.Configure(4, ids: new[] { itemId }, label: "Basic Change Bay Supplies");
            EditorUtility.SetDirty(inv);

            var container = staff.GetComponent<InventoryContainer>() ?? Undo.AddComponent<InventoryContainer>(staff.gameObject);
            container.Configure("Basic Change Bay Staff", 3.5f, "Needed Parts", "Basic Change Bay",
                "Give the worker required parts");
            EditorUtility.SetDirty(container);

            bay.ConfigureInputInventory(inv);
            EditorUtility.SetDirty(bay);

            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                var playerInv = player.GetComponent<InventoryComponent>() ?? Undo.AddComponent<InventoryComponent>(player.gameObject);
                if (playerInv.SlotCount < 9) playerInv.Configure(9, label: "Player");
                if (player.GetComponent<PlayerContainerInteractor>() == null)
                    Undo.AddComponent<PlayerContainerInteractor>(player.gameObject);
                EditorUtility.SetDirty(playerInv);
            }

            Selection.activeObject = staff.gameObject;
            return true;
        }

        private static Transform FindTransform(string objectName)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == objectName);
        }

        private static ServiceBay ResolveBay(GameObject station)
        {
            if (station != null)
            {
                var view = station.GetComponent<BuildingView>();
                if (view != null)
                {
                    var serialized = new SerializedObject(view);
                    var bayProperty = serialized.FindProperty("bay");
                    if (bayProperty != null && bayProperty.objectReferenceValue is ServiceBay referencedBay)
                        return referencedBay;
                }
            }

            return Object.FindFirstObjectByType<ServiceBay>();
        }

        private static void EnsureCatalog()
        {
            var catalog = ResourceCatalog.Instance;
            if (catalog == null) catalog = Undo.AddComponent<ResourceCatalog>(new GameObject("ResourceCatalog"));
            catalog.SeedDefaults();
            EditorUtility.SetDirty(catalog);
        }
    }
}
