namespace PoweredSoft.DbUtils.EF.Generator.Core
{
    public class ColumnTypeMapping
    {
        public string Table { get; set; }
        public string Column { get; set; }
        public string Type { get; set; }
        public bool IsValueType { get; set; }
    }
}
