namespace travelexpensemanagement.Models.Test
{
    public sealed class ConvertedTableData
    {
        public string TableName { get; set; } = "";

        public List<Dictionary<string, object>> Rows { get; set; }
            = new();
    }
}