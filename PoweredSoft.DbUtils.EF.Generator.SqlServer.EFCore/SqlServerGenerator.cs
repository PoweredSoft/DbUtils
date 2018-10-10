using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
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
                                    .Add(RawLineBuilder.Create("#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings."))
                                    .Add(RawLineBuilder.Create($"optionsBuilder.UseSqlServer(\"{Options.ConnectionString}\")"));
                            })
                        );

                        // model creating.
                        contextClass.Method(m => m
                            .AccessModifier(AccessModifiers.Protected)
                            .Override(true)
                            .ReturnType("void")
                            .Name("OnModelCreating")
                            .Parameter(p => p.Type("ModelBuilder").Name("modelBuilder"))
                        );
                    });
                });
            };

            if (Options.OutputToSingleFile)
                GenerationContext.SingleFile(fb => generateContextInline(fb));
            else
                GenerationContext.FileIfPathIsSet(fb => generateContextInline(fb));
        }
    }
}
