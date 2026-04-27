using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.BagsProcess.BagTypeMaster;

namespace travelexpensemanagement.Controllers.Production.BagsProcess
{
    public class BagTypeMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Common.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public BagTypeMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/Production/BagsProcess/BagTypeMaster/Index.cshtml");
        }

        [HttpPost]
        public IActionResult SaveAndUpdateData([FromBody] BagTypeMaster model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            string action = model.CODE > 0 ? "Update" : "Insert";
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_BagType_Master", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    con.Open();

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", model.CODE);
                    cmd.Parameters.AddWithValue("@NAME", model.NAME);
                    cmd.Parameters.AddWithValue("@SHORTNAME", model.SHORTNAME);
                    cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                    cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.ExecuteNonQuery();
                }
                string message = action == "Insert" ? "Data Inserted Successfully" : "Data Updated Successfully";
                return Json(new { success = true, message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult loadDataOnEdit(int code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new BagTypeMaster();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_BagType_Master", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@Action", "Edit");
                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        model = new BagTypeMaster
                        {
                            CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                            NAME = reader["NAME"]?.ToString(),
                            SHORTNAME = reader["SHORTNAME"]?.ToString(),
                            ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                        };
                    }
                }
                return Json(new { success = true, data = model });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
