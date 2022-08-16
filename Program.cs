using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;

static class Program
{
    private static object inputDirLock = new object();
    private static DirectoryInfo input;
    private static DirectoryInfo output;
    private static string tychoExe;

    private static void Main(string[] args)
    {
        if (args.Length != 3)
            Console.WriteLine("Usage: tycho-runner <input> <output> <tycho-exe>");

        Console.WriteLine($"{DateTime.Now:o}|Start|Input:{args[0]}|Output:{args[1]}|Tycho:{args[2]}");

        input = new DirectoryInfo(args[0]);
        output = new DirectoryInfo(args[1]);
        tychoExe = args[2];

        ProccessNewDirectories();

        var watcher = new FileSystemWatcher(input.FullName);
        watcher.Created += Watcher_Created;
        watcher.EnableRaisingEvents = true;
        
        Thread.Sleep(Timeout.Infinite);
    }

    private static void ProccessNewDirectories()
    {
        DirectoryInfo[] dirs;
        while ((dirs = input.GetDirectories()).Length > 1)
        {
            if (dirs.Length == 2)
            {
                Console.WriteLine($"{DateTime.Now:o}|Waiting|Only2DirsFound");
                Thread.Sleep(30 * 1000);
            }

            var oldestDir = dirs
                .Aggregate((d1, d2) => d1.CreationTime < d2.CreationTime ? d1 : d2);

            Console.WriteLine($"{DateTime.Now:o}|NewDirectoryFound|Path:{oldestDir.FullName}");

            var newDirPath = Path.Combine(output.FullName, oldestDir.Name);
            oldestDir.MoveTo(newDirPath);
            Console.WriteLine($"{DateTime.Now:o}|DirectoryMoved|Path:{oldestDir.FullName}");

            Thread.Sleep(2 * 60 * 1000);
            ExecuteTyco(oldestDir.FullName);
        }
    }

    private static void ExecuteTyco(string inputPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = tychoExe,
            Arguments = $"1 \"{inputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = output.FullName,
        };
        var p = new Process { 
            StartInfo = psi,
        };
        p.OutputDataReceived += (sender, args) => Console.WriteLine($"{DateTime.Now:o}|Tycho|Data: {args.Data}");
        p.ErrorDataReceived += (sender, args) => Console.Error.WriteLine($"{DateTime.Now:o}|Tycho|Error: {args.Data}");
        p.Start();
        Console.WriteLine($"{DateTime.Now:o}|DirectoryProccessingStarted|Path:{inputPath}");

        p.WaitForExit();

        Console.WriteLine($"{DateTime.Now:o}|DirectoryProccessed|Path:{inputPath}");
        new ToastContentBuilder()
            .AddText("Tycho processed")
            .AddText(inputPath)
            .Show();
    }

    private static int isProcessing = 0;
    private static void Watcher_Created(object sender, FileSystemEventArgs e)
    {
        var isProc = Interlocked.Exchange(ref isProcessing, 1);
        if (isProc == 0)
        {
            ProccessNewDirectories();
            Interlocked.Exchange(ref isProcessing, 0);
        }
    }
}