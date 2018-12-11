using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.CodeGenerator;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IEachTableService
    {
        void OnTable(IGenerator generator, ITable table);
    }
}
