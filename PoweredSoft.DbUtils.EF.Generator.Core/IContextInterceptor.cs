using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.CodeGenerator;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IContextInterceptor
    {
        void InterceptContext(IGenerator generator);
    }
}
