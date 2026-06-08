using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;


namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class ParameterMasterListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly LogService.LogService _logService;

        public ParameterMasterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, 
            GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "QC Parameter Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel,
            };
            var globalVariables = _globalValue.GetGlobalVariables();

            string databaseName;
            using (var connection = _dbcontext.GetErpConnection())
            {
                databaseName = connection.Database; // Get the database name
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/QualityControl/Master/ParameterMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetQualityParamList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                var pagedList = new List<QCprameterDto>();
                int totalCount = 0;
                
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using(SqlCommand cmd = new SqlCommand("sp_QualityParameterMast_AED", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "GET");
                        cmd.Parameters.AddWithValue("@companyCd", UsersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            // --- RESULT SET 1: QualityParamList ---
                            while (await reader.ReadAsync())
                            {
                                pagedList.Add(new QCprameterDto
                                {
                                    CODE = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : null,
                                    NAME = reader["Name"]?.ToString(),
                                    SHORTNAME = reader["ShortName"]?.ToString(),
                                    QUNIT = reader["Unit"]?.ToString(),
                                    qty = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : null,
                                    ACTIVE = reader["Active"] != DBNull.Value ? Convert.ToInt32(reader["Active"]) : null,
                                });
                            }

                            // --- RESULT SET 2: TotalCount ---
                            if (await reader.NextResultAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    totalCount = (int)reader["TotalCount"];
                                }
                            }
                        }
                    }
                }

                return Json(new { status = true, data = pagedList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult IsQcParamDeletable(int docId)
        {
            var gv = _globalValue.GetGlobalVariables();
            bool isExists = false;
            string msg = "";
            try
            {
                //===========Check Qc Group existence in QC Master===========
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QualityParameterMast_AED", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "Del_CheckInQcMast1");
                        cmd.Parameters.AddWithValue("@Code", docId);
                        cmd.Parameters.AddWithValue("@companyCd", gv.PubCompCode);

                        con.Open();
                        object result = cmd.ExecuteScalar();

                        string qcParamName = result?.ToString();
                        isExists = string.IsNullOrEmpty(qcParamName) ? false : true;

                        msg = $"QC Parameter <b>{qcParamName}</b> exists in QC Master and cannot be deleted.";
                    }
                    return Json(new { success = true, message = msg, isExists = isExists });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DelQParamMast(int docId)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QualityParameterMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(docId));
                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.ReturnValue
                        };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        x = (int)cmd.Parameters["@ReturnVal"].Value;
                    }
                }
                if (x > 0)
                {
                    //===========log insert
                    _logService.InsertLog("QCP_MAST", "QC Parameter Master", "Master", "DELETE", "", docId.ToString(), null);
                    return Json(new { success = true, message = "Data delete successfully" });
                }
                return Json(new { success = false, message = "data delete failed" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "data delete failed" });
            }
        }

        public class QCprameterDto
        {
            public int? CODE { get; set; }
            public string? NAME { get; set; }
            public string? SHORTNAME { get; set; }
            public string? QUNIT { get; set; }
            public int? qty { get; set; }
            public int? ACTIVE { get; set; }
        }

        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalValue.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@companyCd", gv.PubCompCode },
                    { "@AED", "Excel" }
                };

                var fileBytes = _globalValidationdate.ExportToExcel("sp_QualityParameterMast_AED", "Qc Parameter Master", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"QcParameterMaster_{DateTime.Now:ddMMyyyy}.xlsx"
                );
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
