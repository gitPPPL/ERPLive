namespace travelexpensemanagement.Models.Gate_Entry.Transaction
{
    public class VISITOR
    {
        public int? YEAR_CODE { get; set; }
        public int? COMP_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public string? SLIP_NO { get; set; }
        public string? NAME { get; set; }
        public string? ORGANIZATION { get; set; }
        public string? ADDRESS { get; set; }
        public int? MEET_CODE { get; set; }
        public string? MEET_NAME { get; set; }
        public string? IN_TIME { get; set; }
        public DateTime? OUT_DATE { get; set; }
        public string? OUT_TIME { get; set; }
        public string? PURPOSE { get; set; }
        public string? MOBILE_NO { get; set; }
        public string? VEHICLE_NO { get; set; }
        public string? MATERIAL { get; set; }
        public string? CARD_NO { get; set; }
        public int? CARD_CODE { get; set; }
        public byte[]? IMG_FILE { get; set; }
        public string? FILE_NAME { get; set; }
        public string? REMARKS { get; set; }
        public int? UUSER { get; set; }
        public DateTime? UDATE { get; set; }
        public int? EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string? AED { get; set; }
        public string? WSID { get; set; }
        public string? LIP { get; set; }
        public string? LID { get; set; }
    }
    public class VisitorWrapper
    {
        public VISITOR Visitor { get; set; }
        public Base64File Image { get; set; }
    }

    public class Base64File
    {
        public string FileName { get; set; }
        public string Base64Content { get; set; }
    }
}
