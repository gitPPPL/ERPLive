namespace travelexpensemanagement.Models.Sales.Transaction
{
    public class SalesExportCostingModel
    {
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? PARTY_CODE { get; set; }
        public int? DEL_LOCATION { get; set; }
        public int? AGENT_CODE { get; set; }
        public decimal? COMM_RATE { get; set; }
        public string? CURRENCY { get; set; }
        public decimal? EX_RATE { get; set; }
        public decimal? EX_FRTRATE { get; set; }
        public decimal? LOADING_QTY { get; set; }
        public decimal? STUFF_QTY { get; set; }
        public decimal? OCEAN_FRTUSD { get; set; }
        public decimal? OCEAN_FRTEXPS { get; set; }
        public decimal? OCEAN_ANSCH { get; set; }
        public decimal? OCEAN_ILHAUCOST { get; set; }
        public decimal? OCEAN_PORTHANDCH { get; set; }
        public decimal? OCEAN_BLFEE { get; set; }
        public decimal? OCEAN_SEALCOST { get; set; }
        public decimal? OCEAN_DOCFEEEXPORT { get; set; }
        public decimal? CONCER_EXPS { get; set; }
        public decimal? SHIP_RAILFRT { get; set; }
        public decimal? BUSY_SEASONCH { get; set; }
        public decimal? LOCAL_TPTCOST { get; set; }
        public decimal? INSU_COST { get; set; }
        public decimal? BANK_CHARGES { get; set; }
        public decimal? BL_CHARGES { get; set; }
        public decimal? CLEARING_COST { get; set; }
        public decimal? DOOR_DELUSD { get; set; }
        public decimal? DOOR_DELCOST { get; set; }
        public decimal? DOC_CHARGES { get; set; }
        public decimal? CHA_AGENCYCOST { get; set; }
        public decimal? CHA_NOMCOST { get; set; }
        public decimal? CHA_EXAMCOST { get; set; }
        public decimal? CHA_CGMCOST { get; set; }
        public decimal? CHA_CMCCOST { get; set; }
        public decimal? CHA_VGMCOST { get; set; }
        public decimal? CHA_LULCOST { get; set; }
        public string? SAMPLE_COSTING { get; set; }
        public decimal? SAMPLE_COSTAMT { get; set; }
        public decimal? CUSTOM_COST { get; set; }
        public decimal? GRS_LESS1PER { get; set; }
        public decimal? DISC_AMT { get; set; }
        public decimal? EPCG_AMT { get; set; }
        public decimal? ADV_LICAMT { get; set; }
        public decimal? ROAD_TAPEAMT { get; set; }
        public decimal? DDBAK_AMT { get; set; }
        public decimal? COSTPERKG_EXPLANT { get; set; }
        public decimal? ADD_DBK { get; set; }
        public decimal? LESS_MEIS { get; set; }
        public decimal? OUR_OFFERRATE { get; set; }
        public string? COSTING_TYPE { get; set; }
        public string? REMARKS { get; set; }

        public string? ACTION { get; set; }

        public List<SalesExportCostingItemsModel> items { get; set; } = new List<SalesExportCostingItemsModel>();
    }
    public class SalesExportCostingItemsModel
    {
        public int? ITEM_CODE { get; set; }
        public decimal? RATE { get; set; }
        public decimal? QTY { get; set; }
        public string? HSN_CODE { get; set; }
        public string? ITEM_NAME { get; set; }

    }
    public class  SalesExportCostingListModel
    {
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? PARTY_NAME { get; set; }
        public string? DELIVERY_AT { get; set; }
        public string? AGENT_NAME { get; set; }
        public string? ITEM_NAME { get; set; }
    }
}
