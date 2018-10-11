using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public interface ISequence : IHasDataType
    {
        string Name { get; }
        object StartAt { get; }
        object IncrementsBy { get; }
        object MinValue { get; }
        object MaxValue { get; }
        IDatabaseSchema DatabaseSchema { get; }
    }
}
