using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class DiwaliBonusMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public DiwaliBonusMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
              DropdownService dropdownService, DbHelper dbHelper,
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

            ViewBag.CurrentMenu = "Diwali Bonus Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Payroll/Master/DiwaliBonusMaster/Index.cshtml", model);
        }

        public JsonResult DDLType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select distinct type[code],type[name] from emp_mast  WHERE  comp_code = " + getdata.PubCompCode + "  and type<>'' ORDER BY type asc";

                var typeList = _dropdownService.GetDropdownList(query);

                return Json(typeList);
            }

        }


        [HttpPost]
        public IActionResult SaveMaster([FromBody] DiwaliBonusModel data)
        {
            if (data == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            string action = data.Action == "INSERT" ? "Insert" : "Update";

            var result = Submitbtn(data, action);

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
        private string Submitbtn(DiwaliBonusModel data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    if (action == "Insert")
                    {
                        string duplicateData = @"
                            SELECT TYPE 
                            FROM DBONUS_MAST 
                            WHERE COMP_CODE = @CompCode 
                            AND join_date = @joinDate 
                            AND TYPE = @TYPE;  ";

                        string TYPE = null;

                        using (SqlCommand cmdYear = new SqlCommand(duplicateData, conn))
                        {
                            cmdYear.Parameters.AddWithValue("@TYPE", data.TYPE);
                            cmdYear.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                            cmdYear.Parameters.AddWithValue("@joinDate", data.JOIN_DATE);
                         

                            using (SqlDataReader reader = cmdYear.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    TYPE = reader["TYPE"]?.ToString();
                                }
                            }
                        }

                        // ✅ Check if record exists
                        if (!string.IsNullOrEmpty(TYPE))
                        {
                            return "Record Already Exist, Please Check! Type " + data.TYPE +" and "+ data.JOIN_DATE +"";
                        }

                    }

                    using (SqlCommand cmd = new SqlCommand("sp_DewaliBonusMaster", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", data.CODE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                        cmd.Parameters.AddWithValue("@TYPE", data.TYPE);
                        cmd.Parameters.AddWithValue("@JOIN_DATE", data.JOIN_DATE);
                        cmd.Parameters.AddWithValue("@PERC", data.PERC);
                        cmd.Parameters.AddWithValue("@AMT", data.AMT);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);


                        int rowsInserted = cmd.ExecuteNonQuery();

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {

                return $"Error: {ex.Message}";
            }
        }


        public JsonResult Getcode()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            int? code = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                 select max(code) + 1  from DBONUS_MAST   WHERE COMP_CODE = @CompCode ; ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
        
                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        code = Convert.ToInt32(result);
                    }
                    else
                    {
                        code = 1;
                    }
                }
            }
            return Json(new { code = code });
        }

    }
}
