using PoweredSoft.DbUtils.Schema.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.SqlServer.Extensions
{
    public static class TableExtensions
    {
        public static string GetTableSchema(this ITable table) => ((Table)table).Schema;
    }
}
