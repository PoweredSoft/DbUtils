using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public static class Extensions
    {
        public static bool IsHasOne(this IForeignKey fk)
        {
            var primaryKeyCount = fk.ForeignKeyColumn.Table.Columns.Count(t => t.IsPrimaryKey);
            var fkIsPk = fk.ForeignKeyColumn.IsPrimaryKey;
            return primaryKeyCount == 1 && fkIsPk;
        }

        public static bool IsManyToMany(this IForeignKey fk)
        {
            var colCount = fk.ForeignKeyColumn.Table.Columns.Count;
            var allColumnsPkFks = fk.ForeignKeyColumn.Table.Columns.All(t => t.IsPrimaryKey && t.IsForeignKey);
            return colCount == 2 && allColumnsPkFks;
        }

        public static bool IsHasMany(this IForeignKey fk)
        {
            var isManyToMany = fk.IsManyToMany();
            var isHasOne = fk.IsHasOne();
            return !isHasOne && !isManyToMany;
        }

        public static bool IsManyToMany(this ITable table)
        {
            return table.ForeignKeys.Any() && table.ForeignKeys[0].IsManyToMany();
        }

        public static IEnumerable<IForeignKey> ReverseNavigations(this ITable table)
        {
            var dbSchema = table.DatabaseSchema;
            var q = dbSchema.Tables
                .SelectMany(t => t.ForeignKeys)
                .Where(t => t.PrimaryKeyColumn.Table == table);
            return q;
        }

        public static IEnumerable<IForeignKey> HasOne(this ITable table)
        {
            return table.ReverseNavigations().Where(t => t.IsHasOne());
        }

        public static IEnumerable<IForeignKey> HasMany(this ITable table)
        {
            return table.ReverseNavigations().Where(t => t.IsHasMany());
        }

        public static IEnumerable<IForeignKey> ManyToMany(this ITable table)
        {
            return table.ReverseNavigations().Where(t => t.IsManyToMany());
        }
    }
}
