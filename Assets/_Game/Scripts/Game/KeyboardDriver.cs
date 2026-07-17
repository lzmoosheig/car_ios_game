using UnityEngine;
using UnityEngine.InputSystem;

namespace Overhaul.Game
{
    /// <summary>
    /// Desktop convenience: drive the player capsule with WASD so the graybox is
    /// walkable before the on-screen mobile joystick UI exists. Safe no-op if the
    /// new Input System backend is disabled (Keyboard.current is null).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class KeyboardDriver : MonoBehaviour
    {
        private PlayerController _pc;

        private void Awake() => _pc = GetComponent<PlayerController>();

        private void Update()
        {
            var k = Keyboard.current;
            if (k == null) return;

            if (k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame)
                _pc.ToggleSprint();

            Vector2 v = Vector2.zero;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1f;

            _pc.SetMoveInput(v);
        }
    }
}
