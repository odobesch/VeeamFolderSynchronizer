using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VeeamFolderSynchronizer
{
    public class FolderSynchronizerService
    {
        private readonly string sourcePath;
        private readonly string replicaPath;
        private readonly string logFilePath;

        public FolderSynchronizerService(string sourcePath, string replicaPath, string logFilePath)
        {
            this.sourcePath = sourcePath;
            this.replicaPath = replicaPath;
            this.logFilePath = logFilePath;
        }

        public async Task Execute(int interval, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                SynchronizeFolders();
                await Task.Delay(interval * 1000, token);
            }
        }

        private void SynchronizeFolders()
        {
            try
            {
                var sourceDir = new DirectoryInfo(sourcePath);
                var replicaDir = new DirectoryInfo(replicaPath);

                if (!sourceDir.Exists)
                {
                    Log($"Source folder '{sourcePath}' does not exist.");
                    return;
                }

                if (!replicaDir.Exists)
                {
                    Log($"Replica folder '{replicaPath}' does not exist. Creating...");
                    replicaDir.Create();
                }

                SynchronizeDirectories(sourceDir, replicaDir);

                Log("Synchronization completed successfully.");
            }
            catch (Exception ex)
            {
                Log($"Error during synchronization: {ex.Message}");
            }
        }

        private void SynchronizeDirectories(DirectoryInfo sourceDir, DirectoryInfo replicaDir)
        {
            foreach (var sourceFile in sourceDir.GetFiles())
            {
                var replicaFilePath = Path.Combine(replicaDir.FullName, sourceFile.Name);

                if (!File.Exists(replicaFilePath) || !FilesAreEqual(sourceFile.FullName, replicaFilePath))
                {
                    File.Copy(sourceFile.FullName, replicaFilePath, true);
                    Log($"File copied: {sourceFile.FullName} to {replicaFilePath}");
                }
            }

            foreach (var sourceSubDir in sourceDir.GetDirectories())
            {
                var replicaSubDirPath = Path.Combine(replicaDir.FullName, sourceSubDir.Name);

                if (!Directory.Exists(replicaSubDirPath))
                {
                    Directory.CreateDirectory(replicaSubDirPath);
                    Log($"Directory created: {replicaSubDirPath}");
                }

                SynchronizeDirectories(sourceSubDir, new DirectoryInfo(replicaSubDirPath));
            }

            foreach (var replicaFile in replicaDir.GetFiles())
            {
                var sourceFilePath = Path.Combine(sourceDir.FullName, replicaFile.Name);

                if (!File.Exists(sourceFilePath))
                {
                    replicaFile.Delete();
                    Log($"File deleted: {replicaFile.FullName}");
                }
            }

            foreach (var replicaSubDir in replicaDir.GetDirectories())
            {
                var sourceSubDirPath = Path.Combine(sourceDir.FullName, replicaSubDir.Name);

                if (!Directory.Exists(sourceSubDirPath))
                {
                    Directory.Delete(replicaSubDir.FullName, true);
                    Log($"Directory deleted: {replicaSubDir.FullName}");
                }
            }
        }

        public bool FilesAreEqual(string path1, string path2)
        {
            using var md5 = MD5.Create();
            using var stream1 = File.OpenRead(path1);
            using var stream2 = File.OpenRead(path2);
            var hash1 = md5.ComputeHash(stream1);
            var hash2 = md5.ComputeHash(stream2);
            return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
        }

        public void Log(string message)
        {
            var logMessage = $"{DateTime.Now}: {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }

        public bool IsValidPath(string path)
        {
            string pattern = @"^(?:[a-zA-Z]\:|\\\\[\w\.-]+\\[\w\.$-]+)\\(?:[\w\-]+\\)*\w([\w.])+$";
            return Regex.IsMatch(path, pattern);
        }

        public bool IsValidInteger(string value)
        {
            string pattern = @"^\d+$";
            return Regex.IsMatch(value, pattern);
        }

        public bool ValidateArgument(string argument, Func<string, bool> validationFunction, string errorMessage)
        {
            if (!validationFunction(argument))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errorMessage);
                Console.ResetColor();
                return false;
            }
            return true;
        }
    }
}
