namespace MT.Models
{
    public class Column
    {
        public string Name { get; set; }
        public int count { get; set; }
        public int tableCount { get; set; }

        public List<Entry> entries { get; set; }
    }
}
