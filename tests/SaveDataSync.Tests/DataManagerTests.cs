namespace SaveDataSync.Tests
{
    [TestClass]
    public class DataManagerTests
    {
        private static readonly string dataDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        private static readonly DataManager dataManager = new(dataDir);

        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            if (testContext is null)
            {
                throw new ArgumentNullException(nameof(testContext));
            }

            Directory.CreateDirectory(dataDir);
        }

        [TestMethod("Local Saves - Save/Load")]
        public async Task DataManagementTests_LocalSaveListTests()
        {
            var saves = new LocalSaves();
            var testFile = Path.GetTempFileName();
            saves.AddSave("test", testFile);

            // Save to data directory
            await dataManager.SaveLocalSaves(saves);

            // Load from directory
            var saves2 = dataManager.GetLocalSaves();
            Assert.AreEqual(saves.GetSavePath("test"), saves2.GetSavePath("test"));
        }

        [TestMethod("Servers - Save/Load")]
        public async Task DataManagementTests_ServerTests()
        {
            var server = new DropboxServer("random data", "doesn't matter", DateTime.Now, "we're not testing server functionlity");

            // Save to data directory
            await dataManager.SaveServerData(server);

            // Load from directory
            var server2 = dataManager.GetServerData();
            Assert.AreEqual(server.Serialize().ToString(), server2.Serialize().ToString());
        }

        [TestMethod("Settings - Save/Load")]
        public async Task DataManagementTests_Settings()
        {
            var settings = new Settings();

            // Save to data directory
            await dataManager.SaveSettings(settings);

            // Load from directory
            Settings settings2 = dataManager.GetSettings();
            Assert.AreEqual(settings.ToJSON().ToString(), settings2.ToJSON().ToString());
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            Directory.Delete(dataDir, true);
        }
    }
}