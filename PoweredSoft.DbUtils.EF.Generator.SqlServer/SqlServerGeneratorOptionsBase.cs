using System.Collections.Generic;
using PoweredSoft.DbUtils.EF.Generator.SqlServer.Core;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public class SqlServerGeneratorOptionsBase : ISqlServerGeneratorOptions
    {
        public List<string> ExcludedTables { get; set; }
        public List<string> IncludedTables { get; set; }
        public string ContextName { get; set; }
        public string ContextBaseClassName { get; set; }
        public string ConnectionString { get; set; }
        public string OutputDir { get; set; }
        public bool OutputToSingleFile { get; set; }
        public string OutputSingleFileName { get; set; }
        public List<string> IncludedSchemas { get; set; }
        public List<string> ExcludedSchemas { get; set; }
    }
}
