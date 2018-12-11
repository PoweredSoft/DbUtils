
using PoweredSoft.DbUtils.EF.Generator.EFCore.SqlServer;
//using PoweredSoft.DbUtils.EF.Generator.EFCore.MySql;
using PoweredSoft.DbUtils.Schema.SqlServer.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace dev_cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var gen = new DatabaseGenerator();
            gen.InitializeOptionsWithDefault();
            gen.Options.ConnectionString = "Server=ps-sql.dev;Database=TBMS;user id=acme;password=-acmepw2016-";
            //gen.Options.ConnectionString = "server=192.168.100.154;uid=dlebee;pwd=-pssql2016-;database=Acme";
            
            gen.Options.ContextName = "AcmeContext";
            gen.Options.Namespace = "Acme.Dal";
            gen.Options.ConnectionStringName = "Acme";
            gen.Options.GenerateContextSequenceMethods = true;
            gen.Options.AddConnectionStringOnGenerate = true;
            gen.Options.CleanOutputDir = true;

            gen.Options.DynamicAssemblies = new List<string>
            {
                "C:\\PS\\DbUtils\\Acme.DbUtils\\bin\\Debug\\netstandard2.0\\Acme.DbUtils.dll"
            };

            var basePath = "C:\\Users\\PS-DEV2\\source\\repos\\blah1\\blah1\\Dal";

#if false
            
            gen.Options.OutputDir = null;
            gen.Options.ContextOutputDir = $"{basePath}\\Context";
            gen.Options.EntitiesOutputDir = $"{basePath}\\Entities";
            gen.Options.EntitiesInterfacesOutputDir = $"{basePath}\\EntitiesInterfaces";
            gen.Options.ModelsInterfacesOutputDir = $"{basePath}\\ModelsInterfaces";
            gen.Options.ModelsOutputDir = $"{basePath}\\ModelsInterfaces";
#endif

#if true
            gen.Options.OutputDir = basePath;
            gen.Options.ContextOutputSingleFileName = "Context.Generated.cs";
            gen.Options.EntitiesOutputSingleFileName = "Entities.Generated.cs";
            gen.Options.EntitiesInterfacesOutputSingleFileName = "EntitiesInterfaces.generated.cs";
            gen.Options.ModelsInterfacesOutputSingleFileName = "ModelsInterfaces.generated.cs";
            gen.Options.ModelsOutputSingleFileName = "Models.generated.cs";
#endif

            gen.Options.GenerateInterfaces = true;
            gen.Options.EntityInterfaceNamespace = "Acme.Dal.Core";


            // models.
            gen.Options.GenerateModelPropertyAsNullable = true;
            gen.Options.ModelNamespace = "Acme.Models";
            gen.Options.GenerateModels = true;

            // model interfaces.
            gen.Options.ModelInterfaceNamespace = "Acme.Core.Models";
            gen.Options.GenerateModelsInterfaces = true;

            gen.Generate();
        }
    }
}
