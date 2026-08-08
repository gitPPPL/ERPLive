namespace travelexpensemanagement.Models.Purchase.Transaction
{
    public class ImportPaymentEntry
    {
        public class PartyDetailsModel
        {
            public string? EcbLenderCode { get; set; }
            public string? EcbAddress { get; set; }
            public string? BeneficiaryCode { get; set; }
            public string? BeneficiaryName { get; set; }
            public string? BeneficiaryActNo { get; set; }
            public string? BeneficiaryBankAddress { get; set; }
            public string? ImportCategory { get; set; }
            public string? ImportRemit { get; set; }
            public string? PayType { get; set; }
            public string? ForeignBankCharge { get; set; }
            public string? InterestApplicable { get; set; }
            public string? Roi { get; set; }
            public string? RoiPeriod { get; set; }
            public string? BeneficiaryBankCode { get; set; }
            public string? BeneficiarySwift { get; set; }
            public string? BeneficiaryAccount { get; set; }
            public string? CorrBankCode { get; set; }
            public string? CorrSwift { get; set; }
            public string? CorrAccount { get; set; }
        }

        public class SaveImportPaymentEntry
        {
            public InsertHeaderData Header { get; set; } = new();

            public List<InsertFooterData> Footer { get; set; } = new();
        }

        public class InsertHeaderData
        {
            public string? V_TYPE { get; set; }
            public int? V_NO { get; set; }
            public DateTime? V_DATE { get; set; }
            public string? DOC_ID { get; set; }
            public int? PARTY_CODE { get; set; }
            public string? PAY_TYPE { get; set; }
            public int? BANK_CODE { get; set; }
            public string? IMPORT_CAT { get; set; }
            public string? ITEM_CAT { get; set; }
            public string? IMPORT_REMIT { get; set; }
            public string? CURRENCY { get; set; }
            public decimal? TOT_AMT { get; set; }
            public string? FOREIGN_BANKCHARGE { get; set; }
            public string? BENI_BANK { get; set; }
            public string? BENI_ACTNO { get; set; }
            public string? BENI_SWIFT { get; set; }
            public string? BENI_ABA { get; set; }
            public string? BENI_ROUT { get; set; }
            public string? BENI_SC { get; set; }
            public string? BENI_BANKADD { get; set; }
            public string? CORR_BANK { get; set; }
            public string? CORR_ACTNO { get; set; }
            public string? CORR_SWIFT { get; set; }
            public string? CORR_ABA { get; set; }
            public string? CORR_ROUT { get; set; }
            public string? CORR_SC { get; set; }
            public string? CORR_BANKADD { get; set; }
            public string? DOC_EVEDENCE { get; set; }
            public string? INTRATE_APPL { get; set; }
            public decimal? ROI { get; set; }
            public string? ROI_PERIOD { get; set; }
            public int? SPFC_BANK { get; set; }
            public string? CD_BILLREFNO { get; set; }
            public string? CD_CCY { get; set; }
            public decimal? CD_AMTREMITT { get; set; }
            public int? CDFEMA_NC { get; set; }
            public int? CDFEMA_RES { get; set; }
            public int? CD_ATTCH1 { get; set; }
            public int? CD_ATTCH2 { get; set; }
            public int? CD_ATTCH3 { get; set; }
            public int? CD_ATTCH4 { get; set; }
            public int? CD_ATTCH5 { get; set; }
            public int? CD_ATTCH6 { get; set; }
            public int? CD_ATTCH7 { get; set; }
            public int? CD_ATTCH8 { get; set; }
            public int? CD_ATTCH9 { get; set; }
            public int? A2_ISSUEDRAFT { get; set; }
            public int? A2_FEREFFECT { get; set; }
            public int? A2_BENIFICIARY { get; set; }
            public string? A2_ACTNO { get; set; }
            public string? A2_NAMEADD { get; set; }
            public int? A2_ISSUETRAVELLER { get; set; }
            public string? A2_ITFOR { get; set; }
            public int? A2_FCN { get; set; }
            public string? A2_FCNFOR { get; set; }
            public string? A2_AMOUNT { get; set; }
            public string? A2_LRS { get; set; }
            public string? A2_PC { get; set; }
            public string? A2_DESC { get; set; }
            public string? ECB_PURPOSE { get; set; }
            public int? ECB_LENDER { get; set; }
            public string? ECB_NAMEADD { get; set; }
            public int? ECB_NATURE1 { get; set; }
            public int? ECB_NATURE2 { get; set; }
            public int? ECB_NATURE3 { get; set; }
            public int? ECB_NATURE4 { get; set; }
            public int? ECB_NATURE5 { get; set; }
            public int? ECB_NATURE6 { get; set; }
            public int? ECB_NATURE7 { get; set; }
            public int? ECB_NATURE8 { get; set; }
            public int? ECB_NATURE9 { get; set; }
            public int? ECB_NATURE10 { get; set; }
            public string? ECB_ROI { get; set; }
            public decimal? ECB_UPFRONTFEE { get; set; }
            public decimal? ECB_MGMTFEE { get; set; }
            public decimal? ECB_OTHCH { get; set; }
            public string? ECB_ALLINCOST { get; set; }
            public decimal? ECB_COMMITMENTFEE { get; set; }
            public decimal? ECB_ROPI { get; set; }
            public string? ECB_PERIOD { get; set; }
            public string? ECB_CALLPUT { get; set; }
            public string? ECB_GRACE { get; set; }
            public string? ECB_REPAYTERM { get; set; }
            public string? ECB_AVGMATURITY { get; set; }
            public string? ECB_NATUREOFSEC { get; set; }
            public DateTime? PCD_DDMONTH { get; set; }
            public decimal? PCD_DDAMT { get; set; }
            public DateTime? PCD_RPMONTH { get; set; }
            public decimal? PCD_RPAMT { get; set; }
            public DateTime? PCD_IPMONTH { get; set; }
            public decimal? PCD_IPAMT { get; set; }
            public string? PCD_NAMELOC { get; set; }
            public decimal? PCD_TOTALCOST { get; set; }
            public decimal? PCD_PERCOST { get; set; }
            public string? PCD_PIBANKAPPL { get; set; }
            public int? PCD_IS1 { get; set; }
            public int? PCD_IS2 { get; set; }
            public int? PCD_IS3 { get; set; }
            public int? PCD_IS4 { get; set; }
            public int? PCD_IS5 { get; set; }
            public int? PCD_IS6 { get; set; }
            public int? PCD_IS7 { get; set; }   
            public string? PCD_REQSA { get; set; }   
            public string? PCD_AUTHORITY { get; set; }   
            public string? PCD_CLNO { get; set; }   
            public DateTime? PCD_CLDATE { get; set; }   
            public string? REMARKS { get; set; }   
            public string? CLEARANCE_NO { get; set; }   
            public string? OTHDOC_DETAILS { get; set; }   
            public string? SPFC_BANKNAME { get; set; }   


        }

        public class InsertFooterData
        {
            public string? PO_TYPE { get; set; }
            public int? PO_NO { get; set; }
            public DateTime? PO_DATE { get; set; }
            public string? INV_NO { get; set; }
            public DateTime? INV_DATE { get; set; }
            public decimal? AMOUNT { get; set; }
            public decimal? QTY { get; set; }
            public int? ITEM_CODE { get; set; }
            public string? ITEM_NAME { get; set; }
            public string? HSN_CODE { get; set; }
            public string? COUNTRY_ORIGIN { get; set; }
            public string? SHIPMENT_MODE { get; set; }
            public DateTime? SHIPMENT_DATE { get; set; }
            public DateTime? EXPECTED_DOD { get; set; }
            public int? SHIPCOMP_CODE { get; set; }
            public string? SHIPPING_COMP { get; set; }
            public string? POD_CODE { get; set; }
            public string? POD { get; set; }
            public string? DEST_PORTCODE { get; set; }
            public string? DEST_PORT { get; set; }
            public string? BL_NO { get; set; }
            public string? BE_NO { get; set; }
            public DateTime? BE_DATE { get; set; }
            public string? BE_CCYNO { get; set; }
            public decimal? BE_AMT { get; set; }
            public decimal? BE_UTIAMT { get; set; }
            public decimal? FOB_VALUE { get; set; }
            public string? AD_CODE { get; set; }
            public string? PORT_CODE { get; set; }
            public string? ITEM_DESC { get; set; }
            public int? SNO { get; set; }
            public string? Action { get; set; }

        }

    }
}
