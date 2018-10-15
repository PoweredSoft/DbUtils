using System.Linq;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.Schema.MySql
{
    public class Column : IColumnWithDateTimePrecision
    {
        public Table MySqlTable { get; set; }
        public ITable Table => MySqlTable;

        public string Name { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public int? CharacterMaximumLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
        public int? DateTimePrecision { get; set; }
        public bool IsAutoIncrement { get; set; }
        public bool IsPrimaryKey { get; set; } = false;
        public int PrimaryKeyOrder { get; set; }
        public bool IsForeignKey => Table.ForeignKeys.Any(t => t.ForeignKeyColumn.Name == Name);
        public bool IsNullable { get; set; }

        public override string ToString()
        {
            var ret = $"{Table}.`{Name}`";
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
