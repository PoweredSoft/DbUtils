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
        string OutputDir { get; set; }
        bool OutputToSingleFile { get; }
        string OutputSingleFileName { get; }
    }
}
