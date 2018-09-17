using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.SqlServer.Models
{
    internal class SqlServerIndexModel
    {
        public string TableSchemaName { get; set; }
        public string TableName { get; set; }
        public string IndexName { get; set; }
        public string ColumnName { get; set; }
        public bool IsIncludedColumn { get; set; }
        public bool IsUniqueConstraint { get; set; }
        public bool HasFilter { get; set; }
        public string FilterDefinition { get; set; }
    }
}
