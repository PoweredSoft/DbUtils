using System;
using System.Collections.Generic;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EFCore.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var g = new SqlServerGenerator();
            g.Options = new SqlServerGeneratorOptions
            {
                OutputDir = @"C:\test",
                OutputSingleFileName = "All.generated.cs",
                CleanOutputDir = true,
                Namespace = "Acme.[SCHEMA].Dal",
                ContextName = "AcmeContext",
                ConnectionString = "Server=ps-sql.dev;Database=Acme;user id=acme;password=-acmepw2016-",
                GenerateInterfaces = false,
                GenerateModels = false,
                GenerateModelPropertyAsNullable = true,
                GenerateModelsInterfaces = false,
                GenerateContextSequenceMethods = false,
                //AddConnectionStringOnGenerate = true,
                IncludedSchemas = new List<string>()
                {
                    "WorkOrder", "Storage"
                },
                ModelInheritances = new List<string>()
                {
                    //"ITestInherit<[ENTITY], [CONTEXT]>"
                }
            };
            g.Generate();


        }
    }
}
