using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Pluralize.NET;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator
{
    public abstract class DatabaseGeneratorBase<TSchema, TOptions> : 
        IGenerator<TOptions>, 
        IGeneratorUsingGenerationContext, 
        IGeneratorWithMeta 
        where TOptions : IGeneratorOptions
        where TSchema : IDatabaseSchema
    {
        public virtual TOptions Options { get; set; }
        protected TSchema Schema { get; set; }
        protected List<ITable> TablesToGenerate { get; set; }
        protected List<ISequence> SequenceToGenerate { get; set; }
        protected GenerationContext GenerationContext { get; set; }
        public abstract IDataTypeResolver DataTypeResolver { get; }

        public List<Assembly> DynamicAssemblies { get; set; }

        private List<IResolveTypeInterceptor> ResolveTypesInterceptors { get; set; } = null;

        protected Pluralizer Plurializer { get; } = new Pluralizer();

        public abstract TSchema CreateSchema();

        protected virtual void CleanOutputDir()
        {
            if (!Options.CleanOutputDir)
                return;
            
            if (!string.IsNullOrWhiteSpace(Options.OutputDir) && Directory.Exists(Options.OutputDir))
                EmptyDir(Options.OutputDir);

            if (!string.IsNullOrWhiteSpace(Options.EntitiesInterfacesOutputDir) && Directory.Exists(Options.OutputDir))
                EmptyDir(Options.EntitiesInterfacesOutputDir);

            if (!string.IsNullOrWhiteSpace(Options.EntitiesOutputDir) && Directory.Exists(Options.EntitiesOutputDir))
                EmptyDir(Options.EntitiesOutputDir);

            if (!string.IsNullOrWhiteSpace(Options.ModelsInterfacesOutputDir) && Directory.Exists(Options.ModelsInterfacesOutputDir))
                EmptyDir(Options.ModelsInterfacesOutputDir);

            if (!string.IsNullOrWhiteSpace(Options.ModelsOutputDir) && Directory.Exists(Options.ModelsOutputDir))
                EmptyDir(Options.ModelsOutputDir);

            if (!string.IsNullOrWhiteSpace(Options.ContextOutputDir) && Directory.Exists(Options.ContextOutputDir))
                EmptyDir(Options.ContextOutputDir);
        }

        private void EmptyDir(string dirPath)
        {
            var dir = new DirectoryInfo(dirPath);
            foreach (var fi in dir.GetFiles())
                fi.Delete();

            foreach (var di in dir.GetDirectories())
                di.Delete(true);
        }

        public void Generate()
        {
            DynamicAssemblies = LoadDynamicAssemblies();
            RefreshResolveTypeInterceptors();
            Schema = CreateAndLoadSchema();
            TablesToGenerate = ResolveTablesToGenerate();
            SequenceToGenerate = ResolveSequencesToGenerate();
            GenerationContext = GenerationContext.Create();
            CleanOutputDir();
            GenerateCode();
        }

        protected virtual List<Assembly> LoadDynamicAssemblies()
        {
            var ret = new List<Assembly>();
            Options.DynamicAssemblies?.ForEach(da =>
            {
                var a = Assembly.LoadFile(da);
                DynamicAssemblies.Add(a);
            });
            return ret;
        }

        protected void RefreshResolveTypeInterceptors()
        {
            ResolveTypesInterceptors = new List<IResolveTypeInterceptor>();
            DynamicAssemblies.ForEach(da =>
            {
                ResolveTypesInterceptors.AddRange(da.GetTypes()
                    .Where(t => t.IsClass && typeof(IResolveTypeInterceptor).IsAssignableFrom(t))
                    .Select(t => (IResolveTypeInterceptor)Activator.CreateInstance(t))
                );
            });
        }

        public void LoadOptionsFromJson(string configFile)
        {
            var json = File.ReadAllText(configFile);
            Options = JsonConvert.DeserializeObject<TOptions>(json);
        }

        public virtual List<ISequence> ResolveSequencesToGenerate()
        {
            return Schema.Sequences;
        }

        public virtual string Pluralize(string text)
        {
            var ret = Plurializer.Pluralize(text);
            return ret;
        }

        public virtual string TableNamespace(ITable table)
        {
            var tableNamespace = Options.EntityNamespace ?? Options.Namespace;
            var nsName = ReplaceMetas(tableNamespace, table);
            return nsName;
        }

        public virtual string ModelInterfaceNamespace(ITable table)
        {
            var ns = Options.ModelInterfaceNamespace ?? Options.Namespace;
            var nsName = ReplaceMetas(ns, table);
            return nsName;
        }

        public virtual string ModelNamespace(ITable table)
        {
            var ns = Options.ModelNamespace ?? Options.Namespace;
            var nsName = ReplaceMetas(ns, table);
            return nsName;
        }

        public virtual string ModelClassFullName(ITable table)
        {
            return $"{ModelNamespace(table)}.{ModelClassName(table)}";
        }


        public virtual string ModelExtensionsNamespace(ITable table)
        {
            var ns = Options.ModelExtensionsNamespace ?? Options.Namespace;
            var nsName = ReplaceMetas(ns, table);
            return nsName;
        }

        public virtual string ModelExtensionsClassName(ITable table)
        {
            var ret = $"{table.Name}ModelExtensions";
            return ret;
        }

        public virtual string ModelExtensionsFullClassName(ITable table)
        {
            return $"{ModelExtensionsNamespace(table)}.{ModelExtensionsClassName(table)}";
        }

        public virtual string TableInterfaceNamespace(ITable table)
        {
            var ns = Options.EntityInterfaceNamespace ?? Options.Namespace;
            var nsName = ReplaceMetas(ns, table);
            return nsName;
        }

        public virtual string TableClassName(ITable table)
        {
            var ret = table.Name;
            return ret;
        }

        public virtual string ContextNamespace()
        {
            var ns = Options.ContextNamespace ?? Options.Namespace;
            var metaReplacedNamespace = EmptyMetas(ns);
            return string.Join(".", metaReplacedNamespace.Split('.').Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        public virtual string ContextClassName() => Options.ContextName;
        public virtual string ContextFullClassName() => $"{ContextNamespace()}.{ContextClassName()}";

        protected abstract string CollectionInstanceType();
        public abstract bool HasManyShouldBeVirtual();
        public abstract bool OneToShouldBeVirtual();
        public abstract bool ForeignKeysShouldBeVirtual();
        protected abstract void GenerateManyToMany(ITable table);

        public virtual string EmptyMetas(string text)
        {
            return text.Replace("[CONTEXT]", string.Empty).Replace("[ENTITY]", string.Empty);
        }

        public virtual string ReplaceMetas(string text)
        {
            return text.Replace("[CONTEXT]", ContextFullClassName());
        }

        public virtual string ReplaceMetas(string text, ITable table)
        {
            return ReplaceMetas(text).Replace("[ENTITY]", table.Name);
        }

        public string ModelClassName(ITable table)
        {
            var ret = $"{table.Name}Model{Options.ModelSuffix}";
            return ret;
        }

        string IGeneratorWithMeta.ModelNamespace(ITable table)
        {
            return ModelNamespace(table);
        }

        public string ModelInterfaceName(ITable table)
        {
            var ret = $"I{table.Name}Model{Options.ModelInterfaceSuffix}";
            return ret;
        }

        public string TableInterfaceName(ITable table)
        {
            var ret = TableClassName(table);
            ret = $"I{ret}{Options.InterfaceNameSuffix}";
            return ret;
        }

        public string TableClassFullName(ITable table)
        {
            var ns = TableNamespace(table);
            var cn = TableClassName(table);
            return $"{ns}.{cn}";
        }

        protected virtual string OneToOnePropertyName(IForeignKey fk)
        {
            return fk.ForeignKeyColumn.Table.Name;
        }

        protected virtual string HasManyPropertyName(IForeignKey fk, bool withForeignKeyName = false)
        {
            var prop = fk.ForeignKeyColumn.Table.Name;
            prop = Pluralize(prop);

            if (withForeignKeyName)
                prop = $"{prop}_{fk.ForeignKeyColumn.Name}";

            return prop;
        }

        protected string ForeignKeyPropertyName(IForeignKey fk, bool withForeignKeyName = false)
        {
            var prop = fk.IsOneToOne() ? fk.PrimaryKeyColumn.Table.Name : RemoveIdSuffixFromColumnName(fk.ForeignKeyColumn.Name);

            if (withForeignKeyName)
                prop = $"{prop}_{fk.ForeignKeyColumn.Name}";

            return prop;
        }

        public virtual List<ITable> ResolveTablesToGenerate()
        {
            List<ITable> ret;

            // table name filtering.
            if (Options.IncludedTables?.Any() == true)
                ret = Schema.Tables.Where(table => Options.IncludedTables.Any(table.IsNamed)).ToList();
            else if (Options.ExcludedTables?.Any() == true)
                ret = Schema.Tables.Where(table => !Options.ExcludedTables.Any(table.IsNamed)).ToList();
            else
                ret = Schema.Tables;

            return ret;
        }

        public IGeneratorOptions GetOptions() => Options;
        public abstract IGeneratorOptions GetDefaultOptions();
        public abstract void InitializeOptionsWithDefault();

        protected string RemoveIdSuffixFromColumnName(string columnName)
        {
            var suffix = "Id";
            if (columnName.EndsWith(suffix))
                return columnName.Substring(0, columnName.Length - suffix.Length);
   
            return columnName;
        }

        protected TSchema CreateAndLoadSchema()
        {
            var schema = CreateSchema();
            schema.ConnectionString = Options.ConnectionString;
            schema.LoadSchema();
            return schema;
        }

        protected abstract void GenerateGetNextSequenceLines(MethodBuilder method, string outputType, ISequence sequence);

        protected virtual void GenerateSequenceMethods()
        {
            if (!Options.GenerateContextSequenceMethods)
                return;

            var contextNamespace = ContextNamespace();
            var contextClassName = ContextClassName();
            var contextClass = GenerationContext.FindClass(contextClassName, contextNamespace);

            SequenceToGenerate.ForEach(sequence =>
            {
                var dataType = DataTypeResolver.ResolveType(sequence);
                var outputType = dataType.GetOutputType();

                var methodName = $"GetNextValueFor{sequence.Name}";

                contextClass.Method(m =>
                {
                    m.AccessModifier(AccessModifiers.Public)
                        .ReturnType(outputType)
                        .Name(methodName);

                    GenerateGetNextSequenceLines(m, outputType, sequence);
                });
            });
        }
        protected abstract void GenerateContext();

        protected virtual void BeforeSaveToDisk()
        {

        }

        protected void GenerateCode()
        { 
            GenerateEntities();
            GenerateContext();
            GenerateSequenceMethods();
            EachTableHooks();
            ContextHook();
            BeforeSaveToDisk();
            GenerationContext.SaveToDisk(Encoding.UTF8, normalizeNewLines: true, createDir:true);
        }

        private void ContextHook()
        {
            DynamicAssemblies.ForEach(a =>
            {
                var contextServices = a.GetTypes()
                    .Where(t => t.IsClass && typeof(IContextInterceptor).IsAssignableFrom(t))
                    .Select(t => (IContextInterceptor)Activator.CreateInstance(t))
                    .ToList();

                contextServices.ForEach(c => c.InterceptContext(this));
            });
        }

        private void EachTableHooks()
        {
            DynamicAssemblies.ForEach(a =>
            {
                var eachTableServices = a.GetTypes()
                    .Where(t => t.IsClass && typeof(ITableInterceptor).IsAssignableFrom(t))
                    .Select(t => (ITableInterceptor) Activator.CreateInstance(t))
                    .ToList();

                TablesToGenerate.ForEach(table =>
                {
                    eachTableServices.ForEach(t => t.InterceptTable(this, table));
                });
            });
        }


        protected virtual FileBuilder ResolveEntityFileBuilder(ITable table)
        {
            FileBuilder ret = null;
            var outputDir = Options.EntitiesOutputDir ?? Options.OutputDir;
            var outputFile = Options.EntitiesOutputSingleFileName ?? Options.OutputSingleFileName ?? $"{ModelClassName(table)}.generated.cs";
            GenerationContext.File($"{outputDir}{Path.DirectorySeparatorChar}{outputFile}", fb => ret = fb);
            return ret;
        }

        protected virtual FileBuilder ResolveEntityInterfaceFileBuilder(ITable table)
        {
            FileBuilder ret = null;
            var outputDir = Options.EntitiesInterfacesOutputDir ?? Options.OutputDir;
            var outputFile = Options.EntitiesInterfacesOutputSingleFileName ?? Options.OutputSingleFileName ?? $"{TableInterfaceName(table)}.generated.cs";
            GenerationContext.File($"{outputDir}{Path.DirectorySeparatorChar}{outputFile}", fb => ret = fb);
            return ret;
        }

        protected virtual FileBuilder ResolveModelInterfaceFileBuilder(ITable table)
        {
            FileBuilder ret = null;
            var outputDir = Options.ModelsInterfacesOutputDir ?? Options.OutputDir;
            var outputFile = Options.ModelsInterfacesOutputSingleFileName ?? Options.OutputSingleFileName ?? $"{ModelInterfaceName(table)}.generated.cs";
            GenerationContext.File($"{outputDir}{Path.DirectorySeparatorChar}{outputFile}", fb => ret = fb);
            return ret;
        }


        protected virtual FileBuilder ResolveModelFileBuilder(ITable table)
        {
            FileBuilder ret = null;
            var outputDir = Options.ModelsOutputDir ?? Options.OutputDir;
            var outputFile = Options.ModelsOutputSingleFileName ?? Options.OutputSingleFileName ?? $"{ModelClassName(table)}.generated.cs";
            GenerationContext.File($"{outputDir}{Path.DirectorySeparatorChar}{outputFile}", fb => ret = fb);
            return ret;
        }

        protected virtual FileBuilder ResolveContextFileBuilder()
        {
            FileBuilder ret = null;
            var outputDir = Options.ContextOutputDir ?? Options.OutputDir;
            var outputFile = Options.ContextOutputSingleFileName ?? Options.OutputSingleFileName ?? $"{ContextClassName()}.generated.cs";
            GenerationContext.File($"{outputDir}{Path.DirectorySeparatorChar}{outputFile}", fb => ret = fb);
            return ret;
        }

        protected virtual void GenerateModelInterface(ITable table)
        {
            if (!Options.GenerateModelsInterfaces)
                return;

            var modelInterfaceName = ModelInterfaceName(table);
            var modelInterfaceNamespace = ModelInterfaceNamespace(table);
            var fileBuilder = ResolveModelInterfaceFileBuilder(table);

            fileBuilder.Namespace(modelInterfaceNamespace, true, ns =>
            {
                ns.Interface(modelInterface =>
                {
                    // set basic info.
                    modelInterface.Partial(true).Name(modelInterfaceName);

                    // set properties.
                    table.Columns.ForEach(column =>
                    {
                        modelInterface.Property(columnProperty =>
                        {
                            var typeName = GetColumnTypeName(column, Options.GenerateModelPropertyAsNullable);
                            columnProperty
                                .AccessModifier(AccessModifiers.Omit)
                                .Name(column.Name)
                                .Type(typeName)
                                .Meta(column);
                        });
                    });
                });
            });
        }

        protected void GenerateEntities()
        {
            var tables = TablesToGenerate.ToList();

            tables.ForEach(table =>
            {
                GenerateEntityInterface(table);
                GenerateEntity(table);
                GenerateModelInterface(table);
                GenerateModel(table);
                GenerateModelExtensions(table);
            });

            // generate foreign keys and navigation properties.
            tables.ForEach(table =>
            {
                GenerateForeignKeys(table);
                GenerateOneToOnes(table);
                GenerateHasMany(table);
                GenerateManyToMany(table);
            });
        }

        protected virtual void GenerateModelExtensions(ITable table)
        {
            if (!Options.GenerateModelExtensions)
                return;

            if (!Options.GenerateModelsInterfaces && !Options.GenerateModels)
                throw new Exception("Impossible to generate model extensions because neither GenerateModels or GenerateModelsInterfaces is activated.");

            // full paths to model or interface
            var modelFullClassName = ModelClassFullName(table);
            var modelInterfaceFullName = ModelInterfaceFullName(table);

            // poco.
            var pocoFullInterfaceName = TableInterfaceFullName(table);
            var pocoFullClassName = TableClassFullName(table);

            // poco type.
            var pocoClassName = TableClassName(table);
            var pocoClassNamespace = TableNamespace(table);
            var pocoClass = GenerationContext.FindClass(pocoClassName, pocoClassNamespace);

            var modelExtensionNamespaceName = ModelExtensionsNamespace(table);
            var modelExtensionClassName = ModelExtensionsClassName(table);
            var fileBuilder = ResolveModelExtensionFileBuilder(table);

            fileBuilder.Namespace(modelExtensionNamespaceName, true, ns =>
            {
                ns.Class(c =>
                {
                    // set basic info.
                    c.Partial(true).Static(true).Name(modelExtensionClassName);

                    var finalEntityType = Options.GenerateInterfaces ? pocoFullInterfaceName : pocoFullClassName;
                    var finalModelType = Options.GenerateModelsInterfaces ? modelInterfaceFullName : modelFullClassName;

                    c.Method(m =>
                    {
                        m
                            .AccessModifier(AccessModifiers.Public)
                            .IsStatic(true)
                            .Name("ToModel")
                            .ReturnType("void")
                            .Parameter(p => p.Name("source").Type($"this {finalEntityType}"))
                            .Parameter(p => p.Name("model").Type(finalModelType));

                        table.Columns.ForEach(column =>
                        {
                            m.RawLine($"model.{column.Name} = source.{column.Name}");
                        });
                    });

                    c.Method(m =>
                    {
                        m
                            .AccessModifier(AccessModifiers.Public)
                            .IsStatic(true)
                            .Name("FromModel")
                            .ReturnType("void")
                            .Parameter(p => p.Name("model").Type($"this {finalModelType}"))
                            .Parameter(p => p.Name("destination").Type(finalEntityType))
                            .Parameter(p => p.Name("ignorePrimaryKey").Type("bool").DefaultValue("true"))
                            ;

                        table.Columns.ForEach(column =>
                        {
                            var rawLine = "";
                            bool isPropertyNullable = column.IsNullable || Options.GenerateModelPropertyAsNullable;
                            if (isPropertyNullable && !column.IsNullable)
                            {
                                var matchingProp = pocoClass.FindByMeta<PropertyBuilder>(column);
                                var ternary = TernaryBuilder
                                    .Create()
                                    .RawCondition(rc => rc.Condition($"model.{column.Name} != null"))
                                    .True(RawInlineBuilder.Create(
                                        $"destination.{column.Name} = ({matchingProp.GetTypeName()})model.{column.Name}"))
                                    .False(RawInlineBuilder.Create(
                                        $"destination.{column.Name} = default({matchingProp.GetTypeName()})"));

                                rawLine = $"destination.{column.Name} = {ternary.GenerateInline()}";
                            }
                            else
                            {
                                rawLine = $"destination.{column.Name} = model.{column.Name}";
                            }

                            if (column.IsPrimaryKey)
                            {
                                m.Add(IfBuilder.Create().RawCondition(rc => rc.Condition($"ignorePrimaryKey != true")).Add(RawLineBuilder.Create(rawLine)));
                            }
                            else
                            {
                                m.RawLine(rawLine);
                            }
                        });
                    });
                });
            });
        }

        protected virtual FileBuilder ResolveModelExtensionFileBuilder(ITable table)
        {
            FileBuilder ret = null;
            var outputDir = Options.ModelExtensionsOutputDir ?? Options.OutputDir;
            var outputFile = Options.ModelExtensionsOutputSingleFileName ?? Options.OutputSingleFileName ?? $"{ModelExtensionsClassName(table)}.generated.cs";
            GenerationContext.File($"{outputDir}{Path.DirectorySeparatorChar}{outputFile}", fb => ret = fb);
            return ret;
        }


        protected virtual void GenerateHasMany(ITable table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var hasManyList = table.HasMany().ToList();
            hasManyList.ForEach(fk =>
            {
                if (!TablesToGenerate.Contains(fk.ForeignKeyColumn.Table))
                    return;

                var hasMoreThanOne = table.HasMany().Count(t => t.ForeignKeyColumn.Table == fk.ForeignKeyColumn.Table) > 1;
                var propName = HasManyPropertyName(fk, hasMoreThanOne);

                propName = tableClass.GetUniqueMemberName(propName);
                var pocoType = TableClassFullName(fk.ForeignKeyColumn.Table);
                var propType = $"System.Collections.Generic.ICollection<{pocoType}>";
                var defaultValue = $"new {CollectionInstanceType()}<{pocoType}>()";
                tableClass.Property(p => p.Virtual(HasManyShouldBeVirtual()).Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Has Many").Meta(fk));
            });
        }

        protected virtual void GenerateOneToOnes(ITable table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            table.OneToOne().ToList().ForEach(fk =>
            {
                if (!TablesToGenerate.Contains(fk.ForeignKeyColumn.Table))
                    return;

                var propName = OneToOnePropertyName(fk);
                propName = tableClass.GetUniqueMemberName(propName);
                var propType = TableClassFullName(fk.ForeignKeyColumn.Table);
                tableClass.Property(p => p.Virtual(OneToShouldBeVirtual()).Type(propType).Name(propName).Comment("One to One").Meta(fk));
            });
        }

        protected virtual void GenerateForeignKeys(ITable table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            table.ForeignKeys.ForEach(fk =>
            {
                if (!TablesToGenerate.Contains(fk.PrimaryKeyColumn.Table))
                    return;

                var propName = ForeignKeyPropertyName(fk);
                if (tableClass.HasMemberWithName(propName))
                {
                    var tempPropName = ForeignKeyPropertyName(fk, true);
                    if (!tableClass.HasMemberWithName(tempPropName))
                        propName = tempPropName;
                }

                propName = tableClass.GetUniqueMemberName(propName);
                var foreignKeyTypeName = TableClassFullName(fk.PrimaryKeyColumn.Table);
                tableClass.Property(foreignKeyProp => foreignKeyProp.Virtual(ForeignKeysShouldBeVirtual()).Type(foreignKeyTypeName).Name(propName).Comment("Foreign Key").Meta(fk));
            });
        }

        protected virtual void GenerateEntityInterface(ITable table)
        {
            if (!Options.GenerateInterfaces)
                return;
  
            var tableInterfaceName = TableInterfaceName(table);
            var tableInterfaceNamespace = TableInterfaceNamespace(table);
            var fileBuilder = ResolveEntityInterfaceFileBuilder(table);

            fileBuilder.Namespace(tableInterfaceNamespace, true, ns =>
            {
                ns.Interface(tableInterface =>
                {
                    tableInterface.Partial(true).Name(tableInterfaceName);
                    table.Columns.ForEach(column =>
                    {
                        tableInterface.Property(columnProperty =>
                        {
                            var typeName = GetColumnTypeName(column);
                            columnProperty
                                .Name(column.Name)
                                .AccessModifier(AccessModifiers.Omit)
                                .Type(typeName)
                                .Meta(column);
                        });
                    });
                });
            });
        }

        public virtual string GetColumnTypeName(IColumn column, bool alwaysAsNullable = false)
        {
            var typeInfo = GetColumnTypeInfo(column);
            var typeName = typeInfo.Item1;
            if (typeInfo.Item2 && (column.IsNullable || alwaysAsNullable))
                typeName = $"{typeName}?";
            return typeName;
        }

        public virtual Tuple<string, bool> GetColumnTypeInfo(IColumn column)
        {
            var dynamicAssemblyResolvedType = ResolveDynamicAssemblyType(column);
            if (dynamicAssemblyResolvedType != null)
                return dynamicAssemblyResolvedType;

            var type = DataTypeResolver.ResolveType(column);
            var typeName = type.GetOutputType();
            return new Tuple<string, bool>(typeName, type.IsValueType);
        }

        /// <summary>
        /// Tuple parts are the name of the part ad if the type is a value type (not nullable)
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        private Tuple<string, bool> ResolveDynamicAssemblyType(IColumn column)
        {
            foreach (var rti in ResolveTypesInterceptors)
            {
                var temp = rti.InterceptResolveType(column);
                if (temp != null)
                    return temp;
            }

            return null;
        }

        private void GenerateEntity(ITable table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var fileBuilder = ResolveEntityFileBuilder(table);

            fileBuilder.Namespace(tableNamespace, true, ns =>
            {
                ns.Class(tableClass =>
                {
                    // set basic info.
                    tableClass.Partial(true).Name(tableClassName);

                    if (Options.GenerateInterfaces)
                    {
                        var tableInterfaceFullName = TableInterfaceFullName(table);
                        tableClass.Inherits(tableInterfaceFullName);
                    }

                    // set properties.
                    table.Columns.ForEach(column =>
                    {
                        tableClass.Property(columnProperty =>
                        {
                            var typeName = GetColumnTypeName(column);
                            columnProperty
                                .Name(column.Name)
                                .Type(typeName)
                                .Meta(column);
                        });
                    });
                });
            });
        }

        protected virtual string TableInterfaceFullName(ITable table)
        {
            var tableInterfaceNamespace = TableInterfaceNamespace(table);
            var tableInterfaceName = TableInterfaceName(table);
            var tableInterfaceFullName = $"{tableInterfaceNamespace}.{tableInterfaceName}";
            return tableInterfaceFullName;
        }

        protected virtual void GenerateModel(ITable table)
        {
            if (!Options.GenerateModels)
                return;

            var tableNamespace = TableNamespace(table);
            var modelNamespace = ModelNamespace(table);
            var tableClassName = TableClassName(table);
            var modelClassName = ModelClassName(table);
            var tableClassFullName = TableClassFullName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);
            var fileBuilder = ResolveModelFileBuilder(table);

            fileBuilder.Namespace(modelNamespace, true, ns =>
            {
                ns.Class(modelClass =>
                {
                    // set basic info.
                    modelClass.Partial(true).Name(modelClassName);

                    if (Options.GenerateModelsInterfaces)
                    {
                        var modelInterfaceFullName = ModelInterfaceFullName(table);
                        modelClass.Inherits(modelInterfaceFullName);
                    }

                    Options?.ModelInheritances.ForEach(mi =>
                    {
                        modelClass.Inherits(ReplaceMetas(mi, table));
                    });

                    MethodBuilder from = null;
                    MethodBuilder to = null;
                    if (Options.GenerateModelsFromTo)
                    {

                        modelClass.Method(m =>
                        {
                            m
                                .AccessModifier(AccessModifiers.Public)
                                .Virtual(true)
                                .ReturnType("void")
                                .Name("From")
                                .Parameter(p => p.Type(tableClassFullName).Name("entity"));
                            from = m;
                        });

                        modelClass.Method(m =>
                        {
                            m
                                .AccessModifier(AccessModifiers.Public)
                                .Virtual(true)
                                .ReturnType("void")
                                .Name("To")
                                .Parameter(p => p.Type(tableClassFullName).Name("entity"));
                            to = m;
                        });
                    }

                    // set properties.
                    table.Columns.ForEach(column =>
                    {
                        modelClass.Property(columnProperty =>
                        {
                            var typeName = GetColumnTypeName(column, Options.GenerateModelPropertyAsNullable);
                            columnProperty
                                .Virtual(true)
                                .Name(column.Name)
                                .Type(typeName)
                                .Meta(column);

                            if (Options.GenerateModelsFromTo)
                            {
                                from.RawLine($"{column.Name} = entity.{column.Name}");
                                bool isPropertyNullable = column.IsNullable || Options.GenerateModelPropertyAsNullable;
                                if (isPropertyNullable && !column.IsNullable)
                                {

                                    var matchingProp = tableClass.FindByMeta<PropertyBuilder>(column);
                                    var ternary = TernaryBuilder
                                        .Create()
                                        .RawCondition(rc => rc.Condition($"{column.Name} != null"))
                                        .True(RawInlineBuilder.Create($"entity.{column.Name} = ({matchingProp.GetTypeName()}){column.Name}"))
                                        .False(RawInlineBuilder.Create($"entity.{column.Name} = default({matchingProp.GetTypeName()})"));

                                    to.RawLine($"entity.{column.Name} = {ternary.GenerateInline()}");
                                }
                                else
                                {
                                    to.RawLine($"entity.{column.Name} = {column.Name}");
                                }
                            }
                        });
                    });
                });
            });
        }

        protected virtual string ModelInterfaceFullName(ITable table)
        {
            var modelInterfaceNamespace = ModelInterfaceNamespace(table);
            var modelInterfaceName = ModelInterfaceName(table);
            var modelInterfaceFullName = $"{modelInterfaceNamespace}.{modelInterfaceName}";
            return modelInterfaceFullName;
        }

        public GenerationContext GetGenerationContext()
        {
            return GenerationContext;
        }
    }
}
