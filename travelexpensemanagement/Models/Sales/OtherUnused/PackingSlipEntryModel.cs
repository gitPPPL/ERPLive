namespace travelexpensemanagement.Models.Sales.OtherUnused
{
    public class PackingSlipEntryModel
    {
        public string? V_TYPE { get; set; }
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? PARTY_CODE { get; set; }
        public int? PLACE_CODE { get; set; }
        public string? PORD_TYPE { get; set; }
        public int? PORD_NO { get; set; }
        public string? REF_TYPE { get; set; }
        public int? REF_NO { get; set; }
        public string? SHIFT { get; set; }
        public string? SLIP_NO { get; set; }
        public string? REMARKS { get; set; }
        public string? MAC_TYPE { get; set; }
        public int? MAC_CODE { get; set; }
        public int? GODOWN_CODE { get; set; }
        public int? STATUS { get; set; }
        public List<PackingSlipEntryRowModel> Items { get; set; } = new();

        public string ACTION { get; set; }

    }

    public class PackingSlipEntryRowModel
    {
        public int? PLACE_CODE { get; set; }
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public decimal? ITEM_SIZE { get; set; }
        public int? NOS { get; set; }
        public decimal? WASTE { get; set; }
        public string? BALE_NO { get; set; }
        public string? LOT_NO { get; set; }
        public decimal? GROSS_QTY { get; set; }
        public decimal? TARE_QTY { get; set; }
        public decimal? QTY { get; set; }
        public decimal? MTR { get; set; }
        public decimal? AVG_QTY { get; set; }
        public decimal? GRAM { get; set; }
        public decimal? PACK_QTY { get; set; }
        public string? REMARKS { get; set; }
        public int? TENACITY_CODE { get; set; }
        public int? STATUS_CODE { get; set; }
        public int? UOM_CODE { get; set; }
        public string? UOM_NAME { get; set; }
        public int? MAKE_CODE { get; set; }
        public int? FROM_DEPT { get; set; }
        public int? TO_DEPT { get; set; }
        public string? MAC_TYPE { get; set; }
        public int? MAC_CODE { get; set; }
        public decimal? LAND_RATE { get; set; }
        public decimal? LAND_AMT { get; set; }
        public string? REF_TYPE { get; set; }
        public int? REF_NO { get; set; }
        public string? LMRC_TYPE { get; set; }
        public int? LMRC_NO { get; set; }
        public string? CREEL_NO { get; set; }
        public string? QC_SAMPLE { get; set; }
        public int? flg { get; set; }
        public int? LOG_FLG { get; set; }

    }

    public class PackingSlipValidationRequest
    {
        public string VType { get; set; }
        public int VNo { get; set; }
        public string VDate { get; set; }
        public string Shift { get; set; }
        public string MachineType { get; set; }
        public string DOId { get; set; }
        public List<PackingSlipValidationRow> Rows { get; set; } = new();
    }

    public class PackingSlipValidationRow
    {
        public int ItemCode { get; set; }
        public string ItemName { get; set; }
        public string BaleNo { get; set; }
        public string LotNo { get; set; }
        public decimal NetWeight { get; set; }
        public int Tenacity { get; set; }
        public int Status { get; set; }
    }

    public class PackingSlipListModel
    {
        public int? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public string? PARTY { get; set; }
        public string? PLACE { get; set; }
        public string? SHIFT { get; set; }
        public string? SLIP_NO { get; set; }
        public string? PORD_TYPE { get; set; }
        public string? REMARKS { get; set; }
        public string? STATUS { get; set; }
    }

    public class CopyFromItemModel
    {
        public string? BALE_NO { get; set; }
        public string? V_NO { get; set; }
        public string? V_TYPE { get; set; }
        public string? V_DATE { get; set; }
        public string? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public string? UOM_NAME { get; set; }
        public string? LOT_NO { get; set; }
        public string? GROSS_QTY { get; set; }
        public string? TARE_QTY { get; set; }
        public string? QTY { get; set; }
        public string? MTR { get; set; }
        public string? AVG_QTY { get; set; }
        public string? GRAM { get; set; }
        public string? TENACITY { get; set; }
        public string? STATUS { get; set; }
        public string? REMARKS { get; set; }
        public string? LMRC_TYPE { get; set; }
        public string? LMRC_NO { get; set; }
        public string? MAC_TYPE { get; set; }
        public string? MAC_CODE { get; set; }
        public string? PLACE_CODE { get; set; }
        public string? UNIT_CODE { get; set; }
        public string? TENACITY_CODE { get; set; }
        public string? STATUS_CODE { get; set; }
        public string? INCH { get; set; }
    }

    public class PackingValidationResponse
    {
        public bool status { get; set; } = false;
        public string? message { get; set; }
        public string? field { get; set; }
        public int? rowIndex { get; set; }
        public List<string>? warnings { get; set; }
    }
}
