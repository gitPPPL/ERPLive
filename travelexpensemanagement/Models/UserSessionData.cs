namespace travelexpensemanagement.Models
{
    public class UserSessionData
    {
        public string PubCompCode { get; set; }
        public string PubUserId { get; set; }
        public string PubUserName { get; set; }
        public string PubUserLevel { get; set; }
        public string PubWorkStationID { get; set; }
        public string PubLocalId { get; set; }
        public string PubFYearCode { get; set; }
        public int PubBranchCode { get; set; }
        public DateTime PubLoginDate { get; set; }
        public DateTime PubSessiontime { get; set; }
        public string? ip_address { get; set; }
        public string? client_id { get; set; }
        public string? client_secret { get; set; }
        public string? gstin { get; set; }
        public string? auth_access_type { get; set; }
        public string? CompanyName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? PAN { get; set; }
        public string? Phone { get; set; }
        public string? Fax { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Excise { get; set; }
        public string? ServiceTax { get; set; }
        public string? RegAdd1 { get; set; }
        public string? RegAdd2 { get; set; }
        public string? CINNO { get; set; }

    }

    public class GlobalGeneralSettingModel
    {
        public decimal pubCrLimit { get; set; }
        public decimal pubCrLimitSale { get; set; }
        public decimal pubCrLimitOrder { get; set; }
        public decimal pubCrLimitPack { get; set; }

        public int pubPaytermCust { get; set; }
        public int pubPaytermSupp { get; set; }

        public int pubDefTDSGroup { get; set; }
        public int pubDefCashAct { get; set; }

        public int pubDispInactiveTran { get; set; }
        public int pubDispInactiveReport { get; set; }

        public int pubBPAllowInPurSale { get; set; }

        public string pubRateExpiredDays { get; set; }

        public string pubBPPurchTolQty { get; set; }
        public string pubBPSaleTolQty { get; set; }

        public decimal pubBPInsuPer { get; set; }
        public decimal pubBPTCSPer { get; set; }
        public decimal pubBPTotTCSSaleAmt { get; set; }

        public int pubDefRCMConsigneeCode { get; set; }

        public string pubDefSSINSO { get; set; }
        public string pubDefSSINSI { get; set; }
        public string pubDefSOINSI { get; set; }
        public string pubDefPACKINSI { get; set; }
        public string pubDefWBINSI { get; set; }
        public string pubDefISSUEINSI { get; set; }

        public decimal PubDefEWaybillAmt { get; set; }

        public string pubDefTaxonInsuInSI { get; set; }
        public string pubDefTaxonFrtInSI { get; set; }

        public string pubDefInsuInBillAmtInSI { get; set; }
        public string pubDefFrtInBillAmtInSI { get; set; }
        public string pubDefRoundOffInBillAmtInSI { get; set; }

        public string pubDefTonnageRate { get; set; }
        public string pubDefWtCalconBales { get; set; }

        public string pubDefPOInMRN { get; set; }
        public string pubDefGateInMRN { get; set; }
        public string pubDefSaudaInPO { get; set; }
        public string pubDefReqInPO { get; set; }
        public string pubDefRateAppInPO { get; set; }

        public int pubDecimalAmt { get; set; }
        public int pubDecimalRate { get; set; }
        public int pubDecimalQty { get; set; }
        public int pubDecimalPrec { get; set; }

        public string pubThousandSep { get; set; }

        public int pubLanguageCode { get; set; }

        public string pubTimeFormat { get; set; }
        public string pubDateFormat { get; set; }
        public string pubDateSeprator { get; set; }

        public string pubFontName { get; set; }
        public int pubFontSize { get; set; }
        public int pubFontBold { get; set; }

        public string pubFormColor { get; set; }
        public string pubSearchColor { get; set; }
        public string pubGotColor { get; set; }
        public string pubLostColor { get; set; }
        public string pubTextBoxColor { get; set; }

        public string pubDataGridColor { get; set; }
        public string pubFlexGridColor { get; set; }
        public string pubFrameColor { get; set; }

        public string pubForeColor { get; set; }
        public string pubLableForeColor { get; set; }

        public int pubEXRateScr { get; set; }

        public int pubAutoAlerts { get; set; }
        public int pubAutoMsg { get; set; }
        public int pubAutoMail { get; set; }
        public int pubAutoSMS { get; set; }

        public string PubWhatsupInstantId { get; set; }
        public string PubWhatsupTokenId { get; set; }

        public int pubAutoInbox { get; set; }
        public int pubAutoTask { get; set; }

        public int pubMsgUpdate { get; set; }
        public int pubScreenLock { get; set; }
        public int pubCaseSens { get; set; }

        public string pubManageBatchBy { get; set; }

        public int pubModifyDays { get; set; }
        public int pubMaxRequestInADay { get; set; }
        public int pubApprovalSendDays { get; set; }

        public int pubWBUser { get; set; }

        public int pubTimer1 { get; set; }
        public int pubInterval1 { get; set; }

        public int pubTimer2 { get; set; }
        public int pubInterval2 { get; set; }

        public int pubTimer3 { get; set; }
        public int pubInterval3 { get; set; }

        public int pubTimer4 { get; set; }
        public int pubInterval4 { get; set; }

        public int pubTimer5 { get; set; }
        public int pubInterval5 { get; set; }

        public int pubTimer6 { get; set; }
        public int pubInterval6 { get; set; }

        public int pubTimer7 { get; set; }
        public int pubInterval7 { get; set; }

        public int pubTimer8 { get; set; }
        public int pubInterval8 { get; set; }

        public int pubTimer9 { get; set; }
        public int pubInterval9 { get; set; }

        public int pubTimer10 { get; set; }
        public int pubInterval10 { get; set; }

        public int pubZoomLevel { get; set; }

        public int pubDefOnlyform12 { get; set; }

        public int pubDefWBWOImage { get; set; }

        public int pubDefPreQcNos { get; set; }

        // EInvoice
        public int PubEinvFLG { get; set; }

        public string PubEinvIP { get; set; }
        public string PubEinvUName { get; set; }
        public string PubEinvPass { get; set; }

        public string PubEinvCID { get; set; }
        public string PubEinvCSID { get; set; }

        public string PubEinvJSONPath { get; set; }
        public string PubEinvQRPath { get; set; }

        public string PubEWayBillCID { get; set; }
        public string PubEWayBillCSID { get; set; }

        public string PubGSTAPICID { get; set; }
        public string PubGSTAPICSID { get; set; }

        // Path
        public string pubReportPath { get; set; }
        public string pubHRMSPath { get; set; }
        public string pubPicturePath { get; set; }

        public string pubCameraPath { get; set; }
        public string pubBankFilePath { get; set; }
        public string pubQuotationPath { get; set; }

        public string pubDocPath { get; set; }
        public string pubMailPath { get; set; }

        // Dates
        public DateTime? pubPaySDate { get; set; }
        public DateTime? pubPayEDate { get; set; }

        // PF / Payroll
        public string pubPayCompPFCode { get; set; }
        public string pubPayPFEmp { get; set; }
        public string pubPayPFEmplr { get; set; }

        public string pubPayEPFEmp { get; set; }
        public string pubPayEPF1Emplr { get; set; }
        public string pubPayEPF2Emplr { get; set; }

        public string pubPayLIF21 { get; set; }
        public string pubPayLIF22 { get; set; }

        public string pubPayESIEmp { get; set; }
        public string pubPayESIEmplr { get; set; }

        public string pubPayPFLimit { get; set; }
        public string pubPayESILimit { get; set; }

        public string pubPayBonusPer { get; set; }
        public string pubPayBonusMLimit { get; set; }

        public string pubPayBonusMaxWage { get; set; }
        public string pubPayBonusPayLimit { get; set; }

        public string pubPayBonusPDay { get; set; }

        public string pubPayFirstDay { get; set; }
        public string pubPaySecondDay { get; set; }

        public string pubPayFirstMinWDay { get; set; }
        public string pubPaySecMinWDay { get; set; }

        public string pubPayPrevMonthMinWDay { get; set; }
        public string pubPayMinWages { get; set; }

        // SMS
        public string pubSMSKey { get; set; }
        public string pubSMSSender { get; set; }

        public string pubSMSUser1 { get; set; }
        public string pubSMSUser2 { get; set; }
        public string pubSMSUser3 { get; set; }

        public string pubSMSRoute { get; set; }
        public string pubSMSURL { get; set; }

        public string pubSMSAuto { get; set; }

        public int pubignoreRCMAct { get; set; }
        public string PubEinvGSTIN { get; set; }
    }

}
