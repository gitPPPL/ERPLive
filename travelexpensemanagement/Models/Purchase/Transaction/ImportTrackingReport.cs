namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class ImportTrackingReport
    {
        public int? COMP_CODE { get; set; }
        public int? BRANCH_CODE { get; set; }
        public int? YEAR_CODE { get; set; }

        public string V_TYPE { get; set; }
        public int? V_NO { get; set; }

        public string SAUDA_TYPE { get; set; }
        public int? SAUDA_NO { get; set; }

        public DateTime? V_DATE { get; set; }

        public int? PARTY_CODE { get; set; }
        public string PARTY_NAME { get; set; }

        public int? CITY_CODE { get; set; }
        public string CITY_NAME { get; set; }

        public string BILL_NO { get; set; }
        public DateTime? BILL_DATE { get; set; }

        public decimal? BILL_AMT { get; set; }

        public string TRUCK_NO { get; set; }

        public int? SEND_BY { get; set; }
        public int? SEND_TO { get; set; }

        public DateTime? SEND_DATE { get; set; }
        public DateTime? RECD_DATE { get; set; }

        public int? SEND_FLAG { get; set; }
        public int? RECD_FLAG { get; set; }

        public string REMARKS { get; set; }
    }

    public class ImportTrackingReportResult
    {
        public List<ImportTrackingReportRow> Rows { get; set; } = new();
        public int TotalUpdatedRecords { get; set; }
    }

    public class ImportTrackingReportRow
    {
        // Dates
        public DateTime? SaudaDate { get; set; }
        public DateTime? MRNDate { get; set; }
        public DateTime? InvDate { get; set; }
        public DateTime? ETAPortDate { get; set; }
        public DateTime? RailDate { get; set; }
        public DateTime? ICDReachDate { get; set; }
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
        public DateTime? SHIPLINE_PAYDATE { get; set; }
        public DateTime? DO_DATE { get; set; }
        public DateTime? RECEIPT_DATE { get; set; }
        public DateTime? TENTATIVE_HODATE { get; set; }
        public DateTime? CONTAINER_HODATE { get; set; }
        public DateTime? AdvPaymentDate { get; set; }
        public DateTime? BalPaymentDate { get; set; }
        public DateTime? SBLC_APPLDATE { get; set; }
        public DateTime? SBLC_PAYDATE { get; set; }
        public DateTime? SBLC_DUEDATE { get; set; }
        public DateTime? SBLC_DISBDATE { get; set; }
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

        // Strings
        public string SaudaNo { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string ItemName { get; set; } = "";
        public string DeliveryLocation { get; set; } = "";
        public string Country { get; set; } = "";
        public string ExpectedLoading { get; set; } = "";
        public string PaymentTerm { get; set; } = "";
        public string FrtTerm { get; set; } = "";
        public string DelTerm { get; set; } = "";
        public string Remark { get; set; } = "";
        public string ContainerNo { get; set; } = "";
        public string ContainerSize { get; set; } = "";
        public string InvNo { get; set; } = "";
        public string POD { get; set; } = "";
        public string POL { get; set; } = "";
        public string VSLName { get; set; } = "";
        public string ICDName { get; set; } = "";
        public string ETD { get; set; } = "";
        public string PINO { get; set; } = "";
        public string OFFERNO { get; set; } = "";
        public string CHAName { get; set; } = "";
        public string ETA_PLACE { get; set; } = "";
        public string BL_NO { get; set; } = "";
        public string CHA_CHARGEDETAIL { get; set; } = "";
        public string BE_NO { get; set; } = "";
        public string SAMPLE_COLLECTED { get; set; } = "";
        public string ASSESMENT_TYPE { get; set; } = "";
        public string DUTY_PAYMODE { get; set; } = "";
        public string ADVANCE_LICNO { get; set; } = "";
        public string DOC_HANDOVERDTBANK { get; set; } = "";
        public string ShippingLine { get; set; } = "";
        public string AdvPaymentAmount { get; set; } = "";
        public string AdvPaymentReference { get; set; } = "";
        public string BaPaymentAmount { get; set; } = "";
        public string BalPaymentReference { get; set; } = "";
        public string Supplier { get; set; } = "";
        public string Agent { get; set; } = "";
        public string Bank { get; set; } = "";
        public string SHIPLINE_NAME { get; set; } = "";
        public string SBLC_NO { get; set; } = "";
        public string UNDER_PROTEST { get; set; } = "";
        public string CHA_CHARGETYPE { get; set; } = "";
        public string DETENTION_REASON { get; set; } = "";
        public string DETREASON_APPROVBY { get; set; } = "";
        public string LC_NO { get; set; } = "";
        public string LC_BENIFICIARY { get; set; } = "";
        public string LC_ISSUEBANK { get; set; } = "";

        // Decimals
        public decimal SaudaQty { get; set; }
        public decimal TrackingQty { get; set; }
        public decimal MRNQty { get; set; }
        public decimal InvAmt { get; set; }
        public decimal HOLLAGE_CHARGES { get; set; }
        public decimal SECURITY_AMT { get; set; }
        public decimal REFUND_AMT { get; set; }
        public decimal INT_RATE { get; set; }
        public decimal OTH_CHARGES { get; set; }
        public decimal SBLC_AVLAMT { get; set; }
        public decimal LC_AMT { get; set; }
        public decimal QC_AMT { get; set; }
        public decimal SBLC_DISBAMT { get; set; }
        public decimal SBLC_INTRATESOFR { get; set; }
        public decimal DETENTION_AMT { get; set; }
        public decimal QLTY_CLAIMAMT { get; set; }
        public decimal ENHANCE_RATE { get; set; }
        public decimal PASSING_RATE { get; set; }
        public decimal DUTY_AMT { get; set; }
        public decimal CONCOR_EXPS { get; set; }
        public decimal CHA_AMT { get; set; }
        public decimal CFS_CHARGES { get; set; }
        public decimal CFS_AMT { get; set; }
        public decimal QC_AMTRECD { get; set; }

        // Ints
        public int NoofContainer { get; set; }
        public int MRNNo { get; set; }
        public int FreeDays { get; set; }
        public int DIFF_DAYS { get; set; }
        public int IsTrackingUpdated { get; set; }
    }

    public class DocumentSendListItem
    {
        public string? V_TYPE { get; set; }
        public string? V_NO { get; set; }
        public string? SAUDA_TYPE { get; set; }
        public string? SAUDA_NO { get; set; }
        public string? V_DATE { get; set; }
        public string? PartyName { get; set; }
        public string? PARTY_CODE { get; set; }
        public string? PARTY_CITY { get; set; }
        public string? CityName { get; set; }
        public string? BILL_NO { get; set; }
        public string? BILL_DATE { get; set; }
        public decimal INV_AMT { get; set; }
        public string? TRUCK_NO { get; set; }
        public string? SenderCode { get; set; }
        public string? SenderName { get; set; }
        public string? SEND_DATE { get; set; }
        public string? ReceiverCode { get; set; }
        public string? ReceiverName { get; set; }
        public string? RECD_DATE { get; set; }
        public string? SEND_FLAG { get; set; }
        public string? RECD_FLAG { get; set; }
        public string? REMARKS { get; set; }
    }

    public class SendDocumentResult
    {
        public int Sent { get; set; }
        public int Already { get; set; }
    }
}