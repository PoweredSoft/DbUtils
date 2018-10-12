using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EFCore
{
    public class SqlServerGeneratorOptions : SqlServerGeneratorOptionsBase
    {
        public override string ContextBaseClassName { get; set; } = "DbContext";
        public bool AddConnectionStringOnGenerate { get; set; } = false;
        public string Version => "core";
    }
}
