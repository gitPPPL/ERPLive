namespace travelexpensemanagement.Models.Weighbridge.Transaction
{

    public class WBEntryModel
    {
        public List<TypeWB2> WB2Data { get; set; }
        public string DOC_ID { get; set; }
        public string V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string V_SHIFT { get; set; }
        public string WB_TYPE { get; set; }
        public string GATE_TYPE { get; set; }
        public int? GATE_NO { get; set; }
        public decimal? PARTY_QTY { get; set; }
        public int? PARTY_CODE { get; set; }
        public string GROSS_NO { get; set; }
        public string TARE_NO { get; set; }
        public string VEHICLE_NO { get; set; }
        public string REMARKS { get; set; }
        public int? STATUS { get; set; }
        public DateTime? STATUS_DATE { get; set; }
        public decimal? NET_WGT { get; set; }
        public string FINAL_TYPE { get; set; }
        public string FINAL_REM { get; set; }
        public decimal? PARTY_GROSSWT { get; set; }
        public decimal? PARTY_TRWT { get; set; }
        public string PARTY_WBNO { get; set; }
        public int? SMALL_BAG { get; set; }
        public int? MEDIUM_BAG { get; set; }
        public int? LARGE_BAG { get; set; }
        public string ? SaveOrUpdate { get; set; }


        public string? oldGateType { get; set; }
        public int? oldGateNo { get; set; }
    
     
    }

    public class TypeWB2
    {
        public string? V_SHIFT { get; set; }
        public string? TYPE { get; set; }
        public decimal? WEIGHT { get; set; }
        public decimal? TARE_WGT { get; set; }
        public decimal NET_WGT { get; set; }
        public DateTime? WGT_DATE { get; set; }
        public string? WGT_TIME { get; set; }
        public int? FROM_PLACE { get; set; }
        public string? FROM_NAME { get; set; }
        public int? TO_PLACE { get; set; }
        public string? TO_NAME { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public string? REMARKS { get; set; }
        public string? STATUS { get; set; }
        public string? Ref_type { get; set; }
        public int? Ref_no { get; set; }
        public int? SNO { get; set; }
        public string? wb_time { get; set; }
        public string? COND { get; set; }
        public decimal? MOIS_PER { get; set; }
        public decimal? MOIS_WT { get; set; }

    }

}
