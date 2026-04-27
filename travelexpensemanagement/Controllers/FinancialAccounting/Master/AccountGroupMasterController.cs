using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class AccountGroupMasterController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public AccountGroupMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/AccountGroupMaster/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetddlMAINGROUPNAME()
        {
            string query = "Select Code,NAME From GR_MAST order by NAME asc";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        [HttpGet]
        public JsonResult GetddlSUBSCHEDULENAME()
        {
            string query = "Select Code,Name From BS_SCH_MAST order by NAME asc";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        [HttpGet]
        public JsonResult GetddlMAINSCHEDULENAME()
        {
            string query = "Select Code, Name From BS_MSCH_MAST order by NAME asc";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        [HttpPost]
        public async Task<IActionResult> SaveAccountGroupMaster([FromBody] AccountGroupMaster model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM MGROUP_MAST WHERE COMP_CODE = @COMP_CODE AND NAME = @NAME", con))
                    {
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@NAME", model.GROUP_NAME);
                        int exists = (int)await checkCmd.ExecuteScalarAsync();
                        if (exists > 0)
                        {
                            return Json(new { success = false, message = "Account Group already exists!" });
                        }
                    }
                    using (SqlCommand cmd = new SqlCommand("sp_InsertMGROUPMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@NAME", model.GROUP_NAME ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHORTNAME", model.SHORT_NAME ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GR_CODE", model.MAIN_GROUP_NAME.HasValue ? (object)model.MAIN_GROUP_NAME.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@NATURE", model.NATURE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_GROUP", model.SCHEDULE_GROUPING ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_CODE", model.SUB_SCHEDULE_NAME.HasValue ? (object)model.SUB_SCHEDULE_NAME.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@MSCH_CODE", model.MAIN_SCHEDULE_NAME.HasValue ? (object)model.MAIN_SCHEDULE_NAME.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@SRNO", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_NO", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_GRP", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_MAIN", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_BS", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SORT_MAIN", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SORT_SCH", DBNull.Value);
                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE ? 1 : 0);
                        cmd.Parameters.AddWithValue("@GROUP_ON", model.GROUPING_ON_TRAIL ? 1 : 0);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");

                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.Parameters.AddWithValue("@Action", "Insert");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Account Group saved successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }



        //public async Task<IActionResult> SaveAccountGroupMaster([FromBody] AccountGroupMaster model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    try
        //    {
        //        var globalVar = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_InsertMGROUPMast", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@NAME", model.GROUP_NAME ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SHORTNAME", model.SHORT_NAME ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@GR_CODE", model.MAIN_GROUP_NAME.HasValue ? (object)model.MAIN_GROUP_NAME.Value : DBNull.Value);
        //                cmd.Parameters.AddWithValue("@NATURE", model.NATURE ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SCH_GROUP", model.SCHEDULE_GROUPING ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SCH_CODE", model.SUB_SCHEDULE_NAME.HasValue ? (object)model.SUB_SCHEDULE_NAME.Value : DBNull.Value);
        //                cmd.Parameters.AddWithValue("@MSCH_CODE", model.MAIN_SCHEDULE_NAME.HasValue ? (object)model.MAIN_SCHEDULE_NAME.Value : DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SRNO", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SCH_NO", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SCH_GRP", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SCH_NAME", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SCH_MAIN", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SCH_BS", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SORT_MAIN", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SORT_SCH", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE ? 1 : 0);
        //                cmd.Parameters.AddWithValue("@GROUP_ON", model.GROUPING_ON_TRAIL ? 1 : 0);
        //                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //                cmd.Parameters.AddWithValue("@AED", "A");

        //                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
        //                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
        //                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

        //                cmd.Parameters.AddWithValue("@Action", "Insert");

        //                await con.OpenAsync();
        //                await cmd.ExecuteNonQueryAsync();
        //            }
        //        }
        //        return Json(new { success = true, message = "Account Group saved successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Server error: {ex.Message}");
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> UpdateAccountGroupMaster([FromBody] AccountGroupMaster model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertMGROUPMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", model.CODE);
                        cmd.Parameters.AddWithValue("@NAME", model.GROUP_NAME ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHORTNAME", model.SHORT_NAME ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@GR_CODE", model.MAIN_GROUP_NAME.HasValue ? (object)model.MAIN_GROUP_NAME.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@NATURE", model.NATURE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_GROUP", model.SCHEDULE_GROUPING ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_CODE", model.SUB_SCHEDULE_NAME.HasValue ? (object)model.SUB_SCHEDULE_NAME.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@MSCH_CODE", model.MAIN_SCHEDULE_NAME.HasValue ? (object)model.MAIN_SCHEDULE_NAME.Value : DBNull.Value);
                        cmd.Parameters.AddWithValue("@SRNO", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_NO", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_GRP", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_MAIN", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SCH_BS", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SORT_MAIN", DBNull.Value);
                        cmd.Parameters.AddWithValue("@SORT_SCH", DBNull.Value);
                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE ? 1 : 0);
                        cmd.Parameters.AddWithValue("@GROUP_ON", model.GROUPING_ON_TRAIL ? 1 : 0);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);

                        cmd.Parameters.AddWithValue("@AED", "U");

                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.Parameters.AddWithValue("@Action", "Update");

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                return Json(new { success = true, message = "Account Group updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }
        [HttpPost]
        public IActionResult GetAccountByCode([FromBody] CodeRequest request)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            AccountGroupMaster item = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertMGROUPMast", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CODE", request.code);
                    cmd.Parameters.AddWithValue("@Action", "GetID");
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            item = new AccountGroupMaster
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                COMP_CODE = rdr["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["COMP_CODE"]) : 0,
                                GROUP_NAME = rdr["NAME"]?.ToString(),
                                SHORT_NAME = rdr["SHORTNAME"]?.ToString(),
                                MAIN_GROUP_NAME = Convert.ToInt32(rdr["GR_CODE"]?.ToString()),
                                NATURE = rdr["NATURE"]?.ToString(),

                                SCHEDULE_GROUPING = rdr["SCH_GROUP"]?.ToString(),
                                SUB_SCHEDULE_NAME = Convert.ToInt32(rdr["SCH_CODE"]?.ToString()),
                                MAIN_SCHEDULE_NAME = Convert.ToInt32(rdr["MSCH_CODE"]?.ToString()),
                                GROUPING_ON_TRAIL = rdr["GROUP_ON"] != DBNull.Value && Convert.ToInt32(rdr["GROUP_ON"]) == 1,
                                ACTIVE = rdr["ACTIVE"] != DBNull.Value && Convert.ToInt32(rdr["ACTIVE"]) == 1
                            };
                        }
                    }
                }
            }

            if (item == null || item.CODE == 0)
                return NotFound(new { message = "No record found." });

            return Json(item);
        }
        public class CodeRequest
        {
            public int code { get; set; }
        }

    }
}
