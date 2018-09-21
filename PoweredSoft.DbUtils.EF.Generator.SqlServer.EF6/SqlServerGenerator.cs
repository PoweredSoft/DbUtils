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

            GenerationContext.SaveToDisk(Encoding.UTF8);
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
            var nsName = Options.Namespace.Replace("[SCHEMA]", table.Schema);

            fileBuilder.Namespace(nsName, true, ns =>
            {
                ns.Class(tableClass =>
                {
                    // set basic info.
                    tableClass.Partial(true).Name(table.Name);

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
                                .Virtual(true)
                                .SetAccessModifier(AccessModifiers.Public)
                                .Type(typeName);
                        });
                    });
                });
            });
        }

       
    }
}
