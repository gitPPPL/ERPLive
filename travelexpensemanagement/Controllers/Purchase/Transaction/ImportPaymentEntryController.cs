using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Common.DropdownService.DropdownService;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportPaymentEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;

        public ImportPaymentEntryController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportPaymentEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult DocType()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT Code, Name FROM DOCTYPE_MAST WHERE DOCTYPE IN ('ImportPay') order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetDropdown(string type, string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            switch (type)
            {
                case "SupplierName":
                    return Json(_dropdownService.GetSupplierName(gv.PubCompCode, term));

                case "OurBank":
                    return Json(_dropdownService.GetDropdownList($@"
                SELECT CODE, BANK_NAME AS NAME
                FROM BANKTD_MAST
                WHERE V_TYPE='IPAY'
                  AND COMP_CODE={gv.PubCompCode}
                ORDER BY BANK_NAME"));

                case "Bank":
                    return Json(_dropdownService.GetDropdownList(@"
                SELECT CODE, NAME
                FROM BANK_MAST
                ORDER BY NAME"));

                case "Currency":
                    return Json(_dropdownService.GetDropdownList(@"
                SELECT CODE, SHORTNAME AS NAME
                FROM CURRENCY_MAST
                WHERE NAME <> 'INR'
                ORDER BY SHORTNAME"));

                default:
                    return BadRequest("Invalid dropdown type.");
            }
        }

        [HttpGet]
        public JsonResult SearchSupplierName(string term = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var data = _dropdownService.GetSupplierName(gv.PubCompCode, term);

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetOurBankDetails(int bankCode)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"
                SELECT ACT_NUMBER,
                       SWIFT_CODE,
                       AD_CODE
                FROM BANKTD_MAST
                WHERE COMP_CODE = @COMP_CODE
                  AND CODE = @BANK_CODE";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BANK_CODE", bankCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Json(new
                            {
                                accountNo = reader["ACT_NUMBER"]?.ToString(),
                                swiftCode = reader["SWIFT_CODE"]?.ToString(),
                                adCode = reader["AD_CODE"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(new
            {
                accountNo = "",
                swiftCode = "",
                adCode = ""
            });
        }

        [HttpGet]
        public IActionResult GetBeneficiaryBankDetails(int bankCode, string vType)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"
                select ACT_NUMBER, ABA, ROUTINGNO, SORTCODE, SWIFT_CODE,BANK_ADD from BANKTD_MAST 
                where bank_code =@BANK_CODE and COMP_CODE = @COMP_CODE and v_type = @V_TYPE";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BANK_CODE", bankCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Json(new
                            {
                                accountNo = reader["ACT_NUMBER"]?.ToString(),
                                aba = reader["ABA"]?.ToString(),
                                routingNo = reader["ROUTINGNO"]?.ToString(),
                                sortCode = reader["SORTCODE"]?.ToString(),
                                swiftCode = reader["SWIFT_CODE"]?.ToString(),
                                bankAdd = reader["BANK_ADD"]?.ToString()
                            });
                        }
                    }
                }
            }

            return Json(new
            {
                accountNo = "",
                aba = "",
                routingNo = "",
                sortCode = "",
                swiftCode = "",
                bankAdd = ""
            });
        }

        [HttpGet]
        public IActionResult GetPartyDetailsForPartB(int partyCode)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"
                SELECT TOP 1 CODE, NAME, ADD1, ADD2, ADD3 FROM SUBGROUP_MAST
                WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", partyCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Json(new
                            {
                                code = reader["CODE"]?.ToString(),
                                name = reader["NAME"]?.ToString(),
                                address = $"{reader["ADD1"]}{Environment.NewLine}{reader["ADD2"]}"
                            });
                        }
                    }
                }
            }

            return Json(new
            {
                code = "",
                name = "",
                address = ""
            });
        }

        [HttpGet]
        public IActionResult GetPartyDetails(int partyCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var model = new ImportPaymentEntry.PartyDetailsModel();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"
                    --========================
                    -- Part B Details
                    --========================
                    SELECT TOP 1
                        A.CODE,
                        A.ADD1,
                        A.ADD2
                    FROM SUBGROUP_MAST A
                    WHERE A.COMP_CODE = @COMP_CODE
                      AND A.CODE = @PARTY_CODE;

                    --========================
                    -- Beneficiary Details
                    --========================
                    SELECT TOP 1
                        A.CODE,
                        A.NAME,
                        B.BD_ACTNO,
                        B.BD_NAME,
                        B.BD_BRANCH
                    FROM SUBGROUP_MAST A
                    LEFT JOIN SUBGROUP_BANK B
                        ON A.CODE = B.CODE
                       AND A.COMP_CODE = B.COMP_CODE
                    WHERE A.COMP_CODE = @COMP_CODE
                      AND A.CODE = @PARTY_CODE;

                    --========================
                    -- Last Import Details
                    --========================
                    SELECT TOP 1
                        IMPORT_CAT,
                        IMPORT_REMIT,
                        PAY_TYPE,
                        FOREIGN_BANKCHARGE,
                        INTRATE_APPL,
                        ROI,
                        ROI_PERIOD
                    FROM IMPORT_PAY1
                    WHERE COMP_CODE = @COMP_CODE
                      AND PARTY_CODE = @PARTY_CODE
                    ORDER BY V_DATE DESC;

                    --========================
                    -- Bank Details
                    --========================
                    SELECT TOP 1
                        BD_CODE,
                        BD_IFSCCODE,
                        BD_ACTNO,
                        BD_BRANCH
                    FROM SUBGROUP_BANK
                    WHERE COMP_CODE = @COMP_CODE
                      AND CODE = @PARTY_CODE;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        //========================
                        // Part B Details
                        //========================
                        if (reader.Read())
                        {
                            model.EcbLenderCode = reader["CODE"]?.ToString();
                            model.EcbAddress = $"{reader["ADD1"]}{Environment.NewLine}{reader["ADD2"]}";
                        }

                        //========================
                        // Beneficiary Details
                        //========================
                        if (reader.NextResult() && reader.Read())
                        {
                            model.BeneficiaryCode = reader["CODE"]?.ToString();
                            model.BeneficiaryName = reader["NAME"]?.ToString();
                            model.BeneficiaryActNo = reader["BD_ACTNO"]?.ToString();
                            model.BeneficiaryBankAddress = $"{reader["BD_NAME"]},{Environment.NewLine}{reader["BD_BRANCH"]}";
                        }

                        //========================
                        // Last Import Details
                        //========================
                        if (reader.NextResult() && reader.Read())
                        {
                            model.ImportCategory = reader["IMPORT_CAT"]?.ToString();
                            model.ImportRemit = reader["IMPORT_REMIT"]?.ToString();
                            model.PayType = reader["PAY_TYPE"]?.ToString();
                            model.ForeignBankCharge = reader["FOREIGN_BANKCHARGE"]?.ToString();
                            model.InterestApplicable = reader["INTRATE_APPL"]?.ToString();
                            model.Roi = reader["ROI"]?.ToString();
                            model.RoiPeriod = reader["ROI_PERIOD"]?.ToString();
                        }

                        //========================
                        // Bank Details
                        //========================
                        if (reader.NextResult() && reader.Read())
                        {
                            model.BeneficiaryBankCode = reader["BD_CODE"]?.ToString();
                            model.BeneficiarySwift = reader["BD_IFSCCODE"]?.ToString();
                            model.BeneficiaryAccount = reader["BD_ACTNO"]?.ToString();

                            model.CorrBankCode = reader["BD_CODE"]?.ToString();
                            model.CorrSwift = reader["BD_IFSCCODE"]?.ToString();
                            model.CorrAccount = reader["BD_ACTNO"]?.ToString();
                        }
                    }
                }
            }

            return Json(model);
        }

        [HttpGet]
        public IActionResult GetItemMaster(int partyCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = $@"SELECT
                                A.V_TYPE,
                                A.V_NO,
                                FORMAT(A.V_DATE, 'dd/MM/yyyy') AS Sauda_date,
                                C.SUPPLIER_INVNO,
                                C.SUPPLIER_INVDATE,
                                C.SUPPLIER_INVAMT,
                                0 AS Qty,
                                C.ITEM_CODE,
                                C.ITEM_NAME,
                                '' AS HSNCODE,
                                C.ORIGIN_COUNTRY,
                                'SEA' AS MODE,
                                C.ETD,
                                C.LATEST_SHIPDATE,
                                C.POD,
                                C.POD AS DESTINATIONPORT,
                                C.BL_NO,
                                C.BL_DATE,
                                C.BE_NO,
                                C.BE_DATE,
                                C.BANK_NAME AS BANKCODE,
                                B.NAME AS PARTYNAME
                            FROM SAUDA A
                            LEFT JOIN SUBGROUP_MAST B
                                ON A.PARTY_CODE = B.CODE
                               AND A.COMP_CODE = B.COMP_CODE
                            LEFT JOIN EXIM1 C
                                ON C.SAUDA_NO = A.V_NO
                               AND C.SAUDA_TYPE = A.V_TYPE
                               AND C.COMP_CODE = A.COMP_CODE
                            WHERE A.V_TYPE = 'PAUD'
                              AND A.PARTY_CODE = @PARTY_CODE
                              AND A.COMP_CODE = @COMP_CODE
                              AND A.BRANCH_CODE = @BRANCH_CODE
                              AND A.YEAR_CODE = @YEAR_CODE";

                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                cmd.Parameters.AddWithValue("@PARTY_CODE", partyCode);

                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {

                    data.Add(new
                    {
                        VType = rdr["V_TYPE"]?.ToString(),
                        VNo = Convert.ToInt32(rdr["V_NO"]),
                        SaudaDate = rdr["Sauda_date"]?.ToString(),
                        SupplierInvNo = rdr["SUPPLIER_INVNO"]?.ToString(),
                        SupplierInvDate = rdr["SUPPLIER_INVDATE"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["SUPPLIER_INVDATE"]).ToString("dd/MM/yyyy"),
                        SupplierInvAmt = rdr["SUPPLIER_INVAMT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["SUPPLIER_INVAMT"]),
                        Qty = rdr["Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["Qty"]),
                        ItemCode = rdr["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["ITEM_CODE"]),
                        ItemName = rdr["ITEM_NAME"]?.ToString(),
                        HSNCode = rdr["HSNCODE"]?.ToString(),
                        OriginCountry = rdr["ORIGIN_COUNTRY"]?.ToString(),
                        Mode = rdr["MODE"]?.ToString(),
                        ETD = rdr["ETD"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["ETD"]).ToString("dd/MM/yyyy"),
                        LatestShipDate = rdr["LATEST_SHIPDATE"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["LATEST_SHIPDATE"]).ToString("dd/MM/yyyy"),
                        POD = rdr["POD"]?.ToString(),
                        DestinationPort = rdr["DESTINATIONPORT"]?.ToString(),
                        BLNo = rdr["BL_NO"]?.ToString(),
                        BLDate = rdr["BL_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["BL_DATE"]).ToString("dd/MM/yyyy"),
                        BENo = rdr["BE_NO"]?.ToString(),
                        BEDate = rdr["BE_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(rdr["BE_DATE"]).ToString("dd/MM/yyyy"),
                        BankCode = rdr["BANKCODE"]?.ToString(),
                        PartyName = rdr["PARTYNAME"]?.ToString()
                    });

                }

                return Json(data);

            }

        }

        [HttpGet]
        public IActionResult GetRawItemMaster()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"
                SELECT
                    a.CODE,
                    a.NAME,
                    ISNULL(a.HSN_CODE,'') AS HSN
                FROM ITEM_MAST a
                LEFT JOIN ITEM_GROUP c
                    ON a.GROUP_CODE = c.CODE
                   AND c.COMP_CODE = a.COMP_CODE
                LEFT JOIN ITEM_MGROUP d
                    ON c.MGROUP_CODE = d.CODE
                   AND d.COMP_CODE = a.COMP_CODE
                WHERE a.COMP_CODE = @COMP_CODE
                  AND d.MGROUP_TYPE = 'Raw'
                  AND a.ACTIVE = 1
                ORDER BY a.NAME";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);

                SqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    data.Add(new
                    {
                        Code = Convert.ToInt32(rdr["CODE"]),
                        Name = rdr["NAME"].ToString(),
                        HSN = rdr["HSN"].ToString()
                    });
                }
            }

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetCountryMast()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Code,Name from Country_mast where name is not null order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetPartyMastForFooter()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" Select Code,Name from Subgroup_mast where nature in ('Customer','Supplier') and Comp_code= {gv.PubCompCode} and Active=1 order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetPortOfDispatch()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@" Select Code,Name from Port_mast where name is not null order by name ";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public JsonResult GenerateVNo(string vType)
        {
            string newV_NO = "00001";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = @"SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string query = @"SELECT ISNULL(MAX(V_NO), 0) + 1 FROM IMPORT_PAY1 WHERE V_TYPE = @VType AND COMP_CODE = @CompCode  AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@VType", vType);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    int nextNo = Convert.ToInt32(cmd.ExecuteScalar());

                    newV_NO = prefixYR + nextNo.ToString("D5");
                }

                return Json(new
                {
                    v_NO = newV_NO,
                    v_TYPE = vType
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult SaveImportPaymentEntry([FromBody] ImportPaymentEntry.SaveImportPaymentEntry model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    
                    SqlCommand cmd= new SqlCommand("sp_ImportPaymentEntry", con);

                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", model.Header.V_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", model.Header.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", model.Header.V_DATE);
                    cmd.Parameters.AddWithValue("@DOC_ID", model.Header.V_TYPE + model.Header.V_NO);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", model.Header.PARTY_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PAY_TYPE", model.Header.PAY_TYPE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BANK_CODE", model.Header.BANK_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@IMPORT_CAT", model.Header.IMPORT_CAT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ITEM_CAT", model.Header.ITEM_CAT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CURRENCY", model.Header.CURRENCY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@TOT_AMT", model.Header.TOT_AMT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FOREIGN_BANKCHARGE", model.Header.FOREIGN_BANKCHARGE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_BANK", model.Header.BENI_BANK ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_ACTNO", model.Header.BENI_ACTNO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_SWIFT", model.Header.BENI_SWIFT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_ABA", model.Header.BENI_ABA ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_ROUT", model.Header.BENI_ROUT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_SC", model.Header.BENI_SC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@BENI_BANKADD", model.Header.BENI_BANKADD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_BANK", model.Header.CORR_BANK ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_ACTNO", model.Header.CORR_ACTNO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_SWIFT", model.Header.CORR_SWIFT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_ABA", model.Header.CORR_ABA ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_ROUT", model.Header.CORR_ROUT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_SC", model.Header.CORR_SC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CORR_BANKADD", model.Header.CORR_BANKADD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DOC_EVEDENCE", model.Header.DOC_EVEDENCE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@INTRATE_APPL", model.Header.INTRATE_APPL ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ROI", model.Header.ROI ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ROI_PERIOD", model.Header.ROI_PERIOD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SPFC_BANK", model.Header.SPFC_BANK ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SPFC_BANKNAME", model.Header.SPFC_BANKNAME ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_BILLREFNO", model.Header.CD_BILLREFNO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_CCY", model.Header.CD_CCY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_AMTREMITT", model.Header.CD_AMTREMITT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CDFEMA_NC", model.Header.CDFEMA_NC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CDFEMA_RES", model.Header.CDFEMA_RES ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH1", model.Header.CD_ATTCH1 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH2", model.Header.CD_ATTCH2 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH3", model.Header.CD_ATTCH3 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH4", model.Header.CD_ATTCH4 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH5", model.Header.CD_ATTCH5 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH6", model.Header.CD_ATTCH6 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH7", model.Header.CD_ATTCH7 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH8", model.Header.CD_ATTCH8 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CD_ATTCH9", model.Header.CD_ATTCH9 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_ISSUEDRAFT", model.Header.A2_ISSUEDRAFT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_FEREFFECT", model.Header.A2_FEREFFECT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_BENIFICIARY", model.Header.A2_BENIFICIARY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_ACTNO", model.Header.A2_ACTNO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_NAMEADD", model.Header.A2_NAMEADD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_ISSUETRAVELLER", model.Header.A2_ISSUETRAVELLER ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_ITFOR", model.Header.A2_ITFOR ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_FCN", model.Header.A2_FCN ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_FCNFOR", model.Header.A2_FCNFOR ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_AMOUNT", model.Header.A2_AMOUNT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_LRS", model.Header.A2_LRS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_PC", model.Header.A2_PC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@A2_DESC", model.Header.A2_DESC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_PURPOSE", model.Header.ECB_PURPOSE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_LENDER", model.Header.ECB_LENDER ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NAMEADD", model.Header.ECB_NAMEADD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE1", model.Header.ECB_NATURE1 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE2", model.Header.ECB_NATURE2 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE3", model.Header.ECB_NATURE3 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE4", model.Header.ECB_NATURE4 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE5", model.Header.ECB_NATURE5 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE6", model.Header.ECB_NATURE6 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE7", model.Header.ECB_NATURE7 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE8", model.Header.ECB_NATURE8 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE9", model.Header.ECB_NATURE9 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATURE10", model.Header.ECB_NATURE10 ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_ROI", model.Header.ECB_ROI ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_UPFRONTFEE", model.Header.ECB_UPFRONTFEE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_MGMTFEE", model.Header.ECB_MGMTFEE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_OTHCH", model.Header.ECB_OTHCH ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_ALLINCOST", model.Header.ECB_ALLINCOST ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_COMMITMENTFEE", model.Header.ECB_COMMITMENTFEE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_ROPI", model.Header.ECB_ROPI ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_PERIOD", model.Header.ECB_PERIOD ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_CALLPUT", model.Header.ECB_CALLPUT ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_GRACE", model.Header.ECB_GRACE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_REPAYTERM", model.Header.ECB_REPAYTERM ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_AVGMATURITY", model.Header.ECB_AVGMATURITY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ECB_NATUREOFSEC", model.Header.ECB_NATUREOFSEC ?? (object)DBNull.Value);

                    if (model.Header.PCD_DDMONTH.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PCD_DDMONTH", model.Header.PCD_DDMONTH.Value);
                        cmd.Parameters.AddWithValue("@PCD_DDAMT", model.Header.PCD_DDAMT ?? 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PCD_DDMONTH", DBNull.Value);
                        cmd.Parameters.AddWithValue("@PCD_DDAMT", 0);
                    }

                    if (model.Header.PCD_RPMONTH.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PCD_RPMONTH", model.Header.PCD_RPMONTH.Value);
                        cmd.Parameters.AddWithValue("@PCD_RPAMT", model.Header.PCD_RPAMT ?? 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PCD_RPMONTH", DBNull.Value);
                        cmd.Parameters.AddWithValue("@PCD_RPAMT", 0);
                    }
                    if (model.Header.PCD_IPMONTH.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PCD_IPMONTH", model.Header.PCD_IPMONTH.Value);
                        cmd.Parameters.AddWithValue("@PCD_IPAMT", model.Header.PCD_IPAMT ?? 0);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PCD_IPMONTH", DBNull.Value);
                        cmd.Parameters.AddWithValue("@PCD_IPAMT", 0);
                    }

                    cmd.Parameters.AddWithValue("@PCD_NAMELOC", model.Header.PCD_NAMELOC ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_TOTALCOST", model.Header.PCD_TOTALCOST ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_PERCOST", model.Header.PCD_PERCOST ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_PIBANKAPPL", model.Header.PCD_PIBANKAPPL ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@PCD_IS1", model.Header.PCD_IS1 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS2", model.Header.PCD_IS2 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS3", model.Header.PCD_IS3 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS4", model.Header.PCD_IS4 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS5", model.Header.PCD_IS5 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS6", model.Header.PCD_IS6 ?? 0);
                    cmd.Parameters.AddWithValue("@PCD_IS7", model.Header.PCD_IS7 ?? 0);

                    cmd.Parameters.AddWithValue("@PCD_REQSA", model.Header.PCD_REQSA ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_AUTHORITY", model.Header.PCD_AUTHORITY ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PCD_CLNO", model.Header.PCD_CLNO ?? (object)DBNull.Value);

                    if (model.Header.PCD_CLDATE.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@PCD_CLDATE", model.Header.PCD_CLDATE.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@PCD_CLDATE", DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@REMARKS", model.Header.REMARKS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CLEARANCE_NO", model.Header.CLEARANCE_NO ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@OTHDOC_DETAILS", model.Header.OTHDOC_DETAILS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "InsertHeader");
                    cmd.ExecuteNonQuery();

                    foreach (var item in model.Footer)
                    {
                        SqlCommand footerCmd = new SqlCommand("sp_ImportPaymentEntry", con);
                        footerCmd.CommandType = CommandType.StoredProcedure;

                        footerCmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        footerCmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        footerCmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        footerCmd.Parameters.AddWithValue("@V_TYPE", model.Header.V_TYPE);
                        footerCmd.Parameters.AddWithValue("@V_NO", model.Header.V_NO);
                        footerCmd.Parameters.AddWithValue("@V_DATE", model.Header.V_DATE);
                        footerCmd.Parameters.AddWithValue("@DOC_ID", model.Header.V_TYPE + model.Header.V_NO);
                        footerCmd.Parameters.AddWithValue("@PO_TYPE", item.PO_TYPE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@PO_NO", item.PO_NO ?? (object)DBNull.Value);
                        if (item.PO_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@PO_DATE", item.PO_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@PO_DATE", DBNull.Value);
                        }

                        footerCmd.Parameters.AddWithValue("@INV_NO", item.INV_NO ?? (object)DBNull.Value);

                        if (item.INV_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@INV_DATE", item.INV_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@INV_DATE", DBNull.Value);
                        }
                        footerCmd.Parameters.AddWithValue("@AMOUNT", item.AMOUNT ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@QTY", item.QTY ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@ITEM_CODE", item.ITEM_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@ITEM_NAME", item.ITEM_NAME ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@HSN_CODE", item.HSN_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@COUNTRY_ORIGIN", item.COUNTRY_ORIGIN ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@SHIPMENT_MODE", item.SHIPMENT_MODE ?? (object)DBNull.Value);
                        if (item.SHIPMENT_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@SHIPMENT_DATE", item.SHIPMENT_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@SHIPMENT_DATE", DBNull.Value);
                        }

                        if (item.EXPECTED_DOD.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@EXPECTED_DOD", item.EXPECTED_DOD.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@EXPECTED_DOD", DBNull.Value);
                        }
                        footerCmd.Parameters.AddWithValue("@SHIPCOMP_CODE", item.SHIPCOMP_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@SHIPPING_COMP", item.SHIPPING_COMP ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@POD_CODE", item.POD_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@POD", item.POD ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@DEST_PORTCODE", item.DEST_PORTCODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@DEST_PORT", item.DEST_PORT ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@BL_NO", item.BL_NO ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@BE_NO", item.BE_NO ?? (object)DBNull.Value);
                        if (item.BE_DATE.HasValue)
                        {
                            footerCmd.Parameters.AddWithValue("@BE_DATE", item.BE_DATE.Value);
                        }
                        else
                        {
                            footerCmd.Parameters.AddWithValue("@BE_DATE", DBNull.Value);
                        }
                        footerCmd.Parameters.AddWithValue("@BE_CCYNO", item.BE_CCYNO ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@BE_AMT", item.BE_AMT ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@BE_UTIAMT", item.BE_UTIAMT ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@FOB_VALUE", item.FOB_VALUE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@AD_CODE", item.AD_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@PORT_CODE", item.PORT_CODE ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@ITEM_DESC", item.ITEM_DESC ?? (object)DBNull.Value);
                        footerCmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        footerCmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        footerCmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        footerCmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        footerCmd.Parameters.AddWithValue("@Action", "InsertFooter");

                        footerCmd.ExecuteNonQuery();
                    }

                }
                return Json(new { success = true, message = "Record saved successfully." });

            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

    }
}
