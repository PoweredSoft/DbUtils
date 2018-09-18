using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.Schema.SqlServer
{
    public class Table : ITable
    {
        public string Name { get; set; }
        public string Schema { get; set; }
        public List<IColumn> Columns => SqlServerColumns.Cast<IColumn>().ToList();
        public List<IIndex> Indexes => SqlServerIndexes.Cast<IIndex>().ToList();
        public List<IForeignKey> ForeignKeys => SqlServerForeignKeys.Cast<IForeignKey>().ToList();
        public IDatabaseSchema DatabaseSchema { get; set; }

        public List<Column> SqlServerColumns { get; set; } = new List<Column>();
        public List<ForeignKey> SqlServerForeignKeys { get; set; } = new List<ForeignKey>();
        public List<Index> SqlServerIndexes { get; set; } = new List<Index>();

        public override string ToString()
        {
            return $"[{Schema}].[{Name}]";
        }

        public string TableDump()
        {
            var ret = $"{this}";
            ret += "\nColumns\n";
            ret += string.Join("\n", SqlServerColumns.Select(t => $"{t}"));
            ret += "\nForeign Keys\n";
            ret += string.Join("\n", SqlServerForeignKeys.Select(t => $"{t}"));
            ret += "\nIndexes\n";
            ret += string.Join("\n", SqlServerIndexes.Select(t => $"{t}"));
            return ret;
        }

        public bool IsNamed(string name)
        {
            var tableFullName = $"{Schema}.{Name}";
            var tableShortName = $"{Name}";

            if (name.Contains("."))
                return name.Equals(tableFullName, StringComparison.InvariantCultureIgnoreCase);

            return name.Equals(tableShortName, StringComparison.InvariantCultureIgnoreCase);
        }

#if DEBUG
        public string Debug => TableDump();
        #endif
    }
}
