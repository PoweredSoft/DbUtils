using System.Collections.Generic;

namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public interface IGeneratorOptions
    {
        List<string> ExcludedTables { get; set; }
        List<string> IncludedTables { get; set; }
        string Namespace { get; set; }
        string ContextName { get; set; }
        string ContextBaseClassName { get; set; }
        string ConnectionString { get; set; }
        string OutputDir { get; set; }
        bool CleanOutputDir { get; set; }
        bool GenerateContextSequenceMethods { get; set; }
        string OutputSingleFileName { get; set; }
        bool GenerateInterfaces { get; set; }
        string InterfaceNameSuffix { get; set; }
        bool GenerateModels { get; set; }
        bool GenerateModelsFromTo { get; set; }
        bool GenerateModelExtensions { get; set; }
        bool GenerateModelsInterfaces { get; set; }
        bool GenerateModelPropertyAsNullable { get; set; }
        string ModelSuffix { get; set; }
        string ModelInterfaceSuffix { get; set; }
        List<string> ModelInheritances { get; set; }
        string Version { get; }
        string Engine { get; }

        // namespace overrides.
        string EntityNamespace { get; set; }
        string EntityInterfaceNamespace { get; set; }
        string ModelInterfaceNamespace { get; set; }
        string ModelNamespace { get; set; }
        string ModelExtensionsNamespace { get; set; }
        string ContextNamespace { get; set; }

        // file overrides.
        string EntitiesOutputSingleFileName { get; set; }
        string EntitiesInterfacesOutputSingleFileName { get; set; }
        string ModelsInterfacesOutputSingleFileName { get; set; }
        string ModelsOutputSingleFileName { get; set; }
        string ContextOutputSingleFileName { get; set; }
        string ModelExtensionsOutputSingleFileName { get; set; }

        // output dir overrides.
        string EntitiesOutputDir { get; set; }
        string EntitiesInterfacesOutputDir { get; set; }
        string ModelsInterfacesOutputDir { get; set; }
        string ModelExtensionsOutputDir { get; set; }
        string ModelsOutputDir { get; set; }
        string ContextOutputDir { get; set; }

        List<string> DynamicAssemblies { get; set; }
    }
}
