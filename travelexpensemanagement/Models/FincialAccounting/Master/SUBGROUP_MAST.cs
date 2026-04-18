namespace travelexpensemanagement.Models.FincialAccounting.Master
{
    public class SUBGROUP_MAST
    {
        public int COMP_CODE { get; set; }
        public int CODE { get; set; }
        public string M_TYPE { get; set; }
        public int? GROUP_CODE { get; set; }
        public string NATURE { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public string ALIASNAME { get; set; }
        public int CURRENCY_CODE { get; set; }
        public int LANGUAGE_CODE { get; set; }
        public string ADDRESS_TYPE { get; set; }
        public string ADD1 { get; set; }
        public string ADD2 { get; set; }
        public string ADD3 { get; set; }
        public int CITY_CODE { get; set; }
        public int STATE_CODE { get; set; }
        public int COUNTRY_CODE { get; set; }
        public string PINCODE { get; set; }
        public int DISTANCE { get; set; }
        public string PHONE { get; set; }
        public string MOBILE { get; set; }
        public string SMS { get; set; }
        public string WEBSITE { get; set; }
        public string EMAIL { get; set; }
        public string FAX { get; set; }
        public string PARTY_TYPE { get; set; }
        public string GST_TYPE { get; set; }
        public string GSTIN { get; set; }
        public string PAN { get; set; }
        public string DECL_NO { get; set; }
        public DateTime DECL_DATE { get; set; }
        public string MSME_YN { get; set; }
        public string MSME_NO { get; set; }
        public int ACTIVE { get; set; }
        public string ACTPRINT_TYPE { get; set; }
        public string CONTACT_PERSON { get; set; }
        public int AGENT_CODE { get; set; }
        public int OS_CODE { get; set; }
        public string REMARKS { get; set; }
        public int CREDIT_DAYS { get; set; }
        public decimal CREDIT_LIMIT { get; set; }
        public decimal LASTCREDIT_LIMIT { get; set; }
        public int PAYTERM_CODE { get; set; }
        public decimal DISC_PER { get; set; }
        public int DISCGRP_CODE { get; set; }
        public int BANK_COUNTRY { get; set; }
        public int BANK_CODE { get; set; }
        public string BANK_NAME { get; set; }
        public string IFSC_CODE { get; set; }
        public string AC_NO { get; set; }
        public string BANK_BRANCH { get; set; }
        public string PAY_TYPE { get; set; }
        public int MAIN_CODE { get; set; }
        public string TENACITY_TYPE { get; set; }
        public string FAPROV_STATUS { get; set; }
        public string FAPROV_REMARKS { get; set; }
        public string TCS_APPLY { get; set; }
        public int IS_TRAN { get; set; }
        public int ACTIVE_FLG { get; set; }
        public int LOGINUSER_CODE { get; set; }
        public string MOBILE_OTP { get; set; }
        public int USER_CODE { get; set; }
        public int UUSER { get; set; }
        public DateTime UDATE { get; set; }
        public int EUSER { get; set; }
        public DateTime EDATE { get; set; }
        public string AED { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
        public int SRNO { get; set; }
        public string TAN_NO { get; set; }
        public string TDS_206APPLY { get; set; }
        public string BILL_TYPE { get; set; }
        public DateTime GST_CERDATE { get; set; }
        public int DUPLICATE_FLG { get; set; }
        public int DUPLICATEEMP_FLG { get; set; }
        public DateTime MAIL_DUEDATE { get; set; }
        public string AADHAR { get; set; }
        public string CPCBNO { get; set; }
        public string ENTITY_TYPE { get; set; }
        public int LEAD_DAYS { get; set; }
        public decimal MONTH_CAPACITY { get; set; }
        public decimal MIN_QTY { get; set; }
        public decimal MAX_QTY { get; set; }
        public string MSME_TYPE { get; set; }
        public int TOT_BALEMACH { get; set; }
        public int TOT_MANPOWER { get; set; }
        public int TOT_WORKINGPARTY { get; set; }
        public string MATERIAL_TYPES { get; set; }
        public int OS_FLG { get; set; }
        public string LEGAL_NAME { get; set; }
        public string ACTION { get; set; }
    }

    public class SUBGROUPExport
    {
        public string Code { get; set; }
        public string NAME { get; set; }
        public string SHORTNAME { get; set; }
        public string SubGROUP_NAME { get; set; }
        public string NATURE { get; set; }
        public string STATUS { get; set; }
    }


}
