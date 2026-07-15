using System.IO;
using NUnit.Framework;
using UnityEngine;
using Overhaul.Game;

namespace Overhaul.Tests
{
    /// <summary>
    /// Regression cover for the save-clobber bug: a session once restored the wallet but
    /// silently failed to restore construction zones, then autosaved the empty state over
    /// the player's real progress. Saving must never overwrite a file we could not read.
    /// </summary>
    public sealed class SaveManagerTests
    {
        private GameObject _root;
        private SaveManager _save;
        private EconomyManager _eco;
        private ConstructionZoneView _zone;

        private static string TestFile => "test_" + System.Guid.NewGuid().ToString("N") + ".json";

        private void Build(string fileName)
        {
            _root = new GameObject("saveRoot");
            _eco = _root.AddComponent<EconomyManager>();
            _save = _root.AddComponent<SaveManager>();

            var zoneGo = new GameObject("zone");
            _zone = zoneGo.AddComponent<ConstructionZoneView>();
            _zone.Configure("zone_queue_slot_4", 80, _eco);

            SetPrivate(_save, "economy", _eco);
            SetPrivate(_save, "fileName", fileName);
        }

        private static void SetPrivate(Object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            f.SetValue(target, value);
        }

        [TearDown]
        public void Cleanup()
        {
            if (_save != null && File.Exists(_save.SavePath)) File.Delete(_save.SavePath);
            if (_zone != null) Object.DestroyImmediate(_zone.gameObject);
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void Save_ThenLoad_RestoresWalletAndZoneTogether()
        {
            Build(TestFile);
            _save.Load();               // no file yet -> fresh game, saving allowed
            Assert.IsTrue(_save.CanSave, "a missing file is a legitimate new game");

            _eco.SetWallet(777);
            for (int i = 0; i < 200 && !_zone.Built; i++) _zone.Tick(0.1f);
            Assert.IsTrue(_zone.Built, "zone funded for the test");
            long walletAfterBuild = _eco.Wallet;

            _save.Save();
            Assert.IsTrue(File.Exists(_save.SavePath), "save file written");

            // Clobber live state, then restore.
            _eco.SetWallet(0);
            _zone.LoadState(0, false);
            Assert.IsFalse(_zone.Built, "zone reset before load");

            _save.Load();

            Assert.AreEqual(walletAfterBuild, _eco.Wallet, "wallet restored");
            // The original bug: this passed for the wallet but the zone came back unbuilt.
            Assert.IsTrue(_zone.Built, "zone restored alongside the wallet");
            Assert.AreEqual(80, _zone.Funded, "zone funding restored");
        }

        [Test]
        public void Gold_PersistsAcrossSaveAndLoad()
        {
            Build(TestFile);
            _save.Load();

            _eco.SetWallet(999);
            _eco.AddGold(7);
            Assert.AreEqual(7, _eco.Gold, "gold awarded");

            _save.Save();
            _eco.SetGold(0);
            _eco.SetWallet(0);

            _save.Load();

            // Gold is the scarce permanent currency: losing it to a reload would be far
            // worse than losing cash, since it is only earned from milestones.
            Assert.AreEqual(7, _eco.Gold, "gold restored from SaveData.GoldenWrenches");
            Assert.AreEqual(999, _eco.Wallet, "cash restored alongside it");
        }

        [Test]
        public void Gold_CannotBeSpentBelowZero()
        {
            Build(TestFile);
            _eco.SetGold(2);

            Assert.IsFalse(_eco.TrySpendGold(5), "cannot overspend gold");
            Assert.AreEqual(2, _eco.Gold, "balance untouched by a failed spend");
            Assert.IsTrue(_eco.TrySpendGold(2), "exact spend allowed");
            Assert.AreEqual(0, _eco.Gold, "gold spent");
        }

        [Test]
        public void CorruptSaveFile_DisablesSaving_SoGoodProgressIsNotOverwritten()
        {
            Build(TestFile);
            _save.Load();
            _eco.SetWallet(500);
            _save.Save();

            string good = File.ReadAllText(_save.SavePath);
            Assert.IsTrue(good.Contains("500"), "baseline save contains the wallet");

            // Simulate an unreadable file (partial write, disk corruption, bad edit).
            File.WriteAllText(_save.SavePath, "{ this is not valid json");

            _save.Load();
            Assert.IsFalse(_save.CanSave, "saving disabled after a failed load");

            // Autosave must NOT overwrite the file we could not read.
            _eco.SetWallet(1);
            _save.Save();

            string after = File.ReadAllText(_save.SavePath);
            Assert.AreEqual("{ this is not valid json", after,
                "a failed load must leave the file untouched for recovery");
        }

        [Test]
        public void FutureVersionSave_IsNotOverwritten()
        {
            Build(TestFile);
            _save.Load();
            _save.Save(); // establish the file

            // A save written by a newer build of the game.
            const string futureJson = "{\"Version\":99,\"Wallet\":12345,\"CurrentLevelId\":\"L01_CityGarage\"}";
            File.WriteAllText(_save.SavePath, futureJson);

            _save.Load();
            Assert.IsFalse(_save.CanSave, "a newer save version disables writing");

            _eco.SetWallet(7);
            _save.Save();

            Assert.AreEqual(futureJson, File.ReadAllText(_save.SavePath),
                "a future-version save must be left intact, not downgraded");
        }
    }
}
