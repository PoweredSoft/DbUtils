using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.EF.Generator.EF6.Core;
using SysCommand.ConsoleApp;

namespace psdb
{
    public class GeneratorsCommands : Command
    {
        internal static Func<IGenerator> CreateGeneratorFunc { get; set; }

        private void EnsureGenerator(string version, string engine)
        {
            if (version.Equals("core", StringComparison.InvariantCultureIgnoreCase))
            {
                if (engine.Equals("SqlServer", StringComparison.InvariantCultureIgnoreCase))
                    CreateGeneratorFunc = () => new PoweredSoft.DbUtils.EF.Generator.EFCore.SqlServer.DatabaseGenerator();
            }
            else if (version.IndexOf("6", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                if (engine.Equals("SqlServer", StringComparison.InvariantCultureIgnoreCase))
                    CreateGeneratorFunc = () => new PoweredSoft.DbUtils.EF.Generator.EF6.SqlServer.DatabaseGenerator();
            }
        }

        public void OutputEnvironmentDir()
        {
            this.App.Console.Write(Environment.CurrentDirectory);
        }

        public void Generate(string config = "psdb.json")
        {
            if (!File.Exists(config))
            {
                this.App.Console.Error($"{config} file could not be found.", true, true);
                return;
            }

            EnsureVersionFromConfig(config);
            var generator = CreateGeneratorFunc();
            generator.LoadOptionsFromJson(config);
            generator.Generate();
            this.App.Console.Success("Context has been generated successfully", true, true);
        }

        private void EnsureVersionFromConfig(string config)
        {
            var json = File.ReadAllText(config);
            var anonymous = JsonConvert.DeserializeObject(json) as JObject;
            var version = anonymous.GetValue("Version").Value<string>();
            var engine = anonymous.GetValue("Engine").Value<string>();
            EnsureGenerator(version, engine);
        }

        public void Init(string config = "psdb.json", string version = "core", string engine = "SqlServer",
            string contextName = null, string connectionString = null, string outputDir = null, string outputFile = null,
            string @namespace = null, string connectionStringName = null)
        {
            EnsureGenerator(version, engine);

            var options = CreateGeneratorFunc().GetDefaultOptions();
            options.ContextName = contextName;
            options.ConnectionString = connectionString;
            options.OutputDir = outputDir ?? Environment.CurrentDirectory;
            options.OutputSingleFileName = outputFile;
            options.Namespace = @namespace;

            if (options is IEF6GeneratorOptions)
            {
                var ef6Options = options as IEF6GeneratorOptions;
                ef6Options.ConnectionStringName = connectionStringName;
            }

            var json = JsonConvert.SerializeObject(options, Formatting.Indented);
            File.WriteAllText(config, json);
            this.App.Console.Success($"Options file generated successfully {config}");
        }
    }
}
