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
                ConnectionString = "Server=ps-sql.dev;Database=Acme;user id=acme;password=-acmepw2016-",
                //IncludedTables = new List<string>()
                //{
                //    "Carrier",
                //    "CarrierRegistry.CarrierContact"
                //},
                //IncludedSchemas = new List<string>()
                //{
                //    "Core",
                //    "CarrierRegistry"
                //}
                ExcludedTables = new List<string>(){"sysdiagrams"}
            };
            g.Generate();

            
        }
    }
}
