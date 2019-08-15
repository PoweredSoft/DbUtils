using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.EF.Generator.SqlServer.Core;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;
using PoweredSoft.DbUtils.Schema.SqlServer.Extensions;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public static class Extensions
    {
        public static string EmptyMetas(this string text) => text.Replace("[SCHEMA]", string.Empty);
        public static string ReplaceMetas(this string text, Table table) => text.Replace("[SCHEMA]", table.Schema);

        public static bool MatchMappingColumnType(this ColumnTypeMapping mapping, IColumn column)
        {
            var tableNameMatches = mapping.Table?.Equals(column.Table.Name, StringComparison.InvariantCultureIgnoreCase) == true;
            var columnNameMatches = mapping.Column?.Equals(column.Name, StringComparison.InvariantCultureIgnoreCase) == true;
            bool schemaMatches = true;

            // if its a sql server column type mapping and it matches the schema.
            if (mapping is SqlServerColumnTypeMapping sqlMapping && !string.IsNullOrWhiteSpace(sqlMapping.Schema))
                schemaMatches = sqlMapping.Schema.Equals(column.Table.GetTableSchema(), StringComparison.InvariantCultureIgnoreCase);

            return schemaMatches && tableNameMatches && columnNameMatches;
        }

        public static List<ISequence> ShouldGenerate(this List<ISequence> sequences, ISqlServerGeneratorOptions options)
        {
            if (options.IncludedSchemas?.Any() == true)
            {
                sequences = sequences
                    .Cast<Sequence>()
                    .Where(t => options.IncludedSchemas.Any(t2 => t2.Equals(t.Schema, StringComparison.InvariantCultureIgnoreCase)))
                    .Cast<ISequence>()
                    .ToList();
            }
            else if (options.ExcludedSchemas?.Any() == true)
            {
                sequences = sequences
                    .Cast<Sequence>()
                    .Where(t => !options.ExcludedSchemas.Any(t2 => t2.Equals(t.Schema, StringComparison.InvariantCultureIgnoreCase)))
                    .Cast<ISequence>()
                    .ToList();
            }

            return sequences;
        }

        public static List<ITable> ShouldGenerate(this List<ITable> tables, ISqlServerGeneratorOptions options)
        {
            if (options.IncludedSchemas?.Any() == true)
            {
                tables = tables
                    .Cast<Table>()
                    .Where(t => options.IncludedSchemas.Any(t2 => t2.Equals(t.Schema, StringComparison.InvariantCultureIgnoreCase)))
                    .Cast<ITable>()
                    .ToList();
            }
            else if (options.ExcludedSchemas?.Any() == true)
            {
                tables = tables
                    .Cast<Table>()
                    .Where(t => !options.ExcludedSchemas.Any(t2 => t2.Equals(t.Schema, StringComparison.InvariantCultureIgnoreCase)))
                    .Cast<ITable>()
                    .ToList();
            }

            return tables;
        }
    }
}
