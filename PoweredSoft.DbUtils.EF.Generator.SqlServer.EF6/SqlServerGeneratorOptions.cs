using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EF6
{
    public class SqlServerGeneratorOptions : SqlServerGeneratorOptionsBase
    {
        public string FluentConfigurationClassSuffix { get; set; } = "FluentConfiguration";
        public override string ContextBaseClassName { get; set; } = "System.Data.Entity.DbContext";
        public string ConnectionStringName { get; set; }
        public string Version => "6";
    }
}
