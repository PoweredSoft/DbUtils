using System.Collections.Generic;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IGeneratorOptions
    {
        List<string> ExcludedTables { get; }
        List<string> IncludedTables { get; }
        string Namespace { get; set; }
        string ContextName { get; }
        string ContextBaseClassName { get; }
        string ConnectionString { get; }
        string OutputDir { get; }
        bool CleanOutputDir { get; }
        bool OutputToSingleFile { get; }
        string OutputSingleFileName { get; }
        bool GenerateInterfaces { get;}
        string InterfaceNameSuffix { get; }
        bool GenerateModels { get; }
        bool GenerateModelPropertyAsNullable { get; }
        string ModelSuffix { get; }
    }
}
