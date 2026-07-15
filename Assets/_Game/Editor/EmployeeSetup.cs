using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Overhaul.Core;
using Overhaul.Game;

namespace Overhaul.EditorTools
{
    /// <summary>
    /// Adds the Phase A hire pad + Parts Transporter to the OPEN scene (no rebuild, so the
    /// road layout is untouched).
    ///
    /// The pad is just a <see cref="ConstructionZoneView"/>: standing in it drains the hire
    /// cost, and on completion the worker (its "built visual") pops in and starts hauling.
    /// Reusing that component means hiring is automatically covered by the existing
    /// save/restore path and the accelerating drain, with no new systems.
    /// </summary>
    public static class EmployeeSetup
    {
        private const string K = "Assets/_Game/Art/Models/Kenney";

        [MenuItem("Overhaul/Add Employee Hire Pad")]
        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();

            var bay = Object.FindFirstObjectByType<ServiceBay>();
            var rack = Object.FindFirstObjectByType<ResourceRack>();
            var eco = Object.FindFirstObjectByType<EconomyManager>();
            var village = Object.FindFirstObjectByType<VillageController>();
            if (bay == null || rack == null || eco == null || village == null)
            {
                Debug.LogError("[Overhaul] CityGarage gameplay objects not found; open the scene first.");
                return;
            }

            var sources = Object.FindObjectsByType<PartsSource>(FindObjectsInactive.Include);
            var itemTemplate = GameObject.Find("CarriedItemTemplate");

            // Reuse the existing hire pad if this is re-run.
            ConstructionZoneView hireZone = null;
            foreach (var z in Object.FindObjectsByType<ConstructionZoneView>(FindObjectsInactive.Include))
                if (z.ZoneId == "hire_transporter") hireZone = z;

            // The Employee Room lot is the natural home for hiring (Doc 09 §6.20).
            var room = GameObject.Find("Station_EMPLOYEE_ROOM");
            Vector3 padPos = room != null
                ? room.transform.position + new Vector3(0f, 0.12f, -4.6f)
                : new Vector3(22.5f, 0.12f, -4.6f);

            if (hireZone == null)
            {
                var worker = BuildTransporter(rack.transform.position + new Vector3(2.5f, 0f, -3f),
                                              sources, new[] { rack }, itemTemplate);
                hireZone = BuildHirePad("hire_transporter", EconomyFormulas.HireCost(1), eco, padPos, worker);
            }

            // Let the village know about the new zone so arrivals scale and it saves.
            var unlocks = Object.FindFirstObjectByType<VillageUnlocks>();
            if (unlocks != null)
            {
                var all = Object.FindObjectsByType<ConstructionZoneView>(FindObjectsInactive.Include);
                var queueZone = System.Array.Find(all, z => z.ZoneId == "zone_queue_slot_4");
                unlocks.Configure(village, queueZone, all);
                unlocks.SetHireZone(hireZone, Object.FindFirstObjectByType<SaveManager>(), bay, village);
                EditorUtility.SetDirty(unlocks);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Overhaul] Hire pad ready at {padPos} (cost ${EconomyFormulas.HireCost(1)}). Roads untouched.");
        }

        private static GameObject BuildTransporter(Vector3 pos, PartsSource[] sources,
                                                   ResourceRack[] racks, GameObject itemTemplate)
        {
            var root = new GameObject("Employee_Transporter");
            root.transform.position = pos;

            var visual = PlaceCharacter(root.transform);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.7f;
            agent.baseOffset = 0f;

            var carrier = root.AddComponent<CarrierView>();
            var anchor = new GameObject("StackAnchor").transform;
            anchor.SetParent(root.transform);
            anchor.localPosition = new Vector3(0f, 1.5f, 0f);
            SetPrivate(carrier, "stackAnchor", anchor);

            var emp = root.AddComponent<EmployeeAgent>();
            emp.Configure(EmployeeRole.Transporter, "tire", sources, racks, itemTemplate);

            if (visual != null) visual.transform.SetParent(root.transform, false);
            return root;
        }

        private static GameObject PlaceCharacter(Transform parent)
        {
            // character-k is the transporter in the asset catalog.
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{K}/Characters/character-k.fbx");
            if (asset == null) return null;
            var go = (GameObject)Object.Instantiate(asset);
            go.name = "Visual";
            go.transform.localPosition = Vector3.zero;

            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/Kenney/Characters.mat");
            if (mat != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var arr = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < arr.Length; i++) arr[i] = mat;
                    r.sharedMaterials = arr;
                }
            return go;
        }

        private static ConstructionZoneView BuildHirePad(string id, int cost, EconomyManager eco,
                                                         Vector3 pos, GameObject builtVisual)
        {
            var root = new GameObject($"Hire_{id}");
            root.transform.position = pos;

            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.2f;

            var blueprint = new GameObject("Blueprint");
            blueprint.transform.SetParent(root.transform, false);
            var pad = PlaceProp($"{K}/Platformer/button-square.fbx", pos, blueprint.transform);
            PlaceProp($"{K}/Roads/construction-light.fbx", pos + new Vector3(1.4f, 0f, 0f), blueprint.transform);

            if (builtVisual != null)
            {
                builtVisual.transform.SetParent(root.transform, true);
                builtVisual.SetActive(false);
            }

            var view = root.AddComponent<ConstructionZoneView>();
            view.Configure(id, cost, eco);
            SetPrivate(view, "blueprintVisual", blueprint);
            if (builtVisual != null) SetPrivate(view, "builtVisual", builtVisual);
            return view;
        }

        private static GameObject PlaceProp(string path, Vector3 pos, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) return null;
            var go = (GameObject)Object.Instantiate(asset);
            go.transform.position = pos;
            go.transform.SetParent(parent, true);
            return go;
        }

        private static void SetPrivate(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
