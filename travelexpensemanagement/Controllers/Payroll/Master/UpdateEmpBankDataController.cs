using DocumentFormat.OpenXml.EMMA;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Mail;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class UpdateEmpBankDataController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public UpdateEmpBankDataController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            ViewBag.CurrentMenu = "Emp Bank Data";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Payroll/Master/UpdateEmpBankData/Index.cshtml", model);
        }
           
        [HttpGet]

        public JsonResult DDLEmployee()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select  code , name from EMP_MAST WHERE comp_code = " + getdata.PubCompCode + "  and   active = 1  and  name <> '' ORDER BY name asc ";

                var Emplist = _dropdownService.GetDropdownList(query);

                return Json(Emplist);
            }

        }

        [HttpGet]
        public JsonResult DDlBank()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select code,name from BANK_MAST where ACTIVE=1 and  Name <> '' Order by Name asc";

                var BankList = _dropdownService.GetDropdownList(query);

                return Json(BankList);
            }

        }

        [HttpGet]
        public IActionResult GetEmpDataByCode([FromQuery] int code)
        {
            UpdateEmpBankDataModel model = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_UpdateEmpBankDataList", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "EmpSelect");
                    cmd.Parameters.AddWithValue("@EMP_CODE", code);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            model = new UpdateEmpBankDataModel
                            {
                                EMP_Name = rdr["EmployeeName"] != DBNull.Value ? Convert.ToString(rdr["EmployeeName"]) : "",
                                BANK_NAME = rdr["BANK_NAME"] != DBNull.Value ? Convert.ToString(rdr["BANK_NAME"]) : "",
                                BANK_CODE = rdr["BANK_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["BANK_CODE"]) : 0,
                                BRANCH = rdr["BRANCH"] != DBNull.Value ? Convert.ToString(rdr["BRANCH"]) : "",
                                AC_NO = rdr["AC_NO"] != DBNull.Value ? Convert.ToString(rdr["AC_NO"]) : "",
                                IFSC_CODE = rdr["IFSC_CODE"] != DBNull.Value ? Convert.ToString(rdr["IFSC_CODE"]) : "",
                                AC_TYPE = rdr["AC_TYPE"] != DBNull.Value ? Convert.ToString(rdr["AC_TYPE"]) : "",
                                BANK_VERIFY = rdr["BANK_VERIFY"] != DBNull.Value ? Convert.ToString(rdr["BANK_VERIFY"]) : ""
                            };
                        }
                    }
                }

                return Json(new { success = true, data = model });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank details", error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SavedData([FromBody] UpdateEmpBankDataModel data)
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
        private string Submitbtn(UpdateEmpBankDataModel data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
   

                    using (SqlCommand cmd = new SqlCommand("sp_UpdateEmpBankDataList", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", data.CODE);
                        cmd.Parameters.AddWithValue("@M_TYPE", "EMPB");
                        cmd.Parameters.AddWithValue("@EMP_CODE", data.EMP_CODE);
                        cmd.Parameters.AddWithValue("@BANK_CODE", data.BANK_CODE);
                        cmd.Parameters.AddWithValue("@BANK_NAME", data.BANK_NAME);
                        cmd.Parameters.AddWithValue("@BRANCH", data.BRANCH);
                        cmd.Parameters.AddWithValue("@AC_NO", data.AC_NO);
                        cmd.Parameters.AddWithValue("@IFSC_CODE", data.IFSC_CODE);
                        cmd.Parameters.AddWithValue("@AC_TYPE", data.AC_TYPE);
                        cmd.Parameters.AddWithValue("@BANK_VERIFY", data.BANK_VERIFY);
                        cmd.Parameters.AddWithValue("@FileName",(data.FileName ?? ""));
                        cmd.Parameters.AddWithValue("@Filepath", "/attachments/EmpBankData/" + (data.FileName ?? ""));
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

        public JsonResult Getcode()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            int? code = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                 select max(code) + 1  from emp_updatebank where COMP_CODE =@CompCode   ; ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);


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

