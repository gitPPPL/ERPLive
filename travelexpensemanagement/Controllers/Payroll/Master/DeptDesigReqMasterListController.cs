using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Cms;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.DeptDesigReqMastModel;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class DeptDesigReqMasterListController : Controller
    {             
            private readonly DataBaseConnection _dbConnection;
            private readonly GlobalVariableService _globalVariableService;
            private readonly DropdownService _dropdownService;
            private readonly DbHelper _dbHelper;
            private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
            private int? userLevel;
            public DeptDesigReqMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
                    return View("~/Views/Payroll/Master/DeptDesigReqMasterList/Index.cshtml");
                }

        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var DeptDesigReqMastModel = new List<DeptDesigReqMastModel>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_DeptReqMast", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DeptDesigReqMastModel.Add(new DeptDesigReqMastModel
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                DEPT_CODE = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0,
                                DeptName =  reader["Department"] != DBNull.Value ? reader["Department"].ToString() : string.Empty,
                                DESG_CODE = reader["DESG_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DESG_CODE"]) : 0,
                                Desgn = reader["Designation"] != DBNull.Value ? reader["Designation"].ToString() : string.Empty,
                                PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : 0,
                                Place =  reader["Place"] != DBNull.Value ? reader["Place"].ToString() : string.Empty,
                                SHIFT_A = reader["SHIFT_A"] != DBNull.Value ? Convert.ToInt32(reader["SHIFT_A"]) : 0,
                                SHIFT_B = reader["SHIFT_B"] != DBNull.Value ? Convert.ToInt32(reader["SHIFT_B"]) : 0,
                                SHIFT_C = reader["SHIFT_C"] != DBNull.Value ? Convert.ToInt32(reader["SHIFT_C"]) : 0,
                                SHIFT_G = reader["SHIFT_G"] != DBNull.Value ? Convert.ToInt32(reader["SHIFT_G"]) : 0,
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching DeptDesigReqMast", error = ex.Message });
            }

            return Json(new { success = true, lists = DeptDesigReqMastModel, totalCount });
        }



        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            DeptDesigReqMastModel DeptDesigReqMastModel = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeptReqMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);


                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                DeptDesigReqMastModel = new DeptDesigReqMastModel
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                                    DeptName = rdr["Department"] != DBNull.Value ? Convert.ToString(rdr["Department"]) : "",
                                                                        DESG_CODE = rdr["DESG_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DESG_CODE"]) : 0,
                                    Desgn = rdr["Designation"] != DBNull.Value ? Convert.ToString(rdr["Designation"]) : "",
                                    PLACE_CODE = rdr["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["PLACE_CODE"]) : 0,
                                    Place = rdr["Place"] != DBNull.Value ? Convert.ToString(rdr["Place"]) : "" ,

                                    SHIFT_A = rdr["SHIFT_A"] != DBNull.Value ? Convert.ToInt32(rdr["SHIFT_A"]) : 0,
                                    SHIFT_B = rdr["SHIFT_B"] != DBNull.Value ? Convert.ToInt32(rdr["SHIFT_B"]) : 0,
                                    SHIFT_C = rdr["SHIFT_C"] != DBNull.Value ? Convert.ToInt32(rdr["SHIFT_C"]) : 0,
                                    SHIFT_G = rdr["SHIFT_G"] != DBNull.Value ? Convert.ToInt32(rdr["SHIFT_G"]) : 0,
                                    ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, data = DeptDesigReqMastModel });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult Delete(int code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_DeptReqMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
         

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Dept Req  Master deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Dept Req Master.", error = ex.Message });
            }
        }


    }
}
