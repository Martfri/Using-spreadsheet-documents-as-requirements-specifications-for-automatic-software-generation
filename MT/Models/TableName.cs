namespace MT.Models
{
    public class TableName
    {
        public string name { get; set; }
        public int tableCount { get; set; }

        public List<Column> columns { get; set; }
    }
}
