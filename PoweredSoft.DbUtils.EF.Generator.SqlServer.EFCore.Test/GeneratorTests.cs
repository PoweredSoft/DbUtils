using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EFCore.Test
{
    [TestClass]
    public class GeneratorTests
    {
        [TestMethod]
        public void MainTest()
        {
            var g = new SqlServerGenerator();
            g.Options = new SqlServerGeneratorOptions
            {
                ConnectionString = "Server=ps-sql.dev;Database=Acme;user id=acme;password=-acmepw2016-",
                IncludedTables = new List<string>()
                {
                    "Carrier",
                    "CarrierRegistry.CarrierContract",
                    "Contact"
                }
            };
            g.Generate();
        }
    }
}
