using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public interface ITable
    {
        string Name { get; }
        List<IColumn> Columns { get; }
        List<IIndex> Indexes { get; }
        List<IForeignKey> ForeignKeys { get; }

        string TableDump();
    }
}
