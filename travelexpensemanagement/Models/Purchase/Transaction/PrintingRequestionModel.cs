namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class PrintingRequestionModel
    {
        public string SearchCode { get; set; }
        public string VNo { get; set; }
        public string VType { get; set; }
        public string VDate { get; set; }
        public string Department { get; set; }
        public string OwnerName { get; set; }
        public string Place { get; set; }
        public string ValidDate { get; set; }
        public string TargetDate { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }

    }
    public class PrintingRequestModel
    {
        public string ACTION { get; set; }
        public PrintingHeader Header { get; set; }
        public List<PrintingDetail> Details { get; set; }
        public List<IFormFile> Files { get; set; }
    }
    public class PrintingHeader
    {
        public string DocNo { get; set; }
        public DateTime DocDate { get; set; }
        public string Place { get; set; }
        public string Status { get; set; }
        public string Department { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string RequestBy { get; set; }
        public string RequestByName { get; set; }
        public string Reason { get; set; }
        public string Remarks { get; set; }
    }
    public class PrintingDetail
    {
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public string Make { get; set; }
        public string MatType { get; set; }
        public string Unit { get; set; }
        public decimal Qty { get; set; }
        public string PrintingType { get; set; }
        public string Finish { get; set; }
        public string PlaceUse { get; set; }
        public string Reason { get; set; }
        public string Priority { get; set; }
        public string WorkType { get; set; }
        public string ScrapType { get; set; }
        public string Remarks { get; set; }
    }
    public class PrintAttachmentModel
    {
        public IFormFile File { get; set; }
        public string? DOC_ID { get; set; }
        public int V_NO { get; set; }
        public string V_TYPE { get; set; }
        public DateTime V_DATE { get; set; }

        public byte[] IMG_FILE { get; set; }
        public string FILE_NAME { get; set; }
        public string FILE_TYPE { get; set; }
    }
    public class GetByIdRequest
    {
        public int VNo { get; set; }
        public string VType { get; set; }
    }
    public class PrintingRewuestimge
    {
        public string DOC_ID { get; set; }
        public int V_NO { get; set; }
        public string V_TYPE { get; set; }
        public DateTime V_DATE { get; set; }
        public byte[] IMG_FILE { get; set; }
        public string FILE_NAME { get; set; }
        public string FILE_TYPE { get; set; }
    }
    public class PrintReportModelPrintingRequestion
    {
        public string VType { get; set; }
        public string VNo { get; set; }
    }

}
