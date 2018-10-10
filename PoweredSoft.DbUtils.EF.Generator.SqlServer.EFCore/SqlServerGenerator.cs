using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EFCore
{
    public class SqlServerGenerator : SqlServerGeneratorBase<SqlServerGeneratorOptions>
    {
        protected override void GenerateManyToMany(Table table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var manyToManyList = table.ManyToMany().ToList();
            manyToManyList.ForEach(fk =>
            {
                var sqlServerFk = fk as ForeignKey;

                // get the poco of the many to many.
                var manyToManyPocoFullClass = TableClassFullName(sqlServerFk.SqlServerForeignKeyColumn.SqlServerTable);

                // pluralize this name.
                var propName = Pluralize(sqlServerFk.SqlServerForeignKeyColumn.SqlServerTable.Name);
                propName = tableClass.GetUniqueMemberName(propName);

                // the type of the property.
                var propType = $"System.Collections.Generic.ICollection<{manyToManyPocoFullClass}>";
                var defaultValue = $"new {CollectionInstanceType()}<{manyToManyPocoFullClass}>()";

                // generate property :)
                tableClass.Property(p => p.Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Many to Many").Meta(fk));
            });
        }

        protected override string CollectionInstanceType() => "System.Collections.Generic.HashSet";
        public override bool HasManyShouldBeVirtual() => false;
        public override bool OneToShouldBeVirtual() => false;
        public override bool ForeignKeysShouldBeVirtual() => false;

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

                fileBuilder
                    .Using("Microsoft.EntityFrameworkCore")
                    .Using("Microsoft.EntityFrameworkCore.Metadata");

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
                                dbSetProp.Virtual(true).Type($"DbSet<{tableClassFullName}>");
                            });
                        });

                        // empty constructor.
                        contextClass.Constructor(c => c.Class(contextClass));

                        // constructor with options.
                        contextClass.Constructor(c => c
                            .Class(contextClass)
                            .Parameter(p => p.Type($"DbContextOptions<{contextClassName}>").Name("options"))
                            .BaseParameter("options")
                        );

                        // override On Configuring
                        contextClass.Method(m => m
                            .AccessModifier(AccessModifiers.Protected)
                            .Override(true)
                            .ReturnType("void")
                            .Name("OnConfiguring")
                            .Parameter(p => p.Type("DbContextOptionsBuilder").Name("optionsBuilder"))
                            .Add(() =>
                            {
                                return IfBuilder.Create()
                                    .RawCondition(c => c.Condition("!optionsBuilder.IsConfigured"))
                                    .Add(RawLineBuilder.Create("#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.").NoEndOfLine())
                                    .Add(RawLineBuilder.Create($"optionsBuilder.UseSqlServer(\"{Options.ConnectionString}\")"));
                            })
                        );

                        // model creating.
                        contextClass.Method(m =>
                        {
                            m
                                .AccessModifier(AccessModifiers.Protected)
                                .Override(true)
                                .ReturnType("void")
                                .Name("OnModelCreating")
                                .Parameter(p => p.Type("ModelBuilder").Name("modelBuilder"));

                            TablesToGenerate.ForEach(table =>
                            {
                                AddFluentToMethod(m, table as Table);
                            });
                        });
                    });
                });
            };

            if (Options.OutputToSingleFile)
                GenerationContext.SingleFile(fb => generateContextInline(fb));
            else
                GenerationContext.FileIfPathIsSet(fb => generateContextInline(fb));
        }

        protected virtual void AddFluentToMethod(MethodBuilder methodBuilder, Table table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableFullClassName = TableClassFullName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var fluentExpression = MultiLineLambdaExpression.Create()
                .Parameter(p => p.Name("entity"))
                .RawLine($"entity.ToTable(\"{table.Name}\", \"{table.Schema}\")");

            var pks = table.SqlServerColumns.Where(t => t.IsPrimaryKey);
            var hasCompositeKey = pks.Count() > 1;
;           if (hasCompositeKey)
            {
                var def = string.Join(", ", pks.Select(pk =>
                {
                    var pkProp = tableClass.FindByMeta<PropertyBuilder>(pk);
                    return $"t.{pkProp.GetName()}";
                }));
                fluentExpression.RawLine($"entity.HasKey(t => new {{ {def} }})");
            }
            else
            {
                var pk = pks.First();
                var pkProp = tableClass.FindByMeta<PropertyBuilder>(pk);
                fluentExpression.RawLine($"entity.HasKey(t => t.{pkProp.GetName()})");
            }

            table.SqlServerIndexes.ForEach(i =>
            {
                var line = RawLineBuilder.Create();

                string rightExpr;
                if (i.SqlServerColumns.Count == 1)
                {
                    var indexProp = tableClass.FindByMeta<PropertyBuilder>(i.SqlServerColumns.First());
                    rightExpr = $"t.{indexProp.GetName()}";
                }
                else
                {
                    var cols = string.Join(", ",i.SqlServerColumns.Select(t => $"t.{tableClass.FindByMeta<PropertyBuilder>(t).GetName()}"));
                    rightExpr = $"new {{ {cols} }}";
                }

                line.Append($"entity.HasIndex(t => {rightExpr})");
                line.Append($"\n\t.HasName(\"{i.Name}\")");
                if (i.IsUnique)
                    line.Append("\n\t.IsUnique()");

                fluentExpression.Add(line);
            });

            table.SqlServerColumns.ForEach(c =>
            {
                var columnProp = tableClass.FindByMeta<PropertyBuilder>(c);
                var line = RawLineBuilder.Create();
                line.Append($"entity.Property(t => t.{columnProp.GetName()})");
                line.Append($".HasColumnType(\"{FluentColumnType(c)}\")");

                if (c.IsPrimaryKey)
                {
                    if (c.IsAutoIncrement)
                        line.Append(".ValueGeneratedOnAdd()");
                    else
                        line.Append(".ValueGeneratedNever()");
                }
                else if (!string.IsNullOrWhiteSpace(c.DefaultValue))
                {
                    line.Append($".HasDefaultValueSql(\"{c.DefaultValue}\")");
                }

                if (!c.IsNullable)
                    line.Append(".IsRequired()");

                if (c.CharacterMaximumLength.HasValue && c.CharacterMaximumLength != -1)
                    line.Append($".HasMaxLength({c.CharacterMaximumLength})");

                if (DataTypeResolver.IsString(c) && !DataTypeResolver.IsUnicode(c))
                    line.Append(".IsUnicode(false)");

                fluentExpression.Add(line);
            });

            table.SqlServerForeignKeys.ForEach(fk =>
            {
                var fkProp = tableClass.FindByMeta<PropertyBuilder>(fk);
                var fkColumnProp = tableClass.FindByMeta<PropertyBuilder>(fk.ForeignKeyColumn);
                var fkTableNamespace = TableNamespace(fk.SqlServerPrimaryKeyColumn.SqlServerTable);
                var fkTableClassName = TableClassName(fk.SqlServerPrimaryKeyColumn.SqlServerTable);
                var fkTableClass = GenerationContext.FindClass(fkTableClassName, fkTableNamespace);
                var reverseProp = fkTableClass.FindByMeta<PropertyBuilder>(fk);

                var line = RawLineBuilder.Create();

                line.Append($"entity.HasOne(t => t.{fkProp.GetName()})");

                if (!fk.IsOneToOne())
                {
                    line.Append($"\n\t.WithMany(t => t.{reverseProp.GetName()})");
                    line.Append($"\n\t.HasForeignKey(t => t.{fkColumnProp.GetName()})");
                }
                else
                {
                    line.Append($"\n\t.WithOne(t => t.{reverseProp.GetName()})");
                    line.Append($"\n\t.HasForeignKey<{tableFullClassName}>(t => t.{fkColumnProp.GetName()})");
                }


                if (fk.DeleteCascadeAction == "CASCADE")
                    line.Append("\n\t.OnDelete(DeleteBehavior.Delete)");
                else if (fk.DeleteCascadeAction == "SET_NULL")
                    line.Append("\n\t.OnDelete(DeleteBehavior.SetNull)");
                else 
                    line.Append("\n\t.OnDelete(DeleteBehavior.ClientSetNull)");

                line.Append($"\n\t.HasConstraintName(\"{fk.Name}\")");

                line.Comment("Foreign Key");
                fluentExpression.Add(line);
            });
            

            var modelFluentLine = $"modelBuilder.Entity<{tableFullClassName}>({fluentExpression.GenerateInline()})";
            methodBuilder.Add(RawLineBuilder.Create(modelFluentLine));
            methodBuilder.AddEmptyLine();
        }

        private string FluentColumnType(Column column)
        {
            if (column.DateTimePrecision.HasValue)
                return $"{column.DataType}({column.DateTimePrecision})";

            if (column.NumericPrecision.HasValue && column.NumericScale.HasValue && column.NumericScale != 0 && DataTypeResolver.NeedFluentPrecisionSpecification(column))
                return $"{column.DataType}({column.NumericPrecision}, {column.NumericScale})";

            if (column.CharacterMaximumLength.HasValue)
                return $"{column.DataType}({(column.CharacterMaximumLength == -1 ? "MAX" : $"{column.CharacterMaximumLength}")})";

            return column.DataType;
        }
    }
}
