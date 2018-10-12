using System;
using PoweredSoft.DbUtils.EF.Generator.Core;
using SysCommand.ConsoleApp;

namespace PoweredSoft.DbUtils.EF.Generator.Cli.Core
{
    public class GeneratorsCommands : Command
    {
        internal static Func<IGenerator> CreateGeneratorFunc { get; set; }

        public void Generate(string configFile = "GeneratorOptions.json")
        {
            var generator = CreateGeneratorFunc();
            generator.LoadOptionsFromJson(configFile);
            generator.Generate();
        }

        public static void SetGenerator(Func<IGenerator> createGenerator)
        {
            CreateGeneratorFunc = createGenerator;
        }
    }
}
