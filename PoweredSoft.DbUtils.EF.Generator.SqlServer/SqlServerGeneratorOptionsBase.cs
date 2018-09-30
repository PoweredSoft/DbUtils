using System.Collections.Generic;
using PoweredSoft.DbUtils.EF.Generator.SqlServer.Core;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public class SqlServerGeneratorOptionsBase : ISqlServerGeneratorOptions
    {
        public virtual List<string> ExcludedTables { get; set; } = new List<string>{ "dbo.sysdiagrams" };
        public virtual List<string> IncludedTables { get; set; }
        public virtual string Namespace { get; set; } 
        public virtual string ContextName { get; set; }
        public virtual string ContextBaseClassName { get; set; }
        public virtual string ConnectionString { get; set; }
        public virtual string OutputDir { get; set; }
        public virtual bool OutputToSingleFile => !string.IsNullOrWhiteSpace(OutputSingleFileName);
        public virtual string OutputSingleFileName { get; set; }
        public virtual List<string> IncludedSchemas { get; set; }
        public virtual List<string> ExcludedSchemas { get; set; }
    }
}
