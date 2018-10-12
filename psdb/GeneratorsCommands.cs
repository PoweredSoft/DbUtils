using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoweredSoft.DbUtils.EF.Generator.Core;
using SysCommand.ConsoleApp;

namespace psdb
{
    public class GeneratorsCommands : Command
    {
        internal static Func<IGenerator> CreateGeneratorFunc { get; set; }

        private void EnsureGenerator(string version)
        {
            if (version.Equals("core", StringComparison.InvariantCultureIgnoreCase))
                CreateGeneratorFunc = () => new PoweredSoft.DbUtils.EF.Generator.SqlServer.EFCore.SqlServerGenerator();
            else if (version.Contains("6", StringComparison.OrdinalIgnoreCase))
                CreateGeneratorFunc = () => new PoweredSoft.DbUtils.EF.Generator.SqlServer.EF6.SqlServerGenerator();
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
            EnsureGenerator(version);
        }

        public void Init(string config = "psdb.json", string version = "core", 
            string contextName = null, string connectionString = null, string outputDir = null, string outputFile = null,
            string @namespace = null, string connectionStringName = null)
        {
            EnsureGenerator(version);

            var options = CreateGeneratorFunc().GetDefaultOptions();
            options.ContextName = contextName;
            options.ConnectionString = connectionString;
            options.OutputDir = outputDir;
            options.OutputSingleFileName = outputFile;
            options.Namespace = @namespace;

            if (options is PoweredSoft.DbUtils.EF.Generator.SqlServer.EF6.SqlServerGeneratorOptions)
            {
                var ef6Options = options as PoweredSoft.DbUtils.EF.Generator.SqlServer.EF6.SqlServerGeneratorOptions;
                ef6Options.ConnectionStringName = connectionStringName;
            }

            var json = JsonConvert.SerializeObject(options, Formatting.Indented);
            File.WriteAllText(config, json);
            this.App.Console.Success($"Options file generated successfully {config}");
        }
    }
}
