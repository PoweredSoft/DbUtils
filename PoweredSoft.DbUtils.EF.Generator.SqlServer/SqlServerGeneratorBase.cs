using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public abstract class SqlServerGeneratorBase<TOptions> : DatabaseGeneratorBase<DatabaseSchema, TOptions>
        where TOptions : SqlServerGeneratorOptionsBase
    {
        public override DatabaseSchema CreateSchema() => new DatabaseSchema();
        protected override IDataTypeResolver DataTypeResolver { get; } = new DataTypeResolver();

        protected string TableNamespace(Table table)
        {
            var nsName = ReplaceMetas(Options.Namespace, table);
            return nsName;
        }

        protected string TableClassName(Table table)
        {
            var ret = table.Name;
            return ret;
        }

        protected string ContextNamespace()
        {
            return string.Join(".", Options.Namespace.Replace("[SCHEMA]", "").Split('.').Where(t => !string.IsNullOrWhiteSpace(t)));
        }

        protected string ContextClassName() => Options.ContextName;
        protected string ContextFullClassName() => $"{ContextNamespace()}.{ContextClassName()}";

        protected override string ReplaceMetas(string model, ITable table)
        {
            var ret = base.ReplaceMetas(model, table);
            ret = ret.Replace("[SCHEMA]", (table as Table)?.Schema);
            ret = ret.Replace("[CONTEXT]", ContextFullClassName());
            return ret;
        }

        protected string ModelClassName(Table table, bool includeSuffix = true)
        {
            var ret = $"{table.Name}Model{(includeSuffix ? Options.ModelSuffix : "")}";
            return ret;
        }

        protected string ModelInterfaceName(Table table)
        {
            var ret = ModelClassName(table, false);
            ret = $"I{ret}{Options.ModelInterfaceSuffix}";
            return ret;
        }

        protected string TableInterfaceName(Table table)
        {
            var ret = TableClassName(table);
            ret = $"I{ret}{Options.InterfaceNameSuffix}";
            return ret;
        }

        public string TableClassFullName(Table table)
        {
            var ns = TableNamespace(table);
            var cn = TableClassName(table);
            return $"{ns}.{cn}";
        }

        protected virtual string OneToOnePropertyName(ForeignKey fk)
        {
            return fk.ForeignKeyColumn.Table.Name;
        }

        protected virtual string HasManyPropertyName(ForeignKey fk, bool withForeignKeyName = false)
        {
            var prop = fk.ForeignKeyColumn.Table.Name;
            prop = Pluralize(prop);

            if (withForeignKeyName)
                prop = $"{prop}_{fk.ForeignKeyColumn.Name}";

            return prop;
        }

        protected string ForeignKeyPropertyName(ForeignKey fk, bool withForeignKeyName = false)
        {
            var prop = fk.IsOneToOne() ? fk.PrimaryKeyColumn.Table.Name : RemoveIdSuffixFromColumnName(fk.ForeignKeyColumn.Name);

            if (withForeignKeyName)
                prop = $"{prop}_{fk.ForeignKeyColumn.Name}";

            return prop;
        }

        protected override List<ISequence> ResolveSequencesToGenerate()
        {
            var ret = base.ResolveSequencesToGenerate();

            // schema filtering.
            if (Options.IncludedSchemas?.Any() == true)
            {
                ret = ret
                    .Cast<Sequence>()
                    .Where(t => Options.IncludedSchemas.Any(t2 =>
                        t2.Equals(t.Schema, StringComparison.CurrentCultureIgnoreCase)))
                    .Cast<ISequence>()
                    .ToList();
            }
            else if (Options.ExcludedSchemas?.Any() == true)
            {
                ret = ret
                    .Cast<Sequence>()
                    .Where(t => !Options.ExcludedSchemas.Any(t2 =>
                        t2.Equals(t.Schema, StringComparison.CurrentCultureIgnoreCase)))
                    .Cast<ISequence>()
                    .ToList();
            }

            return ret;
        }

        public override List<ITable> ResolveTablesToGenerate()
        {
            var ret = base.ResolveTablesToGenerate();

            // schema filtering.
            if (Options.IncludedSchemas?.Any() == true)
            {
                ret = ret
                    .Cast<Table>()
                    .Where(t => Options.IncludedSchemas.Any(t2 =>
                        t2.Equals(t.Schema, StringComparison.CurrentCultureIgnoreCase)))
                    .Cast<ITable>()
                    .ToList();
            }
            else if (Options.ExcludedSchemas?.Any() == true)
            {
                ret = ret
                    .Cast<Table>()
                    .Where(t => !Options.ExcludedSchemas.Any(t2 =>
                        t2.Equals(t.Schema, StringComparison.CurrentCultureIgnoreCase)))
                    .Cast<ITable>()
                    .ToList();
            }

            return ret;
        }

        protected virtual void GenerateModel(Table table, FileBuilder fileBuilder)
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
                            .ReturnType("void")
                            .Name("From")
                            .Parameter(p => p.Type(tableClassFullName).Name("entity"));
                        from = m;
                    });

                    modelClass.Method(m =>
                    {
                        m
                            .AccessModifier(AccessModifiers.Public)
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
                    table.SqlServerColumns.ForEach(column =>
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

        protected virtual void GenerateModelInterface(Table table, FileBuilder fileBuilder)
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
                    table.SqlServerColumns.ForEach(column =>
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

        protected abstract void GenerateManyToMany(Table table);


        protected abstract string CollectionInstanceType();
        public abstract bool HasManyShouldBeVirtual();
        public abstract bool OneToShouldBeVirtual();
        public abstract bool ForeignKeysShouldBeVirtual();


        protected virtual void GenerateHasMany(Table table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var hasManyList = table.HasMany().ToList();
            hasManyList.ForEach(fk =>
            {
                var sqlServerFk = fk as ForeignKey;
                var hasMoreThanOne = table.HasMany().Count(t => t.ForeignKeyColumn.Table == sqlServerFk.ForeignKeyColumn.Table) > 1;
                var propName = HasManyPropertyName(sqlServerFk, hasMoreThanOne);

                propName = tableClass.GetUniqueMemberName(propName);
                var pocoType = TableClassFullName(sqlServerFk.SqlServerForeignKeyColumn.SqlServerTable);
                var propType = $"System.Collections.Generic.ICollection<{pocoType}>";
                var defaultValue = $"new {CollectionInstanceType()}<{pocoType}>()";
                tableClass.Property(p => p.Virtual(HasManyShouldBeVirtual()).Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Has Many").Meta(fk));
            });
        }

        protected virtual void GenerateOneToOnes(Table table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            table.OneToOne().ToList().ForEach(fk =>
            {
                var sqlServerFk = fk as ForeignKey;
                var propName = OneToOnePropertyName(sqlServerFk);
                propName = tableClass.GetUniqueMemberName(propName);
                var propType = TableClassFullName(sqlServerFk.SqlServerForeignKeyColumn.SqlServerTable);
                tableClass.Property(p => p.Virtual(OneToShouldBeVirtual()).Type(propType).Name(propName).Comment("One to One").Meta(fk));
            });
        }

        protected virtual void GenerateForeignKeys(Table table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            table.SqlServerForeignKeys.ForEach(fk =>
            {
                var propName = ForeignKeyPropertyName(fk);
                if (tableClass.HasMemberWithName(propName))
                {
                    var tempPropName = ForeignKeyPropertyName(fk, true);
                    if (!tableClass.HasMemberWithName(tempPropName))
                        propName = tempPropName;
                }

                propName = tableClass.GetUniqueMemberName(propName);
                var foreignKeyTypeName = TableClassFullName(fk.SqlServerPrimaryKeyColumn.SqlServerTable);
                tableClass.Property(foreignKeyProp => foreignKeyProp.Virtual(ForeignKeysShouldBeVirtual()).Type(foreignKeyTypeName).Name(propName).Comment("Foreign Key").Meta(fk));
            });
        }

        protected virtual void GenerateEntityInterface(Table table, FileBuilder fileBuilder)
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
                    table.SqlServerColumns.ForEach(column =>
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

        private void GenerateEntity(Table table, FileBuilder fileBuilder)
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
                    table.SqlServerColumns.ForEach(column =>
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

                        GenerateEntityInterface(table as Table, fb);
                        GenerateEntity(table as Table, fb);
                        GenerateModelInterface(table as Table, fb);
                        GenerateModel(table as Table, fb);
                    });
                }
                else
                {
                    GenerationContext
                        .FileIfPathIsSet(fb => GenerateEntityInterface(table as Table, fb))
                        .FileIfPathIsSet(fb => GenerateEntity(table as Table, fb))
                        .FileIfPathIsSet(fb => GenerateModelInterface(table as Table, fb))
                        .FileIfPathIsSet(fb => GenerateModel(table as Table, fb));
                }
            });

            // generate foreign keys and navigation properties.
            tables.ForEach(table =>
            {
                GenerateForeignKeys(table as Table);
                GenerateOneToOnes(table as Table);
                GenerateHasMany(table as Table);
                GenerateManyToMany(table as Table);
            });
        }

        protected abstract void GenerateContext();

        protected virtual void BeforeSaveToDisk()
        {

        }

        protected override void GenerateCode()
        {
            GenerateEntities();
            GenerateContext();
            GenerateSequenceMethods();
            BeforeSaveToDisk();
            GenerationContext.SaveToDisk(Encoding.UTF8);
        }

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

                var methodName = $"NextValueFor{sequence.Name}";
                var sqlServerSequence = sequence as Sequence;

                contextClass.Method(m =>
                {
                    m.AccessModifier(AccessModifiers.Public)
                        .ReturnType(outputType)
                        .Name(methodName);

                    GenerateGetNextSequenceLines(m, outputType, sqlServerSequence);
                });

                /*
                var rawQuery = Database.SqlQuery<int>("SELECT NEXT VALUE FOR dbo.TestSequence;");
                var task = rawQuery.SingleAsync();
                int nextVal = task.Result;
                return nextVal;*/
            });
        }

        protected abstract void GenerateGetNextSequenceLines(MethodBuilder method, string outputType, Sequence sequence);
    }



}
