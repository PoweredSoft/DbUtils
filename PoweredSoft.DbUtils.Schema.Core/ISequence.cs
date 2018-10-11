using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public interface ISequence
    {
        string Name { get; }
        string DataType { get; }
        object StartAt { get; }
        object IncrementsBy { get; }
        int? NumericPrecision { get; }
        int? NumericScale { get; }
        object MinValue { get; }
        object MaxValue { get; }
        IDatabaseSchema DatabaseSchema { get; }
    }
}
