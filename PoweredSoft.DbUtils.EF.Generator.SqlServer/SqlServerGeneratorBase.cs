using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        protected string TableInterfaceName(Table table)
        {
            var ret = TableClassName(table);
            ret = $"I{ret}{Options.InterfaceNameSuffix}";
            return ret;
        }

        public string TableClassFullName(Table table)
        {
            var ns = TableNamespace(table);
            var cn = TableClassName(table);
            return $"{ns}.{cn}";
        }

        protected virtual string OneToOnePropertyName(ForeignKey fk)
        {
            return fk.ForeignKeyColumn.Table.Name;
        }

        protected virtual string HasManyPropertyName(ForeignKey fk, bool withForeignKeyName = false)
        {
            var prop = fk.ForeignKeyColumn.Table.Name;
            prop = Pluralize(prop);

            if (withForeignKeyName)
                prop = $"{prop}_{fk.ForeignKeyColumn.Name}";

            return prop;
        }

        protected string ForeignKeyPropertyName(ForeignKey fk, bool withForeignKeyName = false)
        {
            var prop = fk.IsOneToOne() ? fk.PrimaryKeyColumn.Table.Name : RemoveIdSuffixFromColumnName(fk.ForeignKeyColumn.Name);

            if (withForeignKeyName)
                prop = $"{prop}_{fk.ForeignKeyColumn.Name}";

            return prop;
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
