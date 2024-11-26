// See https://aka.ms/new-console-template for more information
using System.Text;

if (args.Length != 1)
{
    Console.Error.WriteLine("[FATAL] Invalid command usage.");
    Console.WriteLine("Usage: {0} \"path-to-sort\"");

    Environment.Exit(1);
}

string moveDir;

try
{
    moveDir = Path.GetFullPath(args[0]);
}
catch
{
    Console.Error.WriteLine("[FATAL] Unable to find sort directory");
    Environment.Exit(6);
    return;
}

if (!Directory.Exists(moveDir))
{
    Console.Error.WriteLine("[FATAL] No sort directory exists");
    Environment.Exit(6);
}

var confPath = Path.Combine(moveDir, ".extsorterconf.tsv");

Console.WriteLine("Configuration Path: \t", confPath);

if (!File.Exists(confPath))
{
    Console.Error.WriteLine("[FATAL] No configuration file exists");
    Environment.Exit(2);
}

// ext, movedir
var confDict = new Dictionary<string, string>();

var confList = await File.ReadAllLinesAsync(confPath, Encoding.UTF8);

for (int i = 0; i < confList.Length; i++)
{
    var conf = confList[i];
    var confElem = conf.Split('\t');

    if (confElem.Length != 2)
    {
        Console.Error.WriteLine($"[FATAL] Config error at line {i + 1}: invalid count of element.");
        Environment.Exit(3);
    }

    if (!confElem[0].StartsWith('.'))
    {
        Console.Error.WriteLine($"[WARN] Config lint error at line {i + 1}: extension declare should start with '.' like '.exe'.");
        confElem[0] = '.' + confElem[0];
    }

    confElem[0] = confElem[0].Trim();
    confElem[1] = confElem[1].Trim();

    if (!Path.IsPathFullyQualified(confElem[1]))
    {
        confElem[1] = Path.Combine(moveDir, confElem[1]);
    }

    if (!Directory.Exists(confElem[1]))
    {
        Console.Error.WriteLine($"[FATAL] Config error at line {i + 1}: target directory '{confElem[1]}' non exists.");
        Environment.Exit(4);
    }

    if (confDict.ContainsKey(confElem[0]))
    {
        Console.Error.WriteLine($"[FATAL] Config error at line {i + 1}: extension {confElem[0]} already declared in previous line.");
        Environment.Exit(5);
    }

    confDict.Add(confElem[0], confElem[1]);
}

Console.WriteLine($"[INFO] Configuration loaded: {confDict.Count} lines available.");

var files = Directory.GetFiles(moveDir, "*", SearchOption.AllDirectories);

for (int i = 0; i < files.Length; i++)
{
    var filePath = files[i];
    var fileName = Path.GetFileName(filePath);

    if (fileName == ".extsorterconf.tsv")
    {
        continue;
    }

    Console.Write($"\r[{i + 1}/{files.Length}] {fileName}");

    var filePathSmall = filePath.ToLower();
    string? dirToMove = null;
    foreach (var ep in confDict)
    {
        if (filePathSmall.EndsWith(ep.Key))
        {
            dirToMove = ep.Value;
            break;
        }
    }

    if (dirToMove == null)
    {
        Console.Error.WriteLine($"\r[INFO] \"{filePath}\" isn't match any move request.");
        continue;
    }

    var newPath = Path.Combine(dirToMove, fileName);

    if (File.Exists(newPath)) {
        Console.Error.WriteLine($"\r[WARN] file named \"{fileName}\" is already exists in targeted directory.");
        continue;
    }

    Console.Write($"\r[{i + 1}/{files.Length}] moving {fileName}");
    await Task.Run(() => File.Move(filePath, newPath));
}
