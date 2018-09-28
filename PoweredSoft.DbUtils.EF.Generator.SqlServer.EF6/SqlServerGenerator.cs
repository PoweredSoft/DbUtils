using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            GenerationContext.SaveToDisk(Encoding.UTF8);
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
                            // to table mapping.
                            constructor.RawLine($"ToTable(\"{table.Schema}.{table.Name}\")");

                            // pk mapping.
                            var pk = table.SqlServerColumns.FirstOrDefault(t => t.IsPrimaryKey);
                            var pkProp = entityClass.FindByMeta<PropertyBuilder>(pk);
                            constructor.RawLine($"HasKey(t => t.{pkProp.GetName()})");

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

                            /*
                             *
                             * itBonus->getMinSpend()
                             */
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
                    GenerationContext.SingleFile(fb => GenerateEntity(table as Table, fb));
                else
                    GenerationContext.File(fb => GenerateEntity(table as Table, fb));
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
                var propName = HasManyPropertyName(sqlServerFk);

                // attempt to get a nicer name. than manies1
                if (tableClass.HasMemberWithName(propName))
                {
                    var tempPropName = HasManyPropertyName(sqlServerFk, true);
                    if (!tableClass.HasMemberWithName(tempPropName))
                        propName = tempPropName;
                }

                propName = tableClass.GetUniqueMemberName(propName);
                var pocoType = TableClassFullName(sqlServerFk.SqlServerForeignKeyColumn.SqlServerTable);
                var propType = $"System.Collections.Generic.ICollection<{pocoType}>";
                var defaultValue = $"new System.Collections.Generic.List<{pocoType}>()";
                tableClass.Property(p => p.Virtual(true).Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Has Many"));
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

                // attempt to get a nicer name. (include foreign key column name in case of multiple fk to same table)
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

        private void GenerateEntity(Table table, FileBuilder fileBuilder)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);

            // set the path.
            var outputDir = Options.OutputDir;
            var filePath = Options.OutputToSingleFile
                ? $"{outputDir}\\{Options.OutputSingleFileName}"
                : $"{outputDir}\\{tableClassName}.generated.cs";

            fileBuilder.Path(filePath);

            fileBuilder.Namespace(tableNamespace, true, ns =>
            {
                ns.Class(tableClass =>
                {
                    // set basic info.
                    tableClass.Partial(true).Name(tableClassName);

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
                                .SetAccessModifier(AccessModifiers.Public)
                                .Type(typeName)
                                .Meta(column);
                        });
                    });
                });
            });
        }

       
    }
}
