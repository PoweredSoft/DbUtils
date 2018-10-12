using System;
using System.Collections.Generic;
using SysCommand.ConsoleApp;

namespace psdb_cli
{
    class Program
    {
        static int Main(string[] args)
        {
            return App.RunApplication(() =>
            {
                var commands = new List<Type>() { typeof(GeneratorsCommands) };
                var consoleApp = new App(commands);
                consoleApp.Console.ColorSuccess = ConsoleColor.Green;
                consoleApp.Console.Verbose = Verbose.All;
                return consoleApp;
            });
        }
    }
}
