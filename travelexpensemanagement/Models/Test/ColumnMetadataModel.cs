namespace travelexpensemanagement.Models.Test
{
    public class ColumnMetadataModel
    {
        public string ColumnName { get; set; } = "";

        public string SqlDataType { get; set; } = "";

        public int? MaxLength { get; set; }

        public byte? Precision { get; set; }

        public byte? Scale { get; set; }

        public bool IsNullable { get; set; }

        public bool IsIdentity { get; set; }

        public string? DefaultValue { get; set; }
    }
}
