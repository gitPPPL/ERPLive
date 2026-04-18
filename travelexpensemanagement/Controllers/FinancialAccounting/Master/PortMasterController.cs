using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class PortMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PortMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/FinancialAccounting/Master/PortMaster/Index.cshtml");
        }
        [HttpPost]
        public async Task<IActionResult> SavePort([FromBody] PORT_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            string result = await SaveOrUpdatePort(model, action); // ✅ Await here

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }
        public async Task<string> SaveOrUpdatePort(PORT_MAST port, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            int exists = 0;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    if (action.Equals("INSERT", StringComparison.OrdinalIgnoreCase))
                    {
                        // Check for duplicate NAME
                        using (SqlCommand checkNameCmd = new SqlCommand("SELECT COUNT(*) FROM PORT_MAST WHERE NAME = @NAME", con))
                        {
                            checkNameCmd.Parameters.AddWithValue("@NAME", _dbHelper.Xnull(port.NAME));
                            exists = (int)await checkNameCmd.ExecuteScalarAsync();
                            if (exists > 0)
                                return "Name already exists!";
                        }
                    }
                    using (SqlCommand cmd = new SqlCommand("sp_PORT_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", port.CODE);
                        cmd.Parameters.AddWithValue("@PORTCODE", port.PORTCODE ?? "");
                        cmd.Parameters.AddWithValue("@NAME", port.NAME ?? "");
                        cmd.Parameters.AddWithValue("@STATE", port.STATE ?? "");
                        cmd.Parameters.AddWithValue("@PORT_TYPE", port.PORT_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", port.ACTIVE);

                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", port.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

                        await cmd.ExecuteNonQueryAsync();
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
        public JsonResult DeletePortByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PORT_MAST", con)) // Replace with your actual stored procedure name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);

                        con.Open();
                        cmd.ExecuteNonQuery();

                        return Json(new { success = true, message = "Port record deleted successfully." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting port: " + ex.Message });
            }
        }


    }
}
 