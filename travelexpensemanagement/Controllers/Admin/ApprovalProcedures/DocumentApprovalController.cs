using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.ApprovalProcedures;
using static iTextSharp.text.pdf.events.IndexEvents;

namespace travelexpensemanagement.Controllers.Admin.ApprovalProcedures
{
    public class DocumentApprovalController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public DocumentApprovalController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Admin/ApprovalProcedures/DocumentApproval/Index.cshtml");
        }

        public async Task<IActionResult> GetTableData()
        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_Document_Approval", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "TableData");
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                                           
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            var results = new List<object>();
                            while (await rdr.ReadAsync())
                            {
                                var result = new
                                {
                                    code = rdr["code"] != DBNull.Value ? Convert.ToInt32(rdr["code"]) : 0,
                                    FULL_NAME = rdr["FULL_NAME"]?.ToString(),
                                    DESIGNATION = rdr["DESIGNATION"]?.ToString(),
                                    DEPARTMENT = rdr["DEPARTMENT"]?.ToString(),
                               
                                };

                                results.Add(result);
                            }

                            if (results.Any())
                            {
                                return Json(new
                                {
                                    success = true,
                                    message = "Data fetched successfully",
                                    data = results
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "No data found"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching purchase requisition data",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        public JsonResult DDLDocumentType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from DOCTYPE_MAST  order by name";

                var DDLDocumentType = _dropdownService.GetDropdownList(query);

                return Json(DDLDocumentType);
            }

        }



        public class Form12SaveRequest
        {
            public DateTime VDate { get; set; }

            public String Action { get; set; }

            public List<DocumentApproval_Model> Data { get; set; }
        }
         


        [HttpPost]
        public IActionResult SaveData([FromBody] Form12SaveRequest request)
        {
            if (request.Data == null || !request.Data.Any())
                return Json(new { success = false, message = "No data received." });

            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                using var transaction = conn.BeginTransaction();

                var docCode = request.Data.First().DOC_CODE;


                if (request.Action == "Insert")
                {
                    string checkSql = @"
                        SELECT COUNT(1) 
                        FROM DOC_USER 
                        WHERE DOC_CODE = @DOC_CODE 
                        AND COMP_CODE = @COMP_CODE;";

                    using (var checkCmd = new SqlCommand(checkSql, conn, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@DOC_CODE", docCode);

                        int existingCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (existingCount > 0)
                        {
                      
                            transaction.Rollback();
                            return Json(new { success = false, message = "Data already exists for this DOC_CODE." });
                        }
                    }
                }


                string deleteSql = @"
                    DELETE FROM DOC_USER 
                    WHERE DOC_CODE = @DOC_CODE 
                    AND COMP_CODE = @COMP_CODE;";

                using (var deleteCmd = new SqlCommand(deleteSql, conn, transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@DOC_CODE", docCode);
                    deleteCmd.ExecuteNonQuery();
                }

              
                foreach (var entry in request.Data)
                {
                    using var cmd = new SqlCommand("sp_Document_Approval", conn, transaction)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@Action", "INSERT");
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@DOC_CODE", entry.DOC_CODE);
                    cmd.Parameters.AddWithValue("@USER_CODE", entry.USER_CODE);

                    // Audit info
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();

                return Json(new { success = true, message = "Shift schedule saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving shift schedule.", error = ex.Message });
            }
        }










    }
}
