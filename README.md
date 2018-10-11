# Still In Development

This project is still under development, unit tests are to come later on.

# Database Schema Discovering

## Core

Only basic interfaces that should be implemented by all architectures

The discover tool supports:
- Tables
- Columns
- Indexes
- Foreign Keys
- Primary Keys 
- Sequences

Also has extension methods for navigations, reverse navigations 
- Has One (Foreign keys)
- Many to Many
- One to one
- Has Many 

## Sql Server
You may use the project PoweredSoft.DbUtils.Schema.SqlServer

```csharp
var schema = new DatabaseSchema
{
    ConnectionString = "Your connection string."
};

schema.LoadSchema();

// what kind of meta data you have after, to be documented later on.
var tables = schema.Tables;
var sequences = schema.Sequences;
var firstTableColumns = tables.FirstOrDefault()?.Columns;

```

# Generators

> the generator will replace [SCHEMA] [ENTITY] [CONTEXT] by their respective context

All generators options will support those generic options.

```csharp
public interface IGeneratorOptions
{
    // table names to exclude
    List<string> ExcludedTables { get; }
    // table names to include
    List<string> IncludedTables { get; }
    // namespace to use
    string Namespace { get; set; }
    // context class name
    string ContextName { get; }
    // context base class name
    string ContextBaseClassName { get; }
    // connection string to use
    string ConnectionString { get; }
    // the output directory
    string OutputDir { get; }
    // should it clean the output dir
    bool CleanOutputDir { get; }
    // should it output to a single file.
    bool OutputToSingleFile { get; }
    // single output file path
    string OutputSingleFileName { get; }
    // should the context generate sequence methods
    bool GenerateContextSequenceMethods { get; }
    // should it generate interfaces for entities
    bool GenerateInterfaces { get;}
    // suffix for your interface
    string InterfaceNameSuffix { get; }
    // should it generate models.
    bool GenerateModels { get; }
    // should it generate model interfaces
    bool GenerateModelsInterfaces { get; }
    // should properties of your model all be nullable
    // (practical for json serialization)
    bool GenerateModelPropertyAsNullable { get; }
    // suffix for your model calss name
    string ModelSuffix { get; }
    // suffix for your model interface class name
    string ModelInterfaceSuffix { get; }
    // what should your model inherit.
    List<string> ModelInheritances { get; }
}
```

## Entity Framework 6.x generator

### Sample:

```csharp
var g = new SqlServerGenerator();
g.Options = new SqlServerGeneratorOptions
{
    // the folder to generate to
    OutputDir = @"C:\test", 
    // the single to generate to, comment if you want multiple files
    OutputSingleFileName = "All.generated.cs", 
    // should it clean the outputdir folder (WATCH OUT IT delets everything inside the dir)
    CleanOutputDir = true,
    // the namespace to use (SCHEMA will be replaced by the tables schema)
    Namespace = "Acme.[SCHEMA].Dal",
    // the context class name
    ContextName = "AcmeContext",
    // connection string to use    
    ConnectionString = "Your connection string",
    // should it generate interfaces for your entity
    GenerateInterfaces = true,
    // should it generate models for your entity.
    GenerateModels = true,
    // should it generate nullable properties for your models
    GenerateModelPropertyAsNullable = true,
    // should it generate interface for your models.
    GenerateModelsInterfaces = true,
    // should it generate methods to get the next sequence.
    GenerateContextSequenceMethods = true,
    // what should the model inherit 
    ModelInheritances = new List<string>()
    {
        //"ITestInherit<[ENTITY], [CONTEXT]>"
    }
};
g.Generate();
```

## Entity Framework core generator

### Sample:

```csharp
var g = new SqlServerGenerator();
g.Options = new SqlServerGeneratorOptions
{
    // the folder to generate to
    OutputDir = @"C:\test", 
    // the single to generate to, comment if you want multiple files
    OutputSingleFileName = "All.generated.cs", 
    // should it clean the outputdir folder (WATCH OUT IT delets everything inside the dir)
    CleanOutputDir = true,
    // the namespace to use (SCHEMA will be replaced by the tables schema)
    Namespace = "Acme.[SCHEMA].Dal",
    // the context class name
    ContextName = "AcmeContext",
    // connection string to use    
    ConnectionString = "Your connection string",
    // should it generate interfaces for your entity
    GenerateInterfaces = true,
    // should it generate models for your entity.
    GenerateModels = true,
    // should it generate nullable properties for your models
    GenerateModelPropertyAsNullable = true,
    // should it generate interface for your models.
    GenerateModelsInterfaces = true,
    // should it generate methods to get the next sequence.
    GenerateContextSequenceMethods = true,
    // should it add the connection string in the constructor 
    // (not recommended, but good for quick prototyping)
    AddConnectionStringOnGenerate = false,
    // what should the model inherit 
    ModelInheritances = new List<string>()
    {
        //"ITestInherit<[ENTITY], [CONTEXT]>"
    }
};
g.Generate();
```