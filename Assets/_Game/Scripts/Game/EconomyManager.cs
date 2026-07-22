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

        /// <summary>
        /// Gold: the scarce permanent currency (Doc 09 §4.1's Golden Wrenches, persisted in
        /// SaveData.GoldenWrenches). Deliberately hard to obtain — it is earned from major
        /// milestones and prestige, never from routine service income, and it must never be
        /// spendable on ranked car performance.
        /// </summary>
        public int Gold { get; private set; }
        public event Action<int> GoldChanged;

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

        /// <summary>Awarded by milestones/prestige only — not by ordinary service revenue.</summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            GoldChanged?.Invoke(Gold);
            return true;
        }

        /// <summary>Restores gold from a save file.</summary>
        public void SetGold(int amount)
        {
            Gold = Math.Max(0, amount);
            GoldChanged?.Invoke(Gold);
        }

        /// <summary>
        /// Reputation: non-spend progression XP (Doc 09 §4.1). Earned from completed jobs;
        /// gates unlocks later. Never spent, never lost.
        /// </summary>
        public int Reputation { get; private set; }
        public event Action<int> ReputationChanged;

        /// <summary>Player level, derived from reputation (Doc 09 §4.1). Gates warehouse expansion.</summary>
        public int Level => Overhaul.Core.EconomyFormulas.LevelForReputation(Reputation);

        /// <summary>Raised when reputation crosses into a new level.</summary>
        public event Action<int> LevelChanged;

        public void AddReputation(int amount)
        {
            if (amount <= 0) return;
            int before = Level;
            Reputation += amount;
            ReputationChanged?.Invoke(Reputation);
            if (Level != before) LevelChanged?.Invoke(Level);
        }

        /// <summary>Restores reputation from a save file.</summary>
        public void SetReputation(int amount)
        {
            int before = Level;
            Reputation = Math.Max(0, amount);
            ReputationChanged?.Invoke(Reputation);
            if (Level != before) LevelChanged?.Invoke(Level);
        }
    }
}
