using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Adds the play-mode inventory demo/inspector to the OPEN scene (see
    /// <see cref="InventoryDemo"/>). Enter Play mode afterwards to watch the worker haul parts
    /// between buildings and to click-move items between the player, buildings and worker grids.
    /// </summary>
    public static class InventoryDemoSetup
    {
        [MenuItem("Overhaul/Inventory/Add Demo To Scene")]
        public static void AddDemo()
        {
            var existing = Object.FindFirstObjectByType<InventoryDemo>();
            if (existing != null)
            {
                Selection.activeObject = existing.gameObject;
                EditorUtility.DisplayDialog("Inventory Demo",
                    "An InventoryDemo already exists in the scene. Press Play to run it.", "OK");
                return;
            }

            var go = new GameObject("InventoryDemo");
            go.AddComponent<InventoryDemo>();
            Undo.RegisterCreatedObjectUndo(go, "Add Inventory Demo");
            Selection.activeObject = go;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Inventory Demo",
                "Added 'InventoryDemo' to the scene. Press Play to build the grids and run the parts worker.",
                "OK");
        }
    }
}
