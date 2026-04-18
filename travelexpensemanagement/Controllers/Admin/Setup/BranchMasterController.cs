using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class BranchMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;

        public BranchMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            //return View();
            return View("~/Views/Admin/Setup/BranchMaster/Index.cshtml");
        }

        public IActionResult GetLoactionList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM PRODPLACE_MAST WHERE COMP_CODE = '"+ compCode + "' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpPost]
        public IActionResult SaveBranch([FromBody] BRANCH_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            // Check for duplicate name before insert
            if (action == "INSERT" && IsDuplicateBranchName(model.NAME))
            {
                return Json(new { success = false, message = "Branch already exists." });
            }

            var result = SaveOrUpdateBranch(model, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }

        [HttpPost]
        public string SaveOrUpdateBranch(BRANCH_MAST branch, string action)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BranchMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var globalVar = _globalVariableService.GetGlobalVariables();

                        cmd.Parameters.Add("@Action", SqlDbType.NVarChar).Value = action;
                        //cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalVar.PubCompCode;
                        cmd.Parameters.Add("@CODE", SqlDbType.Int).Value = branch.CODE;
                        cmd.Parameters.Add("@NAME", SqlDbType.NVarChar, 100).Value = branch.NAME ?? "";
                        cmd.Parameters.Add("@LOCATION", SqlDbType.NVarChar, 100).Value = branch.LOCATION ?? "";

                        cmd.Parameters.Add("@ACTIVE", SqlDbType.Int).Value = branch.ACTIVE;

                        cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;

                        cmd.Parameters.Add("@EUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;

                        cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = branch.AED ?? "A";
                        cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = globalVar.PubWorkStationID ?? "";
                        cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = globalVar.PubLocalId ?? "";
                        cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value = Environment.MachineName;

                        con.Open();
                        cmd.ExecuteNonQuery();

                        return "Success";
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                return $"SQL Error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }




        [HttpPost]
        public JsonResult DeleteBranchByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_BranchMast", con)) // Replace with your actual stored procedure name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Branch deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting branch.", error = ex.Message });
            }
        }

        private bool IsDuplicateBranchName(string branchName)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM BRANCH_MAST WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", branchName ?? "");

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

    }
}
