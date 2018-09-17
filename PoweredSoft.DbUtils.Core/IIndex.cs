using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public interface IIndex
    {
        string Name { get; }
        ITable Table { get; }
        List<IColumn> Columns { get; }
        bool IsUnique { get; }
    }
}
