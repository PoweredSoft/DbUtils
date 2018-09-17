using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.Schema.SqlServer
{
    public class Index : IIndex
    {
        public string Name { get; set; }
        public ITable Table => SqlServerTable;
        public List<IColumn> Columns => SqlServerColumns.Cast<IColumn>().ToList();
        public bool IsUnique { get; set;  }

        public Table SqlServerTable { get; set; }
        public List<Column> SqlServerColumns { get; set; } = new List<Column>();
        public List<Column> SqlServerIncludedColumns { get; set; } = new List<Column>();

        public override string ToString()
        {
            var ret = $"{Name} | Is Unique: {IsUnique} | Columns: {string.Join(",", SqlServerColumns.Select(t => $"{t}"))} | Included Columns: {string.Join(",", SqlServerIncludedColumns.Select(t => $"{t}"))}";
            return ret;
        }
    }
}
