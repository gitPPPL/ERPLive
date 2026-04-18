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
    public class InsurancePolicyMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;

        public InsurancePolicyMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/Setup/InsurancePolicyMaster/Index.cshtml");
        }

        [HttpPost]
        public IActionResult SaveInsurancePolicy([FromBody] INSU_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            // Check for duplicate name before insert
            if (action == "INSERT" && IsDuplicateInsurancePolicyName(model.NAME))
            {
                return Json(new { success = false, message = "Insurance Policy name already exists." });
            }

            var result = SaveOrUpdateInsurancePolicy(model, action);

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
        public string SaveOrUpdateInsurancePolicy(INSU_MAST policy, string action)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsuranceMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        var globalVar = _globalVariableService.GetGlobalVariables();

                        cmd.Parameters.Add("@Action", SqlDbType.NVarChar).Value = action;
                        cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalVar.PubCompCode;
                        cmd.Parameters.Add("@CODE", SqlDbType.Int).Value = policy.CODE;

                        cmd.Parameters.Add("@NAME", SqlDbType.NVarChar, 100).Value = policy.NAME ?? "";
                        cmd.Parameters.Add("@DESCRIPTION", SqlDbType.NVarChar).Value = policy.DESCRIPTION ?? "";
                        cmd.Parameters.Add("@COMP_NAME", SqlDbType.NVarChar, 100).Value = policy.COMP_NAME ?? "";
                        cmd.Parameters.Add("@COMP_ADD", SqlDbType.NVarChar).Value = policy.COMP_ADD ?? "";

                        cmd.Parameters.Add("@POLICY_AMT", SqlDbType.Decimal).Value = policy.POLICY_AMT ?? 0m;

                        cmd.Parameters.Add("@ENTRY_DATE", SqlDbType.SmallDateTime).Value = (object?)policy.ENTRY_DATE ?? DBNull.Value;
                        cmd.Parameters.Add("@EFF_DATE", SqlDbType.SmallDateTime).Value = (object?)policy.EFF_DATE ?? DBNull.Value;
                        cmd.Parameters.Add("@EXP_DATE", SqlDbType.SmallDateTime).Value = (object?)policy.EXP_DATE ?? DBNull.Value;

                        cmd.Parameters.Add("@ACTIVE", SqlDbType.Int).Value = policy.ACTIVE;

                        cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;

                        cmd.Parameters.Add("@EUSER", SqlDbType.Int).Value = globalVar.PubUserId;
                        cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;

                        cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = policy.AED ?? "A";
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
        public JsonResult DeleteInsurancePolicyDetail(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsuranceMast", con))
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting this record.", error = ex.Message });
            }
        }

        private bool IsDuplicateInsurancePolicyName(string branchName)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM INSU_MAST WHERE NAME = @Name", con))
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
