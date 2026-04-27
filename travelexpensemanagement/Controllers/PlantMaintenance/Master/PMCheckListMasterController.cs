
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.PlantMaintenance.Master.PMCheckListMaster;
namespace travelexpensemanagement.Controllers.PlantMaintenance.Master
{
    public class PMCheckListMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public PMCheckListMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/PlantMaintenance/Master/PMCheckListMaster/Index.cshtml");
        }

        [HttpGet]
        public IActionResult CategoryDDL()
        {
            var getData=_globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from PMCAT_MAST where COMP_CODE=1 order by code";
            var category = _dropdownService.GetDropdownList(query);
            return Json(new {success = true, data=category});
        }
        [HttpGet]
        public IActionResult ActivityDDl()
        {
            var getData= _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from PMACTIVITY_MAST  where COMP_CODE=1 order by name";
            var activity= _dropdownService.GetDropdownList(query);
            return Json(new {success = true,data=activity});
        }
        [HttpGet]
        public IActionResult ParameterDDl()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE ,NAME from PMPARAMETER_MAST where COMP_CODE=1 Order by NAME";
            var parameter = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = parameter });
        }

        [HttpPost]
        public IActionResult SaveAndUpdateCheckListMaster([FromBody] PMCheckListMaster model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            if (model == null)
            {
                return Json(new { success = false, message = "Model is null" });
            }
            try
            {
                string action = (model.CODE == null || model.CODE == 0) ? "Insert" : "Update";
                
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    foreach (var item in model.Details)
                    {
                        
                        using (SqlCommand cmd = new SqlCommand("Sp_PMCheckList_Master", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", model.CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SNO", model.SNO ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TYPE", "PMCL");

                            cmd.Parameters.AddWithValue("@CHECKLIST_TYPE", model.CHECKLIST_TYPE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CHECKLIST_NAME", model.CHECKLIST_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CATEGORY_CODE", model.CATEGORY_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ACTIVITY_CODE", model.ACTIVITY_CODE ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@PARAMETER_CODE", item.PARAMETER_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PARAMETER_NAME", item.PARAMETER_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@REMARKS", item.REMARKS ?? (object)DBNull.Value);

                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);

                            cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);

                            cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            cmd.Parameters.AddWithValue("@Action", action);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                string message = action == "Insert" ? "Data Inserted Successfully!!" : "Data Updated Successfully!!";

                return Json(new { success = true, message = message });
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
            var model = new PMCheckListMaster();
            var details = new List<PMCheckListMasterModel>();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_PMCheckList_Master", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@Action", "Select");

                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        model = new PMCheckListMaster
                        {
                            CODE = Convert.ToInt32(reader["CODE"]),
                            CHECKLIST_TYPE = reader["CHECKLIST_TYPE"]?.ToString(),
                            CHECKLIST_NAME = reader["CHECKLIST_NAME"]?.ToString(),
                            ACTIVITY_CODE = reader["ACTIVITY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVITY_CODE"]) : (int?)null,
                            CATEGORY_CODE = reader["CATEGORY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CATEGORY_CODE"]) : (int?)null
                        };

                        details.Add(new PMCheckListMasterModel
                        {
                            PARAMETER_CODE = reader["PARAMETER_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARAMETER_CODE"]) : (int?)null,
                            PARAMETER_NAME = reader["PARAMETER_NAME"]?.ToString(),
                            REMARKS = reader["REMARKS"]?.ToString()
                        });
                    }
                    
                }
                return Json(new { success = true, data = model, details = details });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
