using UnityEngine;
using Overhaul.Core;

namespace Overhaul.Game
{
    /// <summary>
    /// The player-driven hand-off at the Basic Change Bay. While the player stands in the
    /// ring it moves the exact part the car in the bay needs out of the player's inventory
    /// (filled from the Parts Delivery worker) and into the bay's input tray, one at a time.
    /// The bay repairs on its own once enough of the right part is delivered.
    ///
    /// Bringing the wrong part (or nothing) is harmless: a throttled toast reminds the player
    /// what the current car actually needs. Replaces the old tire-only Deposit
    /// <see cref="InteractionZone"/> so parts flow through one clear channel.
    /// </summary>
    public sealed class PartDropoffZone : MonoBehaviour
    {
        [SerializeField] private ServiceBay bay;
        [SerializeField] private InventoryComponent tray;
        [SerializeField] private VillageController village;
        [SerializeField] private float tickInterval = 0.2f;

        private InventoryComponent _player;
        private float _timer;
        private float _lastToast = -999f;
        private const float ToastCooldown = 2.5f;

        public void Configure(ServiceBay serviceBay, InventoryComponent inputTray, VillageController owner)
        {
            bay = serviceBay;
            tray = inputTray;
            village = owner;
        }

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            _player = player.GetComponent<InventoryComponent>();
            _timer = 0f;
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && _player != null && player.GetComponent<InventoryComponent>() == _player)
                _player = null;
        }

        private void Update()
        {
            if (_player == null || bay == null || tray == null) return;
            _timer += Time.deltaTime;
            if (_timer < tickInterval) return;
            _timer = 0f;
            Step();
        }

        /// <summary>One transfer tick. Public so it can be driven deterministically in tests.</summary>
        public void Step()
        {
            string needed = bay.InputResourceId;
            // Only a car actually parked in the bay creates a real demand to satisfy.
            if (!bay.VehiclePresent || string.IsNullOrEmpty(needed)) return;

            int remaining = bay.InputCount - tray.CountOf(needed);
            if (remaining <= 0) return;

            int have = _player.CountOf(needed);
            if (have <= 0)
            {
                if (!_player.IsEmpty && Time.time - _lastToast > ToastCooldown)
                {
                    _lastToast = Time.time;
                    ScreenToast.Show($"Bay needs {remaining}x {ResourceCatalog.DisplayName(needed)} — grab it from Parts Delivery.");
                }
                return;
            }

            int move = Mathf.Min(have, remaining);
            int moved = 0;
            for (int i = 0; i < move; i++)
            {
                if (_player.Remove(needed, 1) <= 0) break;
                tray.Add(needed, 1);
                moved++;
            }
            if (moved <= 0) return;

            int stillNeeded = bay.InputCount - tray.CountOf(needed);
            ScreenToast.Show(stillNeeded <= 0
                ? $"Delivered {bay.InputCount}x {ResourceCatalog.DisplayName(needed)} — repairing!"
                : $"{stillNeeded} more {ResourceCatalog.DisplayName(needed)} to go.");
        }
    }
}
