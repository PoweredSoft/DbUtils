using Microsoft.VisualStudio.TestTools.UnitTesting;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.TestDotNetCore
{
    [TestClass]
    public class TestSchema
    {
        [TestMethod]
        public void LoadSchema()
        {
            var schema = new DatabaseSchema
            {
                ConnectionString = "Server=ps-sql.dev;Database=Acme;user id=sa;password=-pssql2016-"
            };

            schema.LoadSchema();
        }
    }
}
