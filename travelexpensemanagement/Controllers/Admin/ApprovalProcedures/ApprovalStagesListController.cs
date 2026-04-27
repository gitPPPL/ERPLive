using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.ApprovalProcedures;
namespace travelexpensemanagement.Controllers.Admin.ApprovalProcedures
{
    public class ApprovalStagesListController : Controller
    {



        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public ApprovalStagesListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Admin/ApprovalProcedures/ApprovalStagesList/Index.cshtml");
        }

        public IActionResult GetDataList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var ApprovalStage_Model = new List<ApprovalStage_Model>();
            int totalCount = 0;
            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_Approval_Stage", conn))
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
                            ApprovalStage_Model.Add(new ApprovalStage_Model
                            {
                                DOC_CODE = reader["CODE"] != DBNull.Value ? reader["CODE"].ToString() : string.Empty,
                                Fullname = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty

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

            return Json(new { success = true, lists = ApprovalStage_Model, totalCount });
        }

        [HttpGet]
        public IActionResult GetdatabyCode(string code)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_Approval_Stage", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "ShowData");
                    cmd.Parameters.AddWithValue("@DOC_CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                     
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

                                USER_CODE = rdr["USER_CODE"] != DBNull.Value
                                ? Convert.ToInt32(rdr["USER_CODE"])
                                : (int?)null,


                                DOC_CODE = rdr["DOC_CODE"]?.ToString(),
                                    FULL_NAME = rdr["FULL_NAME"]?.ToString(),
                                    DESIGNATION = rdr["DESIGNATION"]?.ToString(),
                                    DEPARTMENT = rdr["DEPARTMENT"]?.ToString(),
                                    approval_user = rdr["APPROV_USER"]?.ToString(),
                                    FLAG_A = rdr["FLAG_A"]?.ToString(),
                                    FLAG_B = rdr["FLAG_B"]?.ToString(),
                                    FLAG_C = rdr["FLAG_C"]?.ToString(),
                                    FLAG_D = rdr["FLAG_D"]?.ToString(),
                                    FLAG_E = rdr["FLAG_E"]?.ToString(),

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
                    using (SqlCommand cmd = new SqlCommand("sp_Approval_Stage", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Approval Stages deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting   Approval Stages  .", error = ex.Message });
            }
        }

    }
}
