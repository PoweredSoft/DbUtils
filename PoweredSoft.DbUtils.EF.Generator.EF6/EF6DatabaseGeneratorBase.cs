using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.EF.Generator.EF6.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.EF6
{
    public abstract class EF6DatabaseGeneratorBase<TSchema, TOptions> : Generator.DatabaseGeneratorBase<TSchema, TOptions>
        where TSchema : IDatabaseSchema
        where TOptions : IEF6GeneratorOptions
    {
        protected override string CollectionInstanceType() => "System.Collections.Generic.List";
        public override bool HasManyShouldBeVirtual() => true;
        public override bool OneToShouldBeVirtual() => true;
        public override bool ForeignKeysShouldBeVirtual() => true;

        /// <summary>
        /// Should return if its a identity (auto increment, or new sequential guid example for sql server
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        protected virtual bool IsGenerateOptionIdentity(IColumn column) => column.IsAutoIncrement;

        protected virtual string ToTableFluent(ITable table) => $"ToTable(\"{table.Name}\")";

        public override List<ITable> ResolveTablesToGenerate()
        {
            var ret = base.ResolveTablesToGenerate();
            ret = ret.Where(t => !t.IsManyToMany()).ToList();
            return ret;
        }

        protected override void GenerateManyToMany(ITable table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var manyToManyList = table.ManyToMany().ToList();
            manyToManyList.ForEach(fk =>
            {
                // get the other foreign key of this many to many.
                var otherFk = fk.ForeignKeyColumn.Table.ForeignKeys.FirstOrDefault(t => t != fk);

                // other table attached to this many to many.
                var otherPk = otherFk.PrimaryKeyColumn.Table;

                // skip if other table is not being generated.
                if (!TablesToGenerate.Contains(otherPk))
                    return;

                // pluralize this name.
                var propName = Pluralize(otherPk.Name);
                propName = tableClass.GetUniqueMemberName(propName);

                // the type of the property.
                var pocoType = TableClassFullName(otherPk);
                var propType = $"System.Collections.Generic.ICollection<{pocoType}>";
                var defaultValue = $"new {CollectionInstanceType()}<{pocoType}>()";

                // generate property :)
                tableClass.Property(p => p.Virtual(true).Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Many to Many").Meta(Tuple.Create(NavigationKind.ManyToMany, fk)));
            });
        }

        protected override void BeforeSaveToDisk()
        {
            base.BeforeSaveToDisk();
            GenerateFluentConfigurations();
        }


        protected override void GenerateContext()
        {
            var fileBuilder = ResolveContextFileBuilder();
            var contextNamespace = ContextNamespace();
            var contextClassName = ContextClassName();

            fileBuilder.Using("System.Linq");
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
                        .BaseParameter($"\"{Options.ConnectionStringName ?? Options.ConnectionString}\"")
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

                        TablesToGenerate.ForEach(table =>
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
        }

        private void GenerateFluentConfigurations()
        {
            TablesToGenerate.ForEach(table =>
            {
                GenerateEntityFluentConfiguration(table);
            });
        }

        // TODO refactor. move to SQL SERVER EF6
        /// <summary>
        /// //method.RawLine($"return Database.SqlQuery<{outputType}>(\"SELECT NEXT VALUE FOR [{sequence.Schema}].[{sequence.Name}];\").First()");
        /// </summary>
        /// <param name="method"></param>
        /// <param name="outputType"></param>
        /// <param name="sequence"></param>
        //protected abstract void GenerateGetNextSequenceLines(MethodBuilder method, string outputType, ISequence sequence);

        

        private void GenerateEntityFluentConfiguration(ITable table)
        {
            var tableNamespace = TableNamespace(table);
            var tableFluentConfigurationClassName = $"{TableClassName(table)}{Options.FluentConfigurationClassSuffix}";
            var tableClassName = TableClassName(table);
            var tableClassFullName = TableClassFullName(table);
            var entityClass = GenerationContext.FindClass(tableClassName, tableNamespace);
            var contextNamespace = ContextNamespace();

            // set the path.
            var outputDir = Options.ContextOutputDir ?? Options.OutputDir;
            var fileName = Options.ContextOutputSingleFileName ?? Options.OutputSingleFileName ?? $"{tableFluentConfigurationClassName}.generated.cs";
            var path = $"{outputDir}{Path.DirectorySeparatorChar}{fileName}";

            GenerationContext.File(path, fileBuilder =>
            {
                // set the namespace.
                fileBuilder.Namespace(contextNamespace, true, ns =>
                {
                    ns.Class(tableFluentConfigurationClassName, true, fluentConfigClass =>
                    {
                        fluentConfigClass
                            .Partial(true)
                            .Inherits($"System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<{tableClassFullName}>")
                            .Constructor(constructor =>
                            {
                                constructor.AddComment("Table mapping & keys");

                                // to table mapping.
                                constructor.RawLine(ToTableFluent(table));

                                // pk mapping.
                                var pk = table.Columns.FirstOrDefault(t => t.IsPrimaryKey);
                                    var pkProp = entityClass.FindByMeta<PropertyBuilder>(pk);
                                    constructor.RawLine($"HasKey(t => t.{pkProp.GetName()})");

                                    constructor.AddComment("Columns");

                                // columns mapping.
                                table.Columns.ForEach(column =>
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
                                        if (IsGenerateOptionIdentity(column))
                                            columnLine.Append(".HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)");
                                        else
                                            columnLine.Append(".HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)");

                                    /*
                                    // TODO make overridable class method here called IsGenerateOptionIdentity(IColumn)
                                    if (column.IsAutoIncrement)
                                        columnLine.Append(".HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)");
                                    else if (column.DataType == "uniqueidentifier" && column.DefaultValue.IndexOf("newsequentialid", StringComparison.InvariantCultureIgnoreCase) > -1)
                                        columnLine.Append(".HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)");
                                    else
                                        columnLine.Append(".HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)");
                                        */
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
                                table.ForeignKeys.ForEach(fk =>
                                {
                                    var fkProp = FindNavigation(entityClass, fk);
                                    var fkColumnProp = entityClass.FindByMeta<PropertyBuilder>(fk.ForeignKeyColumn);

                                    // if null meaning its being filtered. (excluded table from generation)
                                    if (fkProp != null)
                                    {
                                        var line = RawLineBuilder.Create();
                                        var primaryNamespace = TableNamespace(fk.PrimaryKeyColumn.Table);
                                        var primaryClassName = TableClassName(fk.PrimaryKeyColumn.Table);
                                        var primaryEntity = GenerationContext.FindClass(primaryClassName, primaryNamespace);

                                        PropertyBuilder reverseNav;
                                        if (fk.PrimaryKeyColumn.Table == fk.ForeignKeyColumn.Table)
                                            reverseNav = FindNavigation(primaryEntity, fk, NavigationKind.HasMany);
                                        else
                                            reverseNav = FindNavigation(primaryEntity, fk);

                                        if (fk.ForeignKeyColumn.IsNullable)
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


                                    var manyToManyTable = mtm.ForeignKeyColumn.Table;
                                    var otherFk = mtm.ForeignKeyColumn.Table.ForeignKeys.First(t => t.ForeignKeyColumn.PrimaryKeyOrder > 1);
                                    var otherFkTable = otherFk.PrimaryKeyColumn.Table;
                                    var manyProp = FindNavigation(entityClass, mtm);

                                    // exclude if not being generated.
                                    if (!TablesToGenerate.Contains(otherFk.PrimaryKeyColumn.Table))
                                        return;

                                    var otherNamespace = TableNamespace(otherFkTable);
                                    var otherClassName = TableClassName(otherFkTable);
                                    var otherEntity = GenerationContext.FindClass(otherClassName, otherNamespace);
                                    var otherProp = FindNavigation(otherEntity, otherFk);

                                    var line = RawLineBuilder.Create();
                                    line.Append($"HasMany(t => t.{manyProp.GetName()})");
                                    line.Append($".WithMany(t => t.{otherProp.GetName()})");
                                    line.Append($".Map(t => t.{ToTableFluent(manyToManyTable)}");
                                    line.Append($".MapLeftKey(\"{mtm.ForeignKeyColumn.Name}\")");
                                    line.Append($".MapRightKey(\"{otherFk.ForeignKeyColumn.Name}\"))");
                                    constructor.Add(line);
                                });
                            });
                    });
                });
            });
        }
    }
}
