using PoweredSoft.DbUtils.Schema.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.SqlServer
{
    public class Column : IColumn
    {
        public Table SqlServerTable { get; set; }
        public ITable Table => SqlServerTable;

        public string Name { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public int? CharacterMaximumLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
        public int? DateTimePrecision { get; set; }
        public bool IsAutoIncrement { get; set; }
        public bool IsPrimaryKey { get; set; } = false;
        public bool IsForeignKey => Table.ForeignKeys.Any(t => t.ForeignKeyColumn.Name == Name);
        public bool IsNullable { get; set; }

        public Type ResolveDotNetDataType()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            var ret = $"{Table}.[{Name}]";
            ret += $" {DataType} ";
            
            // possible data precision
            ret += $"{(CharacterMaximumLength.HasValue ? $"({CharacterMaximumLength})" : "")}";
            ret += $"{(NumericPrecision.HasValue ? $"({NumericPrecision},{NumericScale})" : "")}";
            ret += $"{(DateTimePrecision.HasValue ? $"({DateTimePrecision})" : "")}";
            ret = ret.TrimEnd();

            ret += $" {(IsNullable ? "NULL" : "NOT NULL")} {(IsPrimaryKey ? "PRIMARY KEY" : "")} {(IsAutoIncrement ? "IDENTITY" : "")} ";
            return ret.TrimEnd();
        }
    }
}
