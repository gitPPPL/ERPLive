using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Crypto.Macs;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.PlantMaintenance.Master.MaintenancePlanMaster;

namespace travelexpensemanagement.Controllers.PlantMaintenance.Master
{
    public class MaintenancePlanMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public MaintenancePlanMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/PlantMaintenance/Master/MaintenancePlanMaster/Index.cshtml");
        }

        [HttpGet]
        public IActionResult ActivityDDl()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from PMACTIVITY_MAST  where COMP_CODE=1 order by name";
            var activity = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = activity });
        }

        [HttpGet]
        public IActionResult SectionDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE ,NAME FROM ITEMDEPT_MAST WHERE TRAN_TYPE='STORE' and COMP_CODE =1 order by Name";
            var section = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = section });
        }

        [HttpGet]
        public IActionResult EquipmentDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT MACHINE_TYPE_CODE 'CODE',MACHINE_TYPE 'NAME' FROM PMMACHINE_MAST WHERE COMP_CODE=1 AND BRANCH_CODE =1 ORDER BY MACHINE_TYPE ";
            var equipment = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = equipment });
        }

        [HttpGet]
        public IActionResult ItemDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE,NAME FROM ITEM_MAST WHERE COMP_CODE=1";
            var item = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = item });
        }

        [HttpGet]
        public IActionResult PlaceDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE ,NAME FROM PLACE_MAST WHERE COMP_CODE =1 order by Name ";
            var place = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = place });
        }

        [HttpGet]
        public IActionResult FrequencyDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select Code,name from PM_frequency_mast Order by code";
            var frequency = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = frequency });
        }

        [HttpGet]
        public IActionResult CategoryDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE ,NAME FROM PMCAT_MAST WHERE COMP_CODE =1 order by code ";
            var category = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = category });
        }
        
        [HttpGet]
        public IActionResult SearchItem(string search)
        {
            var getData = _globalVariableService.GetGlobalVariables();

            string query = "SELECT TOP 100 CODE, NAME FROM ITEM_MAST WHERE COMP_CODE = 1";

            if (!string.IsNullOrEmpty(search))
            {
                query += " AND NAME LIKE '%" + search + "%'";
            }

            query += " ORDER BY NAME";

            var items = _dropdownService.GetDropdownList(query);

            return Json(items);
        }

        [HttpPost]
        public IActionResult SaveOrUpdateData([FromBody] MaintenancePlanMaster model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            if (model == null)
            {
                return Json(new { success = false, error = "Model binding failed" });
            }
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    int planCode = model.CODE ?? 0;
                    bool isInsert = (planCode == 0);

                    string action = isInsert ? "InsertMaster" : "Update";

                    // ===== MASTER SAVE =====
                    SqlCommand cmd = new SqlCommand("Sp_MaintenancePlan_Master", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@CODE", model.CODE);
                    cmd.Parameters.AddWithValue("@PLAN_NAME", model.PLAN_NAME ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@M_CODE", model.M_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@M_NAME", model.M_NAME ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PLACE_NAME", model.PLACE_NAME ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@SECTION_CODE", model.SECTION_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SECTION_NAME", model.SECTION_NAME ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@FREQUENCY_CODE", model.FREQUENCY_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FREQUENCY", model.FREQUENCY ?? (object)DBNull.Value);
                   
                    cmd.Parameters.AddWithValue("@CAT_CODE", model.CAT_CODE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@CAT_NAME", model.CAT_NAME ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@DUE_DAYS", model.DUE_DAYS ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@DUE_DATE", model.DUE_DATE ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE ?? (object)DBNull.Value);

                    cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                    
                    cmd.Parameters.AddWithValue("@Action", action);

                    if (isInsert)
                    {
                        planCode = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    else
                    {
                        cmd.ExecuteNonQuery();

                        // ===== DELETE OLD ACTIVITY & SPARES =====
                        SqlCommand delCmd = new SqlCommand("Sp_MaintenancePlan_Master", con);
                        delCmd.CommandType = CommandType.StoredProcedure;

                        delCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        delCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        delCmd.Parameters.AddWithValue("@CODE", planCode);
                        delCmd.Parameters.AddWithValue("@Action", "DeleteDetails");

                        delCmd.ExecuteNonQuery();
                    }

                    // ===== ACTIVITY LOOP =====
                    if (model.Details != null)
                    {
                        foreach (var act in model.Details)
                        {
                            SqlCommand cmdAct = new SqlCommand("Sp_MaintenancePlan_Master", con);
                            cmdAct.CommandType = CommandType.StoredProcedure;

                            cmdAct.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmdAct.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmdAct.Parameters.AddWithValue("@CODE", planCode);

                            cmdAct.Parameters.AddWithValue("@ACTIVITY_CODE", act.ACTIVITY_CODE ?? (object)DBNull.Value);
                            cmdAct.Parameters.AddWithValue("@ACTIVITY_NAME", act.ACTIVITY_NAME ?? (object)DBNull.Value);
                            
                            cmdAct.Parameters.AddWithValue("@ACTIVITY_REMARKS", act.ACTIVITY_REMARKS ?? (object)DBNull.Value);
                            cmdAct.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmdAct.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmdAct.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmdAct.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmdAct.Parameters.AddWithValue("@FREQUENCY", model.FREQUENCY ?? (object)DBNull.Value);
                            cmdAct.Parameters.AddWithValue("@SECTION_CODE", model.SECTION_CODE ?? (object)DBNull.Value);
                            cmdAct.Parameters.AddWithValue("@SECTION_NAME", model.SECTION_NAME ?? (object)DBNull.Value);
                            cmdAct.Parameters.AddWithValue("@LIP",globalVariable.PubLocalId);

                            cmdAct.Parameters.AddWithValue("@Action", "InsertActivity");

                            cmdAct.ExecuteNonQuery();
                        }
                    }

                    // ===== SPARE LOOP =====
                    if (model.Details1 != null)
                    {
                        foreach (var spare in model.Details1)
                        {
                            SqlCommand cmdSpare = new SqlCommand("Sp_MaintenancePlan_Master", con);
                            cmdSpare.CommandType = CommandType.StoredProcedure;

                            cmdSpare.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmdSpare.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmdSpare.Parameters.AddWithValue("@CODE", planCode);

                            cmdSpare.Parameters.AddWithValue("@ITEM_CODE", spare.ITEM_CODE ?? (object)DBNull.Value);
                            cmdSpare.Parameters.AddWithValue("@ITEM_NAME", spare.ITEM_NAME ?? (object)DBNull.Value);
                            cmdSpare.Parameters.AddWithValue("@QUANTITY", spare.QUANTITY ?? (object)DBNull.Value);
                            cmdSpare.Parameters.AddWithValue("@SPARE_REMARKS", spare.SPARE_REMARKS ?? (object)DBNull.Value);

                            cmdSpare.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmdSpare.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmdSpare.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmdSpare.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmdSpare.Parameters.AddWithValue("@Action", "InsertSpare");
                            cmdSpare.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmdSpare.ExecuteNonQuery();
                        }
                    }
                }

                return Json(new { success = true, message = "Data Saved Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult LoadDataOnEdit(int code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            var model = new MaintenancePlanMaster();
            var activityMaster= new List<PMActivityMaster>();
            var spares= new List<PMSpareMaster>();

            try
            {
                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_MaintenancePlan_Master", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@Action", "Select");

                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        model = new MaintenancePlanMaster
                        {
                            CODE = Convert.ToInt32(reader["CODE"]),
                            PLAN_NAME = reader["PLAN_NAME"]?.ToString(),
                            M_CODE = reader["M_CODE"] != DBNull.Value ? Convert.ToInt32(reader["M_CODE"]) : (int?)null,
                            PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : (int?)null,
                            SECTION_CODE = reader["SECTION_CODE"] != DBNull.Value ? Convert.ToInt32(reader["SECTION_CODE"]) : (int?)null,
                            FREQUENCY_CODE = reader["FREQUENCY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["FREQUENCY_CODE"]) : (int?)null,
                            DUE_DAYS = reader["DUE_DAYS"] != DBNull.Value ? Convert.ToInt32(reader["DUE_DAYS"]) : (int?)null,
                            DUE_DATE = reader["DUE_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["DUE_DATE"]) : (DateTime?)null,
                            ACTIVE = reader["ACTIVE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACTIVE"]),
                            CAT_CODE = reader["CAT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CAT_CODE"]) : (int?)null,
                        };
                    }
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            activityMaster.Add(new PMActivityMaster
                            {
                                ACTIVITY_CODE = reader["ACTIVITY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVITY_CODE"]) : (int?)null,
                                ACTIVITY_NAME = reader["ACTIVITY_NAME"]?.ToString(),
                                ACTIVITY_REMARKS = reader["REMARKS"]?.ToString()
                            });
                        }
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            spares.Add(new PMSpareMaster
                            {
                                ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : (int?)null,
                                ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                QUANTITY = reader["QUANTITY"] != DBNull.Value ? Convert.ToInt32(reader["QUANTITY"]) : (int?)null,
                                SPARE_REMARKS = reader["REMARKS"]?.ToString()
                            });
                        }
                    }
                    return Json(new { success = true, data = model, details = activityMaster, details1 = spares });
                }
            }catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
