using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EF6.Cli
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
                ConnectionStringName = "Acme",
                //GenerateInterfaces = true,
                //GenerateModels = true,
                //GenerateModelPropertyAsNullable = true,
                //GenerateModelsInterfaces = true,
                ModelInheritances = new List<string>()
                {
                    //"ITestInherit<[ENTITY], [CONTEXT]>"
                }
            };
            g.Generate();
        }
    }
}
