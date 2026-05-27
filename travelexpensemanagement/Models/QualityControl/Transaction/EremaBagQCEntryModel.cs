namespace travelexpensemanagement.Models.QualityControl.Transaction
{
    public class EremaBagQCEntryModel
    {
        public EremaBagQCEntry_Header Header { get; set; }
        public List<EremaBagQCEntry_Details> Deatils { get; set; }
    }
    public class EremaBagQCEntry_Header
    {
        public string? DOC_ID { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? SHIFT { get; set; }
        public int? PLACE_CODE { get; set; }
        public int? EMP_CODE { get; set; }
        public string? REMARKS { get; set; }
        public string? QCTIME { get; set; }
        public int? QC_INCHARGE { get; set; }
        public int? CHEMIST { get; set; }
        public string? QC_INCHARGENAME { get; set; }
        public string? CHEMISTNAME { get; set; }
        public string? action { get; set; }
        public string? EnmpName { get; set; }
        public string? Place { get; set; }
    }

    public class EremaBagQCEntry_Details
    {

        public int? ITEM_CODE { get; set; }
        public int? DEPT_CODE { get; set; }
        public string? Item_Name { get; set; }
        public string? DEPT_NAME { get; set; }
        public decimal? NET_WT { get; set; }
        public string? REMARKS { get; set; }
        public string? COLOR_NAME { get; set; }
        public string? PTYPE_NAME { get; set; }
        public decimal? WIDTH { get; set; }
        public decimal? GRAM { get; set; }
        public decimal? RESULT1 { get; set; }
        public decimal? RESULT2 { get; set; }
        public decimal? PRKG { get; set; }
        public decimal? WASTE { get; set; }
        public string? DNR { get; set; }
        public string? GRADE { get; set; }
        public decimal? TIME1_WIDTH { get; set; }
        public decimal? TIME2_WIDTH { get; set; }
        public decimal? TIME3_WIDTH { get; set; }
        public decimal? TIME4_WIDTH { get; set; }
        public decimal? TIME5_WIDTH { get; set; }
        public int? COLOR_CODE { get; set; }
        public decimal? CPRDN { get; set; }
        public decimal? MOISTURE { get; set; }
        public decimal? BULKDENSITY { get; set; }
        public decimal? PH_FLAKES { get; set; }
        public decimal? YELLOW160C { get; set; }
        public decimal? OVERSIZED { get; set; }
        public decimal? BLUEP { get; set; }
        public decimal? OTHERP { get; set; }
        public decimal? PC_LOWMELT { get; set; }
        public decimal? GLUE_CONTENT { get; set; }
        public decimal? OTHERS { get; set; }
        public decimal? YELLOWP { get; set; }
        public int? EMP_CODE { get; set; }
        public int? Pord_No { get; set; }
        public string? BatchNo { get; set; }
        public decimal? BagNo { get; set; }
        public decimal? WBWt { get; set; }
        public decimal? GrWt { get; set; }
        public decimal? TrWt { get; set; }
        public string? REfType { get; set; }
        public string? Pord_Type { get; set; }
        public int? Refcode { get; set; }
        public int? HD { get; set; }
        public int? PlaceCode { get; set; }
        public string? PlaceName { get; set; }
    }
}
