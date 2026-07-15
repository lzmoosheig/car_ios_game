using System;
using System.Collections.Generic;
using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>One labeled action a selected interactable offers ("Accept job", …).</summary>
    public readonly struct InteractableAction
    {
        public readonly string Label;
        public readonly Action Invoke;
        public readonly bool Enabled;

        public InteractableAction(string label, Action invoke, bool enabled = true)
        {
            Label = label;
            Invoke = invoke;
            Enabled = enabled;
        }
    }

    /// <summary>
    /// Anything the player can tap in the world: buildings and NPCs (Doc 09 §3.3
    /// management pads, first slice). Implementors feed the shared
    /// <see cref="InfoPanelView"/>; panels only configure/inform — physical logistics stay
    /// in the world (master-plan rule).
    /// </summary>
    public interface IInteractable
    {
        string Title { get; }
        Transform PivotTransform { get; }
        /// <summary>Fills status lines shown in the panel. Cheap; called ~4x/s while open.</summary>
        void GetInfoLines(List<string> into);
        /// <summary>Fills the panel's action buttons. Cheap; called ~4x/s while open.</summary>
        void GetActions(List<InteractableAction> into);
        void OnSelected();
        void OnDeselected();
    }

    /// <summary>
    /// The shared selection ring: a flattened glowing disc dropped under whatever is
    /// selected. One instance reused for everything - no per-object material mutation,
    /// which keeps mobile batching intact.
    /// </summary>
    public sealed class SelectionRing : MonoBehaviour
    {
        private static SelectionRing _instance;
        private Transform _target;
        private float _baseScale = 3.2f;

        public static void Show(Transform target, float diameter = 3.2f)
        {
            if (_instance == null) _instance = Create();
            _instance._target = target;
            _instance._baseScale = diameter;
            _instance.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            if (_instance != null)
            {
                _instance._target = null;
                _instance.gameObject.SetActive(false);
            }
        }

        private static SelectionRing Create()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "SelectionRing";
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());

            var mat = new Material(Shader.Find("Standard"));
            var gold = new Color(1f, 0.82f, 0.25f, 1f);
            mat.color = gold;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", gold * 0.6f);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;

            return go.AddComponent<SelectionRing>();
        }

        private void LateUpdate()
        {
            if (_target == null) { gameObject.SetActive(false); return; }

            // Sit just above the pad, pulse gently so it reads as "selected", not scenery.
            float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f);
            transform.position = _target.position + Vector3.up * 0.03f;
            transform.localScale = new Vector3(_baseScale * pulse, 0.02f, _baseScale * pulse);
        }
    }
}
