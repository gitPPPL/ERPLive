using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class TapeAndFabricMasterListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly ModuleService.ModuleService _moduleService;
        private readonly LogService.LogService _logService;

        public TapeAndFabricMasterListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, GlobalValidationdate globalValidationdate, 
            ModuleService.ModuleService moduleService, LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _globalValidationdate = globalValidationdate;
            _moduleService = moduleService;
            _logService = logService;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Tape And Fabric Master";
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
            return View("~/Views/QualityControl/Master/TapeAndFabricMasterList/Index.cshtml", model);
        }
        [HttpGet]
        public async Task<IActionResult> GetTape_FabricList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                
                var pagedList = new List<QCStandardMasterDto>();
                int totalCount = 0;

                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TapeNFabricMast_AED", con))
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
                                pagedList.Add(new QCStandardMasterDto
                                {
                                    CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : null,
                                    NAME = reader["NAME"]?.ToString(),
                                    MESH_NAME = reader["MESH_NAME"]?.ToString(),

                                    STD_GRAM = reader["STD_GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["STD_GRAM"]) : null,
                                    MIN_GRAM = reader["MIN_GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["MIN_GRAM"]) : null,
                                    MAX_GRAM = reader["MAX_GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["MAX_GRAM"]) : null,

                                    GSM = reader["GSM"] != DBNull.Value ? Convert.ToDecimal(reader["GSM"]) : null,
                                    DENIER = reader["DENIER"] != DBNull.Value ? Convert.ToDecimal(reader["DENIER"]) : null,

                                    UNIT_NAME = reader["UNIT_NAME"]?.ToString(),
                                    COLOR_NAME = reader["COLOR_NAME"]?.ToString(),

                                    WIDTH = reader["WIDTH"] != DBNull.Value ? Convert.ToDecimal(reader["WIDTH"]) : null,

                                    GPD = reader["GPD"] != DBNull.Value ? Convert.ToDecimal(reader["GPD"]) : null,
                                    MIN_GPD = reader["MIN_GPD"] != DBNull.Value ? Convert.ToDecimal(reader["MIN_GPD"]) : null,
                                    MAX_GPD = reader["MAX_GPD"] != DBNull.Value ? Convert.ToDecimal(reader["MAX_GPD"]) : null,

                                    STD_STRENGTH = reader["STD_STRENGTH"] != DBNull.Value ? Convert.ToDecimal(reader["STD_STRENGTH"]) : null,
                                    STRENGTH_MAX = reader["STRENGTH_MAX"] != DBNull.Value ? Convert.ToDecimal(reader["STRENGTH_MAX"]) : null,
                                    STRENGTH_MIN = reader["STRENGTH_MIN"] != DBNull.Value ? Convert.ToDecimal(reader["STRENGTH_MIN"]) : null,

                                    STD_ELONG = reader["STD_ELONG"] != DBNull.Value ? Convert.ToDecimal(reader["STD_ELONG"]) : null,
                                    ELONG_MAX = reader["ELONG_MAX"] != DBNull.Value ? Convert.ToDecimal(reader["ELONG_MAX"]) : null,
                                    ELONG_MIN = reader["ELONG_MIN"] != DBNull.Value ? Convert.ToDecimal(reader["ELONG_MIN"]) : null,

                                    UNLAM_FAB = reader["UNLAM_FAB"] != DBNull.Value ? Convert.ToDecimal(reader["UNLAM_FAB"]) : null,
                                    LAM_FAB = reader["LAM_FAB"] != DBNull.Value ? Convert.ToDecimal(reader["LAM_FAB"]) : null,

                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : null
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

        [HttpPost]
        public async Task<IActionResult> DelTape_FabricMast(int docId)
        {
            try
            {
                int x;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TapeNFabricMast_AED]", con))
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
                    _logService.InsertLog("TAPE_NFABRIC_MAST", "Tape And Fabric Master", "Master", "Delete", "", docId.ToString(), null);
                    return Json(new { success = true, message = "Data delete successfully" });
                }
                return Json(new { success = false, message = "data delete failed" });

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "data delete failed" });
            }
        }
        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalValue.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@CompanyCd", gv.PubCompCode },
                    { "@AED", "Excel" }
                };

                var fileBytes = _globalValidationdate.ExportToExcel("sp_TapeNFabricMast_AED", "QC Tape And Fabric Master", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"QCTapeAndFabricMaster_{DateTime.Now:ddMMyyyy}.xlsx"
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

        public class QCStandardMasterDto
        {
            public int? CODE { get; set; }
            public string? NAME { get; set; }
            public string? MESH_NAME { get; set; }

            public decimal? STD_GRAM { get; set; }
            public decimal? MIN_GRAM { get; set; }
            public decimal? MAX_GRAM { get; set; }

            public decimal? GSM { get; set; }
            public decimal? DENIER { get; set; }

            public string? UNIT_NAME { get; set; }
            public string? COLOR_NAME { get; set; }

            public decimal? WIDTH { get; set; }

            public decimal? GPD { get; set; }
            public decimal? MIN_GPD { get; set; }
            public decimal? MAX_GPD { get; set; }

            public decimal? STD_STRENGTH { get; set; }
            public decimal? STRENGTH_MAX { get; set; }
            public decimal? STRENGTH_MIN { get; set; }

            public decimal? STD_ELONG { get; set; }
            public decimal? ELONG_MAX { get; set; }
            public decimal? ELONG_MIN { get; set; }

            public decimal? UNLAM_FAB { get; set; }
            public decimal? LAM_FAB { get; set; }

            public int? ACTIVE { get; set; }
        }
    }
}
