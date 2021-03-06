using System.Security.Cryptography;

namespace SaveDataSync.Tests
{
    [TestClass]
    public class LocalSavesTests
    {
        private static readonly LocalSaves localSaves = new LocalSaves();
        private static readonly string testPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        [ClassInitialize]
        public static void Setup(TestContext testContext)
        {
            // Create test directory
            Directory.CreateDirectory(testPath);

            // Add single test files
            var testFile1 = Path.GetTempFileName();
            File.WriteAllText(testFile1, "foo");
            var testFile2 = Path.GetTempFileName();
            File.WriteAllText(testFile1, "bar");
            localSaves.AddSave("test_file1", testFile1);
            localSaves.AddSave("test_file2", testFile2);

            // Add test folders
            var testFolder1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testFolder1);
            var tmpTestFile1_1 = Path.Combine(testFolder1, Path.GetRandomFileName());
            File.WriteAllText(tmpTestFile1_1, "Hello");
            var tmpTestFile1_2 = Path.Combine(testFolder1, Path.GetRandomFileName());
            File.WriteAllText(tmpTestFile1_2, "World");
            var testFolder2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(testFolder2);
            var tmpTestFile2_1 = Path.Combine(testFolder2, Path.GetRandomFileName());
            File.WriteAllText(tmpTestFile2_1, "Hi");
            var tmpTestFile2_2 = Path.Combine(testFolder2, Path.GetRandomFileName());
            File.WriteAllText(tmpTestFile2_2, "Mom");
            localSaves.AddSave("test_folder1", testFolder1);
            localSaves.AddSave("test_folder2", testFolder2);
        }

        [TestMethod("Name Validation Functionality")]
        public void LocalSaves_NameValidationWorks()
        {
            // Dupe file name
            Assert.ThrowsException<InvalidSaveException>(() => localSaves.AddSave("test_file1", testPath));
            Assert.ThrowsException<InvalidSaveException>(() => localSaves.AddSave("test_folder1", testPath));

            // Dupe folder
            Assert.ThrowsException<InvalidSaveException>(() => localSaves.AddSave("test_file3", localSaves.GetSavePath("test_file1")));
            Assert.ThrowsException<InvalidSaveException>(() => localSaves.AddSave("test_folder3", localSaves.GetSavePath("test_folder1")));

            // Contains current folder
            Assert.ThrowsException<InvalidSaveException>(() => localSaves.AddSave("temp_directory", Path.GetTempPath()));

            // Illegal characters
            Assert.ThrowsException<InvalidSaveException>(() => localSaves.AddSave("\\ /", testPath));

            // Longer than 32 chars
            Assert.ThrowsException<InvalidSaveException>(() => localSaves.AddSave("Lorem ipsum dolor sit amet fusce.", testPath));
        }

        [TestMethod("Save Management Tests")]
        public async Task LocalSaves_SaveManagementTests()
        {
            localSaves.AddSave("testing", testPath);
            var saves = localSaves.Saves;
            Assert.AreEqual(saves["testing"], FileUtils.Normalize(testPath));
            localSaves.RemoveSave("testing");

            // Delete a file that doesn't exist
            Assert.ThrowsException<Exception>(() => localSaves.RemoveSave("testing"));

            // Get save path of a file that doesn't exist
            Assert.ThrowsException<Exception>(() => localSaves.GetSavePath("testing"));

            // Get zip data of a file that doesn't exist
            await Assert.ThrowsExceptionAsync<Exception>(async () => await localSaves.ArchiveSaveData("testing", "random_location"));
        }

        [TestMethod("Serialization")]
        public void LocalSaves_JsonTest()
        {
            var json = localSaves.Serialize();
            var fromJson = LocalSaves.Deserialize(json);
            var json2 = fromJson.Serialize();
            Assert.AreEqual(json, json2);
        }

        [TestMethod("Zip is Deterministic")]
        public async Task LocalSaves_ZipIsDeterministic()
        {
            var sha256 = SHA256.Create();

            // File test
            using var tmpFile1 = new FileUtils.TemporaryFile();
            using var tmpFile2 = new FileUtils.TemporaryFile();
            using var tmpFile3 = new FileUtils.TemporaryFile();
            using var tmpFile4 = new FileUtils.TemporaryFile();
            await localSaves.ArchiveSaveData("test_file1", tmpFile1.FilePath);
            using var stream1 = File.OpenRead(tmpFile1.FilePath);
            var hash1 = sha256.ComputeHash(stream1);
            var hex1 = BitConverter.ToString(hash1, 0, hash1.Length).Replace("-", "").ToLower();
            await Task.Delay(2000);
            await localSaves.ArchiveSaveData("test_file1", tmpFile2.FilePath);
            using var stream2 = File.OpenRead(tmpFile2.FilePath);
            var hash2 = sha256.ComputeHash(stream2);
            var hex2 = BitConverter.ToString(hash2, 0, hash2.Length).Replace("-", "").ToLower();
            Assert.AreEqual(hex1, hex2);

            // Folder test
            await localSaves.ArchiveSaveData("test_folder1", tmpFile3.FilePath);
            using var stream3 = File.OpenRead(tmpFile3.FilePath);
            var hash3 = sha256.ComputeHash(stream3);
            var hex3 = BitConverter.ToString(hash3, 0, hash3.Length).Replace("-", "").ToLower();
            await Task.Delay(2000);
            await localSaves.ArchiveSaveData("test_folder1", tmpFile4.FilePath);
            using var stream4 = File.OpenRead(tmpFile4.FilePath);
            var hash4 = sha256.ComputeHash(stream4);
            var hex4 = BitConverter.ToString(hash4, 0, hash4.Length).Replace("-", "").ToLower();
            Assert.AreEqual(hex3, hex4);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            var saveLocations = localSaves.Saves.Values.ToList();
            foreach (var location in saveLocations)
            {
                FileAttributes attr = File.GetAttributes(location);
                bool isDirectory = attr.HasFlag(FileAttributes.Directory);
                if (isDirectory)
                {
                    Directory.Delete(location, true);
                }
                else
                {
                    File.Delete(location);
                }
            }

            Directory.Delete(testPath, true);
        }
    }
}