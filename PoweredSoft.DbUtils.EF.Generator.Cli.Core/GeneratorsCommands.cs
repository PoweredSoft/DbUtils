﻿using System;
using System.IO;
using Newtonsoft.Json;
using PoweredSoft.DbUtils.EF.Generator.Core;
using SysCommand.ConsoleApp;

namespace PoweredSoft.DbUtils.EF.Generator.Cli.Core
{
    public class GeneratorsCommands : Command
    {
        internal static Func<IGenerator> CreateGeneratorFunc { get; set; }

        public void Generate(string configFile = "GeneratorOptions.json")
        {
            if (!File.Exists(configFile))
            {
                this.App.Console.Error($"{configFile} file could not be found.", true, true);
                return;
            }

            var generator = CreateGeneratorFunc();
            generator.LoadOptionsFromJson(configFile);
            generator.Generate();
            this.App.Console.Success("Context has been generated successfully", true, true);
        }

        public void Init(string configFile = "GeneratorOptions.json", string contextName = null, string connectionString = null, string outputDir = null, string outputFile = null)
        {
            var options = CreateGeneratorFunc().GetDefaultOptions();
            options.ContextName = contextName;
            options.ConnectionString = connectionString;
            options.OutputDir = outputDir;
            options.OutputSingleFileName = outputFile;

            var json = JsonConvert.SerializeObject(options, Formatting.Indented);
            File.WriteAllText(configFile, json);
            this.App.Console.Success($"Options file generated successfully {configFile}");
        }

        public static void SetGenerator(Func<IGenerator> createGenerator)
        {
            CreateGeneratorFunc = createGenerator;
        }
    }
}