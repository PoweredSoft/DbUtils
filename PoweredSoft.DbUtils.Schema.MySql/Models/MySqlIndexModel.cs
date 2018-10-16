namespace PoweredSoft.DbUtils.Schema.MySql.Models
{
    internal class MySqlIndexModel
    {
        public string TableName { get; set; }
        public string IndexName { get; set; }
        public string ColumnName { get; set; }
        public bool IsUniqueConstraint { get; set; }
        public int KeyOrdinal { get; set; }
    }
}
