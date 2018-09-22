using System;
using System.Collections.Generic;
using System.Linq;
using PoweredSoft.CodeGenerator;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public abstract class SqlServerGeneratorBase<TOptions> : DatabaseGeneratorBase<DatabaseSchema, TOptions>
        where TOptions : SqlServerGeneratorOptionsBase
    {
        public override DatabaseSchema CreateSchema() => new DatabaseSchema();
        protected override IDataTypeResolver DataTypeResolver { get; } = new DataTypeResolver();

        protected string TableNamespace(Table table)
        {
            var nsName = Options.Namespace.Replace("[SCHEMA]", table.Schema);
            return nsName;
        }

        protected string TableClassName(Table table)
        {
            var ret = table.Name;
            return ret;
        }

        public string TableClassFullName(Table table)
        {
            var ns = TableNamespace(table);
            var cn = TableClassName(table);
            return $"{ns}.{cn}";
        }

        protected string ForeignKeyPropertyName(ForeignKey fk)
        {
            if (fk.IsOneToOne())
                return fk.PrimaryKeyColumn.Table.Name;

            return RemoveIdSuffixFromColumnName(fk.ForeignKeyColumn.Name);
        }

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
