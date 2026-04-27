using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Payroll.Transaction;
using travelexpensemanagement.Controllers.Purchase.Transaction;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.PlantMaintenance.Master.FrequencyMaster;

namespace travelexpensemanagement.Controllers.PlantMaintenance.Master
{
    public class FrequencyMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public FrequencyMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/PlantMaintenance/Master/FrequencyMaster/Index.cshtml");
        }
        [HttpPost]
        public IActionResult SaveAndUpdateData([FromBody] FrequencyMaster model)
        {
            var globalVariable =_globalVariableService.GetGlobalVariables();

            if (model == null)
            {
                return Json(new { success = false, message = "Model is null" });
            }
            try
            {
                string action = (model.CODE == null || model.CODE == 0) ? "Insert" : "Update";

                using (SqlConnection con= _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("SP_PM_FREQUENCYMASTER", con);

                    cmd.Parameters.AddWithValue("@CODE", model.CODE);
                    cmd.Parameters.AddWithValue("@NAME",model.NAME);
                    cmd.Parameters.AddWithValue("@SHORTNAME",model.SHORTNAME);
                    cmd.Parameters.AddWithValue("@DAYS", model.DAYS);
                    cmd.Parameters.AddWithValue("@ACTIVE",model.ACTIVE);
                    
                    cmd.Parameters.AddWithValue("@UUSER",globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Action", action);
                    
                    con.Open();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();

                }
                string message = action == "Insert" ? "Data Inserted Successfully!!" : "Data Updated Successfully!!";

                return Json(new { success = true, message = message });
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }
     
        [HttpGet]
        public IActionResult loadDataOnEdit(int code)
        {
            object data = null;

            var globalVariable = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("SP_PM_FREQUENCYMASTER", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Select");
                    cmd.Parameters.AddWithValue("@CODE", code);

                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        data = new
                        {
                            code = reader["CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CODE"]),
                            name = reader["NAME"]?.ToString(),
                            shortName = reader["SHORTNAME"]?.ToString(),
                            days = reader["DAYS"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DAYS"]),
                            active = reader["ACTIVE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACTIVE"])
                        };
                    }
                }

                return Json(new { success = true, data = data });
            }catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
