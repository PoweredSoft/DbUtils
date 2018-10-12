using System.Collections.Generic;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IGeneratorOptions
    {
        List<string> ExcludedTables { get; set; }
        List<string> IncludedTables { get; set; }
        string Namespace { get; set; }
        string ContextName { get; set; }
        string ContextBaseClassName { get; set; }
        string ConnectionString { get; set; }
        string OutputDir { get; set; }
        bool CleanOutputDir { get; set; }
        bool OutputToSingleFile { get; }
        bool GenerateContextSequenceMethods { get; set; }
        string OutputSingleFileName { get; set; }
        bool GenerateInterfaces { get; set; }
        string InterfaceNameSuffix { get; set; }
        bool GenerateModels { get; set; }
        bool GenerateModelsInterfaces { get; set; }
        bool GenerateModelPropertyAsNullable { get; set; }
        string ModelSuffix { get; set; }
        string ModelInterfaceSuffix { get; set; }
        List<string> ModelInheritances { get; set; }
        string Version { get; }
    }
}
