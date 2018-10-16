using PoweredSoft.DbUtils.EF.Generator.EFCore.Core;
using System.Collections.Generic;

namespace PoweredSoft.DbUtils.EF.Generator.EFCore.MySql
{
    public class GeneratorOptions : IEFCoreGeneratorOptions
    {
        public List<string> ExcludedTables { get; set; } = new List<string>();
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
        public string Engine => "MySql";
        public string ConnectionStringName { get; set; }
        public bool AddConnectionStringOnGenerate { get; set; } = false;
        public bool DontGeneratePrimaryIndexes { get; set; } = true;
        public bool DontGenerateForeignKeyIndexes { get; set; } = true;
    }
}
