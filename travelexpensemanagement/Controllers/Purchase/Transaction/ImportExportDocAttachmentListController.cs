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
                        cmd.Parameters.Add("@partycode", SqlDbType.Int).Value =  partycode.HasValue && partycode.Value > 0 ? partycode.Value : DBNull.Value;
                        cmd.Parameters.Add("@Citycode", SqlDbType.Int).Value = Citycode.HasValue && Citycode.Value > 0 ? Citycode.Value : DBNull.Value;

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
                                    OthCopy7 = HasColumn(rdr, "OthCopy7") ? rdr["OthCopy7"]?.ToString() : null,

                                    PiCopyFILE_Path = HasColumn(rdr, "PiCopyFILE_Path") ? rdr["PiCopyFILE_Path"]?.ToString() : null,
                                    BlCopyFILE_Path = HasColumn(rdr, "BlCopyFILE_Path") ? rdr["BlCopyFILE_Path"]?.ToString() : null,
                                    BeCopyFILE_Path = HasColumn(rdr, "BeCopyFILE_Path") ? rdr["BeCopyFILE_Path"]?.ToString() : null,
                                    LcCopyFILE_Path = HasColumn(rdr, "LcCopyFILE_Path") ? rdr["LcCopyFILE_Path"]?.ToString() : null,
                                    InvCopyFILE_Path = HasColumn(rdr, "InvCopyFILE_Path") ? rdr["InvCopyFILE_Path"]?.ToString() : null,
                                    DpCopyFILE_Path = HasColumn(rdr, "DpCopyFILE_Path") ? rdr["DpCopyFILE_Path"]?.ToString() : null,
                                    SblcCopyFILE_Path = HasColumn(rdr, "SblcCopyFILE_Path") ? rdr["SblcCopyFILE_Path"]?.ToString() : null,
                                    OthCopy1FILE_Path = HasColumn(rdr, "OthCopy1FILE_Path") ? rdr["OthCopy1FILE_Path"]?.ToString() : null,
                                    OthCopy2FILE_Path = HasColumn(rdr, "OthCopy2FILE_Path") ? rdr["OthCopy2FILE_Path"]?.ToString() : null,           
                                    OthCopy3FILE_Path = HasColumn(rdr, "OthCopy3FILE_Path") ? rdr["OthCopy3FILE_Path"]?.ToString() : null,
                                    SbCopyFILE_Path = HasColumn(rdr, "SbCopyFILE_Path") ? rdr["SbCopyFILE_Path"]?.ToString() : null,                     
                                    BrcCopyFILE_Path = HasColumn(rdr, "BrcCopyFILE_Path") ? rdr["BrcCopyFILE_Path"]?.ToString() : null,
                                    OthCopy4FILE_Path = HasColumn(rdr, "OthCopy4FILE_Path") ? rdr["OthCopy4FILE_Path"]?.ToString() : null,
                                    OthCopy5FILE_Path = HasColumn(rdr, "OthCopy5FILE_Path") ? rdr["OthCopy5FILE_Path"]?.ToString() : null,
                                    OthCopy6FILE_Path = HasColumn(rdr, "OthCopy6FILE_Path") ? rdr["OthCopy6FILE_Path"]?.ToString() : null,
                                    OthCopy7FILE_Path = HasColumn(rdr, "OthCopy7FILE_Path") ? rdr["OthCopy7FILE_Path"]?.ToString() : null

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



        public class AttachmentRequest
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
        }


        [HttpPost]
        public IActionResult DownloadAttachments( [FromBody] List<AttachmentRequest> attachments)
        {
            if (attachments == null || attachments.Count == 0)
                return BadRequest("No attachments selected.");

            using var memoryStream = new MemoryStream();

            using (var archive = new System.IO.Compression.ZipArchive( memoryStream, System.IO.Compression.ZipArchiveMode.Create,  true))
            {
                foreach (var attachment in attachments)
                {
                    if (string.IsNullOrWhiteSpace(attachment.FilePath))
                        continue;

                    string fullPath = Path.Combine(  Directory.GetCurrentDirectory(), "wwwroot",  attachment.FilePath.TrimStart('/', '\\'));

                    if (!System.IO.File.Exists(fullPath))
                        continue;

                    string fileName = Path.GetFileName(attachment.FileName);

                    if (string.IsNullOrWhiteSpace(fileName))
                        fileName = Path.GetFileName(fullPath);

                    var entry = archive.CreateEntry( fileName,  System.IO.Compression.CompressionLevel.Fastest);

                    using var entryStream = entry.Open();
                    using var fileStream = System.IO.File.OpenRead(fullPath);

                    fileStream.CopyTo(entryStream);
                }
            }

            memoryStream.Position = 0;

            return File( memoryStream.ToArray(),  "application/zip", "Attachments.zip");
        }




    }
}
