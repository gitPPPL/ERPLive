namespace travelexpensemanagement.Models
{
    public class SaveFileModel
    {
        public string FileName { get; set; }
        public string RelativePath { get; set; }
        public string FullPath { get; set; }
        public byte[] FileBytes { get; set; }
    }
}
