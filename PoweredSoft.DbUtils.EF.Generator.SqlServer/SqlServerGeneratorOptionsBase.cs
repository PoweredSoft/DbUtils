using System.Collections.Generic;
using PoweredSoft.DbUtils.EF.Generator.SqlServer.Core;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public class SqlServerGeneratorOptionsBase : ISqlServerGeneratorOptions
    {
        public virtual List<string> ExcludedTables { get; set; } = new List<string>{ "dbo.sysdiagrams" };
        public virtual List<string> IncludedTables { get; set; } = new List<string>();
        public virtual string Namespace { get; set; } 
        public virtual string ContextName { get; set; }
        public virtual string ContextBaseClassName { get; set; }
        public virtual string ConnectionString { get; set; }
        public virtual string OutputDir { get; set; }
        public bool CleanOutputDir { get; set; } = false;
        public virtual bool OutputToSingleFile => !string.IsNullOrWhiteSpace(OutputSingleFileName);
        public bool GenerateContextSequenceMethods { get; set; }
        public virtual string OutputSingleFileName { get; set; }
        public bool GenerateInterfaces { get; set; } = false;
        public bool GenerateModelsInterfaces { get; set; } = false;
        public bool GenerateModels { get; set; } = false;
        public bool GenerateModelPropertyAsNullable { get; set; } = false;
        public string ModelSuffix { get; set; } = "Base";
        public string ModelInterfaceSuffix { get; set; } = "";
        public List<string> ModelInheritances { get; set; } = new List<string>();
        public virtual List<string> IncludedSchemas { get; set; } = new List<string>();
        public virtual List<string> ExcludedSchemas { get; set; } = new List<string>();
        public string InterfaceNameSuffix { get; set; }

        public bool ShouldSerializeOutputToSingleFile() => false;
    }
}
