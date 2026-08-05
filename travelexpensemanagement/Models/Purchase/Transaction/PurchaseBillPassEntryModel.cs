using travelexpensemanagement.Models.Purchase.Transiction;

namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class PurchaseBillPassEntryModel
    {
        public class PurchaseDetailsDto
        {
            public int? V_No { get; set; }

            public decimal? DeductAmt { get; set; }
            public string DeductNarr { get; set; }

            public string BILL_NO { get; set; }
            public DateTime? BILL_DATE { get; set; }
            public string CHALL_NO { get; set; }
            public DateTime? CHALL_DATE { get; set; }
            public string WAYBILL_NO { get; set; }
            public string TRANSIT_NO { get; set; }
            public decimal? EXCH_RATE { get; set; }

            public int? PARTY_CODE { get; set; }
            public string Party { get; set; }

            public string BILL_ADD1 { get; set; }
            public string BILL_ADD2 { get; set; }
            public string BILL_ADD3 { get; set; }
            public string BILL_CITY { get; set; }
            public string BILL_GST { get; set; }
            public string BILL_PINCODE { get; set; }
            public string BILL_STATE { get; set; }

            public int? SHIP_CODE { get; set; }
            public string ShipTo { get; set; }
            public string SHIP_ADD1 { get; set; }
            public string SHIP_ADD2 { get; set; }
            public string SHIP_ADD3 { get; set; }
            public string SHIP_CITY { get; set; }
            public string SHIP_GST { get; set; }
            public string SHIP_PINCODE { get; set; }
            public string SHIP_STATE { get; set; }

            public string REMARKS { get; set; }

            public int? TRANSPORT_CODE { get; set; }
            public string Transport { get; set; }
            public string TRANSPORT_NAME { get; set; }
            public string TRUCK_NO { get; set; }
            public string CONTAINER_NO { get; set; }

            public string GR_NO { get; set; }
            public DateTime? GR_DATE { get; set; }

            public decimal? FRTPAY_AMT { get; set; }
            public decimal? FRTPAY_TAXPER { get; set; }
            public decimal? FRTPAY_TAX { get; set; }

            public string FRTPAY_NAR { get; set; }

            public string? HOLD_PAY { get; set; }
            public string HOLD_REASON { get; set; }
            public DateTime? HOLD_DATE { get; set; }

            public decimal? TCS_PER { get; set; }
            public decimal? TCS_AMT { get; set; }

            public DateTime? EWB_DATE { get; set; }
            public DateTime? EWB_EXPDATE { get; set; }

            public string EWB_INVNO { get; set; }
        }
        public class PurchaseItemDto
        {
            public string ITEM_CODE { get; set; }
            public string ITEM_NAME { get; set; }
            public string Unit { get; set; }
            public string HSN_CODE { get; set; }
            public decimal NOS { get; set; }
            public decimal RECD_QTY { get; set; }
            public decimal BILL_QTY { get; set; }
            public decimal USD_RATE { get; set; }
            public decimal EXCH_RATE { get; set; }
            public decimal RATE { get; set; }
            public decimal PACK_PER { get; set; }
            public decimal PACK_AMT { get; set; }
            public decimal DISC_PER { get; set; }
            public decimal DISC_AMT { get; set; }
            public string TaxType { get; set; }
            public decimal CGST_PER { get; set; }
            public decimal SGST_PER { get; set; }
            public decimal IGST_PER { get; set; }
            public decimal VAT_PER { get; set; }
            public decimal OTH_AMT { get; set; }
            public string PO_TYPE { get; set; }
            public string PO_NO { get; set; }
            public string REF_TYPE { get; set; }
            public int REF_NO { get; set; }
            public string REQ_TYPE { get; set; }
            public string REQ_NO { get; set; }
            public string KANTA_TYPE { get; set; }
            public string KANTA_NO { get; set; }
            public string Make { get; set; }
            public string Department { get; set; }
            public string DEPT_CODE { get; set; }
            public string TAX_CODE { get; set; }
            public string MAKE_CODE { get; set; }
            public string UOM_CODE { get; set; }
        }

        //------------- CR / DR NOTE --------

        public class DebitNoteRequest
        {
            public string VType { get; set; }
            public int VNo { get; set; }
            public DateTime vDate { get; set; }
            public int billToPartyCode { get; set; }
            public string billToPartyName { get; set; }
            public decimal txtQualityDiffDebitAmt { get; set; }
            public decimal txtQualityDiffDebitTax { get; set; }

            public List<DebitNoteItem> Items { get; set; } = new();

            public decimal totalRcvdQty { get; set; }
            public decimal totalBillQty { get; set; }
            public decimal totalNetAmt { get; set; }
            public decimal totalTCSAmt { get; set; }
            public decimal totalPackingAmt { get; set; }
            public bool isSealedVehicle { get; set; }

            public int mrnNo { get; set; }
            public string mrnType { get; set; }

            public string inputType { get; set; }
            public decimal FreightAmountPay { get; set; }
            public decimal FreightTax { get; set; }
            public decimal FreightTaxPercent { get; set; }
            public bool IsFreightTaxChanged { get; set; }
        }
        public class DebitNoteItem
        {
            public int ItemCode { get; set; }

            public decimal Amount { get; set; }

            public decimal CGSTPer { get; set; }

            public decimal SGSTPer { get; set; }

            public decimal IGSTPer { get; set; }

            public decimal RecdQty { get; set; }

            public decimal BillQty { get; set; }

            public string PoType { get; set; }

            public int PoNo { get; set; }

            public decimal LandRate { get; set; }

            public decimal PORate { get; set; }

            public decimal POLandRate { get; set; }

            public string ItemName { get; set; }

            public string Unit { get; set; }

        }
        public class DebitNoteResponse
        {
            public decimal RateDiffDebitAmt { get; set; }
            public decimal RateDiffDebitTax { get; set; }
            public string RateDiffDebitNarration { get; set; }

            public decimal QualityDiffDebitAmt { get; set; }
            public decimal QualityDiffDebitTax { get; set; }
            public string QualityDiffDebitNarration { get; set; }

            public decimal WeightDiffDebitAmt { get; set; }
            public decimal WeightDiffDebitTax { get; set; }
            public string WeightDiffDebitNarration { get; set; }

            public decimal QCDebitAmt { get; set; }
            public decimal QCDebitTax { get; set; }
            public string QCDebitNarration { get; set; }

            public decimal txtFrtTaxVal { get; set; }
            public int frtDrAcCode { get; set; }

            public HashSet<string> Warnings { get; set; } = new();
        }
        public class DebitNoteCalculationState
        {
            public decimal RateDiffDrGAmt { get; set; }
            public decimal RateDiffDrGTax { get; set; }
            public string RateDiffDrNarr { get; set; } = string.Empty;

            public decimal QltDiffDrAmt { get; set; }
            public decimal QltDiffDrTax { get; set; }
            public string QltDiffDrNarr { get; set; } = string.Empty;

            public decimal Q15DrAmt { get; set; }
            public decimal Q15DrTax { get; set; }
            public string Q15Narr { get; set; } = string.Empty;

            public decimal QtyDiffGAmt { get; set; }
            public decimal QtyDiffGTax { get; set; }
            public string QtyDiffNarr { get; set; } = string.Empty;

            public decimal QCDrAmt { get; set; }
            public decimal QCDrTax { get; set; }
            public string QCDrNarr { get; set; } = string.Empty;

            public decimal frtAmt { get; set; }
            public decimal frtTax { get; set; }
            public string frtNarr { get; set; } = string.Empty;
        }
        public class Q15Result
        {
            public decimal Amount { get; set; }

            public decimal Tax { get; set; }

            public string Narration { get; set; } = string.Empty;

            public decimal DiscItemRate { get; set; }
        }
        public class RMDiscountDetails
        {
            public bool SaudaExists { get; set; }
            public decimal DiscRate { get; set; }
            public decimal Rate { get; set; }
            public decimal AbovePer { get; set; }
            public decimal AboveAmt { get; set; }
        }
        public class OrderRateDetailsDto
        {
            public bool Exists { get; set; }
            public decimal LandRate { get; set; }
            public decimal Rate { get; set; }
            public decimal Qty { get; set; }
        }
        public class QCDetailsDto
        {
            public decimal DeductAmount { get; set; }
            public string Narration { get; set; } = string.Empty;
        }
        public class SaudaInfo
        {
            public int ItemCode { get; set; }
            public decimal Qty { get; set; }
            public decimal Rate { get; set; }
            public DateTime VDate { get; set; }
        }
        public class NaturalBottleDto
        {
            public bool OnlyNatural { get; set; }

            public int ItemCode { get; set; }
        }

        public class PurchaseQtyValidationResult
        {
            public bool IsExcess { get; set; }
            public decimal TotalPurchaseQty { get; set; }
            public decimal AllowedQty { get; set; }
        }

        public class PurchaseRowValidationResult
        {
            public bool IsValid { get; set; } = true;
            public string? pubDefPOInMRN { get; set; }

            // Freight validation
            public bool FreightWarning { get; set; }
            public string FreightMessage { get; set; }

            // HSN validation
            public bool HsnMismatch { get; set; }
            public string HsnMessage { get; set; }

            // QC validation
            public bool QcPending { get; set; }
            public string QcMessage { get; set; }

            public bool Item_vs_Bill_HSNCodeDiff { get; set; } = false;
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string Message { get; set; }
        }

        public class PurchaseBillAttachments
        {
            public string? FILE_NAME { get; set; }
            public string? FILE_Path { get; set; }
            public string FILE_DATA { get; set; }

        }

        public class LandAmountRow
        {
            public int ItemCode { get; set; }
            public int PoNo { get; set; }
            public int Sno { get; set; }
            public int MrnNo { get; set; }
            public string MrnType { get; set; } = "";
            public string VType { get; set; } = "";
            public int VNo { get; set; }

            public int CompCode { get; set; }
            public int BranchCode { get; set; }
            public int YearCode { get; set; }

            public decimal LandAmt { get; set; }
        }

        public class FullPurchaseBillResponse
        {
            public PURCHASE1? Header { get; set; }
            public List<PURCHASE2> Items { get; set; } = new();
            public List<PurchaseBillAttachments> Attachments { get; set; } = new();
            public List<PurchaseBillAttachments> EprAttachments { get; set; } = new();
        }


        public class PBTdsCalculation
        {
            public decimal AdvTds { get; set; }
            public decimal NetAmt { get; set; }
            public decimal DrNote { get; set; }
            public decimal CrNote { get; set; }
            public decimal Tds194Q { get; set; }
        }

        public class CopyFromMenuItem
        {
            public string? Code { get; set; }
            public string? Name { get; set; }
            public string? Modal { get; set; }
        }

        public class CopyFromRequest
        {
            public string vType { get; set; } = string.Empty;
            public int BillTo { get; set; }
            public string BillNo { get; set; } = string.Empty;
            public int VNo { get; set; }
            public string CurrentVType { get; set; } = string.Empty;
        }

        public class CopyFromColumn
        {
            public string Field { get; set; } = "";
            public string Title { get; set; } = "";
        }

        public class CopyFromGridResponse
        {
            public List<CopyFromColumn> Columns { get; set; } = new();
            public List<Dictionary<string, object?>> Rows { get; set; } = new();
        }

        public class PendingApprovalModel
        {
            public string? Type { get; set; }
            public int DocID { get; set; }
            public string? DocDate { get; set; }
            public string? SendBy { get; set; }
            public string? SendDate { get; set; }
            public string? SendTo { get; set; }
            public string? Status { get; set; }
            public string? ApprovalStatus { get; set; }
            public string? Remarks { get; set; }
            public string? CreatedBy { get; set; }
            public string? CreatedDate { get; set; }
            public string? PartyName { get; set; }
            public decimal BillAmount { get; set; }
        }
    }

}


