using System;
using System.Collections.Generic;
using System.Linq;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.Schema.MySql
{
    public class Table : ITable
    {
        public string Name { get; set; }
        public List<IColumn> Columns => MySqlColumns.Cast<IColumn>().ToList();
        public List<IIndex> Indexes => MySqlIndexes.Cast<IIndex>().ToList();
        public List<IForeignKey> ForeignKeys => MySqlForeignKeys.Cast<IForeignKey>().ToList();
        public IDatabaseSchema DatabaseSchema { get; set; }

        public List<Column> MySqlColumns { get; set; } = new List<Column>();
        public List<ForeignKey> MySqlForeignKeys { get; set; } = new List<ForeignKey>();
        public List<Index> MySqlIndexes { get; set; } = new List<Index>();

        public override string ToString()
        {
            return $"`{Name}`";
        }

        public string TableDump()
        {
            var ret = $"{this}";
            ret += "\nColumns\n";
            ret += string.Join("\n", MySqlColumns.Select(t => $"{t}"));
            ret += "\nForeign Keys\n";
            ret += string.Join("\n", MySqlForeignKeys.Select(t => $"{t}"));
            ret += "\nIndexes\n";
            ret += string.Join("\n", MySqlIndexes.Select(t => $"{t}"));
            return ret;
        }

        public bool IsNamed(string name)
        {
            return name == Name;
        }

#if DEBUG
        public string Debug => TableDump();
        #endif
    }
}
