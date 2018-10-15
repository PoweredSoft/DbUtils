using System.Collections.Generic;
using PoweredSoft.DbUtils.EF.Generator.EFCore.Core;
using PoweredSoft.DbUtils.EF.Generator.SqlServer.Core;

namespace PoweredSoft.DbUtils.EF.Generator.EFCore.SqlServer
{
    public class GeneratorOptions : IEFCoreGeneratorOptions, ISqlServerGeneratorOptions
    {
        public List<string> ExcludedTables { get; set; } = new List<string>()
        {
            "dbo.sysdiagrams"
        };

        public List<string> IncludedTables { get; set; } = new List<string>();
        public string Namespace { get; set; } 
        public string ContextName { get; set; }
        public string ContextBaseClassName { get; set; } = "DbContext";
        public string ConnectionString { get; set; }
        public string OutputDir { get; set; }
        public bool CleanOutputDir { get; set; } = false;
        public bool OutputToSingleFile => !string.IsNullOrWhiteSpace(OutputSingleFileName);
        public bool GenerateContextSequenceMethods { get; set; }
        public string OutputSingleFileName { get; set; }
        public bool GenerateInterfaces { get; set; } = false;
        public string InterfaceNameSuffix { get; set; } = "";
        public bool GenerateModels { get; set; } = false;
        public bool GenerateModelsInterfaces { get; set; } = false;
        public bool GenerateModelPropertyAsNullable { get; set; } = false;
        public string ModelSuffix { get; set; } = "Base";
        public string ModelInterfaceSuffix { get; set; } = "";
        public List<string> ModelInheritances { get; set; } = new List<string>();
        public string Version => "core";
        public string Engine => "SqlServer";
        public string ConnectionStringName { get; set; }
        public List<string> IncludedSchemas { get; } = new List<string>();
        public List<string> ExcludedSchemas { get; } = new List<string>();
        public bool AddConnectionStringOnGenerate { get; set; } = false;
    }
}
