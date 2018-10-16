using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.EF.Generator.EFCore.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.EFCore
{
    public abstract class EFCoreDatabaseGeneratorBase<TSchema, TOptions> : DatabaseGeneratorBase<TSchema, TOptions>
        where TSchema : IDatabaseSchema
        where TOptions : IEFCoreGeneratorOptions
    {
        protected override string CollectionInstanceType() => "System.Collections.Generic.HashSet";
        public override bool HasManyShouldBeVirtual() => false;
        public override bool OneToShouldBeVirtual() => false;
        public override bool ForeignKeysShouldBeVirtual() => false;

        protected abstract RawLineBuilder UseDatabaseEngineConnectionStringLine();
        
        protected abstract bool IsCascade(string action);
        protected abstract bool IsSetNull(string action);
        protected abstract string GetNextValueRawSql(ISequence sequence);

        protected virtual string ToTableFluent(ITable table) => $"ToTable(\"{table.Name}\")";

        protected override void GenerateManyToMany(ITable table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var manyToManyList = table.ManyToMany().ToList();
            manyToManyList.ForEach(fk =>
            {
                if (!TablesToGenerate.Contains(fk.ForeignKeyColumn.Table))
                    return;

                // get the poco of the many to many.
                var manyToManyPocoFullClass = TableClassFullName(fk.ForeignKeyColumn.Table);

                // pluralize this name.
                var propName = Pluralize(fk.ForeignKeyColumn.Table.Name);
                propName = tableClass.GetUniqueMemberName(propName);

                // the type of the property.
                var propType = $"System.Collections.Generic.ICollection<{manyToManyPocoFullClass}>";
                var defaultValue = $"new {CollectionInstanceType()}<{manyToManyPocoFullClass}>()";

                // generate property :)
                tableClass.Property(p => p.Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Many to Many").Meta(fk));
            });
        }

        protected override void GenerateGetNextSequenceLines(MethodBuilder method, string outputType, ISequence sequence)
        {
            method.RawLine("var command = Database.GetDbConnection().CreateCommand()");
            method.RawLine($"command.CommandText = \"{GetNextValueRawSql(sequence)}\"");
            method.RawLine("Database.OpenConnection()");
            method.RawLine($"return ({outputType})command.ExecuteScalar()");
        }

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

                fileBuilder.Using("Microsoft.EntityFrameworkCore");

                fileBuilder.Namespace(contextNamespace, true, ns =>
                {
                    ns.Class(contextClassName, true, contextClass =>
                    {
                        contextClass.Partial(true).Inherits(Options.ContextBaseClassName);

                        TablesToGenerate.ForEach(table =>
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
                        contextClass.Method(m =>
                        {
                            m
                                .AccessModifier(AccessModifiers.Protected)
                                .Override(true)
                                .ReturnType("void")
                                .Name("OnConfiguring")
                                .Parameter(p => p.Type("DbContextOptionsBuilder").Name("optionsBuilder"));

                            if (Options.AddConnectionStringOnGenerate)
                            {
                                m.Add(() =>
                                {
                                    return IfBuilder.Create()
                                        .RawCondition(c => c.Condition("!optionsBuilder.IsConfigured"))
                                        .Add(RawLineBuilder.Create(
                                                "#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.")
                                            .NoEndOfLine())
                                        .Add(UseDatabaseEngineConnectionStringLine());
                                });
                            }
                        });

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
                                AddFluentToMethod(m, table);
                            });

                            SequenceToGenerate.ForEach(sequence =>
                            {
                                var dataType = DataTypeResolver.ResolveType(sequence);
                                var outputType = dataType.GetOutputType();
                                m.RawLine($"modelBuilder.HasSequence<{outputType}>(\"{sequence.Name}\").StartsAt({sequence.StartAt}).IncrementsBy({sequence.IncrementsBy})");
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
       
     

        protected virtual void AddFluentToMethod(MethodBuilder methodBuilder, ITable table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableFullClassName = TableClassFullName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var fluentExpression = MultiLineLambdaExpression.Create()
                .Parameter(p => p.Name("entity"))
                .RawLine($"entity.{ToTableFluent(table)}");   
            //.RawLine($"entity.ToTable(\"{table.Name}\", \"{table.Schema}\")");

            var pks = table.Columns.Where(t => t.IsPrimaryKey);
            var hasCompositeKey = pks.Count() > 1;
            ; if (hasCompositeKey)
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

            table.Indexes.ForEach(i =>
            {
                var line = RawLineBuilder.Create();

                string rightExpr;
                if (i.Columns.Count == 1)
                {
                    var indexProp = tableClass.FindByMeta<PropertyBuilder>(i.Columns.First());
                    rightExpr = $"t.{indexProp.GetName()}";
                }
                else
                {
                    var cols = string.Join(", ", i.Columns.Select(t => $"t.{tableClass.FindByMeta<PropertyBuilder>(t).GetName()}"));
                    rightExpr = $"new {{ {cols} }}";
                }

                line.Append($"entity.HasIndex(t => {rightExpr})");
                line.Append($"\n\t.HasName(\"{i.Name}\")");
                if (i.IsUnique)
                    line.Append("\n\t.IsUnique()");

                OnBeforeIndexLineAdded(line, i);

                fluentExpression.Add(line);
            });

            table.Columns.ForEach(c =>
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

            table.ForeignKeys.ForEach(fk =>
            {
                if (!TablesToGenerate.Contains(fk.PrimaryKeyColumn.Table))
                    return;

                var fkProp = tableClass.FindByMeta<PropertyBuilder>(fk);
                var fkColumnProp = tableClass.FindByMeta<PropertyBuilder>(fk.ForeignKeyColumn);
                var fkTableNamespace = TableNamespace(fk.PrimaryKeyColumn.Table);
                var fkTableClassName = TableClassName(fk.PrimaryKeyColumn.Table);
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

                if (IsCascade(fk.DeleteCascadeAction))
                    line.Append("\n\t.OnDelete(DeleteBehavior.Delete)");
                else if (IsSetNull(fk.DeleteCascadeAction))
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

        protected virtual void OnBeforeIndexLineAdded(RawLineBuilder line, IIndex index)
        {

        }

        protected virtual string FluentColumnType(IColumn column)
        {
            if (column is IColumnWithDateTimePrecision)
            {
                var cdtp = column as IColumnWithDateTimePrecision;
                if (cdtp.DateTimePrecision.HasValue)
                    return $"{column.DataType}({cdtp.DateTimePrecision})";
            }

            if (column.NumericPrecision.HasValue && column.NumericScale.HasValue && column.NumericScale != 0 && DataTypeResolver.NeedFluentPrecisionSpecification(column))
                return $"{column.DataType}({column.NumericPrecision}, {column.NumericScale})";

            if (column.CharacterMaximumLength.HasValue)
                return $"{column.DataType}({(column.CharacterMaximumLength == -1 ? "MAX" : $"{column.CharacterMaximumLength}")})";

            return column.DataType;
        }
    }
}
