using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    public class DocumentTypeMasterListController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalVal;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public DocumentTypeMasterListController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalVal, ModuleService.ModuleService moduleService)
        {
            _dbcontext = dbcontext;
            _dbHelper = dbHelper;
            _globalVal = globalVal;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {

            ViewBag.CurrentMenu = "Document Type Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/SystemInitilization/DocumentTypeMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> getDocumentMaster()
        {
            try
            {
                var LanguagedtList = new List<object>();
                DataTable dt = new DataTable();
                var con = _dbcontext.GetErpConnection();
                string strqry = "select code, name as documentType,  DOCTYPE as transactionType, isnull(STOCK, '') as stock, SNO as sno, case when ACTIVE=1 then 'Yes' else 'No' end as active from DOCTYPE_MAST order by documentType ";
                dt = await _dbHelper.ExecuteQueryAsync(strqry);
                foreach (DataRow row in dt.Rows)
                {
                    LanguagedtList.Add(new
                    {
                        code = row["code"].ToString(),
                        documentType = row["documentType"].ToString(),
                        stock = row["stock"].ToString(),
                        sno = row["sno"].ToString(),
                        transactionType = row["transactionType"].ToString(),
                        active = row["active"].ToString()
                    });
                }

                return Json(new { status = true, data = LanguagedtList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "Daata Load failed" + ex.Message });
            }
        }

        [HttpDelete]
        public JsonResult DelDocumentMastDt(string code)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[DocumentTypeMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "D");
                        cmd.Parameters.AddWithValue("@doctype", code);
                        int x = cmd.ExecuteNonQuery();
                        if (x > 0)
                            return Json(new { status = true });
                        else
                            return Json(new { status = false });

                    }
                }
            }
            catch { return Json(new { status = false }); }

        }
        public IActionResult ExportAllDocs()
        {
            var currencyList = new List<DocumentTypeExportDto>();
            try
            {
                using (SqlConnection conn = _dbcontext.GetErpConnection())
                {
                    string query = @"select SNO, code, name as documentType, DOCTYPE as transactionType, isnull(STOCK, '') as stock, case when ACTIVE=1 then 'Yes' else 'No' end as active 
                from DOCTYPE_MAST order by documentType";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                currencyList.Add(new DocumentTypeExportDto
                                {
                                    SNO = reader["SNO"]?.ToString(),
                                    Code = reader["code"]?.ToString(),
                                    Name = reader["documentType"]?.ToString(),
                                    ShortName = reader["transactionType"]?.ToString(), // Using DOCTYPE alias as ShortName here, adjust if needed
                                    CurrCode = reader["stock"]?.ToString(),            // Using STOCK alias as CurrCode here, adjust if needed
                                    Active = (reader["active"]?.ToString() == "Yes")
                                });
                            }
                        }
                    }
                }
                return Json(currencyList);
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
            var docDetails = new List<DocDetailDto>();

            try
            {
                using (SqlConnection conn = _dbcontext.GetErpConnection())
                {
                    string query = @"SELECT DISTINCT da.CODE AS DOC_CODE,um.USER_NAME AS UUser,da.UDATE, ume.USER_NAME AS EUSER, da.EDATE, da.WSID, da.LIP, da.LID FROM DOCTYPE_MAST da
                    LEFT JOIN CONDATABASE..USER_MAST um ON da.UUSER = um.CODE LEFT JOIN CONDATABASE..USER_MAST ume ON da.EUSER = ume.CODE WHERE da.CODE = @Code";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", docCode);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                docDetails.Add(new DocDetailDto
                                {
                                    DOC_CODE = reader["DOC_CODE"]?.ToString(),
                                    UUser = reader["UUser"]?.ToString(),
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : null,
                                    EUSER = reader["EUSER"]?.ToString(),
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : null,
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString()
                                });
                            }
                        }
                    }
                }
                return Json(new { success = true, data = docDetails });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error while fetching document details.",
                    error = ex.Message
                });
            }
        }

    }
    public class DocumentTypeExportDto
    {
        public string SNO { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }  
        public string CurrCode { get; set; }  
        public bool Active { get; set; }
    }

    public class DocDetailDto
    {
        public string DOC_CODE { get; set; }
        public string UUser { get; set; }
        public DateTime? UDATE { get; set; }
        public string EUSER { get; set; }
        public DateTime? EDATE { get; set; }
        public string WSID { get; set; }
        public string LIP { get; set; }
        public string LID { get; set; }
    }

}
