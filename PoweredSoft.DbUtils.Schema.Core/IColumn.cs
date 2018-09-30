using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public interface IColumn
    {
        string Name { get; }
        string DataType { get;  }
        string DefaultValue { get; }
        int? CharacterMaximumLength { get; }
        int? NumericPrecision { get; }
        int? NumericScale { get; }
        bool IsAutoIncrement { get; }
        bool IsPrimaryKey { get; }
        int PrimaryKeyOrder { get; set; }
        bool IsForeignKey { get; }
        bool IsNullable { get; }
        ITable Table { get; }
    }
}
