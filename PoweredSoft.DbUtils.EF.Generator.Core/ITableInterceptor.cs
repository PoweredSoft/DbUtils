using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.CodeGenerator;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface ITableInterceptor
    {
        void InterceptTable(IGenerator generator, ITable table);
    }
}
