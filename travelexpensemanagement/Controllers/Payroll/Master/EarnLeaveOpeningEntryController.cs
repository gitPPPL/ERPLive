using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class EarnLeaveOpeningEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public EarnLeaveOpeningEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Master/EarnLeaveOpeningEntry/Index.cshtml");
        }

        public JsonResult DDLLeaveType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select name,code from PAY_LEAVETYPE order by name asc ";

                var LeaveTypelist = _dropdownService.GetDropdownList(query);

                return Json(LeaveTypelist);
            }

        }

        public JsonResult DDLEmployee()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = "select NAME,CODE from EMP_MAST  where COMP_CODE  = " + getdata.PubCompCode  +" order by name asc";
             
                var Emplist = _dropdownService.GetDropdownList(query);

                return Json(Emplist);
            }

        }

        public JsonResult GetVNo()
        {
            string newV_NO = "00000";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {

                    con.Open();

                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";
                    prefixYR = prefixYR.Substring(prefixYR.Length - 2);


                    string lastV_NO_Query = "SELECT MAX(V_NO) FROM PAY_LEAVEBAL WHERE COMP_CODE = @CompCode AND YEAR_CODE = @YearCode";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    object result = lastVnoCmd.ExecuteScalar();

                    if (result != DBNull.Value && result != null)
                    {
                        int lastV_NO = Convert.ToInt32(result);
                        newV_NO = (lastV_NO + 1).ToString("D5");
                    }
                    else
                    {
                        newV_NO = "00001";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }


        [HttpPost]
        public IActionResult SavedData([FromBody] EarnLeaveOpeningEntryModel data)
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

        [HttpPost]
        private string Submitbtn(EarnLeaveOpeningEntryModel data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                int v_NO = 0;

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_EarnLeaveOpeningEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", data.V_NO);
                        cmd.Parameters.AddWithValue("@DOC_ID", (data.V_TYPE ?? "BAL") + data.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", "BAL");
                        cmd.Parameters.AddWithValue("@V_DATE", data.V_DATE);
                        cmd.Parameters.AddWithValue("@EMP_CODE", data.EMP_CODE);
                        cmd.Parameters.AddWithValue("@LEAVE_CODE", data.LEAVE_CODE);
                        cmd.Parameters.AddWithValue("@LEAVE_TYPE", data.LEAVE_TYPE);
                        cmd.Parameters.AddWithValue("@OP_DAYS", data.OP_DAYS);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", "");
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
