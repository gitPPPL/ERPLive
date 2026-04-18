namespace travelexpensemanagement.Models.LoomMonitoring
{
    public class LOOM_ALLOC
    {
        public DateTime? V_DATE { get; set; }
        public string? DOC_ID { get; set; }
        public int? YEAR_CODE { get; set; }
        public int? COMP_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public string? LOOM_CODE { get; set; }
        public string? LOOM_NO { get; set; }
        public string? ITEM_CODE { get; set; }
        public string? EMP_CODE { get; set; }
        public string? MESH_CODE { get; set; }
        public string? DNR { get; set; }
        public string? SCH_SHIFT { get; set; }
        public string? PPM { get; set; }
        public int? UUSER { get; set; }
        public DateTime? UDATE { get; set; }
        public int? EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string? AED { get; set; }
        public string? WSID { get; set; }
        public string? LIP { get; set; }
        public string? LID { get; set; }
        public string ACTION { get; set; }
        public string GROUP_CODE { get; set; }
        public int WASTAGE { get; set; }
    }
    public class LoomProductionInfo
    {
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public string? DOC_ID { get; set; }
        public string? LOOM_CODE { get; set; }
        public string? MACHINE_NAME { get; set; }
        public string? EMP_CODE { get; set; }
        public string? EMP_NAME { get; set; }
        public string? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public string? DENIER { get; set; }
        public string? MESH_CODE { get; set; }
        public string? MESH_NAME { get; set; }
        public string? SCH_SHIFT { get; set; }
        public DateTime? LAST_PROD_DATE { get; set; }
        public int? PPM { get; set; }
        public int? WASTAGE { get; set; }

    }
    public class LOOM_2HRS
    {
        public int COMP_CODE { get; set; }
        public int BRANCH_CODE { get; set; }
        public int YEAR_CODE { get; set; }
        public string V_TYPE { get; set; }
        public int V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string DOC_ID { get; set; }
        public string SHIFT { get; set; }
        public DateTime? READING_TIME { get; set; }
        public int? LOOM_CODE { get; set; }
        public string LOOM_NAME { get; set; }
        public int ITEM_CODE { get; set; }
        public string ITEM_NAME { get; set; }
        public int EMP_CODE { get; set; }
        public string EMP_NAME { get; set; }
        public decimal? OP_READING { get; set; }
        public decimal? CL_READING { get; set; }
        public decimal PROD { get; set; }
        public int STD_PPM { get; set; }
        public int ACTUAL_PPM { get; set; }
        public decimal EFFICIENCY { get; set; }
        public string REASON { get; set; }
        public string REMARKS { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
    }
}
