using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Core;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Turns the Parts Delivery staff character into an openable, self-restocking parts
    /// container and makes sure the player can interact with it. Idempotent - safe to re-run.
    /// After running: Play, press V for first person, walk up to him and press E.
    /// </summary>
    public static class PartsDeliveryNpcSetup
    {
        private const string NpcName = "StaffCharacter_PARTS_DELIVERY";

        [MenuItem("Overhaul/Inventory/Setup Parts Delivery NPC")]
        public static void Setup()
        {
            var npc = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == NpcName);
            if (npc == null)
            {
                EditorUtility.DisplayDialog("Parts Delivery NPC",
                    $"Couldn't find '{NpcName}' in the open scene.", "OK");
                return;
            }

            EnsureCatalog();

            // NPC inventory: parts only, roomy, self-restocking.
            var inv = npc.GetComponent<InventoryComponent>() ?? Undo.AddComponent<InventoryComponent>(npc.gameObject);
            inv.Configure(12, categories: new[] { ItemCategory.Part }, label: "Parts Delivery");
            EditorUtility.SetDirty(inv);

            if (npc.GetComponent<RandomPartsRestocker>() == null)
                Undo.AddComponent<RandomPartsRestocker>(npc.gameObject);

            var container = npc.GetComponent<InventoryContainer>() ?? Undo.AddComponent<InventoryContainer>(npc.gameObject);
            container.Configure("Parts Delivery", 3.5f);
            EditorUtility.SetDirty(container);

            // Player side: needs an inventory (shared with the hotbar) + the interactor.
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                var pinv = player.GetComponent<InventoryComponent>() ?? Undo.AddComponent<InventoryComponent>(player.gameObject);
                if (pinv.SlotCount < 9) pinv.Configure(9, label: "Player");
                if (player.GetComponent<PlayerContainerInteractor>() == null)
                    Undo.AddComponent<PlayerContainerInteractor>(player.gameObject);
                EditorUtility.SetDirty(pinv);
            }

            Selection.activeObject = npc.gameObject;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Parts Delivery NPC",
                "Done. The parts delivery worker now carries a random, growing parts stock.\n\n" +
                "Play → press V (first person) → walk up to him → press E to open his inventory, " +
                "then click items to take them.", "OK");
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
