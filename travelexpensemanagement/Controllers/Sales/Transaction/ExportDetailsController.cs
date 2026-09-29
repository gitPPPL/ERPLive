using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Implementations.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class ExportDetailsController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IExportDetailsRepository _exportDetailsRepository;

        public ExportDetailsController(DataBaseConnection dbConnection,  GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IExportDetailsRepository exportDetailsRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _exportDetailsRepository = exportDetailsRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/ExportDetails/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GenerateVNo()
        {
            string newV_NO = "00001";
            string vType = "EXSD";

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

                    string query = @"
                    SELECT ISNULL(MAX(CAST(RIGHT(CAST(V_NO AS VARCHAR(20)), 5) AS INT)), 0) + 1 FROM SAUDA_EXPORT WHERE V_TYPE = @VType AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@VType", vType);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    int nextNo = Convert.ToInt32(cmd.ExecuteScalar());

                    newV_NO = prefixYR + nextNo.ToString("D5");
                }

                return Json(new { v_NO = newV_NO, v_TYPE = vType });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetDropdown(string type)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = "";

            switch (type)
            {
                case "NotifyTo":
                    query = $@"Select Code,Name from SUBGROUP_MAST where COMP_CODE= {globalVariables.PubCompCode} and NATURE='Customer' and active=1 Order by NAME";
                    break;

                case "ComissionAgent":
                    query = $@"Select code,name from SALESEXECUTIVE_MAST where COMP_CODE = {globalVariables.PubCompCode} and active=1 order by name";
                    break;

                case "CountryMast":
                    query = $@"Select Code,Name from country_mast where name<>'India' Order by Name";
                    break;

            }

            var dropdownList = _dropdownService.GetDropdownList(query);

            return Json(dropdownList);
        }

        [HttpGet]
        public async Task<IActionResult> GetBankList(string vType)
        {
            var global = _globalVariableService.GetGlobalVariables();

            using var con = _dbConnection.GetErpConnection();

            const string query = @"
            SELECT BANK_CODE, BANK_NAME
            FROM BANKTD_MAST
            WHERE COMP_CODE = @CompCode
              AND V_TYPE = @VType
            ORDER BY BANK_NAME";

            using var cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@CompCode", global.PubCompCode);
            cmd.Parameters.AddWithValue("@VType", vType);

            await con.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();

            var banks = new List<object>();

            while (await reader.ReadAsync())
            {
                banks.Add(new
                {
                    value = reader["BANK_CODE"].ToString(),
                    text = reader["BANK_NAME"].ToString()
                });
            }

            return Json(banks);
        }

        [HttpGet]
        public async Task<IActionResult> GetBankDetails(int bankCode, string vType)
        {
            try
            {
                var global = _globalVariableService.GetGlobalVariables();

                using var con = _dbConnection.GetErpConnection();

                const string query = @"
                SELECT ACT_NUMBER, IFSC_CODE, AD_CODE,
                       SWIFT_CODE, BRANCH_NAME
                FROM BANKTD_MAST
                WHERE BANK_CODE = @BankCode
                  AND COMP_CODE = @CompCode
                  AND V_TYPE = @VType";

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@BankCode", bankCode);
                cmd.Parameters.AddWithValue("@CompCode", global.PubCompCode);
                cmd.Parameters.AddWithValue("@VType", vType);

                await con.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return Json(new
                    {
                        success = true,
                        accountNumber = reader["ACT_NUMBER"]?.ToString(),
                        ifscCode = reader["IFSC_CODE"]?.ToString(),
                        adCode = reader["AD_CODE"]?.ToString(),
                        swiftCode = reader["SWIFT_CODE"]?.ToString(),
                        branchName = reader["BRANCH_NAME"]?.ToString()
                    });
                }

                return Json(new { success = false, message = "Bank not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPortList()
        {
            try
            {
                const string query = @"
                SELECT 
                    PORTCODE,
                    NAME,
                    STATE
                FROM PORT_MAST
                WHERE ACTIVE = 1
                ORDER BY STATE, NAME";

                using var con = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand(query, con);

                await con.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                var result = new List<object>();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        code = reader["PORTCODE"]?.ToString(),
                        name = reader["NAME"]?.ToString(),
                        state = reader["STATE"]?.ToString()
                    });
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new {success = false, message = ex.Message});
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateData([FromForm] ExportDetailsModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                var result = await _exportDetailsRepository.SaveAndUpdateDataAsync(model);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadEditData(int REF_NO, string REF_TYPE)
        {
            var result = await _exportDetailsRepository.LoadEditDataAsync(REF_NO, REF_TYPE);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteData(int refNo, string refType, int vNo)
        {
            try
            {
                var result = await _exportDetailsRepository.DeleteDataAsync(refNo,refType, vNo);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

    }
}
