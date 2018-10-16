using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.EF.Generator.Core;

namespace PoweredSoft.DbUtils.EF.Generator.EFCore.Core
{
    public interface IEFCoreGeneratorOptions : IGeneratorOptions
    {
        bool AddConnectionStringOnGenerate { get; set; } 
    }
}
