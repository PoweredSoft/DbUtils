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
        public override List<ITable> ResolveTablesToGenerate()
        {
            var ret = base.ResolveTablesToGenerate();
            ret = ret.Where(t => !t.IsManyToMany()).ToList();
            return ret;
        }

        protected override void BeforeSaveToDisk()
        {
            base.BeforeSaveToDisk();
            GenerateFluentConfigurations();
        }

        protected override void GenerateGetNextSequenceLines(MethodBuilder method, string outputType, Sequence sequence)
        {
            
        }

        protected override void GenerateManyToMany(Table table)
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
                var defaultValue = $"new {CollectionInstanceType()}<{pocoType}>()";

                // generate property :)
                tableClass.Property(p => p.Virtual(true).Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Many to Many").Meta(fk));
            });
        }

        protected override string CollectionInstanceType() => "System.Collections.Generic.List";
        public override bool HasManyShouldBeVirtual() => true;
        public override bool OneToShouldBeVirtual() => true;
        public override bool ForeignKeysShouldBeVirtual() => true;

        protected override void GenerateContext()
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

                        TablesToGenerate.Cast<Table>().ToList().ForEach(table =>
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

                            TablesToGenerate.Cast<Table>().ToList().ForEach(table =>
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
            var tables = TablesToGenerate.ToList();
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
    }
}
