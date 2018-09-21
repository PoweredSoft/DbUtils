using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IDataTypeResolver
    {
        Type ResolveType(IColumn column);
    }
}
