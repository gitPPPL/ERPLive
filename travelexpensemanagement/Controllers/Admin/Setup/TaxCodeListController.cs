using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class TaxCodeListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public TaxCodeListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, travelexpensemanagement.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Tax Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); 

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/TaxCodeList/Index.cshtml", model);
        }
        [HttpPost]
        public IActionResult GetTaxCodeList([FromBody] TaxCodeRequest request)
        {
            var taxCodeList = new List<object>();
            int totalCount = 0;
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_InsertTaxMastDetails", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Main parameters
                    cmd.Parameters.AddWithValue("@PageNumber", request.PageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", request.PageSize);
                    cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(request.NAME) ? (object)DBNull.Value : request.NAME);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // First result: paginated data
                        while (reader.Read())
                        {
                            taxCodeList.Add(new
                            {
                                srno = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0,
                                name = reader["NAME"]?.ToString(),
                                taX_DESCRIPTION = reader["TAX_DESCRIPTION"]?.ToString(),
                                taX_TYPE = reader["TAX_TYPE"]?.ToString(),
                                cgsT_PER = reader["CGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_PER"]) : (decimal?)null,
                                sgsT_PER = reader["SGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_PER"]) : (decimal?)null,
                                igsT_PER = reader["IGST_PER"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_PER"]) : (decimal?)null,
                                tdS_PER = reader["TDS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["TDS_PER"]) : (decimal?)null,
                                tcS_PER = reader["TCS_PER"] != DBNull.Value ? Convert.ToDecimal(reader["TCS_PER"]) : (decimal?)null,
                                otH_PER = reader["OTH_PER"] != DBNull.Value ? Convert.ToDecimal(reader["OTH_PER"]) : (decimal?)null
                            });
                        }
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalRecords"] != DBNull.Value ? Convert.ToInt32(reader["TotalRecords"]) : 0;
                        }
                    }
                }

                return Json(new { success = true, data = taxCodeList, totalRecords = totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult ExportAllDocs()
        {
            var TaxCodeList = new List<TaxCodeListExportDto>();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = "Select Code, Name, TAX_DESCRIPTION, TAX_TYPE,CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, OTH_PER From TAX_MAST";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TaxCodeList.Add(new TaxCodeListExportDto
                                {
                                    Code = reader["Code"]?.ToString(),
                                    Name = reader["Name"]?.ToString(),
                                    TaxDescription = reader["TAX_DESCRIPTION"]?.ToString(),
                                    TaxType = reader["TAX_TYPE"]?.ToString(),
                                    CGST = reader["CGST_PER"]?.ToString(),
                                    SGST = reader["SGST_PER"]?.ToString(),
                                    IGST = reader["IGST_PER"]?.ToString(),
                                    TDS = reader["TDS_PER"]?.ToString(),
                                    TCS = reader["TCS_PER"]?.ToString(),
                                    OTH = reader["OTH_PER"]?.ToString()
                                });
                            }
                        }
                    }
                }
                return Json(TaxCodeList);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while exporting currency data.",
                    error = ex.Message
                });
            }
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            List<DocDetailDto> docDetails = new List<DocDetailDto>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT DISTINCT da.Code, um.USER_NAME as UUser, da.UDATE, ume.USER_NAME as EUSER, da.EDATE, 
          da.WSID, da.LIP, da.LID FROM TAX_MAST da
          LEFT JOIN CONDATABASE..USER_MAST um ON da.UUSER = um.CODE
          LEFT JOIN CONDATABASE..USER_MAST ume ON da.EUSER = ume.CODE
          WHERE da.Code = @Code";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Code", docCode);
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var detail = new DocDetailDto
                            {
                                DOC_CODE = reader["Code"]?.ToString(),
                                UUser = reader["UUser"]?.ToString(),
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                EUSER = reader["EUSER"]?.ToString(),
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString()
                            };
                            docDetails.Add(detail);
                        }
                    }
                }
            }

            return Json(new { success = true, data = docDetails });
        }

    }
}
