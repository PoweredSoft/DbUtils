using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EF6
{
    public class SqlServerGenerator : SqlServerGeneratorBase<SqlServerGeneratorOptions>
    {
        protected override void GenerateCode()
        {
            GenerateEntities();
        }

        protected void GenerateEntities()
        {
            TablesToGenerate.ForEach(table =>
            {
                if (Options.OutputToSingleFile)
                    GenerationContext.SingleFile(fb => GenerateEntity(table as Table, fb));
                else
                    GenerationContext.File(fb => GenerateEntity(table as Table, fb));
            });

            // generate foreign keys and navigation properties.
            TablesToGenerate.ForEach(table =>
            {
                GenerateForeignKeys(table as Table);
            });

            GenerationContext.SaveToDisk(Encoding.UTF8);
        }

        private void GenerateForeignKeys(Table table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            table.SqlServerForeignKeys.ForEach(fk =>
            {
                var foreignKeyName = ForeignKeyPropertyName(fk);
                var foreignKeyTypeName = TableClassFullName(fk.SqlServerPrimaryKeyColumn.SqlServerTable);
                tableClass.Property(foreignKeyProp => foreignKeyProp.Virtual(true).Type(foreignKeyTypeName).Name(foreignKeyName));
            });
        }

     


        private void GenerateEntity(Table table, FileBuilder fileBuilder)
        {
            // set the path.
            var outputDir = Options.OutputDir;
            var filePath = Options.OutputToSingleFile
                ? $"{outputDir}\\{Options.OutputSingleFileName}"
                : $"{outputDir}\\{table.Name}.generated.cs";
            fileBuilder.Path(filePath);

            // set the namespace.
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);

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
                                .Type(typeName);
                        });
                    });
                });
            });
        }

       
    }
}
