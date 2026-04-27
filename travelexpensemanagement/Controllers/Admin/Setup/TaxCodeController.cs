using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class TaxCodeController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        public TaxCodeController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            //return View();
            return View("~/Views/Admin/Setup/TaxCode/Index.cshtml");
        }
        //[HttpPost]
        //public async Task<IActionResult> SaveTaxCode([FromBody] TaxMastViewModel model)
        //{
        //    var sessionData = _globalVariableService.GetGlobalVariables();
        //    try
        //    {
        //        var parameters = new List<SqlParameter>
        //    {
        //        new SqlParameter("@NAME", model.NAME ?? (object)DBNull.Value),
        //        new SqlParameter("@TAX_DESCRIPTION", model.TAX_DESCRIPTION ?? (object)DBNull.Value),
        //        new SqlParameter("@TAX_TYPE", model.TAX_TYPE ?? (object)DBNull.Value),
        //        new SqlParameter("@CGST_PER", model.CGST_PER ?? (object)DBNull.Value),
        //        new SqlParameter("@SGST_PER", model.SGST_PER ?? (object)DBNull.Value),
        //        new SqlParameter("@IGST_PER", model.IGST_PER ?? (object)DBNull.Value),
        //        new SqlParameter("@VAT_PER", model.VAT_PER ?? (object)DBNull.Value),
        //        new SqlParameter("@TDS_PER", model.TDS_PER ?? (object)DBNull.Value),
        //        new SqlParameter("@TCS_PER", model.TCS_PER ?? (object)DBNull.Value),
        //        new SqlParameter("@OTH_PER", model.OTH_PER ?? (object)DBNull.Value),
        //        new SqlParameter("@OTH_PER2", model.OTH_PER2 ?? (object)DBNull.Value),
        //        new SqlParameter("@PACK_ONBASIC", model.PACK_ONBASIC ?? (object)DBNull.Value),
        //        new SqlParameter("@ACTIVE", model.ACTIVE ?? (object)DBNull.Value),
        //        // Audit fields
        //        new SqlParameter("@UUSER", sessionData.PubUserId),
        //        new SqlParameter("@UDATE", DateTime.Now),
        //        new SqlParameter("@WSID", sessionData.PubWorkStationID),
        //        new SqlParameter("@LIP", sessionData.PubLocalId),
        //        new SqlParameter("@LID", Environment.UserName)
        //    };

        //        await _dbHelper.GetDataTableFromStoredProcedureAsync("sp_InsertTaxMast", parameters);

        //        return Json(new { success = true, message = "Data inserted successfully via stored procedure" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SaveTaxCode([FromBody] TaxMastViewModel model)
        {
            var sessionData = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string checkQuery = "SELECT COUNT(1) FROM TAX_MAST WHERE NAME = @NAME";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(model.NAME) ? (object)DBNull.Value : model.NAME);
                        await con.OpenAsync();
                        int existingRecordCount = (int)await checkCmd.ExecuteScalarAsync();
                        if (existingRecordCount > 0)
                        {
                            return Json(new { success = false, message = "This tax code already exists." });
                        }
                    }
                    var parameters = new List<SqlParameter>
                    {
                         new SqlParameter("@NAME", model.NAME ?? (object)DBNull.Value),
                        new SqlParameter("@TAX_DESCRIPTION", model.TAX_DESCRIPTION ?? (object)DBNull.Value),
                        new SqlParameter("@TAX_TYPE", model.TAX_TYPE ?? (object)DBNull.Value),
                        new SqlParameter("@CGST_PER", model.CGST_PER ?? (object)DBNull.Value),
                        new SqlParameter("@SGST_PER", model.SGST_PER ?? (object)DBNull.Value),
                        new SqlParameter("@IGST_PER", model.IGST_PER ?? (object)DBNull.Value),
                        new SqlParameter("@VAT_PER", model.VAT_PER ?? (object)DBNull.Value),
                        new SqlParameter("@TDS_PER", model.TDS_PER ?? (object)DBNull.Value),
                        new SqlParameter("@TCS_PER", model.TCS_PER ?? (object)DBNull.Value),
                        new SqlParameter("@OTH_PER", model.OTH_PER ?? (object)DBNull.Value),
                        new SqlParameter("@OTH_PER2", model.OTH_PER2 ?? (object)DBNull.Value),
                        new SqlParameter("@PACK_ONBASIC", model.PACK_ONBASIC ?? (object)DBNull.Value),
                        new SqlParameter("@ACTIVE", model.ACTIVE ?? (object)DBNull.Value),
                        // Audit fields
                        new SqlParameter("@UUSER", sessionData.PubUserId),
                        new SqlParameter("@UDATE", DateTime.Now),
                        new SqlParameter("@WSID", sessionData.PubWorkStationID),
                        new SqlParameter("@LIP", sessionData.PubLocalId),
                        new SqlParameter("@LID", Environment.UserName)
                    };
                    await _dbHelper.GetDataTableFromStoredProcedureAsync("sp_InsertTaxMast", parameters);
                    return Json(new { success = true, message = "Data inserted successfully via stored procedure" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateTaxCode([FromBody] TaxMastViewModel model)
        {
            var sessionData = _globalVariableService.GetGlobalVariables();
            try
            {
                var parameters = new List<SqlParameter>
        {
            new SqlParameter("@CODE", model.SRNO),
            new SqlParameter("@NAME", model.NAME ?? (object)DBNull.Value),
            new SqlParameter("@TAX_DESCRIPTION", model.TAX_DESCRIPTION ?? (object)DBNull.Value),
            new SqlParameter("@TAX_TYPE", model.TAX_TYPE ?? (object)DBNull.Value),
            new SqlParameter("@CGST_PER", model.CGST_PER ?? (object)DBNull.Value),
            new SqlParameter("@SGST_PER", model.SGST_PER ?? (object)DBNull.Value),
            new SqlParameter("@IGST_PER", model.IGST_PER ?? (object)DBNull.Value),
            new SqlParameter("@VAT_PER", model.VAT_PER ?? (object)DBNull.Value),
            new SqlParameter("@TDS_PER", model.TDS_PER ?? (object)DBNull.Value),
            new SqlParameter("@TCS_PER", model.TCS_PER ?? (object)DBNull.Value),
            new SqlParameter("@OTH_PER", model.OTH_PER ?? (object)DBNull.Value),
            new SqlParameter("@OTH_PER2", model.OTH_PER2 ?? (object)DBNull.Value),
            new SqlParameter("@PACK_ONBASIC", model.PACK_ONBASIC ?? (object)DBNull.Value),
            new SqlParameter("@ACTIVE", model.ACTIVE ?? (object)DBNull.Value),
            // Audit fields
            new SqlParameter("@UUSER", sessionData.PubUserId),
            new SqlParameter("@UDATE", DateTime.Now),
            new SqlParameter("@WSID", sessionData.PubWorkStationID),
            new SqlParameter("@LIP", sessionData.PubLocalId),
            new SqlParameter("@LID", Environment.UserName)
        };
                await _dbHelper.GetDataTableFromStoredProcedureAsync("sp_UpdateTaxMast", parameters);
                return Json(new { success = true, message = "Tax code updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetTaxCodeBySrno(int srno)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string sql = @"
                SELECT code, NAME, TAX_DESCRIPTION, TAX_TYPE, CGST_PER, SGST_PER, IGST_PER, VAT_PER, 
                       TDS_PER, TCS_PER, OTH_PER, OTH_PER2, PACK_ONBASIC, ACTIVE 
                FROM TAX_MAST 
                WHERE code = @code";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@code", srno);

                        con.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var taxCode = new TaxMastViewModel
                                {
                                    SRNO = reader.GetInt32(reader.GetOrdinal("code")),
                                    NAME = reader["NAME"].ToString(),
                                    TAX_DESCRIPTION = reader["TAX_DESCRIPTION"].ToString(),
                                    TAX_TYPE = reader["TAX_TYPE"].ToString(),
                                    CGST_PER = reader["CGST_PER"] as decimal?,
                                    SGST_PER = reader["SGST_PER"] as decimal?,
                                    IGST_PER = reader["IGST_PER"] as decimal?,
                                    VAT_PER = reader["VAT_PER"] as decimal?,
                                    TDS_PER = reader["TDS_PER"] as decimal?,
                                    TCS_PER = reader["TCS_PER"] as decimal?,
                                    OTH_PER = reader["OTH_PER"] as decimal?,
                                    OTH_PER2 = reader["OTH_PER2"] as decimal?,
                                    PACK_ONBASIC = reader["PACK_ONBASIC"] as int?,
                                    ACTIVE = reader["ACTIVE"] as int?
                                };

                                return Json(new { success = true, data = taxCode });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Tax code not found." });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception (for example, to a file or logging service)
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
    }

}

