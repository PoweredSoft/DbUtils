using System.Collections.Generic;
using PoweredSoft.CodeGenerator;
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
        IDataTypeResolver DataTypeResolver { get; }
        void InitializeOptionsWithDefault();
    }

    public interface IGenerator<TOptions> : IGenerator
        where TOptions : IGeneratorOptions
    {
        TOptions Options { get; }
    }

    public interface IGeneratorUsingGenerationContext : IGenerator
    {
        GenerationContext GetGenerationContext();
    }

    public interface IGeneratorWithMeta : IGenerator
    {
        string ContextClassName();
        string ContextFullClassName();
        string ContextNamespace();
        string EmptyMetas(string text);
        string ModelClassName(ITable table);
        string ModelNamespace(ITable table);
        string ModelClassFullName(ITable table);
        string ModelInterfaceName(ITable table);
        string Pluralize(string text);
        string ReplaceMetas(string text);
        string TableClassFullName(ITable table);
        string TableClassName(ITable table);
        string TableInterfaceName(ITable table);
        string TableInterfaceNamespace(ITable table);
        string TableNamespace(ITable table);
        string ModelInterfaceNamespace(ITable table);
    }


}
