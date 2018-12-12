using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IResolveTypeInterceptor
    {
        /// <summary>
        /// Must return type name and if its a value type.
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        Tuple<string, bool> InterceptResolveType(IColumn column);
    }
}
