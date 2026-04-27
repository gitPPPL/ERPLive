using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.DeptDesigReqMastModel;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class DeptDesigReqMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public DeptDesigReqMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Master/DeptDesigReqMaster/Index.cshtml");
        }

        // Department dropdown
        [HttpGet]

        public JsonResult DDLDeptMaster()
        {
                 var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT code, name FROM Dept_mast WHERE active = 1 AND comp_code = " + getdata.PubCompCode + " ORDER BY name";

                var DeptList = _dropdownService.GetDropdownList(query);

                return Json(DeptList);
            }

        }


        // Designation dropdown
        [HttpGet]
        public JsonResult DDLDesigination()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT Code, name FROM Desg_mast WHERE active = 1 AND comp_code = " + getdata.PubCompCode + " ORDER BY name";
                var DesigList = _dropdownService.GetDropdownList(query);
                return Json(DesigList);
            }
        }

        // Place dropdown
        [HttpGet]
        public JsonResult DDLPlaceMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT Code, name FROM PLACE_MAST WHERE comp_code = " + getdata.PubCompCode + " ORDER BY name";
                var PlaceList = _dropdownService.GetDropdownList(query);
                return Json(PlaceList);
            }
        }

        // Save   Request
        [HttpPost]
        public IActionResult SavedDeptReqMaster([FromBody] DeptDesigReqMastModel data)
        {
            if (data == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            string action = data.action == "INSERT" ? "Insert" : "Update";
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

        // Submit Data Method
        [HttpPost]
        private string Submitbtn(DeptDesigReqMastModel data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_DeptReqMast", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", data.CODE);
                        cmd.Parameters.AddWithValue("@DEPT_CODE", data.DEPT_CODE);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", data.PLACE_CODE);
                        cmd.Parameters.AddWithValue("@DESG_CODE", data.DESG_CODE);
                        cmd.Parameters.AddWithValue("@SHIFT_A", data.SHIFT_A);
                        cmd.Parameters.AddWithValue("@SHIFT_B", data.SHIFT_B);
                        cmd.Parameters.AddWithValue("@SHIFT_C", data.SHIFT_C);
                        cmd.Parameters.AddWithValue("@SHIFT_G", data.SHIFT_G);
                        cmd.Parameters.AddWithValue("@ACTIVE", data.ACTIVE);
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
    }
}
