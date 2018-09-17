using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.Schema.SqlServer
{
    public class ForeignKey : IForeignKey
    {
        public string Name { get; set;  }
        public IColumn ForeignKeyColumn => SqlServerForeignKeyColumn;
        public IColumn PrimaryKeyColumn => SqlServerPrimaryKeyColumn;
        public string DeleteCascadeAction { get; set; }
        public string UpdateCascadeAction { get; set; }

        public Column SqlServerForeignKeyColumn { get; set; }
        public Column SqlServerPrimaryKeyColumn { get; set; }

        public override string ToString()
        {
            var ret = $"{Name} - {ForeignKeyColumn} -> {PrimaryKeyColumn} | Delete: {DeleteCascadeAction} | Update: {UpdateCascadeAction}";
            return ret;
        }
    }
}
