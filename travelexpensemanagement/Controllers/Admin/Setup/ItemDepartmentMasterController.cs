using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class ItemDepartmentMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        public ItemDepartmentMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     DropdownService dropdownService, DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/Setup/ItemDepartmentMaster/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetTransactionType()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"SELECT DISTINCT TRAN_TYPE AS Code, TRAN_TYPE AS Name FROM ITEMDEPT_MAST WHERE COMP_CODE = '{compCode}' AND ISNULL(TRAN_TYPE, '') <> ''";
            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }
        [HttpGet]
        public JsonResult GetddlProductionPlace()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"SELECT DISTINCT PLACE_TYPE AS Code, PLACE_TYPE AS Name FROM ITEMDEPT_MAST WHERE COMP_CODE = '{compCode}' AND ISNULL(PLACE_TYPE, '') <> '' order by PLACE_TYPE ";
            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }
        [HttpGet]
        public JsonResult GetddlPlace()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"SELECT Code, Name FROM PLACE_MAST WHERE COMP_CODE = '{compCode}' AND ISNULL(Name, '') <> '' ORDER BY CODE ASC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        // Unit Name
        [HttpGet]
        public JsonResult GetddlUnitName()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"SELECT DISTINCT UNIT_TYPE AS Code, UNIT_TYPE AS Name FROM ITEMDEPT_MAST WHERE COMP_CODE = '{compCode}' AND ISNULL(UNIT_TYPE, '') <> ''";
            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }
        //Report Filter Type
        [HttpGet]
        public JsonResult GetddlReportFilterType()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"SELECT DISTINCT REPORT_TYPE AS Code, REPORT_TYPE AS Name FROM ITEMDEPT_MAST WHERE COMP_CODE = '{compCode}' AND ISNULL(REPORT_TYPE, '') <> ''";
            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }
        // Cost
        [HttpGet]
        public JsonResult GetddlCost()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var nameCondition = DbHelper.NotNullOrEmptyCondition("Name");
            //string query = $"SELECT Code, Name FROM COSTCENTER_MAST WHERE COMP_CODE = '{compCode}' AND ISNULL(Name, '') <> ''";
            string query = $"SELECT Code, Name FROM COSTCENTER_MAST WHERE COMP_CODE = '{compCode}' AND {nameCondition}";
            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }
        [HttpPost]
        public async Task<IActionResult> SaveItemDepartment([FromBody] ItemDeptViewModel model)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    // Check if the record already exists
                        string checkQuery = "SELECT COUNT(1) FROM ITEMDEPT_MAST WHERE COMP_CODE = @COMP_CODE AND NAME = @NAME";
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                        {
                            checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            checkCmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(model.NAME) ? (object)DBNull.Value : model.NAME);

                            await con.OpenAsync();
                            int existingRecordCount = (int)await checkCmd.ExecuteScalarAsync();

                            if (existingRecordCount > 0)
                            {
                                // If the record exists, return a message
                                return Json(new { success = false, message = "This item department already exists." });
                            }
                        }
                    // Insert the new record if it does not exist
                    using (SqlCommand cmd = new SqlCommand("sp_InsertItemDept", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(model.NAME) ? (object)DBNull.Value : model.NAME);
                        cmd.Parameters.AddWithValue("@SHORTNAME", string.IsNullOrWhiteSpace(model.SHORTNAME) ? (object)DBNull.Value : model.SHORTNAME);
                        cmd.Parameters.AddWithValue("@TRAN_TYPE", string.IsNullOrWhiteSpace(model.TRAN_TYPE) ? (object)DBNull.Value : model.TRAN_TYPE);
                        cmd.Parameters.AddWithValue("@PLACE_TYPE", string.IsNullOrWhiteSpace(model.PLACE_TYPE) ? (object)DBNull.Value : model.PLACE_TYPE);
                        cmd.Parameters.AddWithValue("@REPORT_TYPE", string.IsNullOrWhiteSpace(model.REPORT_TYPE) ? (object)DBNull.Value : model.REPORT_TYPE);
                        cmd.Parameters.AddWithValue("@UNIT_TYPE", string.IsNullOrWhiteSpace(model.UNIT_TYPE) ? (object)DBNull.Value : model.UNIT_TYPE);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SORT_ON", model.SORT_ON ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", "");
                        cmd.Parameters.AddWithValue("@EDATE", "");
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                        cmd.Parameters.AddWithValue("@MPURCH_BUDGET", model.MPURCH_BUDGET ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MCONSUMP_BUDGET", model.MCONSUMP_BUDGET ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@COST_CODE", model.Cost);
                        cmd.Parameters.AddWithValue("@Action", "Insert");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Inserted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        public async Task<IActionResult> UpdateItemDepartment([FromBody] ItemDeptViewModel model)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertItemDept", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", model.CODE); // <-- For update
                        cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(model.NAME) ? (object)DBNull.Value : model.NAME);
                        cmd.Parameters.AddWithValue("@SHORTNAME", string.IsNullOrWhiteSpace(model.SHORTNAME) ? (object)DBNull.Value : model.SHORTNAME);
                        cmd.Parameters.AddWithValue("@TRAN_TYPE", string.IsNullOrWhiteSpace(model.TRAN_TYPE) ? (object)DBNull.Value : model.TRAN_TYPE);
                        cmd.Parameters.AddWithValue("@PLACE_TYPE", string.IsNullOrWhiteSpace(model.PLACE_TYPE) ? (object)DBNull.Value : model.PLACE_TYPE);
                        cmd.Parameters.AddWithValue("@REPORT_TYPE", string.IsNullOrWhiteSpace(model.REPORT_TYPE) ? (object)DBNull.Value : model.REPORT_TYPE);
                        cmd.Parameters.AddWithValue("@UNIT_TYPE", string.IsNullOrWhiteSpace(model.UNIT_TYPE) ? (object)DBNull.Value : model.UNIT_TYPE);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SORT_ON", model.SORT_ON ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", "");
                        cmd.Parameters.AddWithValue("@EDATE", "");
                        cmd.Parameters.AddWithValue("@AED", "E"); 
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                        cmd.Parameters.AddWithValue("@MPURCH_BUDGET", model.MPURCH_BUDGET ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MCONSUMP_BUDGET", model.MCONSUMP_BUDGET ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@COST_CODE", model.Cost);
                        cmd.Parameters.AddWithValue("@Action", "Update");

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
        public IActionResult GetDepartmentByCode([FromBody] CodeRequest request)
        {
            ItemDeptViewModel itemDeptment = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertItemDept", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetID");
                    cmd.Parameters.AddWithValue("@CODE", request.code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            itemDeptment = new ItemDeptViewModel
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : null,
                                NAME = rdr["NAME"]?.ToString(),
                                SHORTNAME = rdr["SHORTNAME"]?.ToString(),
                                TRAN_TYPE = rdr["TRAN_TYPE"]?.ToString(),
                                PLACE_TYPE = rdr["PLACE_TYPE"]?.ToString(),
                                REPORT_TYPE = rdr["REPORT_TYPE"]?.ToString(),
                                UNIT_TYPE = rdr["UNIT_TYPE"]?.ToString(),
                                PLACE_CODE = rdr["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PLACE_CODE"]) : null,
                                SORT_ON = rdr["SORT_ON"] != DBNull.Value ? Convert.ToInt32(rdr["SORT_ON"]) : null,
                                ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
                                MPURCH_BUDGET = rdr["MPURCH_BUDGET"] != DBNull.Value ? Convert.ToDecimal(rdr["MPURCH_BUDGET"]) : null,
                                MCONSUMP_BUDGET = rdr["MCONSUMP_BUDGET"] != DBNull.Value ? Convert.ToDecimal(rdr["MCONSUMP_BUDGET"]) : null,
                                Cost = rdr["COST_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COST_CODE"]) : 0
                            };
                        }
                    }
                }
            }

            if (itemDeptment == null)
            {
                return NotFound(new { message = "No department found for the given code." });
            }

            return Json(itemDeptment);
        }
        public class CodeRequest
        {
            public int code { get; set; }
        }
    }
}

