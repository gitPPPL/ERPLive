using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Implementations.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class ITInventoryEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IITInventoryEntryRepository _itInventoryEntryRepository;

        public ITInventoryEntryController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IITInventoryEntryRepository itInventoryEntryRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _itInventoryEntryRepository = itInventoryEntryRepository;
        }

        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.GlobalVariables = globalVar;
            return View("~/Views/Inventory/Transaction/ITInventoryEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetDropdown(string type)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = "";

            switch (type)
            {
                case "AssetType":
                    query = $@"select code ,name from DEVICE_MAST order by code";
                    break;

                case "EmployeeName":
                    query = $@"Select code,ltrim(rtrim(CODE))+ space(10- LEN (ltrim(rtrim(CODE))))+'|'+SPACE(5)+CAST (NAME as varchar )'NAME' from EMP_MAST where COMP_CODE = {globalVariables.PubCompCode} order by name";
                    break;
            } 

            var dropdownList = _dropdownService.GetDropdownList(query);

            return Json(dropdownList);
        }
        
        [HttpGet]
        public IActionResult GetDeviceList()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            int compCode = Convert.ToInt32(globalVariables.PubCompCode);

            string query = @"
            SELECT 
                CODE,
                NAME,
                SHORTNAME
            FROM DEVICE_MAST
            ORDER BY CODE";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            var list = new List<object>();

            while (reader.Read())
            {
                list.Add(new
                {
                    value = reader["CODE"],
                    text = reader["NAME"],
                    shortName = reader["SHORTNAME"]
                });
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetUnitNameList()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = $@"
            Select Distinct UNIT_NAME from IT_INVENTORY Where  V_TYPE in ('ITIV') and isnull(UNIT_NAME,'')<>'' and Comp_Code= {globalVariables.PubCompCode} and 
            Branch_code= {globalVariables.PubBranchCode} order by UNIT_NAME";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            var list = new List<string>();

            while (reader.Read())
            {
                list.Add(reader["UNIT_NAME"].ToString());
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetServerIpList()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"
            Select Distinct Server_IP from IT_INVENTORY where V_TYPE in ('ITIV') and isnull(server_IP,'')<>'' and Comp_Code= {globalVariables.PubCompCode} and 
            Branch_code= {globalVariables.PubBranchCode} order by Server_IP";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            var list = new List<string>();

            while (reader.Read())
            {
                list.Add(reader["server_IP"].ToString());
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetPurchaseList()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"
            Select Distinct Purchase_From from IT_INVENTORY Where  V_TYPE in ('ITIV') and isnull(Purchase_From,'')<>'' and Comp_Code= {globalVariables.PubCompCode} and 
            Branch_code= {globalVariables.PubBranchCode} order by Purchase_From";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            var list = new List<string>();

            while (reader.Read())
            {
                list.Add(reader["Purchase_From"].ToString());
            }

            return Json(list);
        }

        [HttpGet]
        public IActionResult GetDepartmentList()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            string query = $@"
            Select Distinct DEPT from IT_INVENTORY Where  V_TYPE in ('ITIV') and isnull(DEPT,'')<>'' and Comp_Code= {globalVariables.PubCompCode} and 
            Branch_code= {globalVariables.PubBranchCode} order by DEPT";

            using var con = _dbConnection.GetErpConnection();
            con.Open();

            using var cmd = new SqlCommand(query, con);
            using var reader = cmd.ExecuteReader();

            var list = new List<string>();

            while (reader.Read())
            {
                list.Add(reader["DEPT"].ToString());
            }

            return Json(list);
        }

        [HttpGet]
        public JsonResult GenerateVNo()
        {
            string newV_NO = "00001";
            string vType = "ITIV";

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
                    SELECT ISNULL(MAX(CAST(RIGHT(CAST(V_NO AS VARCHAR(20)), 5) AS INT)), 0) + 1 FROM IT_INVENTORY WHERE V_TYPE = @VType AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
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

        [HttpGet]
        public IActionResult GetEmployeeDetails(int empCode)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"
                    SELECT TOP 1
                        e.CODE,
                        e.NAME,
                        d.CODE AS DeptCode,
                        d.NAME AS DeptName,
                        de.CODE AS DesgCode,
                        de.NAME AS DesgName
                    FROM EMP_MAST e
                    LEFT JOIN DEPT_MAST d
                        ON d.CODE = e.DEPT_CODE
                        AND d.COMP_CODE = e.COMP_CODE
                    LEFT JOIN DESG_MAST de
                        ON de.CODE = e.DESG_CODE
                        AND de.COMP_CODE = e.COMP_CODE
                    WHERE
                        e.CODE = @EMP_CODE
                        AND e.COMP_CODE = @COMP_CODE";

                    using SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@EMP_CODE", empCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (!reader.Read())
                    {
                        return Json(new {success = false, message = "Invalid Employee Code."});
                    }

                    var employee = new
                    {
                        code = reader["CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CODE"]),
                        name = reader["NAME"] == DBNull.Value  ? "" : reader["NAME"].ToString(),
                        deptCode = reader["DeptCode"] == DBNull.Value ? 0: Convert.ToInt32(reader["DeptCode"]),
                        deptName = reader["DeptName"] == DBNull.Value  ? "" : reader["DeptName"].ToString(),
                        desgCode = reader["DesgCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DesgCode"]),
                        desgName = reader["DesgName"] == DBNull.Value ? "" : reader["DesgName"].ToString()
                    };

                    return Json(new { success = true, employee = employee });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message});
            }
        }

        [HttpGet]
        public IActionResult GetNextAssetSrNo(string assetType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(assetType))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Asset Type is required."
                    });
                }

                var globalVariables = _globalVariableService.GetGlobalVariables();

                int compCode = Convert.ToInt32(globalVariables.PubCompCode);

                string query = @"
                SELECT ISNULL(MAX(ASSET_SRNO), 0) + 1
                FROM IT_INVENTORY
                WHERE ISNULL(ASSET_TYPE, '') = @AssetType
                  AND COMP_CODE = @CompCode";

                using var con = _dbConnection.GetErpConnection();
                con.Open();

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@AssetType", assetType);
                cmd.Parameters.AddWithValue("@CompCode", compCode);

                int nextSrNo = Convert.ToInt32(cmd.ExecuteScalar());

                return Json(new
                {
                    success = true,
                    assetSrNo = nextSrNo
                });
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

        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateData([FromBody] ITInventoryEntryModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                var result = await _itInventoryEntryRepository.SaveAndUpdateDataAsync(model);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadEditData(string docId)
        {
            var result = await _itInventoryEntryRepository.LoadEditDataAsync(docId);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("IT_INVENTORY", vdate, vtype, vno);
            return Ok(result);
        }

    }
}
