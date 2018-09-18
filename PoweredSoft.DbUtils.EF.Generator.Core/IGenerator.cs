using System.Collections.Generic;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IGenerator
    {
        void Generate();
        List<ITable> ResolveTablesToGenerate();
    }

    public interface IGenerator<TOptions> : IGenerator
        where TOptions : IGeneratorOptions
    {
        TOptions Options { get; }
    }
}
