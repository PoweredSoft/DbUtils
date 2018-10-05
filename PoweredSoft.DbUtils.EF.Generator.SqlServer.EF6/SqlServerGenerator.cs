using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EF6
{
    public class SqlServerGenerator : SqlServerGeneratorBase<SqlServerGeneratorOptions>
    {
        private IEnumerable<ITable> TablesToGenerateWithoutManyToMany => TablesToGenerate.Where(t => !t.IsManyToMany());

        protected override void GenerateCode()
        {
            GenerateEntities();
            GenerateFluentConfigurations();
            GenerateContext();
            GenerationContext.SaveToDisk(Encoding.UTF8);
        }

     

        private void GenerateContext()
        {
            var contextNamespace = ContextNamespace();
            var contextClassName = ContextClassName();

            Action<FileBuilder> generateContextInline = (FileBuilder fileBuilder) =>
            {
                if (!Options.OutputToSingleFile)
                {
                    var filePath = $"{Options.OutputDir}{Path.DirectorySeparatorChar}{Options.ContextName}.generated.cs";
                    fileBuilder.Path(filePath);
                }

                fileBuilder.Namespace(contextNamespace, true, ns =>
                {
                    ns.Class(contextClassName, true, contextClass =>
                    {
                        contextClass.Partial(true).Inherits(Options.ContextBaseClassName);

                        TablesToGenerateWithoutManyToMany.Cast<Table>().ToList().ForEach(table =>
                        {
                            var tableClassFullName = TableClassFullName(table);
                            var tableNamePlural = Pluralize(table.Name);
                            contextClass.Property(tableNamePlural, true, dbSetProp =>
                            {
                                dbSetProp.Type($"System.Data.Entity.DbSet<{tableClassFullName}>");
                            });
                        });

                        contextClass.Constructor(c => c
                            .AccessModifier(AccessModifiers.Omit)
                            .IsStatic(true)
                            .Class(contextClass)
                            .RawLine($"System.Data.Entity.Database.SetInitializer<{Options.ContextName}>(null)")
                        );

                        contextClass.Constructor(c => c
                            .Class(contextClass)
                            .BaseParameter($"\"{Options.ConnectionStringName}\"")
                            .RawLine("InitializePartial()")
                        );

                        contextClass.Constructor(c => c
                            .Class(contextClass)
                            .Parameter(p => p.Type("string").Name("connectionString"))
                            .BaseParameter("connectionString")
                            .RawLine("InitializePartial()")
                        );

                        contextClass.Constructor(c => c
                            .Class(contextClass)
                            .Parameter(p => p.Type("string").Name("connectionString"))
                            .Parameter(p => p.Type("System.Data.Entity.Infrastructure.DbCompiledModel").Name("model"))
                            .BaseParameter("connectionString")
                            .BaseParameter("model")
                            .RawLine("InitializePartial()")
                        );

                        contextClass.Method(addConfigurationMethod =>
                        {
                            addConfigurationMethod
                                .IsStatic(true)
                                .ReturnType("void")
                                .AccessModifier(AccessModifiers.Protected)
                                .Name("AddFluentConfigurations")
                                .Parameter(p => p.Type("System.Data.Entity.DbModelBuilder").Name("modelBuilder"));

                            TablesToGenerateWithoutManyToMany.Cast<Table>().ToList().ForEach(table =>
                            {
                                var fcc = TableClassFullName(table) + Options.FluentConfigurationClassSuffix;
                                addConfigurationMethod.RawLine($"modelBuilder.Configurations.Add(new {fcc}())");
                            });
                        });

                        contextClass.Method(m => m
                            .AccessModifier(AccessModifiers.Omit)
                            .Name("OnModelCreatingPartial")
                            .ReturnType("void")
                            .Partial(true)
                            .Parameter(p => p.Type("System.Data.Entity.DbModelBuilder").Name("modelBuilder"))
                        );

                        contextClass.Method(m => m
                            .AccessModifier(AccessModifiers.Omit)
                            .Name("InitializePartial")
                            .ReturnType("void")
                            .Partial(true)
                        );

                        contextClass.Method(m =>
                        {
                            m
                                .AccessModifier(AccessModifiers.Protected)
                                .Override(true)
                                .ReturnType("void")
                                .Name("OnModelCreating")
                                .Parameter(p => p.Type("System.Data.Entity.DbModelBuilder").Name("modelBuilder"))
                                .RawLine("base.OnModelCreating(modelBuilder)")
                                .RawLine("AddFluentConfigurations(modelBuilder)")
                                .RawLine("OnModelCreatingPartial(modelBuilder)");
                        });

                        contextClass.Method(m =>
                        {
                            m
                                .AccessModifier(AccessModifiers.Public)
                                .IsStatic(true)
                                .ReturnType("System.Data.Entity.DbModelBuilder")
                                .Name("CreateModel")
                                .Parameter(p => p.Type("System.Data.Entity.DbModelBuilder").Name("modelBuilder"))
                                .Parameter(p => p.Type("string").Name("schema"))
                                .RawLine("AddFluentConfigurations(modelBuilder)")
                                .RawLine("return modelBuilder");
                        });

                    });
                });
            };

            if (Options.OutputToSingleFile)
                GenerationContext.SingleFile(fb => generateContextInline(fb));
            else
                GenerationContext.FileIfPathIsSet(fb => generateContextInline(fb));
        }

        private void GenerateFluentConfigurations()
        {
            var tables = TablesToGenerateWithoutManyToMany.ToList();
            tables.ForEach(table =>
            {
                if (Options.OutputToSingleFile)
                    GenerationContext.SingleFile(fb => GenerateEntityFluentConfiguration(table as Table, fb));
                else
                    GenerationContext.File(fb => GenerateEntityFluentConfiguration(table as Table, fb));
            });
        }

        private void GenerateEntityFluentConfiguration(Table table, FileBuilder fileBuilder)
        {
            var tableNamespace = TableNamespace(table);
            var tableFluentConfigurationClassName = $"{TableClassName(table)}{Options.FluentConfigurationClassSuffix}";
            var tableClassName = TableClassName(table);
            var tableClassFullName = TableClassFullName(table);
            var entityClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            // set the path.
            var outputDir = Options.OutputDir;
            var filePath = Options.OutputToSingleFile
                ? $"{outputDir}\\{Options.OutputSingleFileName}"
                : $"{outputDir}\\{tableFluentConfigurationClassName}.generated.cs";

            fileBuilder.Path(filePath);

            // set the namespace.
            fileBuilder.Namespace(tableNamespace, true, ns =>
            {
                ns.Class(tableFluentConfigurationClassName, true, fluentConfigClass =>
                {
                    fluentConfigClass
                        .Partial(true)
                        .Inherits($"System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<{tableClassName}>")
                        .Constructor(constructor =>
                        {
                            constructor.AddComment("Table mapping & keys");

                            // to table mapping.
                            constructor.RawLine($"ToTable(\"{table.Schema}.{table.Name}\")");

                            // pk mapping.
                            var pk = table.SqlServerColumns.FirstOrDefault(t => t.IsPrimaryKey);
                            var pkProp = entityClass.FindByMeta<PropertyBuilder>(pk);
                            constructor.RawLine($"HasKey(t => t.{pkProp.GetName()})");

                            constructor.AddComment("Columns");

                            // columns mapping.
                            table.SqlServerColumns.ForEach(column =>
                            {
                                var columnProp = entityClass.FindByMeta<PropertyBuilder>(column);
                                var columnLine = RawLineBuilder.Create();
                                columnLine.Append($"Property(t => t.{columnProp.GetName()})");
                                columnLine.Append($".HasColumnName(\"{column.Name}\")");

                                if (column.IsNullable)
                                    columnLine.Append(".IsOptional()");
                                else
                                    columnLine.Append(".IsRequired()");

                                columnLine.Append($".HasColumnType(\"{column.DataType}\")");

                                if (column.IsPrimaryKey)
                                {
                                    if (column.IsAutoIncrement)
                                        columnLine.Append(".HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)");
                                    else if (column.DataType == "uniqueidentifier" && column.DefaultValue.IndexOf("newsequentialid", StringComparison.InvariantCultureIgnoreCase) > -1)
                                        columnLine.Append(".HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)");
                                    else
                                        columnLine.Append(".HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)");
                                }

                                if (column.CharacterMaximumLength.HasValue && column.CharacterMaximumLength != -1)
                                    columnLine.Append($".HasMaxLength({column.CharacterMaximumLength})");

                                if (DataTypeResolver.IsFixLength(column))
                                    columnLine.Append(".IsFixedLength()");

                                if (DataTypeResolver.IsString(column) && !DataTypeResolver.IsUnicode(column))
                                    columnLine.Append(".IsUnicode(false)");

                                if (column.NumericPrecision.HasValue && column.NumericScale.HasValue && column.NumericScale != 0)
                                    columnLine.Append($".HasPrecision({column.NumericPrecision}, {column.NumericScale})");


                                constructor.Add(columnLine);
                            });

                            constructor.AddComment("Navigations");
                            table.SqlServerForeignKeys.ForEach(fk =>
                            {
                                // todo skip if table is filtered.

                                var line = RawLineBuilder.Create();
                                var fkProp = entityClass.FindByMeta<PropertyBuilder>(fk);
                                var fkColumnProp = entityClass.FindByMeta<PropertyBuilder>(fk.SqlServerForeignKeyColumn);
                                if (fkProp != null)
                                {
                                    var primaryNamespace = TableNamespace(fk.SqlServerPrimaryKeyColumn.SqlServerTable);
                                    var primaryClassName = TableClassName(fk.SqlServerPrimaryKeyColumn.SqlServerTable);
                                    var primaryEntity = GenerationContext.FindClass(primaryClassName, primaryNamespace);
                                    var reverseNav = primaryEntity.FindByMeta<PropertyBuilder>(fk);


                                    if (fk.SqlServerForeignKeyColumn.IsNullable)
                                        line.Append($"HasOptional(t => t.{fkProp.GetName()})");
                                    else
                                        line.Append($"HasRequired(t => t.{fkProp.GetName()})");

                                    if (fk.IsOneToOne())
                                    {
                                        line.Append($".WithOptional(t => t.{reverseNav.GetName()})");
                                    }
                                    else
                                    {
                                        line.Append($".WithMany(t => t.{reverseNav.GetName()})");
                                        line.Append($".HasForeignKey(t => t.{fkColumnProp.GetName()})");
                                    }
                                    constructor.Add(line);
                                }
                            });

                            constructor.AddComment("Many to Many");
                            table.ManyToMany().ToList().ForEach(mtm =>
                            {
                                if (mtm.ForeignKeyColumn.PrimaryKeyOrder > 1)
                                    return;

                                var manyToManyTable = mtm.ForeignKeyColumn.Table as Table;
                                var manyProp = entityClass.FindByMeta<PropertyBuilder>(mtm);

                                // other prop.
                                var otherFk = mtm.ForeignKeyColumn.Table.ForeignKeys.First(t => t.ForeignKeyColumn.PrimaryKeyOrder > 1);
                                var otherTable = otherFk.PrimaryKeyColumn.Table as Table;
                                var otherNamespace = TableNamespace(otherTable);
                                var otherClassName = TableClassName(otherTable);
                                var otherEntity = GenerationContext.FindClass(otherClassName, otherNamespace);
                                var otherProp = otherEntity.FindByMeta<PropertyBuilder>(otherFk);

                                var line = RawLineBuilder.Create();
                                line.Append($"HasMany(t => t.{manyProp.GetName()})");
                                line.Append($".WithMany(t => t.{otherProp.GetName()})");
                                line.Append($".Map(t => t.ToTable(\"{manyToManyTable.Name}\", \"{manyToManyTable.Schema}\")");
                                line.Append($".MapLeftKey(\"{mtm.ForeignKeyColumn.Name}\")");
                                line.Append($".MapRightKey(\"{otherFk.ForeignKeyColumn.Name}\"))");
                                constructor.Add(line);
                            });
                        });
                });
            });
        }

        protected void GenerateEntities()
        {
            var tables = TablesToGenerateWithoutManyToMany.ToList();

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

        private void GenerateModel(Table table, FileBuilder fileBuilder)
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
                                    .True(RawInlineBuilder.Create($"entity.{column.Name} = {column.Name}"))
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

        private void GenerateModelInterface(Table table, FileBuilder fileBuilder)
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

        private void GenerateManyToMany(Table table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var manyToManyList = table.ManyToMany().ToList();
            manyToManyList.ForEach(fk =>
            {
                var sqlServerFk = fk as ForeignKey;

                // get the other foreign key of this many to many.
                var otherFk = sqlServerFk.SqlServerForeignKeyColumn.SqlServerTable.SqlServerForeignKeys.FirstOrDefault(t => t != sqlServerFk);

                // other table attached to this many to many.
                var otherPk = otherFk.SqlServerPrimaryKeyColumn.SqlServerTable;

                // pluralize this name.
                var propName = Pluralize(otherPk.Name);
                propName = tableClass.GetUniqueMemberName(propName);

                // the type of the property.
                var pocoType = TableClassFullName(otherPk);
                var propType = $"System.Collections.Generic.ICollection<{pocoType}>";
                var defaultValue = $"new System.Collections.Generic.List<{pocoType}>()";

                // generate property :)
                tableClass.Property(p => p.Virtual(true).Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Many to Many").Meta(fk));
            });
        }

        private void GenerateHasMany(Table table)
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
                var defaultValue = $"new System.Collections.Generic.List<{pocoType}>()";
                tableClass.Property(p => p.Virtual(true).Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Has Many").Meta(fk));
            });
        }

        private void GenerateOneToOnes(Table table)
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
                tableClass.Property(p => p.Virtual(true).Type(propType).Name(propName).Comment("One to One").Meta(fk));
            });
        }

        private void GenerateForeignKeys(Table table)
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
                tableClass.Property(foreignKeyProp => foreignKeyProp.Virtual(true).Type(foreignKeyTypeName).Name(propName).Comment("Foreign Key").Meta(fk));
            });
        }

        private void GenerateEntityInterface(Table table, FileBuilder fileBuilder)
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
    }
}
