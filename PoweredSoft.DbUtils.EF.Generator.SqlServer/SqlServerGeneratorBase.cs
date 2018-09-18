using System;
using System.Collections.Generic;
using System.Linq;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public abstract class SqlServerGeneratorBase<TOptions> : DatabaseGeneratorBase<DatabaseSchema, TOptions>
        where TOptions : SqlServerGeneratorOptionsBase
    {
        public override DatabaseSchema CreateSchema() => new DatabaseSchema();

        public override List<ITable> ResolveTablesToGenerate()
        {
            var ret = base.ResolveTablesToGenerate();

            // schema filtering.
            if (Options.IncludedSchemas?.Any() == true)
            {
                ret = ret
                    .Cast<Table>()
                    .Where(t => Options.IncludedSchemas.Any(t2 =>
                        t2.Equals(t.Schema, StringComparison.CurrentCultureIgnoreCase)))
                    .Cast<ITable>()
                    .ToList();
            }
            else if (Options.ExcludedSchemas?.Any() == true)
            {
                ret = ret
                    .Cast<Table>()
                    .Where(t => !Options.ExcludedSchemas.Any(t2 =>
                        t2.Equals(t.Schema, StringComparison.CurrentCultureIgnoreCase)))
                    .Cast<ITable>()
                    .ToList();
            }

            return ret;
        }
    }
}
