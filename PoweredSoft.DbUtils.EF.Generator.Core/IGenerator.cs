using System.Collections.Generic;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IGenerator
    {
        void Generate();
        void LoadOptionsFromJson(string configFile);
        List<ITable> ResolveTablesToGenerate();
        List<ISequence> ResolveSequencesToGenerate();
        IGeneratorOptions GetOptions();
        IGeneratorOptions GetDefaultOptions();
        void InitializeOptionsWithDefault();
    }

    public interface IGenerator<TOptions> : IGenerator
        where TOptions : IGeneratorOptions
    {
        TOptions Options { get; }
    }
}
