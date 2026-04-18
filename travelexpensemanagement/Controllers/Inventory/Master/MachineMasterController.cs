using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Master;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    public class MachineMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        private int? userLevel;
        public MachineMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Inventory/Master/MachineMaster/Index.cshtml");
        }
        public IActionResult GetDepartmentList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM DEPT_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 ORDER BY NAME DESC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetMachineSubGroupList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM MACHINE_SGRP_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 ORDER BY NAME DESC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetMachineMainGroupList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM MACHINE_GRP_MAST WHERE COMP_CODE = '" + compCode + "' AND ACTIVE=1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetMachineTypeList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var types = new List<string>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT TYPE FROM MACHINE_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 AND TYPE<>'' ORDER BY TYPE ASC", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["TYPE"].ToString());
                        }
                    }
                }
            }

            return Json(types);
        }
        public IActionResult GetMakeTypeList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var types = new List<string>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT M_TYPE FROM ITEM_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 AND M_TYPE<>'' ORDER BY M_TYPE ASC", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["M_TYPE"].ToString());
                        }
                    }
                }
            }

            return Json(types);
        }
        public IActionResult GetRunningQualityList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,SHORTNAME FROM ITEM_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 ORDER BY NAME DESC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetConviersionQualityList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,SHORTNAME FROM ITEM_MAST WHERE COMP_CODE='" + compCode + "'  AND ACTIVE=1 ORDER BY NAME DESC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetPlaceMastList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM PLACE_MAST WHERE COMP_CODE='" + compCode + "' ORDER BY NAME DESC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpPost]
        public IActionResult SaveMachine([FromBody] MACHINE_MAST model)
        {
            if (string.IsNullOrWhiteSpace(model.NAME))
            {
                return Json(new { success = false, message = "Machine name cannot be blank." });
            }

            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            if (action == "INSERT" && IsDuplicateMachine(model.NAME))
            {
                return Json(new { success = false, message = "Machine name already exists." });
            }

            var result = SaveOrUpdateMachine(model, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }

        public string SaveOrUpdateMachine(MACHINE_MAST machine, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", machine.CODE);
                        cmd.Parameters.AddWithValue("@NAME", machine.NAME ?? "");
                        cmd.Parameters.AddWithValue("@SHORTNAME", machine.SHORTNAME ?? "");
                        cmd.Parameters.AddWithValue("@PLACE_CODE", machine.PLACE_CODE);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", machine.DEPT_CODE);
                        cmd.Parameters.AddWithValue("@MACHSGRP", machine.MACHSGRP);
                        cmd.Parameters.AddWithValue("@MACHMGRP", machine.MACHMGRP);
                        cmd.Parameters.AddWithValue("@TYPE", machine.TYPE ?? "");
                        cmd.Parameters.AddWithValue("@MAKE_TYPE", machine.MAKE_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@PROD_CODE", machine.PROD_CODE);
                        cmd.Parameters.AddWithValue("@BLOCK", machine.BLOCK ?? "");
                        cmd.Parameters.AddWithValue("@STD_PPM", machine.STD_PPM);
                        cmd.Parameters.AddWithValue("@EFF", machine.EFF);
                        cmd.Parameters.AddWithValue("@BLOCK_TYPE", machine.BLOCK_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@STATUS", machine.STATUS ?? "");
                        cmd.Parameters.AddWithValue("@CPROD_CODE", machine.CPROD_CODE);
                        cmd.Parameters.AddWithValue("@REMARK", machine.REMARK ?? "");
                        // Handle nullable DateTime for CPROD_DATE
                        cmd.Parameters.AddWithValue("@CPROD_DATE",machine.CPROD_DATE == DateTime.MinValue ? DBNull.Value : machine.CPROD_DATE);
                        cmd.Parameters.AddWithValue("@ACTIVE", machine.ACTIVE);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
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
        public JsonResult DeleteMachineByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Machine deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private bool IsDuplicateMachine(string machineName)
        {
            if (string.IsNullOrWhiteSpace(machineName))
            {
                return false;
            }

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM MACHINE_MAST WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", machineName.Trim());

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }


    }
}
