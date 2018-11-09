using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Pluralize.NET;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator
{
    public abstract class DatabaseGeneratorBase<TSchema, TOptions> : IGenerator<TOptions>
        where TOptions : IGeneratorOptions
        where TSchema : IDatabaseSchema
    {
        public virtual TOptions Options { get; set; }
        protected TSchema Schema { get; set; }
        protected List<ITable> TablesToGenerate { get; set; }
        protected List<ISequence> SequenceToGenerate { get; set; }
        protected GenerationContext GenerationContext { get; set; }
        protected abstract IDataTypeResolver DataTypeResolver { get; }

        protected Pluralizer Plurializer { get; } = new Pluralizer();

        public abstract TSchema CreateSchema();

        protected virtual void CleanOutputDir()
        {
            if (!Options.CleanOutputDir)
                return;
            
            var dir = new DirectoryInfo(Options.OutputDir);
            foreach (var fi in dir.GetFiles())
                fi.Delete();

            foreach (var di in dir.GetDirectories())
                di.Delete(true);
        }

        public void Generate()
        {
            Schema = CreateAndLoadSchema();
            TablesToGenerate = ResolveTablesToGenerate();
            SequenceToGenerate = ResolveSequencesToGenerate();
            GenerationContext = GenerationContext.Create();
            CleanOutputDir();
            GenerateCode();
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

        protected virtual string Pluralize(string text)
        {
            var ret = Plurializer.Pluralize(text);
            return ret;
        }

        protected virtual string TableNamespace(ITable table)
        {
            var nsName = ReplaceMetas(Options.Namespace, table);
            return nsName;
        }

        protected virtual string TableClassName(ITable table)
        {
            var ret = table.Name;
            return ret;
        }

        protected virtual string ContextNamespace()
        {
            var metaReplacedNamespace = EmptyMetas(Options.Namespace);
            return string.Join(".", metaReplacedNamespace.Split('.').Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        protected virtual string ContextClassName() => Options.ContextName;
        protected virtual string ContextFullClassName() => $"{ContextNamespace()}.{ContextClassName()}";

        protected abstract string CollectionInstanceType();
        public abstract bool HasManyShouldBeVirtual();
        public abstract bool OneToShouldBeVirtual();
        public abstract bool ForeignKeysShouldBeVirtual();
        protected abstract void GenerateManyToMany(ITable table);

        protected virtual string EmptyMetas(string text)
        {
            return text.Replace("[CONTEXT]", string.Empty).Replace("[ENTITY]", string.Empty);
        }

        protected virtual string ReplaceMetas(string text)
        {
            return text.Replace("[CONTEXT]", ContextFullClassName());
        }

        protected virtual string ReplaceMetas(string text, ITable table)
        {
            return ReplaceMetas(text).Replace("[ENTITY]", table.Name);
        }

        protected string ModelClassName(ITable table, bool includeSuffix = true)
        {
            var ret = $"{table.Name}Model{(includeSuffix ? Options.ModelSuffix : "")}";
            return ret;
        }

        protected string ModelInterfaceName(ITable table)
        {
            var ret = ModelClassName(table, false);
            ret = $"I{ret}{Options.ModelInterfaceSuffix}";
            return ret;
        }

        protected string TableInterfaceName(ITable table)
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
            BeforeSaveToDisk();
            GenerationContext.SaveToDisk(Encoding.UTF8);
        }

        protected virtual void GenerateModelInterface(ITable table, FileBuilder fileBuilder)
        {
            if (!Options.GenerateModelsInterfaces)
                return;

            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var modelInterfaceName = ModelInterfaceName(table);
            var tableClassFullName = TableClassFullName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);


            if (!Options.OutputToSingleFile)
            {
                var filePath = $"{Options.OutputDir}{Path.DirectorySeparatorChar}{modelInterfaceName}.generated.cs";
                fileBuilder.Path(filePath);
            }

            fileBuilder.Namespace(tableNamespace, true, ns =>
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
                            var type = DataTypeResolver.ResolveType(column);
                            var typeName = type.GetOutputType();
                            bool isPropertyNullable = column.IsNullable || Options.GenerateModelPropertyAsNullable;
                            if (type.IsValueType && isPropertyNullable)
                                typeName = $"{typeName}?";

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
                if (Options.OutputToSingleFile)
                {
                    GenerationContext.SingleFile(fb =>
                    {
                        var filePath = $"{Options.OutputDir}{Path.DirectorySeparatorChar}{Options.OutputSingleFileName}";
                        fb.Path(filePath);

                        GenerateEntityInterface(table, fb);
                        GenerateEntity(table, fb);
                        GenerateModelInterface(table, fb);
                        GenerateModel(table, fb);
                    });
                }
                else
                {
                    GenerationContext
                        .FileIfPathIsSet(fb => GenerateEntityInterface(table, fb))
                        .FileIfPathIsSet(fb => GenerateEntity(table, fb))
                        .FileIfPathIsSet(fb => GenerateModelInterface(table, fb))
                        .FileIfPathIsSet(fb => GenerateModel(table, fb));
                }
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

        protected virtual void GenerateEntityInterface(ITable table, FileBuilder fileBuilder)
        {
            if (!Options.GenerateInterfaces)
                return;

            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableInterfaceName = TableInterfaceName(table);

            if (!Options.OutputToSingleFile)
            {
                var filePath = $"{Options.OutputDir}{Path.DirectorySeparatorChar}{tableInterfaceName}.generated.cs";
                fileBuilder.Path(filePath);
            }

            fileBuilder.Namespace(tableNamespace, true, ns =>
            {
                ns.Interface(tableInterface =>
                {
                    tableInterface.Partial(true).Name(tableInterfaceName);
                    table.Columns.ForEach(column =>
                    {
                        tableInterface.Property(columnProperty =>
                        {
                            var type = DataTypeResolver.ResolveType(column);
                            var typeName = type.GetOutputType();
                            if (type.IsValueType && column.IsNullable)
                                typeName = $"{typeName}?";

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

        private void GenerateEntity(ITable table, FileBuilder fileBuilder)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableInterfaceName = TableInterfaceName(table);

            if (!Options.OutputToSingleFile)
            {
                var filePath = $"{Options.OutputDir}{Path.DirectorySeparatorChar}{tableClassName}.generated.cs";
                fileBuilder.Path(filePath);
            }

            fileBuilder.Namespace(tableNamespace, true, ns =>
            {
                ns.Class(tableClass =>
                {
                    // set basic info.
                    tableClass.Partial(true).Name(tableClassName);

                    if (Options.GenerateInterfaces)
                        tableClass.Inherits(tableInterfaceName);

                    // set properties.
                    table.Columns.ForEach(column =>
                    {
                        tableClass.Property(columnProperty =>
                        {
                            var type = DataTypeResolver.ResolveType(column);
                            var typeName = type.GetOutputType();
                            if (type.IsValueType && column.IsNullable)
                                typeName = $"{typeName}?";

                            columnProperty
                                .Name(column.Name)
                                .Type(typeName)
                                .Meta(column);
                        });
                    });
                });
            });
        }

        protected virtual void GenerateModel(ITable table, FileBuilder fileBuilder)
        {
            if (!Options.GenerateModels)
                return;

            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var modelClassName = ModelClassName(table);
            var modelInterfaceName = ModelInterfaceName(table);
            var tableClassFullName = TableClassFullName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);


            if (!Options.OutputToSingleFile)
            {
                var filePath = $"{Options.OutputDir}{Path.DirectorySeparatorChar}{modelClassName}.generated.cs";
                fileBuilder.Path(filePath);
            }

            fileBuilder.Namespace(tableNamespace, true, ns =>
            {
                ns.Class(modelClass =>
                {
                    // set basic info.
                    modelClass.Partial(true).Name(modelClassName);

                    if (Options.GenerateModelsInterfaces)
                        modelClass.Inherits(modelInterfaceName);

                    Options?.ModelInheritances.ForEach(mi =>
                    {
                        modelClass.Inherits(ReplaceMetas(mi, table));
                    });


                    MethodBuilder from = null;
                    MethodBuilder to = null;

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

                    modelClass.Method(m => m
                        .Virtual(true)
                        .ReturnType("System.Type")
                        .Name("GetContextType")
                        .RawLine($"return typeof({ContextFullClassName()})")
                    );

                    modelClass.Method(m => m
                        .ReturnType("System.Type")
                        .Name("GetEntityType")
                        .RawLine($"return typeof({tableClassFullName})")
                    );

                    // set properties.
                    table.Columns.ForEach(column =>
                    {
                        modelClass.Property(columnProperty =>
                        {
                            var type = DataTypeResolver.ResolveType(column);
                            var typeName = type.GetOutputType();
                            bool isPropertyNullable = column.IsNullable || Options.GenerateModelPropertyAsNullable;
                            if (type.IsValueType && isPropertyNullable)
                                typeName = $"{typeName}?";

                            columnProperty
                                .Virtual(true)
                                .Name(column.Name)
                                .Type(typeName)
                                .Meta(column);

                            from.RawLine($"{column.Name} = entity.{column.Name}");

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
                        });
                    });
                });
            });
        }
    }
}
