namespace travelexpensemanagement.Models.QualityControl.Transaction
{
    public class LoomFabricEntryModel
    {
        public List<Prod2QCDetailModel> Prod2QCData { get; set; }
        public int V_No { get; set; }
        public string? VType { get; set; }
        public DateTime V_DATE { get; set; }        
        public string? SHIFT { get; set; }
        public int? PLACE_CODE { get; set; }
        public int? EMP_CODE { get; set; }
        public string? REMARKS { get; set; }
        public string? QCTIME { get; set; }
        public int? QC_INCHARGE { get; set; }
        public int? CHEMIST { get; set; }
        public string? QC_INCHARGENAME { get; set; }
        public string? CHEMISTNAME { get; set; }        
        public int? SRNO { get; set; }
        public string? SaveOrUpdate { get; set; }

    }

    public class Prod2QCDetailModel
    {       
        public int SNO { get; set; }
        public int? PLACE_CODE { get; set; }
        public int? LOOM_CODE { get; set; }
        public int EMP_CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public int? PTYPE_CODE { get; set; }
        public string PTYPE_NAME { get; set; }
        public decimal WIDTH { get; set; }
        public decimal? GRAM { get; set; }
        public string MESH { get; set; }
        public int? MESH_CODE { get; set; }
        public int? COLOR_CODE { get; set; }
        public string COLOR_NAME { get; set; }
        public int? RUNNO { get; set; }
        public string LOOM_TYPE { get; set; }
        public string MAKE_T { get; set; }
        public string DNR { get; set; }
        public decimal? RESULT1 { get; set; }
        public string? REMARKS1 { get; set; }
        public decimal? RESULT2 { get; set; }
        public string? REMARKS2 { get; set; }
        public decimal? PRKG { get; set; }
        public decimal? WASTE { get; set; }
        public decimal? PSIZE { get; set; }
        public string? REMARKS { get; set; }
        public decimal? CPRDN { get; set; }
        public string? PAISA_TYPE { get; set; }
        public string? PAISA_SIZE { get; set; }
        public int? PAISA_MTR { get; set; }
        public string? PAISA_TYPE1 { get; set; }
        public string? PORD_TYPE { get; set; }
        public int? PORD_NO { get; set; }
        public short? COND1 { get; set; }
        public short? COND2 { get; set; }
        public string? SHIFT_SCH { get; set; }
        public int? REPORT_FILTER { get; set; }
        public decimal? TIME1_WIDTH { get; set; }
        public decimal? TIME2_WIDTH { get; set; }
        public decimal? TIME3_WIDTH { get; set; }
        public decimal? TIME4_WIDTH { get; set; }
        public decimal? TIME5_WIDTH { get; set; }
        public decimal? PC_LOWMELT { get; set; }
        public decimal? GLUE_CONTENT { get; set; }
        public decimal? OTHERS { get; set; }
        public decimal? YELLOWP { get; set; }
        public decimal? BLUEP { get; set; }
        public decimal? OTHERP { get; set; }
        public string? GRADE { get; set; }
        public decimal? YELLOW160C { get; set; }
        public decimal? MOISTURE { get; set; }
        public decimal? BULKDENSITY { get; set; }
        public decimal? PH_FLAKES { get; set; }
        public decimal? OVERSIZED { get; set; }       
        public int? SRNO { get; set; }    
        public decimal? WARP_ELONG { get; set; }
        public decimal? WEFT_ELONG { get; set; }
        public decimal? WARP_MESH { get; set; }
        public decimal? WEFT_MESH { get; set; }
        public string SUPPLY_TYPE { get; set; }
        public string COLOR_TYPE { get; set; }
    }

}
