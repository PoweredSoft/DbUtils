using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IDataTypeResolver
    {
        Type ResolveType(IHasDataType meta);
        bool IsFixLength(IHasDataType meta);
        bool IsString(IHasDataType meta);
        bool IsUnicode(IHasDataType meta);
        bool NeedFluentPrecisionSpecification(IHasDataType meta);
    }
}
