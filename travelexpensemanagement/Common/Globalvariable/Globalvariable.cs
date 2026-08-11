using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Common.Globalvariable
{
    public class GlobalVariableService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataBaseConnection _dbConnection;

        public GlobalVariableService(DataBaseConnection dbConnection, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _dbConnection = dbConnection;
        }
        public UserSessionData GetGlobalVariables()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                throw new Exception("HttpContext is null.");

            var userCode = httpContext.Session.GetString("CODE");
            var sessionYearCode = httpContext.Session.GetString("SessionYearCode");
            var sessionComp = httpContext.Session.GetString("COMP_CODE");
            var formattedDate = httpContext.Session.GetString("SessionLogindate");

            var CompanyData = GetCompanydata();
            string pubCompGSTIN = CompanyData.gstin;

            if (string.IsNullOrEmpty(userCode))
                throw new Exception("User code not found in session. Login first.");
            DateTime loginDate = DateTime.Now;

            if (!string.IsNullOrEmpty(formattedDate)) DateTime.TryParse(formattedDate, out loginDate);
            UserSessionData sessionData = null;
            string query = @" SELECT COMP_CODE, CODE, USER_NAME, USER_LEVEL, PC_NAME, LIP  FROM USER_MAST WHERE CODE = @UserCode";

            using (SqlConnection con = _dbConnection.GetConDbConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@UserCode", userCode);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        sessionData = new UserSessionData
                        {
                            PubCompCode = sessionComp,
                            PubUserId = reader["CODE"]?.ToString(),
                            PubUserName = reader["USER_NAME"]?.ToString(),
                            PubUserLevel = reader["USER_LEVEL"]?.ToString(),
                            PubWorkStationID = reader["PC_NAME"]?.ToString(),
                            PubLocalId = reader["LIP"]?.ToString(),

                            // Session Info
                            PubFYearCode = sessionYearCode,
                            PubBranchCode = 1,
                            PubLoginDate = loginDate,
                            PubSessiontime = DateTime.Now,
                            //PubCurrentMenuId = CurrentMenuId,

                            // Company Info
                            CompanyName = CompanyData.CompanyName,
                            Address1 = CompanyData.Address1,
                            Address2 = CompanyData.Address2,
                            Address3 = CompanyData.Address3,
                            gstin = CompanyData.gstin,
                            PAN = CompanyData.PAN,
                            Phone = CompanyData.Phone,
                            Fax = CompanyData.Fax,
                            Email = CompanyData.Email,
                            Website = CompanyData.Website,
                            Excise = CompanyData.Excise,
                            ServiceTax = CompanyData.ServiceTax,
                            RegAdd1 = CompanyData.RegAdd1,
                            RegAdd2 = CompanyData.RegAdd2,
                            CINNO = CompanyData.CINNO,
                            // API
                            ip_address = "103.74.69.13",
                            client_id = "8a2017bb-6f67-4bf9-bc62-46bd802ed390",
                            client_secret = "5e3dd92c-64ba-440f-a964-1a396397da66",
                            auth_access_type = "read"
                        };
                    }
                }
            }
            return sessionData;
        }
        public CompanyModel GetCompanydata()
        {
            CompanyModel CompanyData = new CompanyModel();
            var httpContext = _httpContextAccessor.HttpContext;
            var sessionComp = httpContext.Session.GetString("COMP_CODE");

            string query = @"SELECT NAME, ADD1, ADD2, ADD3, GSTIN, PAN, PHONE, FAX, EMAIL, WEBSITE, EXCISE, SERVICETAX,
            RegAdd1, RegAdd2, CINNO  FROM COMP_MAST WHERE CODE = @Code";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Code", sessionComp);
                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        CompanyData = new CompanyModel
                        {
                            CompanyName = reader["NAME"]?.ToString(),
                            Address1 = reader["ADD1"]?.ToString(),
                            Address2 = reader["ADD2"]?.ToString(),
                            Address3 = reader["ADD3"]?.ToString(),
                            gstin = reader["GSTIN"]?.ToString(),
                            PAN = reader["PAN"]?.ToString(),
                            Phone = reader["PHONE"]?.ToString(),
                            Fax = reader["FAX"]?.ToString(),
                            Email = reader["EMAIL"]?.ToString(),
                            Website = reader["WEBSITE"]?.ToString(),
                            Excise = reader["EXCISE"]?.ToString(),
                            ServiceTax = reader["SERVICETAX"]?.ToString(),
                            RegAdd1 = reader["RegAdd1"]?.ToString(),
                            RegAdd2 = reader["RegAdd2"]?.ToString(),
                            CINNO = reader["CINNO"]?.ToString()
                        };
                    }
                }
            }

            return CompanyData;
        }
        public async Task<GlobalGeneralSettingModel> LoadGeneralSetting()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
                throw new Exception("HttpContext is null.");

            var compCode = httpContext.Session.GetString("COMP_CODE");
            var userData = GetGlobalVariables();

            string pubCompGSTIN = userData.gstin;
            if (string.IsNullOrEmpty(compCode))
                throw new Exception("COMP_CODE not found in session.");

            GlobalGeneralSettingModel model = new GlobalGeneralSettingModel();

            try
            {
                string qry = @"
                SELECT * 
                FROM SYS_BP a
                LEFT JOIN SYS_DISPLAY b ON a.COMP_CODE = b.COMP_CODE
                LEFT JOIN SYS_PATH c ON a.COMP_CODE = c.COMP_CODE
                LEFT JOIN SYS_SERVICE d ON a.COMP_CODE = d.COMP_CODE
                LEFT JOIN SYS_PAY e ON a.COMP_CODE = e.COMP_CODE
                LEFT JOIN SYS_SMS f ON a.COMP_CODE = f.COMP_CODE 
                    AND f.ACTIVE = 1
                WHERE a.COMP_CODE = @COMP_CODE";

                //using (SqlConnection con = new SqlConnection())
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(qry, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            if (await rdr.ReadAsync())
                            {
                                model.pubCrLimit = Convert.ToDecimal(rdr["CR_LIMIT"] ?? 0);
                                model.pubCrLimitSale = Convert.ToDecimal(rdr["CR_LIMIT_SALE"] ?? 0);
                                model.pubCrLimitOrder = Convert.ToDecimal(rdr["CR_LIMIT_ORDER"] ?? 0);
                                model.pubCrLimitPack = Convert.ToDecimal(rdr["CR_LIMIT_PACK"] ?? 0);

                                model.pubPaytermCust = Convert.ToInt32(rdr["PAYTERM_CUST"] ?? 0);
                                model.pubPaytermSupp = Convert.ToInt32(rdr["PAYTERM_SUPP"] ?? 0);

                                model.pubDefTDSGroup = Convert.ToInt32(rdr["TDS_ACGRP"] ?? 0);
                                model.pubDefCashAct = Convert.ToInt32(rdr["CASH_AC"] ?? 0);

                                model.pubDispInactiveTran = Convert.ToInt32(rdr["DISP_INACTIVE_TRAN"] ?? 0);
                                model.pubDispInactiveReport = Convert.ToInt32(rdr["DISP_INACTIVE_REP"] ?? 0);

                                model.pubBPAllowInPurSale = Convert.ToInt32(rdr["BP_ALLOW_PURCH_SALE"] ?? 0);

                                model.pubRateExpiredDays = Convert.ToString(rdr["RATE_EXPIRED"]);
                                model.pubBPPurchTolQty = Convert.ToString(rdr["PURCH_TOLQTY"]);
                                model.pubBPSaleTolQty = Convert.ToString(rdr["SALE_TOLQTY"]);

                                model.pubBPInsuPer = Convert.ToDecimal(rdr["INSU_PER"] ?? 0);
                                model.pubBPTCSPer = Convert.ToDecimal(rdr["TCS_PER"] ?? 0);
                                model.pubBPTotTCSSaleAmt = Convert.ToDecimal(rdr["TCS_TOTSALEAMT"] ?? 0);

                                model.pubDefRCMConsigneeCode = Convert.ToInt32(rdr["RCM_CONSIGNEE"] ?? 0);

                                model.pubDefSSINSO = Convert.ToString(rdr["SSINSO"]);
                                model.pubDefSSINSI = Convert.ToString(rdr["SSINSI"]);
                                model.pubDefSOINSI = Convert.ToString(rdr["SOINSI"]);
                                model.pubDefPACKINSI = Convert.ToString(rdr["PACKINSI"]);
                                model.pubDefWBINSI = Convert.ToString(rdr["WBINSI"]);
                                model.pubDefISSUEINSI = Convert.ToString(rdr["ISSUEINSI"]);

                                model.PubDefEWaybillAmt = Convert.ToDecimal(rdr["EWAYBILL_AMT"] ?? 0);

                                model.pubDefTaxonInsuInSI = Convert.ToString(rdr["TAXINSINSI"]);
                                model.pubDefTaxonFrtInSI = Convert.ToString(rdr["TAXFRTINSI"]);

                                model.pubDefInsuInBillAmtInSI = Convert.ToString(rdr["INSBILLAMTINSI"]);
                                model.pubDefFrtInBillAmtInSI = Convert.ToString(rdr["FRTBILLAMTINSI"]);
                                model.pubDefRoundOffInBillAmtInSI = Convert.ToString(rdr["RNDOFFBILLAMTINSI"]);

                                model.pubDefTonnageRate = Convert.ToString(rdr["RATEONTONNAGE"]);
                                model.pubDefWtCalconBales = Convert.ToString(rdr["WTCALCONBALES"]);

                                model.pubDefPOInMRN = Convert.ToString(rdr["POINMRN"]);
                                model.pubDefGateInMRN = Convert.ToString(rdr["GATEINMRN"]);
                                model.pubDefSaudaInPO = Convert.ToString(rdr["SAUDAINPO"]);
                                model.pubDefReqInPO = Convert.ToString(rdr["REQINPO"]);
                                model.pubDefRateAppInPO = Convert.ToString(rdr["RATEAPPROVINPO"]);

                                model.pubDecimalAmt = Convert.ToInt32(rdr["DECIMAL_AMT"] ?? 0);
                                model.pubDecimalRate = Convert.ToInt32(rdr["DECIMAL_RATE"] ?? 0);
                                model.pubDecimalQty = Convert.ToInt32(rdr["DECIMAL_QTY"] ?? 0);
                                model.pubDecimalPrec = Convert.ToInt32(rdr["DECIMAL_PERC"] ?? 0);

                                model.pubThousandSep = Convert.ToString(rdr["THOUS_SEP"]);

                                model.pubLanguageCode = Convert.ToInt32(rdr["LANGUAGE_CODE"] ?? 0);

                                model.pubTimeFormat = Convert.ToString(rdr["TIME_FORMAT"]);
                                model.pubDateFormat = Convert.ToString(rdr["DATE_FORMAT"]);
                                model.pubDateSeprator = Convert.ToString(rdr["DATE_SEPRATOR"]);

                                model.pubFontName = Convert.ToString(rdr["FONT_NAME"]);
                                model.pubFontSize = Convert.ToInt32(rdr["FONT_SIZE"] ?? 0);
                                model.pubFontBold = Convert.ToInt32(rdr["FONT_BOLD"] ?? 0);

                                model.pubFormColor = Convert.ToString(rdr["FORM_COLOR"]);
                                model.pubSearchColor = Convert.ToString(rdr["SEARCH_COLOR"]);
                                model.pubGotColor = Convert.ToString(rdr["GOT_COLOR"]);
                                model.pubLostColor = Convert.ToString(rdr["LOST_COLOR"]);
                                model.pubTextBoxColor = Convert.ToString(rdr["TEXTBOX_COLOR"]);

                                model.pubDataGridColor = Convert.ToString(rdr["DATAGRID_COLOR"]);
                                model.pubFlexGridColor = Convert.ToString(rdr["FLEXGRD_COLOR"]);
                                model.pubFrameColor = Convert.ToString(rdr["FRAME_COLOR"]);

                                model.pubForeColor = Convert.ToString(rdr["FORE_COLOR"]);
                                model.pubLableForeColor = Convert.ToString(rdr["LBLFORE_COLOR"]);

                                model.pubEXRateScr = Convert.ToInt32(rdr["EX_RATE_SCR"] ?? 0);

                                model.pubAutoAlerts = Convert.ToInt32(rdr["AUTO_ALERTS"] ?? 0);
                                model.pubAutoMsg = Convert.ToInt32(rdr["AUTO_MSG"] ?? 0);
                                model.pubAutoMail = Convert.ToInt32(rdr["AUTO_MAIL"] ?? 0);
                                model.pubAutoSMS = Convert.ToInt32(rdr["AUTO_SMS"] ?? 0);

                                model.PubWhatsupInstantId = Convert.ToString(rdr["WHATSUP_INSTANTID"]);
                                model.PubWhatsupTokenId = Convert.ToString(rdr["WHATSUP_TOKENID"]);

                                model.pubAutoInbox = Convert.ToInt32(rdr["AUTO_INBOX"] ?? 0);
                                model.pubAutoTask = Convert.ToInt32(rdr["AUTO_TASK"] ?? 0);

                                model.pubMsgUpdate = Convert.ToInt32(rdr["MSG_UPDATE"] ?? 0);
                                model.pubScreenLock = Convert.ToInt32(rdr["SCR_LOCK"] ?? 0);
                                model.pubCaseSens = Convert.ToInt32(rdr["CASE_SENS"] ?? 0);

                                model.pubManageBatchBy = Convert.ToString(rdr["MANAGE_BATCHBY"]);

                                model.pubModifyDays = Convert.ToInt32(rdr["MODIFY_DAYS"] ?? 0);
                                model.pubMaxRequestInADay = Convert.ToInt32(rdr["MAX_REQUEST"] ?? 0);

                                model.pubApprovalSendDays = Convert.ToInt32(rdr["APPROVAL_SENDDAYS"] ?? 0);

                                model.pubWBUser = Convert.ToInt32(rdr["WB_USER"] ?? 0);

                                model.pubTimer1 = Convert.ToInt32(rdr["T1"] ?? 0);
                                model.pubInterval1 = Convert.ToInt32(rdr["INT1"] ?? 0);

                                model.pubTimer2 = Convert.ToInt32(rdr["T2"] ?? 0);
                                model.pubInterval2 = Convert.ToInt32(rdr["INT2"] ?? 0);

                                model.pubTimer3 = Convert.ToInt32(rdr["T3"] ?? 0);
                                model.pubInterval3 = Convert.ToInt32(rdr["INT3"] ?? 0);

                                model.pubZoomLevel = Convert.ToInt32(rdr["ZOOM_LEVEL"] ?? 0);

                                model.pubDefOnlyform12 = Convert.ToInt32(rdr["ONLY_FORM12"] ?? 0);

                                model.pubDefWBWOImage = Convert.ToInt32(rdr["WBWOIMAGE"] ?? 0);

                                model.pubDefPreQcNos = Convert.ToInt32(rdr["NOS_PREQC"] ?? 0);

                                // EInvoice
                                if (Convert.ToInt32(rdr["EINV_LIVECONFIG"] ?? 0) == 1)
                                {
                                    model.PubEinvFLG = 1;

                                    model.PubEinvIP = Convert.ToString(rdr["EINV_LIP"]);
                                    model.PubEinvUName = Convert.ToString(rdr["EINV_LUSERNAME"]);
                                    model.PubEinvPass = Convert.ToString(rdr["EINV_LPASS"]);
                                    model.PubEinvCID = Convert.ToString(rdr["EINV_LCLIENTID"]);
                                    model.PubEinvCSID = Convert.ToString(rdr["EINV_LCLIENTSID"]);
                                    model.PubEinvGSTIN = pubCompGSTIN;
                                }
                                else
                                {
                                    model.PubEinvFLG = 0;

                                    model.PubEinvIP = Convert.ToString(rdr["EINV_TIP"]);
                                    model.PubEinvUName = Convert.ToString(rdr["EINV_TUSERNAME"]);
                                    model.PubEinvPass = Convert.ToString(rdr["EINV_TPASS"]);
                                    model.PubEinvCID = Convert.ToString(rdr["EINV_TCLIENTID"]);
                                    model.PubEinvCSID = Convert.ToString(rdr["EINV_TCLIENTSID"]);
                                    model.PubEinvGSTIN = pubCompGSTIN;
                                }

                                model.PubEinvJSONPath = Convert.ToString(rdr["EINV_JSONPATH"]);
                                model.PubEinvQRPath = Convert.ToString(rdr["EINV_QRIMGPATH"]);

                                model.PubEWayBillCID = Convert.ToString(rdr["EINV_EWAYBILLID"]);
                                model.PubEWayBillCSID = Convert.ToString(rdr["EINV_EWAYBILLSID"]);

                                model.PubGSTAPICID = Convert.ToString(rdr["EINV_GSTAPIID"]);
                                model.PubGSTAPICSID = Convert.ToString(rdr["EINV_GSTAPISID"]);

                                // Report Path
                                string reportLoc = "S";

                                if (reportLoc.ToUpper() == "S")
                                {
                                    model.pubReportPath = Convert.ToString(rdr["REPORT_PATH"]);
                                    model.pubHRMSPath = Convert.ToString(rdr["EXCEL_PATH"]);
                                    model.pubPicturePath = Convert.ToString(rdr["PICTURE_PATH"]);
                                    model.pubCameraPath = Convert.ToString(rdr["CAMERA_PATH"]);
                                    model.pubBankFilePath = Convert.ToString(rdr["BANKFILE_PATH"]);
                                    model.pubQuotationPath = Convert.ToString(rdr["QUOTATION_PATH"]);
                                    model.pubDocPath = Convert.ToString(rdr["DOC_PATH"]);
                                    model.pubMailPath = Convert.ToString(rdr["MAIL_PATH"]);
                                }
                                else
                                {
                                    model.pubReportPath = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
                                    model.pubHRMSPath = Path.Combine(Directory.GetCurrentDirectory(), "HRMS");
                                    model.pubPicturePath = Path.Combine(Directory.GetCurrentDirectory(), "Picture");
                                    model.pubCameraPath = Path.Combine(Directory.GetCurrentDirectory(), "Camera");
                                    model.pubBankFilePath = Path.Combine(Directory.GetCurrentDirectory(), "BankFile");
                                    model.pubQuotationPath = Path.Combine(Directory.GetCurrentDirectory(), "Quotation");
                                    model.pubDocPath = Path.Combine(Directory.GetCurrentDirectory(), "Document");
                                    model.pubMailPath = Path.Combine(Directory.GetCurrentDirectory(), "Mail");
                                }

                                // Dates
                                if (rdr["START_DATE"] != DBNull.Value)
                                    model.pubPaySDate = Convert.ToDateTime(rdr["START_DATE"]);

                                if (rdr["END_DATE"] != DBNull.Value)
                                    model.pubPayEDate = Convert.ToDateTime(rdr["END_DATE"]);

                                // SMS
                                model.pubSMSKey = Convert.ToString(rdr["SMS_KEY"]);
                                model.pubSMSSender = Convert.ToString(rdr["SMS_SENDER"]);
                                model.pubSMSUser1 = Convert.ToString(rdr["SMS_USER1"]);
                                model.pubSMSUser2 = Convert.ToString(rdr["SMS_USER2"]);
                                model.pubSMSUser3 = Convert.ToString(rdr["SMS_USER3"]);

                                model.pubSMSRoute = Convert.ToString(rdr["SMS_ROUTE"]);
                                model.pubSMSURL = Convert.ToString(rdr["SMS_URL"]);
                                model.pubSMSAuto = Convert.ToString(rdr["AUTO_SMS"]);

                                model.pubignoreRCMAct = Convert.ToInt32(rdr["IGNORE_FRTRCMACT"] ?? 0);
                            }
                        }
                    }
                }

                return model;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //var userData = _globalService.GetGlobalVariables();
        //var generalSetting = await _globalService.LoadGeneralSetting();
    }
}

