using System;
using UnityEngine;

namespace Overhaul.Game
{
    /// <summary>
    /// Scene-side wallet. The math (revenue, costs, offline) lives in the verified
    /// <see cref="Overhaul.Core.EconomyFormulas"/>; this only holds the balance and
    /// raises change notifications for the HUD (Doc 04 §6).
    /// </summary>
    public sealed class EconomyManager : MonoBehaviour
    {
        public long Wallet { get; private set; }
        public event Action<long> WalletChanged;

        public void Add(int amount)
        {
            if (amount == 0) return;
            Wallet += amount;
            WalletChanged?.Invoke(Wallet);
        }

        public bool TrySpend(int amount)
        {
            if (Wallet < amount) return false;
            Wallet -= amount;
            WalletChanged?.Invoke(Wallet);
            return true;
        }

        /// <summary>Restores the balance from a save file (Doc 06 §3).</summary>
        public void SetWallet(long amount)
        {
            Wallet = Math.Max(0, amount);
            WalletChanged?.Invoke(Wallet);
        }
    }
}
