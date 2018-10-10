using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IDataTypeResolver
    {
        Type ResolveType(IColumn column);
        bool IsFixLength(IColumn column);
        bool IsString(IColumn column);
        bool IsUnicode(IColumn column);
        bool NeedFluentPrecisionSpecification(IColumn column);
    }
}
