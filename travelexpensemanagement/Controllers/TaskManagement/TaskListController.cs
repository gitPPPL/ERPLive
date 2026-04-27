
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.TODO;

namespace travelexpensemanagement.Controllers.TodoList
{
    public class TaskListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public TaskListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/TaskManagement/TaskList/Index.cshtml");
        }

        public JsonResult DDLDropdown()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //string preIP = "";
                //if (getdata.PubCompCode == "7" || getdata.PubCompCode == "5")
                //{
                //    preIP = "[192.168.1.214].";
                //}

                string query = "select distinct a.code,upper(a.Full_name)Full_name from  CONDATABASE.dbo.User_mast a " +
                    "Left join Subuser_Mast b on a.code=b.User_code where a.Active=1 and b.Comp_code=" + getdata.PubCompCode + " Order by Full_name ";


                var DDLDropdown = _dropdownService.GetDropdownList(query);
                return Json(DDLDropdown);
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetTaskListData(string TASK)
        {
            try
            {
                var userSession = _globalVariableService.GetGlobalVariables();
                DateTime loginDate = Convert.ToDateTime(userSession.PubLoginDate);
                DateTime yesterday = loginDate.AddDays(-1);

                var parameters = new Dictionary<string, object>
                {
                    { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                    { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                    { "@BRANCH_CODE", userSession.PubBranchCode },
                    { "@UUSER", userSession.PubUserId },
                    { "@V_DATE", loginDate },
                    { "@YesterDay", yesterday },
                    { "@Action", "TASKLIST" },
                    { "@SUBAction", TASK }
                };

                var result = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TodoList]", parameters);  
                return Json(new  { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new
                {  status = false,  message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShowData(  DateOnly? FromDate, DateOnly? ToDate, int? UOM_CODE,
           int? DEPT_CODE, int? FROM_DEPT, int? USERCODE, string? Status)
        {
            try
            {
                var userSession = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                    { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                    { "@BRANCH_CODE", userSession.PubBranchCode },
                    { "@FromDate", FromDate.HasValue ? FromDate.Value : DBNull.Value },
                    { "@ToDate", ToDate.HasValue ? ToDate.Value : DBNull.Value },
                    { "@USER_CODE", USERCODE.HasValue && USERCODE != 0 ? USERCODE.Value : DBNull.Value },
                    { "@UOM_CODE", UOM_CODE.HasValue && UOM_CODE != 0 ? UOM_CODE.Value : DBNull.Value },
                    { "@DEPT_CODE", DEPT_CODE.HasValue && DEPT_CODE != 0 ? DEPT_CODE.Value : DBNull.Value },
                    { "@FROM_DEPT", FROM_DEPT.HasValue && FROM_DEPT != 0 ? FROM_DEPT.Value : DBNull.Value },
                    { "@UUSER", userSession.PubUserId },
                    { "@Action", "SHOWDATA" },
                    { "@Status", string.IsNullOrWhiteSpace(Status) ? DBNull.Value : Status }
                };

                var result = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_TodoList]", parameters);

                return Json(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReportWithReplyTaskExcel(  string FromDate, string ToDate, int? UOM_CODE,   int? DEPT_CODE,  int? FROM_DEPT,   int? USERCODE, string? Status , string? ReportAction)
        {
            try
            {
                var userSession = _globalVariableService.GetGlobalVariables();
                DataTable dt = new DataTable();

                // ================== GET DATA ==================
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TodoList]", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", int.Parse(userSession.PubCompCode));
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", int.Parse(userSession.PubFYearCode));
                    cmd.Parameters.AddWithValue("@FromDate", FromDate);
                    cmd.Parameters.AddWithValue("@ToDate", ToDate);
                    cmd.Parameters.AddWithValue("@USER_CODE", (object?)USERCODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UOM_CODE", (object?)UOM_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DEPT_CODE", (object?)DEPT_CODE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FROM_DEPT", (object?)FROM_DEPT ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", userSession.PubUserId);
                    cmd.Parameters.AddWithValue("@Status", (object?)Status ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Action", ReportAction);
                    con.Open();
                    new SqlDataAdapter(cmd).Fill(dt);

                    if(dt.Rows.Count == 0 )
                    {        
                        return Ok(new
                        {
                            status = false,
                            message = "No Data Found For This Condition"                          
                        });
                    }
                }

                // ================== FILE PATH ==================
                var saveFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "TaskReport");
                if (!Directory.Exists(saveFolder))
                    Directory.CreateDirectory(saveFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = $"WithReplyTaskReport_{timestamp}";
                var fullFilePath = Path.Combine(saveFolder, $"{fileName}.xlsx");

                // ================== EXCEL ==================
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Todo Report");
                    int colCount = dt.Columns.Count;

                    // ----- Title -----
                    ws.Cell(1, 1).Value = "TASK REPORT";
                    ws.Range(1, 1, 1, colCount).Merge();
                    ws.Row(1).Height = 30;
                    ws.Cell(1, 1).Style.Font.Bold = true;
                    ws.Cell(1, 1).Style.Font.FontSize = 16;
                    ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // ----- Date Range -----
                    ws.Cell(2, 1).Value = $"From: {FromDate:yyyy-MM-dd}   To: {ToDate:yyyy-MM-dd}";
                    ws.Range(2, 1, 2, colCount).Merge();
                    ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(2, 1).Style.Font.Italic = true;

                    // ----- Header Row (Row 4) -----
                    for (int i = 0; i < colCount; i++)
                    {
                        var headerCell = ws.Cell(4, i + 1);
                        headerCell.Value = dt.Columns[i].ColumnName;
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Font.FontColor = XLColor.Black;
                        headerCell.Style.Fill.BackgroundColor = XLColor.LightGreen;
                        headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        headerCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        headerCell.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    }

                    // ----- Data -----
                    ws.Cell(5, 1).InsertData(dt.Rows);

                    // ----- Alternating row colors (Light Green / White) -----
                    for (int r = 5; r < dt.Rows.Count + 5; r++)
                    {
                        var rowRange = ws.Range(r, 1, r, colCount);
                        if ((r - 5) % 2 == 0)
                            rowRange.Style.Fill.BackgroundColor = XLColor.White;
                        else
                            rowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(217, 234, 211); // light green shade
                    }

                    // ----- Borders & Wrap Text -----
                    var dataRange = ws.Range(4, 1, dt.Rows.Count + 4, colCount);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Alignment.WrapText = true;
                    dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // ----- Adjust Columns -----
                    ws.Columns().AdjustToContents();
                    ws.SheetView.FreezeRows(4); // header visible always
                    ws.Range(4, 1, 4, colCount).SetAutoFilter(); // enable filter

                    // ----- Save safely -----
                    using (var fs = new FileStream(fullFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        workbook.SaveAs(fs);
                    }
                }

                // ================== RESPONSE ==================
                return Ok(new
                {
                    status = true,
                    message = "Excel report generated successfully",
                    filePath = $"/TaskReport/{fileName}.xlsx"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        public IActionResult SaveData([FromBody] TaskDetail_Model model)
        {
            if (model == null)
            {
                return BadRequest("No  data provided.");
            }

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                try
                {
                    con.Open();

                    using (var transaction = con.BeginTransaction())
                    {
                        SaveDispatchDeliveryData(con, transaction, model);

                        transaction.Commit();
                    }

                    return Ok(new { success = true, message = "Dispatch data saved successfully!" });
                }
                catch (Exception ex)
                {

                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }
        }

        private void SaveDispatchDeliveryData(SqlConnection connection, SqlTransaction transaction, TaskDetail_Model model)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using var conn = _dbConnection.GetErpConnection();
     
            using (var cmd1 = new SqlCommand("sp_TaskReply", connection, transaction))
            {
                cmd1.CommandType = CommandType.StoredProcedure;
                cmd1.Parameters.AddWithValue("@Action", "UpdatedatabyList");
                cmd1.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                cmd1.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                cmd1.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode); 
                cmd1.Parameters.AddWithValue("@V_TYPE", "TASK"); 
                cmd1.Parameters.AddWithValue("@V_NO", model.V_NO);
                cmd1.Parameters.AddWithValue("@REMARKS", model.REMARKS);                 

                cmd1.ExecuteNonQuery();
            }
        }

    }
}
