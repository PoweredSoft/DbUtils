using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.CodeGenerator;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IContextService
    {
        void OnContext(IGenerator generator);
    }
}
