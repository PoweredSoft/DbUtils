using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public interface IHasDataType
    {
        string DataType { get; }
        int? NumericPrecision { get; }
        int? NumericScale { get; }
    }
}
