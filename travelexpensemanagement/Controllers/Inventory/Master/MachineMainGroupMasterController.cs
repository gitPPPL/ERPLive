using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Master;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    [SessionAuthorize]
    public class MachineMainGroupMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        private int? userLevel;
        public MachineMainGroupMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
          
            return View("~/Views/Inventory/Master/MachineMainGroupMaster/Index.cshtml");
        }
        [HttpPost]
        public IActionResult SaveMachineGRP([FromBody] MACHINE_GRP_MAST model)
        {
            if (string.IsNullOrWhiteSpace(model.NAME))
            {
                return Json(new { success = false, message = "Machine group name cannot be blank." });
            }
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            // Check for duplicate name before insert
            if (action == "INSERT" && IsDuplicateMachingGrpName(model.NAME))
            {
                return Json(new { success = false, message = "Machaine group name already exists." });
            }
            var result = SaveOrUpdateMachineGroup(model, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }

        public string SaveOrUpdateMachineGroup(MACHINE_GRP_MAST model, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_GRP_MAST", con)) // Replace with actual SP name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", model.CODE);
                        cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
                        cmd.Parameters.AddWithValue("@SHORTNAME", model.SHORTNAME ?? "");
                        cmd.Parameters.AddWithValue("@COST_DEPT", model.COST_DEPT ?? "");
                        cmd.Parameters.AddWithValue("@COST_FL", model.COST_FL);
                        cmd.Parameters.AddWithValue("@COST_WL", model.COST_WL);
                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", model.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

                        con.Open();
                        cmd.ExecuteNonQuery();

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [HttpPost]
        public JsonResult DeleteMachinMainGrpByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_GRP_MAST", con))
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
                return Json(new { success = false, message = "Error deleting record: " + ex.Message });
            }
        }

        private bool IsDuplicateMachingGrpName(string BankName)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM MACHINE_GRP_MAST WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", BankName ?? "");

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }
    }
}
