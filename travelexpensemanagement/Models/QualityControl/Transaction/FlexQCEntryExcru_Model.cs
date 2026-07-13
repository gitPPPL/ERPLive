
namespace travelexpensemanagement.Models.QualityControl.Transaction
{
    public class FlexQCEntryExcru_Model
    {
        public FlexQCEntryExcru_Header Header { get; set; }
        public List<FlexQCEntryExcru_Details> Deatils { get; set; }
    }

    public class FlexQCEntryExcru_Header
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
    public class FlexQCEntryExcru_Details
    {   
        public int? ITEM_CODE { get; set; }
        public int? DEPT_CODE { get; set; }     
        public string? Item_Name { get; set; }
        public string? DEPT_NAME { get; set; }     
        public decimal? NET_WT { get; set; }      
        public string? JUMBO_NO { get; set; }      
        public string? REMARKS { get; set; }
        public decimal? FOAM { get; set; }
        public decimal? RUBBER { get; set; }
        public string? BatchNo  { get; set; }
        public decimal? BagNo { get; set; }
        public decimal? WBWt { get; set; }
        public decimal? GrWt { get; set; }
        public decimal? MFI { get; set; }
        public decimal? TrWt { get; set; }
        public decimal? ASH_CONTENT { get; set; }
        public decimal? LD { get; set; }
        public decimal? COLOR_MIX { get; set; }
        public decimal? WRAPPER { get; set; }
        public decimal? MOIS_CONTENT { get; set; }
        public decimal? BOTTOM { get; set; }
        public decimal? PP { get; set; }
        public string? REfType { get; set; }       
        public int? Refcode { get; set; }
        public decimal? HD { get; set; }       
         public int? STATUS_CODE { get; set; } 
        public string? STATUSS { get; set; }   
        public string? Ref_Type { get; set; }
        public int? Ref_No { get; set; }

    }

}