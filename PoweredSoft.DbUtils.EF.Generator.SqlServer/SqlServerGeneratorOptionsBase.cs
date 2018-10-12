using System.Collections.Generic;
using PoweredSoft.DbUtils.EF.Generator.SqlServer.Core;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public abstract class SqlServerGeneratorOptionsBase : ISqlServerGeneratorOptions
    {
        public virtual List<string> ExcludedTables { get; set; } = new List<string>{ "dbo.sysdiagrams" };
        public virtual List<string> IncludedTables { get; set; } = new List<string>();
        public virtual string Namespace { get; set; } 
        public virtual string ContextName { get; set; }
        public virtual string ContextBaseClassName { get; set; }
        public virtual string ConnectionString { get; set; }
        public virtual string OutputDir { get; set; }
        public virtual bool CleanOutputDir { get; set; } = false;
        public virtual bool OutputToSingleFile => !string.IsNullOrWhiteSpace(OutputSingleFileName);
        public virtual bool GenerateContextSequenceMethods { get; set; }
        public virtual string OutputSingleFileName { get; set; }
        public virtual bool GenerateInterfaces { get; set; } = false;
        public virtual bool GenerateModelsInterfaces { get; set; } = false;
        public virtual bool GenerateModels { get; set; } = false;
        public virtual bool GenerateModelPropertyAsNullable { get; set; } = false;
        public virtual string ModelSuffix { get; set; } = "Base";
        public virtual string ModelInterfaceSuffix { get; set; } = "";
        public virtual List<string> ModelInheritances { get; set; } = new List<string>();
        public virtual string Version { get; }
        public virtual List<string> IncludedSchemas { get; set; } = new List<string>();
        public virtual List<string> ExcludedSchemas { get; set; } = new List<string>();
        public virtual string InterfaceNameSuffix { get; set; }

        public bool ShouldSerializeOutputToSingleFile() => false;
    }
}
