using System.Collections.Generic;
using PoweredSoft.DbUtils.EF.Generator.Core;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.Core
{
    public interface ISqlServerGeneratorOptions : IGeneratorOptions
    {
        List<string> IncludedSchemas { get; }
        List<string> ExcludedSchemas { get; }
    }
}
