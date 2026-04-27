using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.AddAttachmentService;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Gate_Entry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VisitorEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public VisitorEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
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
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/GateEntry/Transaction/VisitorEntry/Index.cshtml");
        }
        [HttpGet]
        public int GetNextV_NO(string yearCode)
        {
            string newV_NO = "00000";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // Execute query to get PREFIXYR
                string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = '" + yearCode + "'";
                SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                // Execute query to get last V_NO
                string lastV_NO_Query = "SELECT TOP 1 V_NO FROM VISITOR ORDER BY V_NO DESC";
                SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                string lastV_NO = lastVnoCmd.ExecuteScalar()?.ToString();

                int lastNumber = 0;
                if (!string.IsNullOrEmpty(lastV_NO) && lastV_NO.Length >= 9)
                {
                    string numericPart = lastV_NO.Substring(lastV_NO.Length - 5);
                    int.TryParse(numericPart, out lastNumber);
                }

                // Increment and format the new V_NO
                string newRunningNo = (lastNumber + 1).ToString("D5");
                newV_NO = prefixYR + newRunningNo;
            }

            return Convert.ToInt32(newV_NO);
        }

        public IActionResult GetEmpList()
        {
            string query = "SELECT CODE,NAME FROM EMP_MAST WHERE ACTIVE=1 AND NAME<>'' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpPost]
        public async Task<IActionResult> SaveVisitorEntry([FromBody] VisitorWrapper data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            int vNo = GetNextV_NO(globalVar.PubFYearCode);
            var model = data.Visitor;

            string action = "";
            //string action = IsDuplicateVisitorEntry(vNo, Convert.ToInt32(globalVar.PubCompCode), Convert.ToInt32(globalVar.PubFYearCode)) ? "UPDATE" : "INSERT";
            if (IsDuplicateVisitorEntry(data.Visitor.V_NO, Convert.ToInt32(globalVar.PubCompCode), Convert.ToInt32(globalVar.PubFYearCode)))
            {
                action = "UPDATE";
            }
            else
            {
                action = "INSERT";
            }

            try
            {
                if (data.Image != null && !string.IsNullOrEmpty(data.Image.Base64Content))
                {
                    try
                    {
                        string base64 = data.Image.Base64Content;

                        // ✅ Remove data:image/...;base64, prefix if present
                        var base64Parts = base64.Split(',');
                        if (base64Parts.Length == 2)
                            base64 = base64Parts[1];

                        model.IMG_FILE = Convert.FromBase64String(base64);
                        model.FILE_NAME = $"{model.V_NO}_{data.Image.FileName}";
                    }
                    catch (FormatException)
                    {
                        model.IMG_FILE = null;
                        model.FILE_NAME = string.Empty;
                    }
                }
                else if (action == "UPDATE")
                {
                    // 🔒 Preserve existing image if not changed
                    using (SqlConnection con = _dbConnection.GetErpConnection())
                    {
                        await con.OpenAsync();
                        using (SqlCommand cmd = new SqlCommand("SELECT IMG_FILE, FILE_NAME FROM VISITOR WHERE V_NO = @vNo AND COMP_CODE = @cCode AND YEAR_CODE = @yCode AND BRANCH_CODE = @bCode", con))
                        {
                            cmd.Parameters.AddWithValue("@vNo", model.V_NO);
                            cmd.Parameters.AddWithValue("@cCode", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@yCode", globalVar.PubFYearCode);
                            cmd.Parameters.AddWithValue("@bCode", 1);

                            using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow))
                            {
                                if (await reader.ReadAsync())
                                {
                                    model.IMG_FILE = reader["IMG_FILE"] != DBNull.Value ? (byte[])reader["IMG_FILE"] : null;
                                    model.FILE_NAME = reader["FILE_NAME"]?.ToString() ?? string.Empty;
                                }
                            }
                        }
                    }
                }
                else
                {
                    model.IMG_FILE = null;
                    model.FILE_NAME = string.Empty;
                }

                // Fill common fields
                model.V_TYPE = "VISI";
                model.V_DATE = DateTime.Now;
                model.YEAR_CODE = Convert.ToInt32(globalVar.PubFYearCode);
                model.COMP_CODE = Convert.ToInt32(globalVar.PubCompCode);
                model.BRANCH_CODE = 1;
                model.DOC_ID = model.V_TYPE + model.V_NO;

                // Audit fields
                model.UUSER = Convert.ToInt32(globalVar.PubUserId);
                model.UDATE = DateTime.Now;
                model.EUSER = Convert.ToInt32(globalVar.PubUserId);
                model.EDATE = DateTime.Now;
                model.AED = model.AED ?? "A";
                model.WSID = globalVar.PubWorkStationID ?? "WEB";
                model.LIP = globalVar.PubLocalId ?? "127.0.0.1";
                model.LID = Environment.MachineName;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Stored Procedure Params
                        cmd.Parameters.AddWithValue("@Action", action);
                        //cmd.Parameters.AddWithValue("@SubAction", subAction);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", model.YEAR_CODE);
                        cmd.Parameters.AddWithValue("@COMP_CODE", model.COMP_CODE);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", model.BRANCH_CODE);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? DateTime.Now);
                        cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID ?? "");
                        cmd.Parameters.AddWithValue("@SLIP_NO", model.SLIP_NO ?? "");
                        cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
                        cmd.Parameters.AddWithValue("@ORGANIZATION", model.ORGANIZATION ?? "");
                        cmd.Parameters.AddWithValue("@ADDRESS", model.ADDRESS ?? "");
                        cmd.Parameters.AddWithValue("@MEET_CODE", model.MEET_CODE ?? 0);
                        cmd.Parameters.AddWithValue("@MEET_NAME", model.MEET_NAME ?? "");
                        cmd.Parameters.AddWithValue("@IN_TIME", model.IN_TIME ?? "");
                        cmd.Parameters.AddWithValue("@OUT_DATE", model.OUT_DATE);
                        cmd.Parameters.AddWithValue("@OUT_TIME", model.OUT_TIME ?? "");
                        cmd.Parameters.AddWithValue("@PURPOSE", model.PURPOSE ?? "");
                        cmd.Parameters.AddWithValue("@MOBILE_NO", model.MOBILE_NO ?? "");
                        cmd.Parameters.AddWithValue("@VEHICLE_NO", model.VEHICLE_NO ?? "");
                        cmd.Parameters.AddWithValue("@MATERIAL", model.MATERIAL ?? "");
                        cmd.Parameters.AddWithValue("@CARD_NO", model.CARD_NO ?? "");
                        cmd.Parameters.AddWithValue("@CARD_CODE", model.CARD_CODE ?? 0);
                        cmd.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary, -1)
                           .Value = model.IMG_FILE ?? (object)DBNull.Value;

                        cmd.Parameters.AddWithValue("@FILE_NAME", model.FILE_NAME ?? "");
                        cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? "");

                        // Audit Fields
                        cmd.Parameters.AddWithValue("@UUSER", model.UUSER ?? 0);
                        cmd.Parameters.AddWithValue("@UDATE", model.UDATE ?? DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", model.EUSER ?? 0);
                        cmd.Parameters.AddWithValue("@EDATE", model.EDATE ?? DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", model.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", model.WSID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", model.LIP ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", model.LID ?? Environment.MachineName);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Visitor entry saved." });
            }
            catch (SqlException sqlEx)
            {
                return Json(new { success = false, message = "SQL Error: " + sqlEx.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private bool IsDuplicateVisitorEntry(int? vNo, int? cCode, int? yCode)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM VISITOR WHERE V_NO = @vNo AND COMP_CODE = @cCode AND YEAR_CODE = @yCode", con))
                {
                    cmd.Parameters.AddWithValue("@vno", vNo);
                    cmd.Parameters.AddWithValue("@cCode", cCode);
                    cmd.Parameters.AddWithValue("@yCode", yCode);

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }


        [HttpPost]
        public JsonResult DeleteVisitorEntryByCode(int code, int compCode, int branchCode, int yearCode)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@V_TYPE", "VISI");
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", branchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", yearCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Quotation deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
 