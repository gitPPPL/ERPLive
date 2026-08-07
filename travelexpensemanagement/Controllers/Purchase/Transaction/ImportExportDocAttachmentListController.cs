using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Ocsp;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportExportDocAttachmentListController : Controller
    {

        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;

        public ImportExportDocAttachmentListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportExportDocAttachmentList/Index.cshtml");
        }

        public JsonResult cmbPartyName()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"Select CODE,NAME from SUBGROUP_MAST where comp_code="  + getdata.PubCompCode + " and  Active=1 order by name ";
                var cmbPartyName = _dropdownService.GetDropdownList(query);
                return Json(cmbPartyName);
            }
        }

        public JsonResult cmbLocation()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"Select CODE,NAME from CITY_MAST where Active=1 order by name ";
                var cmbLocation = _dropdownService.GetDropdownList(query);
                return Json(cmbLocation);
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetViewData( DateTime FromDate, DateTime ToDate, string V_TYPE, int? partycode, int? Citycode)
        {
            var gv = _globalValue.GetGlobalVariables();
            var dataList = new List<object>();

            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_ImportExportDocAttachmentList", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 50).Value = "Viewdata";
                        cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = gv.PubFYearCode;
                        cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = gv.PubCompCode;
                        cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = gv.PubBranchCode;
                        cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = FromDate;
                        cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = ToDate;
                        cmd.Parameters.Add("@V_TYPE", SqlDbType.NVarChar, 10).Value = V_TYPE ?? "";
                        cmd.Parameters.Add("@partycode", SqlDbType.Int).Value =
                            partycode.HasValue && partycode.Value > 0
                                ? partycode.Value
                                : DBNull.Value;

                        cmd.Parameters.Add("@Citycode", SqlDbType.Int).Value = Citycode.HasValue && Citycode.Value > 0 ? Citycode.Value
                                : DBNull.Value;

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                dataList.Add(new
                                {
                                    SAUDA_NO = rdr["SAUDA_NO"]?.ToString(),
                                    V_NO = rdr["V_NO"]?.ToString(),
                                    Sauda_Date = rdr["Sauda_Date"]?.ToString(),
                                    EximDate = rdr["EximDate"]?.ToString(),
                                    PartyName = rdr["PartyName"]?.ToString(),
                                    BE_NO = rdr["BE_NO"]?.ToString(),
                                    City = rdr["City"]?.ToString(),
                                    PARTY_CODE = rdr["PARTY_CODE"]?.ToString(),

                                    // Import Columns
                                    PiCopy = HasColumn(rdr, "PiCopy") ? rdr["PiCopy"]?.ToString() : null,
                                    BlCopy = HasColumn(rdr, "BlCopy") ? rdr["BlCopy"]?.ToString() : null,
                                    BeCopy = HasColumn(rdr, "BeCopy") ? rdr["BeCopy"]?.ToString() : null,
                                    LcCopy = HasColumn(rdr, "LcCopy") ? rdr["LcCopy"]?.ToString() : null,
                                    InvCopy = HasColumn(rdr, "InvCopy") ? rdr["InvCopy"]?.ToString() : null,
                                    DpCopy = HasColumn(rdr, "DpCopy") ? rdr["DpCopy"]?.ToString() : null,
                                    SblcCopy = HasColumn(rdr, "SblcCopy") ? rdr["SblcCopy"]?.ToString() : null,

                                    // Export Columns
                                    SbCopy = HasColumn(rdr, "SbCopy") ? rdr["SbCopy"]?.ToString() : null,
                                    BrcCopy = HasColumn(rdr, "BrcCopy") ? rdr["BrcCopy"]?.ToString() : null,

                                    // Common Columns
                                    OthCopy1 = HasColumn(rdr, "OthCopy1") ? rdr["OthCopy1"]?.ToString() : null,
                                    OthCopy2 = HasColumn(rdr, "OthCopy2") ? rdr["OthCopy2"]?.ToString() : null,
                                    OthCopy3 = HasColumn(rdr, "OthCopy3") ? rdr["OthCopy3"]?.ToString() : null,
                                    OthCopy4 = HasColumn(rdr, "OthCopy4") ? rdr["OthCopy4"]?.ToString() : null,
                                    OthCopy5 = HasColumn(rdr, "OthCopy5") ? rdr["OthCopy5"]?.ToString() : null,
                                    OthCopy6 = HasColumn(rdr, "OthCopy6") ? rdr["OthCopy6"]?.ToString() : null,
                                    OthCopy7 = HasColumn(rdr, "OthCopy7") ? rdr["OthCopy7"]?.ToString() : null
                                });
                            }
                        }
                    }
                }

                return Ok(new  { success = true, data = dataList });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }         

        private static DateTime? SafeDate(SqlDataReader rdr, string col)
        {
            if (rdr[col] == DBNull.Value) return null;
            var raw = rdr[col].ToString();
            if (string.IsNullOrWhiteSpace(raw)) return null;
            return DateTime.TryParse(raw, out var dt) ? dt : (DateTime?)null;
        }

    }
}
