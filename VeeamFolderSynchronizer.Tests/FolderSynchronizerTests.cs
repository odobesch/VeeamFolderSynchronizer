using VeeamFolderSynchronizer;

namespace FolderSynchronizer.NUnitTests
{
    public class FolderSynchronizerTests
    {
        private string sourcePath;
        private string replicaPath;
        private string logFilePath;
        private FolderSynchronizerService synchronizer;

        [SetUp]
        public void Setup()
        {
            sourcePath = Path.Combine("C:\\Test", "Source");
            replicaPath = Path.Combine("C:\\Test", "Replica");
            logFilePath = Path.Combine("C:\\Test", "Logs.txt");

            Directory.CreateDirectory(sourcePath);
            Directory.CreateDirectory(replicaPath);

            synchronizer = new FolderSynchronizerService(sourcePath, replicaPath, logFilePath);
        }

        [Test]
        public void ValidateArgument_ValidPath_ReturnsTrue()
        {
            Assert.IsTrue(synchronizer.ValidateArgument(sourcePath, synchronizer.IsValidPath, "Invalid path"));
        }

        [Test]
        public void ValidateArgument_InvalidPath_ReturnsFalse()
        {
            string invalidPath = "invalid:path";
            Assert.IsFalse(synchronizer.ValidateArgument(invalidPath, synchronizer.IsValidPath, "Invalid path"));
        }

        [Test]
        public void ValidateArgument_ValidInteger_ReturnsTrue()
        {
            string validInteger = "123";
            Assert.IsTrue(synchronizer.ValidateArgument(validInteger, synchronizer.IsValidInteger, "Invalid integer"));
        }

        [Test]
        public void ValidateArgument_InvalidInteger_ReturnsFalse()
        {
            string invalidInteger = "12a3";
            Assert.IsFalse(synchronizer.ValidateArgument(invalidInteger, synchronizer.IsValidInteger, "Invalid integer"));
        }

        [Test]
        public async Task Execute_CreatesReplicaFolder()
        {
            if (Directory.Exists(replicaPath))
            {
                Directory.Delete(replicaPath, true);
            }

            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            try
            {
                var task = synchronizer.Execute(1, token);

                await Task.Delay(3000);
                cts.Cancel();
                await task;
            }
            catch (TaskCanceledException)
            {
            }

            Assert.IsTrue(Directory.Exists(replicaPath), $"Replica folder {replicaPath} should be created.");
        }

        [Test]
        public async Task Execute_CopiesNewFiles()
        {
            string sourceFile = Path.Combine(sourcePath, "test.txt");
            await File.WriteAllTextAsync(sourceFile, "Hello, World!");

            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            try
            {
                var task = synchronizer.Execute(1, token);

                await Task.Delay(3000);
                cts.Cancel();
                await task;
            }
            catch (TaskCanceledException)
            {
            }

            string replicaFile = Path.Combine(replicaPath, "test.txt");
            Assert.IsTrue(File.Exists(replicaFile));
            Assert.AreEqual(await File.ReadAllTextAsync(sourceFile), await File.ReadAllTextAsync(replicaFile));
        }

        [Test]
        public void FilesAreEqual_SameContent_ReturnsTrue()
        {
            string file1 = Path.Combine(sourcePath, "file1.txt");
            string file2 = Path.Combine(sourcePath, "file2.txt");

            File.WriteAllText(file1, "Hello, World!");
            File.WriteAllText(file2, "Hello, World!");

            Assert.IsTrue(synchronizer.FilesAreEqual(file1, file2));
        }

        [Test]
        public void FilesAreEqual_DifferentContent_ReturnsFalse()
        {
            string file1 = Path.Combine(sourcePath, "file1.txt");
            string file2 = Path.Combine(sourcePath, "file2.txt");

            File.WriteAllText(file1, "Hello, World!");
            File.WriteAllText(file2, "Hello, Universe!");

            Assert.IsFalse(synchronizer.FilesAreEqual(file1, file2));
        }

        [Test]
        public void IsValidPath_ValidPath_ReturnsTrue()
        {
            string validPath = @"C:\valid\path";
            Assert.IsTrue(synchronizer.IsValidPath(validPath));
        }

        [Test]
        public void IsValidPath_InvalidPath_ReturnsFalse()
        {
            string invalidPath = @"C::\invalid\path";
            Assert.IsFalse(synchronizer.IsValidPath(invalidPath));
        }

        [Test]
        public void IsValidInteger_ValidInteger_ReturnsTrue()
        {
            string validInteger = "123";
            Assert.IsTrue(synchronizer.IsValidInteger(validInteger));
        }

        [Test]
        public void IsValidInteger_InvalidInteger_ReturnsFalse()
        {
            string invalidInteger = "12a3";
            Assert.IsFalse(synchronizer.IsValidInteger(invalidInteger));
        }
    }
}
