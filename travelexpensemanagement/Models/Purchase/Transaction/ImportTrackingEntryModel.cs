namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class ImportTrackingEntryModel
    {
    }

    public class ImportTrackingSaudaDetailsModel
    {
        public DateTime? VDate { get; set; }
        public int? PartyCode { get; set; }
        public int? ItemCode { get; set; }
        public int? PayTermCode { get; set; }
        public string? Currency { get; set; }
    }

    public class ImportTrackingSaveRequest
    {
        public string ACTION { get; set; }
        public string V_TYPE { get; set; }
        public int V_NO { get; set; }
        public DateTime V_DATE { get; set; }
        public ImportTrackingHeader Header { get; set; }
        public List<ImportTrackingContainer> Containers { get; set; } = new();
        public List<ImportTrackingExpense> Expenses { get; set; } = new();
        public List<ImportTrackingFile>? NewFiles { get; set; } = new();
    }

    public class ImportTrackingContainer
    {
        public string CONTAINER_NO { get; set; }
        public decimal GRS_WEIGHT { get; set; }
        public decimal PKGS { get; set; }
        public string CONT_SIZE { get; set; }
        public int TRANSPORT_CODE { get; set; }
        public decimal FREIGHT { get; set; }
    }

    public class ImportTrackingExpense
    {
        public string EXPS_TYPE { get; set; }
        public int PARTY_CODE { get; set; }
        public string INV_NO { get; set; }
        public DateTime? INV_DATE { get; set; }
        public decimal INV_AMT { get; set; }
        public string REMARKS { get; set; }
        public string REASON { get; set; }
        public string APPROVED_BY { get; set; }
        public DateTime? PAY_DATE { get; set; }
        public string PAY_REFNO { get; set; }
    }

    public class ImportTrackingFile
    {
        public string FILE_TYPE { get; set; }
        public string FILE_NAME { get; set; }
        public string FILE_DATA { get; set; }
    }

    public class ImportTrackingHeader
    {
        public string SAUDA_TYPE { get; set; }
        public string ITEM_NAME { get; set; }
        public string SIZE { get; set; }
        public string DEL_LOCATION { get; set; }
        public string SUPPLIER_INVNO { get; set; }
        public string CURRENCY { get; set; }
        public string BL_NO { get; set; }
        public string CHA_CHARGEDETAIL { get; set; }
        public string BE_NO { get; set; }
        public string SAMPLE_COLLECTED { get; set; }
        public string ASSESMENT_TYPE { get; set; }
        public string DUTY_PAYMODE { get; set; }
        public string ADVANCE_LICNO { get; set; }
        public string SBLC_NO { get; set; }
        public string VSL_NAME { get; set; }
        public string POD { get; set; }
        public string POL { get; set; }
        public string DETENTION_REASON { get; set; }
        public string DETREASON_APPROVBY { get; set; }
        public string LC_NO { get; set; }
        public string ENHANCE_RATE { get; set; }
        public string UNDER_PROTEST { get; set; }
        public string CHA_CHARGETYPE { get; set; }
        public string CFS_CHARGES { get; set; }
        public string QC_AMTRECD { get; set; }
        public string PI_NO { get; set; }
        public string OFFER_NO { get; set; }
        public string PAY_TYPE { get; set; }
        public string PAY_REFNO { get; set; }

        public int SAUDA_NO { get; set; }
        public int ITEM_CODE { get; set; }
        public int SUPPLIER { get; set; }
        public int AGENT { get; set; }
        public int PAYMENT_TERMS { get; set; }
        public int PAYMENT_BANK { get; set; }
        public int CHA_NAME { get; set; }
        public int BANK_NAME { get; set; }
        public int SHIPLINE_NAME { get; set; }
        public int LC_BENIFICIARY { get; set; }
        public int LC_ISSUEBANK { get; set; }
        public int ORIGIN_COUNTRY { get; set; }

        public decimal EXPECTED_LOADING { get; set; }
        public decimal SUPPLIER_INVAMT { get; set; }
        public decimal EXCH_RATE { get; set; }
        public decimal INV_AMTINR { get; set; }
        public decimal DUTY_AMT { get; set; }
        public decimal HOLLAGE_CHARGES { get; set; }
        public decimal SECURITY_AMT { get; set; }
        public decimal REFUND_AMT { get; set; }
        public decimal FREE_DAYS { get; set; }
        public decimal SBLC_AVLAMT { get; set; }
        public decimal SBLC_DISBAMT { get; set; }
        public decimal SBLC_INTRATESOFR { get; set; }
        public decimal OTH_CHARGES { get; set; }
        public decimal DIFF_DAYS { get; set; }
        public decimal INT_RATE { get; set; }
        public decimal DETENTION_AMT { get; set; }
        public decimal LC_AMT { get; set; }
        public decimal QLTY_CLAIMAMT { get; set; }
        public decimal QC_AMT { get; set; }
        public decimal PASSING_RATE { get; set; }
        public decimal CONCOR_EXPS { get; set; }
        public decimal CHA_AMT { get; set; }
        public decimal CFS_AMT { get; set; }
        public decimal ACCESSIBLE_VALUE { get; set; }
        public decimal BCD { get; set; }
        public decimal SWS { get; set; }
        public decimal GST { get; set; }
        public decimal PAY_AMT { get; set; }

        public DateTime? SAUDA_DATE { get; set; }
        public DateTime? ETD { get; set; }
        public DateTime? ETA { get; set; }
        public DateTime? SUPPLIER_INVDATE { get; set; }
        public DateTime? SCANDOC_VERIFYDATE { get; set; }
        public DateTime? CHAHANDOVER_DATE { get; set; }
        public DateTime? BL_DATE { get; set; }
        public DateTime? BL_RECDDATE { get; set; }
        public DateTime? BL_HANDOVERDATE { get; set; }
        public DateTime? CHKLIST_APPROVDATE { get; set; }
        public DateTime? BE_DATE { get; set; }
        public DateTime? ETA_BEDATE { get; set; }
        public DateTime? IGM_DATE { get; set; }
        public DateTime? SAMPLE_COLDATE { get; set; }
        public DateTime? SAMPLE_TESTREPORTDATE { get; set; }
        public DateTime? ASSESMENT_DATE { get; set; }
        public DateTime? DUTY_PAYDATE { get; set; }
        public DateTime? DOC_HANDOVERDTBANK { get; set; }
        public DateTime? SHIPLINE_PAYDATE { get; set; }
        public DateTime? DO_DATE { get; set; }
        public DateTime? RECEIPT_DATE { get; set; }
        public DateTime? TENTATIVE_HODATE { get; set; }
        public DateTime? SBLC_APPLDATE { get; set; }
        public DateTime? SBLC_PAYDATE { get; set; }
        public DateTime? SBLC_DUEDATE { get; set; }
        public DateTime? SBLC_DISBDATE { get; set; }
        public DateTime? RAIL_DATE { get; set; }
        public DateTime? ICD_DATE { get; set; }
        public DateTime? DO_VALIDDATE { get; set; }
        public DateTime? SEC_DATE { get; set; }
        public DateTime? REFUND_RECDATE { get; set; }
        public DateTime? DETENTION_DATE { get; set; }
        public DateTime? CUSTOM_OOCDATE { get; set; }
        public DateTime? FACTORY_DATE { get; set; }
        public DateTime? LC_DATE { get; set; }
        public DateTime? LC_EXPIRYDATE { get; set; }
        public DateTime? TENTATIVE_HORETDATE { get; set; }
        public DateTime? MOVE_DATE { get; set; }
        public DateTime? TENCONT_HORETDATE { get; set; }
        public DateTime? LATEST_SHIPDATE { get; set; }
        public DateTime? BOE_SUBMITDATE { get; set; }
        public DateTime? CFS_DSTUFFDATE { get; set; }
        public DateTime? CFS_PAYDATE { get; set; }
        public DateTime? ACTUAL_CONTHODATE { get; set; }
        public DateTime? PI_DATE { get; set; }
        public DateTime? LC_RCPTDATEBANK { get; set; }
        public DateTime? LC_ACCPTDATEFACTORY { get; set; }
        public DateTime? PAY_DATE { get; set; }

        public string? TEST_MEMONO { get; set; }
        public DateTime? TEST_MEMODATE { get; set; }
    }

    public class ImportTrackingEntryListModel
    {
        public int? V_NO { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? SAUDA_NO { get; set; }
        public DateTime? SAUDA_DATE { get; set; }
        public string? V_TYPE { get; set; }
        public string? SUPPLIER { get; set; }
        public string? LOCATION { get; set; }
        public string? INVOICE_NO { get; set; }
    }
}
