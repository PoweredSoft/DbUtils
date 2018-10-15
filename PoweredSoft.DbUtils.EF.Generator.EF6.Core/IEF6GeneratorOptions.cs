using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.EF.Generator.Core;

namespace PoweredSoft.DbUtils.EF.Generator.EF6.Core
{
    public interface IEF6GeneratorOptions : IGeneratorOptions
    {
        string ConnectionStringName { get; set; }
        string FluentConfigurationClassSuffix { get; set; }
    }
}
