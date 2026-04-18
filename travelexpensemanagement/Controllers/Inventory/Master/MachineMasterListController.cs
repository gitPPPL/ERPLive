using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Master;

namespace travelexpensemanagement.Controllers.Inventory.Master
{
    public class MachineMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        private int? userLevel;
        public MachineMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Inventory/Master/MachineMasterList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetAllMachineMast(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var machineList = new List<MACHINE_MAST>();
            int totalCount = 0;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                machineList.Add(new MACHINE_MAST
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    LDMS_CODE = reader["LDMS_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LDMS_CODE"]) : 0,
                                    DEPT_CODE = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0,
                                    MACHSGRP = reader["MACHSGRP"] != DBNull.Value ? Convert.ToInt32(reader["MACHSGRP"]) : 0,
                                    MACHMGRP = reader["MACHMGRP"] != DBNull.Value ? Convert.ToInt32(reader["MACHMGRP"]) : 0,
                                    SORT_SR = reader["SORT_SR"] != DBNull.Value ? Convert.ToInt32(reader["SORT_SR"]) : 0,
                                    BLOCK = reader["BLOCK"]?.ToString(),
                                    TYPE = reader["TYPE"]?.ToString(),
                                    MAKE_TYPE = reader["MAKE_TYPE"]?.ToString(),
                                    BLOCK_TYPE = reader["BLOCK_TYPE"]?.ToString(),
                                    PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : 0,
                                    PROD_CODE = reader["PROD_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PROD_CODE"]) : 0,
                                    PROD_GRAM = reader["PROD_GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["PROD_GRAM"]) : 0,
                                    PROD_TYPE = reader["PROD_TYPE"]?.ToString(),
                                    PROD_SIZE = reader["PROD_SIZE"] != DBNull.Value ? Convert.ToDecimal(reader["PROD_SIZE"]) : 0,
                                    PROD_COLOR = reader["PROD_COLOR"]?.ToString(),
                                    CPROD_CODE = reader["CPROD_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CPROD_CODE"]) : 0,
                                    CPROD_DATE = reader["CPROD_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CPROD_DATE"]) : DateTime.MinValue,
                                    PROD_CODE1 = reader["PROD_CODE1"] != DBNull.Value ? Convert.ToInt32(reader["PROD_CODE1"]) : 0,
                                    REMARK = reader["REMARK"]?.ToString(),
                                    OLD_NAME = reader["OLD_NAME"]?.ToString(),
                                    REPORT_FILTER = reader["REPORT_FILTER"] != DBNull.Value ? Convert.ToInt32(reader["REPORT_FILTER"]) : 0,
                                    STD_PPM = reader["STD_PPM"] != DBNull.Value ? Convert.ToInt32(reader["STD_PPM"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    STATUS = reader["STATUS"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0,
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    EFF = reader["EFF"] != DBNull.Value ? Convert.ToDecimal(reader["EFF"]) : 0,
                                    COSTCAT_CODE = reader["COSTCAT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COSTCAT_CODE"]) : 0
                                });
                            }


                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, lists = machineList, totalCount }); 
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching subgroups", error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetMachineByCode(int code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            MACHINE_MAST machine = null;

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MACHINE_MAST", conn))
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
                                machine = new MACHINE_MAST
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                    NAME = reader["NAME"]?.ToString(),
                                    SHORTNAME = reader["SHORTNAME"]?.ToString(),
                                    LDMS_CODE = reader["LDMS_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LDMS_CODE"]) : 0,
                                    DEPT_CODE = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0,
                                    MACHSGRP = reader["MACHSGRP"] != DBNull.Value ? Convert.ToInt32(reader["MACHSGRP"]) : 0,
                                    MACHMGRP = reader["MACHMGRP"] != DBNull.Value ? Convert.ToInt32(reader["MACHMGRP"]) : 0,
                                    SORT_SR = reader["SORT_SR"] != DBNull.Value ? Convert.ToInt32(reader["SORT_SR"]) : 0,
                                    BLOCK = reader["BLOCK"]?.ToString(),
                                    TYPE = reader["TYPE"]?.ToString(),
                                    MAKE_TYPE = reader["MAKE_TYPE"]?.ToString(),
                                    BLOCK_TYPE = reader["BLOCK_TYPE"]?.ToString(),
                                    PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : 0,
                                    PROD_CODE = reader["PROD_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PROD_CODE"]) : 0,
                                    PROD_GRAM = reader["PROD_GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["PROD_GRAM"]) : 0,
                                    PROD_TYPE = reader["PROD_TYPE"]?.ToString(),
                                    PROD_SIZE = reader["PROD_SIZE"] != DBNull.Value ? Convert.ToDecimal(reader["PROD_SIZE"]) : 0,
                                    PROD_COLOR = reader["PROD_COLOR"]?.ToString(),
                                    CPROD_CODE = reader["CPROD_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CPROD_CODE"]) : 0,
                                    CPROD_DATE = reader["CPROD_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CPROD_DATE"]) : DateTime.MinValue,
                                    PROD_CODE1 = reader["PROD_CODE1"] != DBNull.Value ? Convert.ToInt32(reader["PROD_CODE1"]) : 0,
                                    REMARK = reader["REMARK"]?.ToString(),
                                    OLD_NAME = reader["OLD_NAME"]?.ToString(),
                                    REPORT_FILTER = reader["REPORT_FILTER"] != DBNull.Value ? Convert.ToInt32(reader["REPORT_FILTER"]) : 0,
                                    STD_PPM = reader["STD_PPM"] != DBNull.Value ? Convert.ToInt32(reader["STD_PPM"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    STATUS = reader["STATUS"]?.ToString(),
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                    SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0,
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    EFF = reader["EFF"] != DBNull.Value ? Convert.ToDecimal(reader["EFF"]) : 0,
                                    COSTCAT_CODE = reader["COSTCAT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COSTCAT_CODE"]) : 0
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = machine });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching machine data", error = ex.Message });
            }
        }

    }
}
 