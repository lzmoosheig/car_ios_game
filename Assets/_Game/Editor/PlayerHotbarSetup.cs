using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Wires the 9-slot first-person hotbar into the OPEN scene: gives the Player an
    /// <see cref="InventoryComponent"/> (mirrored by <see cref="CarrierView"/> as items are
    /// picked up) and adds a <see cref="HotbarView"/> bound to it. Idempotent.
    /// </summary>
    public static class PlayerHotbarSetup
    {
        private const int HotbarSlots = 9;

        [MenuItem("Overhaul/Inventory/Add Player Hotbar")]
        public static void AddHotbar()
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                EditorUtility.DisplayDialog("Player Hotbar",
                    "No PlayerController found in the open scene. Open the gameplay scene first.", "OK");
                return;
            }

            EnsureCatalog();

            // 1) Player inventory (the hotbar's backing store).
            var inv = player.GetComponent<InventoryComponent>();
            if (inv == null) inv = Undo.AddComponent<InventoryComponent>(player.gameObject);
            inv.Configure(HotbarSlots, label: "Player");
            EditorUtility.SetDirty(inv);

            // 2) Hotbar UI object bound to that inventory + the player's view controller.
            var existing = Object.FindFirstObjectByType<HotbarView>();
            var hotbarGo = existing != null ? existing.gameObject : new GameObject("PlayerHotbar", typeof(RectTransform));
            if (existing == null) Undo.RegisterCreatedObjectUndo(hotbarGo, "Add Player Hotbar");
            var hotbar = existing != null ? existing : hotbarGo.AddComponent<HotbarView>();

            var so = new SerializedObject(hotbar);
            so.FindProperty("target").objectReferenceValue = inv;
            so.FindProperty("viewController").objectReferenceValue = player.GetComponent<PlayerViewController>();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hotbar);

            Selection.activeObject = hotbarGo;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Player Hotbar",
                "Added a 9-slot hotbar bound to the Player inventory. Press Play, press V for first person, " +
                "and pick up parts to see them stack. Select slots with 1-9 or the mouse wheel.", "OK");
        }

        private static void EnsureCatalog()
        {
            var catalog = ResourceCatalog.Instance;
            if (catalog == null)
                catalog = Undo.AddComponent<ResourceCatalog>(new GameObject("ResourceCatalog"));
            catalog.SeedDefaults();
            EditorUtility.SetDirty(catalog);
        }
    }
}
