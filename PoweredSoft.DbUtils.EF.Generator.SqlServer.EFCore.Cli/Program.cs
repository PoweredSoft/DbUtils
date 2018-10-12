﻿using System;
using System.Collections.Generic;
using PoweredSoft.DbUtils.EF.Generator.Cli.Core;
using SysCommand.ConsoleApp;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EFCore.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            GeneratorsCommands.SetGenerator(() => new SqlServerGenerator());
            return App.RunApplication(() =>
            {
                var commands = new List<Type>() {typeof(GeneratorsCommands)};
                var consoleApp = new App(commands);
                consoleApp.Console.ColorSuccess = ConsoleColor.Green;
                consoleApp.Console.Verbose = Verbose.All;
                return consoleApp;
            });
        }
    }
}
