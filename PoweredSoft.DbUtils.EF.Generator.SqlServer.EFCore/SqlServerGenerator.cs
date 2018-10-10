using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EFCore
{
    public class SqlServerGenerator : SqlServerGeneratorBase<SqlServerGeneratorOptions>
    {
        protected override string CollectionInstanceType() => "System.Collections.Generic.HashSet";

        protected override void GenerateContext()
        {
            
        }
    }
}
