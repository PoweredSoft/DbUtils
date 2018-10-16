using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.Schema.MySql
{
    public class ForeignKey : IForeignKey
    {
        public string Name { get; set;  }
        public IColumn ForeignKeyColumn => MySqlForeignKeyColumn;
        public IColumn PrimaryKeyColumn => MySqlPrimaryKeyColumn;
        public string DeleteCascadeAction { get; set; }
        public string UpdateCascadeAction { get; set; }

        public Column MySqlForeignKeyColumn { get; set; }
        public Column MySqlPrimaryKeyColumn { get; set; }

        public override string ToString()
        {
            var ret = $"{Name} - {ForeignKeyColumn} -> {PrimaryKeyColumn} | Delete: {DeleteCascadeAction} | Update: {UpdateCascadeAction}";
            return ret;
        }
    }
}
