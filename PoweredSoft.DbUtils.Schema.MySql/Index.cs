using System.Collections.Generic;
using System.Linq;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.Schema.MySql
{
    public class Index : IIndex
    {
        public string Name { get; set; }
        public ITable Table => MySqlTable;
        public List<IColumn> Columns => MySqlColumns.Cast<IColumn>().ToList();
        public bool IsUnique { get; set;  }
        public string FilterDefinition { get; set; }

        public Table MySqlTable { get; set; }
        public List<Column> MySqlColumns { get; set; } = new List<Column>();

        public override string ToString()
        {
            var ret = $"{Name} | Is Unique: {IsUnique} | Columns: {string.Join(",", MySqlColumns.Select(t => t.Name))}";
            return ret;
        }
    }
}
