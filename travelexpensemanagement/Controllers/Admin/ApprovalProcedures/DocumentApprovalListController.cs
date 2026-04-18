using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.ApprovalProcedures;
using travelexpensemanagement.Models.PayRoll;

namespace travelexpensemanagement.Controllers.Admin.ApprovalProcedures
{
    public class DocumentApprovalListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public DocumentApprovalListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
        ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/ApprovalProcedures/DocumentApprovalList/Index.cshtml");
        }
        public IActionResult GetDataList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var DocumentApproval_Model = new List<DocumentApproval_Model>();
            int totalCount = 0;
            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_Document_Approval", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@DOC_CODE", DBNull.Value);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DocumentApproval_Model.Add(new DocumentApproval_Model
                            {
                                DOC_CODE = reader["CODE"] != DBNull.Value ? reader["CODE"].ToString() : string.Empty,
                                NAME = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty
                       
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
                return Json(new { success = false, message = "Error fetching categories", error = ex.Message });
            }

            return Json(new { success = true, lists = DocumentApproval_Model, totalCount });
        }
        [HttpGet]
        public IActionResult GetdatabyCode(string code)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_Document_Approval", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "ShowData");
                    cmd.Parameters.AddWithValue("@DOC_CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        // ===== HEADER =====
                        var header = new List<object>();
                        while (rdr.Read())
                        {
                            header.Add(new
                            {
                                DOC_CODE = rdr["DOC_CODE"]?.ToString(),
                                DOCUMENT_NAME = rdr["DOCUMENT_NAME"]?.ToString()
                            });
                        }

                        // ===== DETAILS =====
                        var details = new List<object>();
                        if (rdr.NextResult()) // <-- Move to 2nd result set
                        {
                            while (rdr.Read())
                            {
                                details.Add(new
                                {
                                    CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                    FULL_NAME = rdr["FULL_NAME"]?.ToString(),
                                    DESIGNATION = rdr["DESIGNATION"]?.ToString(),
                                    DEPARTMENT = rdr["DEPARTMENT"]?.ToString()
                                });
                            }
                        }

                        return Json(new { success = true, header = header, details = details });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data", error = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult Delete(string code)
        {
            var globalvariable = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_Document_Approval", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);
                                           
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = " Document Approval deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting  Document Approval  Master.", error = ex.Message });
            }
        }

    }
}
