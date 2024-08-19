// See https://aka.ms/new-console-template for more information

using pacman_speedster;

Console.WriteLine("Hello, World!");
for (int i = 0; i < args.Length; i++)
{
    var item = args[i].Trim('\'').Trim('"');
    switch (item.ToLower())
    {
        case "--help":
        case "/?":
        case "-h":
        case "help":
            Console.WriteLine("use: pacman-speedster [Options] ");
            Console.WriteLine(" -c, --max-errors\t max erros count");
            Console.WriteLine(" -s, --self-sig\t\t use server self sig file");
            Console.WriteLine(" -d, --down-path\tset target output directory");
            Console.WriteLine(" -y\t\t\tpacman -Sy before download");
            Console.WriteLine(" -m, --no-move\t\tdon't move file when download completed");
            Console.WriteLine(" -k, --skip-exist\t skip exists package");
            return;
        case "-c":
        case "--max-errors":
            if (int.TryParse(args[i + 1], out var value))
                DownloadPackage.MaxErrorCount = value;
            i++;
            break;
        case "-d":
            DownloadPackage.TmpPath = args[i + 1];
            i++;
            break;
        case "-y":
        case "--down-path":
            InitDataBase.NoPacmanSy = true;
            break;
        case "-k":
        case "--skip-exist":
            DownloadPackage.SkipExistPackage = true;
            break;
        case "-m":
        case "--no-move":
        case "-s":
        case "--self-sig":
        default:
            throw new Exception("UnSupport Argument: " + item);
    }
}
try
{
    InitDataBase.LoadPacmanData();
    InitDataBase.AnalysePacmanData();
    DownloadPackage.Start();
    DownloadPackage.WaitEnd();
}
catch (Exception e)
{
    Console.WriteLine(e);
}