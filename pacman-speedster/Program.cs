// See https://aka.ms/new-console-template for more information

using pacman_speedster;

Console.WriteLine("Hello, World!");
foreach (var item in args)
{
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
            return;
        default:
            break;
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