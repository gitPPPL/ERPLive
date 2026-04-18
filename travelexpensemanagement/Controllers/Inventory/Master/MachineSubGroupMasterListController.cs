using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Master;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    [SessionAuthorize]
    public class MachineSubGroupMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public MachineSubGroupMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Inventory/Master/MachineSubGroupMasterList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAllMachingSubGrp(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var subGroupList = new List<MACHINE_SGRP_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_SGRP_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value); // not filtering by specific code

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                subGroupList.Add(new MACHINE_SGRP_MAST
                                {
                                    COMP_CODE = Convert.ToInt32(compCode),
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    MGROUP_CODE = reader["MGROUP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MGROUP_CODE"]) : 0,
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    WSID = reader["WSID"]?.ToString()
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, list = subGroupList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching subgroups", error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetMachineSubByCode(int code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            MACHINE_SGRP_MAST machineSub = null;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_SGRP_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", code);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                machineSub = new MACHINE_SGRP_MAST
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    MGROUP_CODE = reader["MGROUP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MGROUP_CODE"]) : 0,
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = machineSub });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching machine sub-group data", error = ex.Message });
            }
        }

    }
}
