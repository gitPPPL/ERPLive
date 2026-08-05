using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
            var moduleList= _dropdownService.GetDropdownList(query);
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
        public IActionResult GetBeneficiaryBankDetails(int bankCode, string  vType)
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
                            model.EcbAddress =$"{reader["ADD1"]}{Environment.NewLine}{reader["ADD2"]}";
                        }

                        //========================
                        // Beneficiary Details
                        //========================
                        if (reader.NextResult() && reader.Read())
                        {
                            model.BeneficiaryCode = reader["CODE"]?.ToString();
                            model.BeneficiaryName = reader["NAME"]?.ToString();
                            model.BeneficiaryActNo = reader["BD_ACTNO"]?.ToString();
                            model.BeneficiaryBankAddress =$"{reader["BD_NAME"]},{Environment.NewLine}{reader["BD_BRANCH"]}";
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
        public IActionResult GetItemMaster( int partyCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new List<object>();
            using (SqlConnection con= _dbConnection.GetErpConnection())
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

                SqlDataReader rdr= cmd.ExecuteReader();

                while (rdr.Read()) {

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
        public JsonResult GenerateVNo(string vType)
        {
            string newV_NO = "00001";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = @"SELECT PREFIXYR
                                     FROM YEAR_MAST
                                     WHERE CODE = @YearCode";

                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string query = @"
                    SELECT ISNULL(MAX(V_NO), 0) + 1
                    FROM IMPORT_PAY1
                    WHERE V_TYPE = @VType
                      AND COMP_CODE = @CompCode
                      AND BRANCH_CODE = @BranchCode
                      AND YEAR_CODE = @YearCode";

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


    }
}
