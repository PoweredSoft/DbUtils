# Generators

## Meta data replacement 
> the generator will replace [SCHEMA] [ENTITY] [CONTEXT] by their respective context

## using the CLI to generate your EF Context

### Command Line 

#### Installing

> dotnet tool install --global psdb

You can then run 
> psdb help

#### init
> will create your configuration file 

```
 init
      --config-file          Is optional (default <GeneratorOptions.json>).
      --context-name         Is optional.
      --connection-string    Is optional.
      --output-dir           Is optional.
      --output-file          Is optional.
```

> here is how the options looks like

```js
{
  // for EF6 suffix of the fluent
  "FluentConfigurationClassSuffix": "FluentConfiguration", 
  // base class to inherit
  "ContextBaseClassName": "System.Data.Entity.DbContext",
  // the name of the connection string to generate for EF6
  "ConnectionStringName": null,
  // excluded tables 
  "ExcludedTables": [
    "dbo.sysdiagrams" // example we are exlcuding diagrams table.
  ],
  // included tables (using this will only generate tables in this list.)
  "IncludedTables": [],
  // namespace to use during generation supports meta data replacement read higher, if you missed that information
  "Namespace": null,
  // the context class name
  "ContextName": null,
  // the connection string to use to parse the database schema
  "ConnectionString": null,
  // the output directory
  "OutputDir": null,
  // should we clean the output directory before generating (watch out with this.)
  "CleanOutputDir": false,
  // should the generator create method for your sequences
  "GenerateContextSequenceMethods": false,
  // set this if you want all the code generated in a single file (ONLY FILE name, ex: all.generated.cs)
  "OutputSingleFileName": null,
  // should it generate interfaces for your entities
  "GenerateInterfaces": false,
  // suffix name for the interfaces of your pocos
  "InterfaceNameSuffix": null
  // should it generate interfaces for the models of your entity
  "GenerateModelsInterfaces": false,
  // should it generate models
  "GenerateModels": false,
  // should the models property all be nullables.
  "GenerateModelPropertyAsNullable": false,
  // the suffix of your model class name
  "ModelSuffix": "Base",
  // the suffix of the interface name for generated model
  "ModelInterfaceSuffix": "",
  // the inheritance of generated models, supports meta data replacements
  "ModelInheritances": [],
  // included schemas if set only tables in included schema will be generated
  "IncludedSchemas": [],
  // excluded schema, if set will ignore certain schemas.
  "ExcludedSchemas": []
}
```

### generate

> will generate the code

```
generate
      --config-file          Is optional (default <GeneratorOptions.json>).
```


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