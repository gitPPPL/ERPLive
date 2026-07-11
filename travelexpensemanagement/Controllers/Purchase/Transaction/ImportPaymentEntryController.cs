using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
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

        //[HttpGet]
        //public JsonResult GetDropdown(string type, string term = "")
        //{
        //    var gv = _globalVariableService.GetGlobalVariables();

        //    var data = type switch
        //    {
        //        "SupplierName" => _dropdownService.GetSupplierName(gv.PubCompCode, term),
        //        _ => new List<DropdownService.DropdownModel>()
        //    };

        //    return Json(data);
        //}

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

        //[HttpGet]
        //public IActionResult GetDropdown(string type)
        //{
        //    var globalVariable = _globalVariableService.GetGlobalVariables();

        //    string query = "";

        //    switch (type)
        //    {
        //        case "OurBank":
        //            query = $@"
        //             SELECT CODE, BANK_NAME AS NAME FROM BANKTD_MAST WHERE V_TYPE = 'IPAY' AND COMP_CODE = {globalVariable.PubCompCode} ORDER BY BANK_NAME";
        //        break;

        //        case "Bank":
        //            query = $@"SELECT CODE, NAME FROM BANK_MAST ORDER BY NAME";
        //        break;

        //        case "Currency":
        //            query = $@"SELECT CODE, SHORTNAME AS NAME FROM CURRENCY_MAST WHERE NAME <> 'INR' ORDER BY SHORTNAME";
        //        break;

        //        default:
        //            return BadRequest("Invalid dropdown type.");

        //    }
        //    var dropdownList = _dropdownService.GetDropdownList(query);
        //    return Json(dropdownList);

        //}

        //[HttpGet]
        //public IActionResult GetOurBankDetails(int bankCode)
        //{
        //    var gv = _globalVariableService.GetGlobalVariables();

        //    string query = $@"
        //    SELECT ACT_NUMBER,
        //           SWIFT_CODE,
        //           AD_CODE
        //    FROM BANKTD_MAST
        //    WHERE COMP_CODE = {gv.PubCompCode}
        //      AND CODE = {bankCode}";

        //    var data = _dropdownService.GetDropdownList(query);

        //    return Json(data);
        //}

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
