using VeeamFolderSynchronizer;

if (args.Length != 4)
{
    Console.WriteLine("Usage: FolderSynchronizer <sourcePath> <replicaPath> <intervalInSeconds> <logFilePath>");
    return;
}

string sourcePath = args[0];
string replicaPath = args[1];
string logFilePath = args[3];

var synchronizer = new FolderSynchronizerService(sourcePath, replicaPath, logFilePath);

if (!synchronizer.ValidateArgument(sourcePath, synchronizer.IsValidPath, "Provided source path argument is not a valid path.") ||
    !synchronizer.ValidateArgument(replicaPath, synchronizer.IsValidPath, "Provided replica path argument is not a valid path.") ||
    !synchronizer.ValidateArgument(logFilePath, synchronizer.IsValidPath, "Provided log file path argument is not a valid path.") ||
    !synchronizer.ValidateArgument(args[2], synchronizer.IsValidInteger, "Provided interval argument is not a valid integer."))
{
    return;
}

int interval = int.Parse(args[2]);

synchronizer.Log("Synchronization started.");

using CancellationTokenSource cts = new();
var token = cts.Token;

try
{
    await synchronizer.Execute(interval, token);
}
catch (TaskCanceledException)
{
    synchronizer.Log("Synchronization stopped.");
}
