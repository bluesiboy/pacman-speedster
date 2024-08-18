// See https://aka.ms/new-console-template for more information

using pacman_speedster;

Console.WriteLine("Hello, World!");

try
{
    InitDataBase.LoadPacmanData();
    InitDataBase.AnalysePacmanData();
}
catch (Exception e)
{
    Console.WriteLine(e);
}