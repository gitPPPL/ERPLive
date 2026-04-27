using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class AccountGeneralMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public AccountGeneralMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/FinancialAccounting/Master/AccountGeneralMaster/Index.cshtml");
        }
        public IActionResult GetGroupList()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            string query = $@" SELECT CODE, NAME FROM MGROUP_MAST WHERE COMP_CODE = {globalVar.PubCompCode} AND ISNULL(NATURE, '') IN ('CASH', 'BANK', 'OTHERS') 
            ORDER BY NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetNextSubGroupId()
        {
            int nextId = 1;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(CODE), 0) + 1 FROM SUBGROUP_MAST", con))
                    {
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        nextId = Convert.ToInt32(result);
                    }
                }

                return Json(new { success = true, nextId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving next ID", error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetNature(int id)
        {
            string nature = "";
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = "SELECT TOP 1 NATURE FROM SUBGROUP_MAST WHERE COMP_CODE = @COMP_CODE AND GROUP_CODE = @Id";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                        con.Open();
                        var result = cmd.ExecuteScalar();

                        if (result != null)
                        {
                            nature = result.ToString();
                        }
                    }
                }
                return Json(new { success = true, nature });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving Nature", error = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult SaveSubgroup([FromBody] SUBGROUP_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            var result = SaveOrUpdateSubgroup(model, action);
            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }
        public string SaveOrUpdateSubgroup(SUBGROUP_MAST subgroup, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    if (action == "INSERT")
                    {
                        using (SqlCommand checkCmd = new SqlCommand(@" SELECT COUNT(*) FROM SUBGROUP_MAST WHERE COMP_CODE = @COMP_CODE AND NAME = @NAME ", con))
                        {
                            checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            checkCmd.Parameters.AddWithValue("@NAME", subgroup.NAME.Trim());

                            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (exists > 0)
                            {
                                return "Subgroup Name already exists!";
                            }
                        }
                    }
                    using (SqlCommand cmd = new SqlCommand("sp_SUBGROUP_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", subgroup.CODE);
                        cmd.Parameters.AddWithValue("@AC_NO", subgroup.AC_NO ?? "");
                        cmd.Parameters.AddWithValue("@NATURE", subgroup.NATURE ?? "");
                        cmd.Parameters.AddWithValue("@NAME", subgroup.NAME ?? "");
                        cmd.Parameters.AddWithValue("@SHORTNAME", subgroup.SHORTNAME ?? "");
                        cmd.Parameters.AddWithValue("@GROUP_CODE", subgroup.GROUP_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ALIASNAME", subgroup.ALIASNAME ?? "");
                        cmd.Parameters.AddWithValue("@ADD1", subgroup.ADD1 ?? "");
                        cmd.Parameters.AddWithValue("@ADD2", subgroup.ADD2 ?? "");
                        cmd.Parameters.AddWithValue("@ADD3", subgroup.ADD3 ?? "");
                        cmd.Parameters.AddWithValue("@PAN", subgroup.PAN ?? "");
                        cmd.Parameters.AddWithValue("@BANK_NAME", subgroup.BANK_NAME ?? "");
                        cmd.Parameters.AddWithValue("@BANK_BRANCH", subgroup.BANK_BRANCH ?? "");
                        cmd.Parameters.AddWithValue("@IFSC_CODE", subgroup.IFSC_CODE ?? "");
                        cmd.Parameters.AddWithValue("@REMARKS", subgroup.REMARKS ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", subgroup.ACTIVE);

                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", subgroup.AED);
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.ExecuteNonQuery();
                    }
                    return "Success";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
        //public string SaveOrUpdateSubgroup(SUBGROUP_MAST subgroup, string action)
        //{
        //    var globalVar = _globalVariableService.GetGlobalVariables();

        //    if (action == "INSERT")
        //    {
        //        using (SqlCommand checkCmd = new SqlCommand())
        //        {
        //            checkCmd.Connection = con;
        //            checkCmd.CommandText = @"SELECT COUNT(*) 
        //                                     FROM SUBGROUP_MAST 
        //                                     WHERE COMP_CODE = @COMP_CODE 
        //                                       AND NAME = @NAME";

        //            checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //            checkCmd.Parameters.AddWithValue("@NAME", subgroup.NAME.Trim());

        //            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

        //            if (exists > 0)
        //            {
        //                return "Subgroup Name already exists!";
        //            }
        //        }
        //    }



        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_SUBGROUP_MAST", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@Action", action);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                // CODE is typically the primary key
        //                cmd.Parameters.AddWithValue("@CODE", subgroup.CODE);
        //                // AC_NO is account number, should be separate
        //                cmd.Parameters.AddWithValue("@AC_NO", subgroup.AC_NO ?? "");

        //                cmd.Parameters.AddWithValue("@NATURE", subgroup.NATURE ?? "");
        //                cmd.Parameters.AddWithValue("@NAME", subgroup.NAME ?? "");
        //                cmd.Parameters.AddWithValue("@SHORTNAME", subgroup.SHORTNAME ?? "");
        //                cmd.Parameters.AddWithValue("@GROUP_CODE", subgroup.GROUP_CODE ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@ALIASNAME", subgroup.ALIASNAME ?? "");
        //                cmd.Parameters.AddWithValue("@ADD1", subgroup.ADD1 ?? "");
        //                cmd.Parameters.AddWithValue("@ADD2", subgroup.ADD2 ?? "");
        //                cmd.Parameters.AddWithValue("@ADD3", subgroup.ADD3 ?? "");
        //                cmd.Parameters.AddWithValue("@PAN", subgroup.PAN ?? "");
        //                cmd.Parameters.AddWithValue("@BANK_NAME", subgroup.BANK_NAME ?? "");
        //                cmd.Parameters.AddWithValue("@BANK_BRANCH", subgroup.BANK_BRANCH ?? "");

        //                cmd.Parameters.AddWithValue("@IFSC_CODE", subgroup.IFSC_CODE ?? "");
        //                cmd.Parameters.AddWithValue("@REMARKS", subgroup.REMARKS ?? "");
        //                cmd.Parameters.AddWithValue("@ACTIVE", subgroup.ACTIVE);

        //                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@AED", subgroup.AED ?? "A");
        //                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
        //                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
        //                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

        //                con.Open();
        //                cmd.ExecuteNonQuery();

        //                return "Success";
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return "Error: " + ex.Message;
        //    }
        //}

        [HttpPost]
        public JsonResult DeleteSubGroupByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_SUBGROUP_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { success = true, message = "Record deleted successfully." });
        }
        [HttpGet]
        public IActionResult GetID()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var sql = "SELECT ISNULL(MAX(CODE), 0)  AS NextID FROM SUBGROUP_MAST WHERE COMP_CODE = @COMP_CODE";
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    con.Open();
                    var nextId = cmd.ExecuteScalar();
                    return Json(new { nextId });
                }
            }
        }
    }
}
