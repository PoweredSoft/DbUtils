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
        public string EntityNamespace { get; set; }
        public string EntityInterfaceNamespace { get; set; }
        public string ModelInterfaceNamespace { get; set; }
        public string ModelNamespace { get; set; }
        public string ContextNamespace { get; set; }
        public string EntitiesOutputSingleFileName { get; set; }
        public string EntitiesInterfacesOutputSingleFileName { get; set; }
        public string ModelsInterfacesOutputSingleFileName { get; set; }
        public string ModelsOutputSingleFileName { get; set; }
        public string ContextOutputSingleFileName { get; set; }
        public string EntitiesOutputDir { get; set; }
        public string EntitiesInterfacesOutputDir { get; set; }
        public string ModelsInterfacesOutputDir { get; set; }
        public string ModelsOutputDir { get; set; }
        public string ContextOutputDir { get; set; }
        public string ConnectionStringName { get; set; }
        public bool AddConnectionStringOnGenerate { get; set; } = false;
        public bool DontGeneratePrimaryIndexes { get; set; } = true;
        public bool DontGenerateForeignKeyIndexes { get; set; } = true;
    }
}
