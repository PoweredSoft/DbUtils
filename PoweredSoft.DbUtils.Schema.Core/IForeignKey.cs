using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public interface IForeignKey
    {
        string Name { get; }
        IColumn ForeignKeyColumn { get; }
        IColumn PrimaryKeyColumn { get; }
        string DeleteCascadeAction { get; }
        string UpdateCascadeAction { get; }
    }
}
