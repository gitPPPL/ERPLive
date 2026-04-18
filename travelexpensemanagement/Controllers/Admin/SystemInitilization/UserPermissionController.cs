using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Metadata;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    [SessionAuthorize(1)]
    public class UserPermissionController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService.DropdownService _dropdownService;
        private readonly DbHelper.DbHelper _dbHelper;

        public UserPermissionController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService.DropdownService dropdownService, DbHelper.DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            //return View();
            return View("~/Views/Admin/SystemInitilization/UserPermission/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetUserListddl()
        {
            string query = "Select Code, User_Name From USER_MAST order by Code asc";
            var userList = _dropdownService.GetDropdownListcon(query);
            return Json(userList);
        }
        // Module List Start Block
        public JsonResult GetCopyotheruser()
        {
            string query = "Select Code, User_Name From USER_MAST order by Code asc";
            var userList = _dropdownService.GetDropdownListcon(query);
            return Json(userList);
        }
        public JsonResult GetYearList()
        {
            string query = "SELECT code, CONVERT(varchar, CURR_YEAR, 103) AS Name  FROM YEAR_MAST";
            var userList = _dropdownService.GetDropdownListcon(query);
            return Json(userList);
        }
        public JsonResult ModuleNamelist()
        {
            string query = "Select Code, DISPLAY_NAME From MODULE_MAST order by Code asc";
            var userList = _dropdownService.GetDropdownList(query);
            return Json(userList);
        }
        // Module List End Block
        //Department list Start Block
        public JsonResult Departmentlist()
        {
            var globals = _globalVariableService.GetGlobalVariables();
            List<object> Department = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" SELECT Code, NAME, Tran_type AS Type FROM ITEMDEPT_MAST
                WHERE COMP_CODE = @CompCode AND TRAN_TYPE IN ('Store', 'Production') ORDER BY TRAN_TYPE, NAME";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CompCode", globals.PubCompCode);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Department.Add(new
                    {
                        Code = reader["Code"].ToString(),
                        NAME = reader["NAME"].ToString(),
                        Type = reader["Type"].ToString()
                    });
                }
            }
            return Json(Department);                 
        }
        //Department list End Block
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string UserID)
        {
            if (string.IsNullOrWhiteSpace(UserID) || !int.TryParse(UserID, out int parsedUserId))
            {
                return BadRequest("Invalid or missing UserID.");
            }
            var globals = _globalVariableService.GetGlobalVariables();
            // Step 1: Get DEPT_CODE from USER_DEPT
            string checkDeptSql = @"SELECT DEPT_CODE FROM USER_DEPT WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";

            var checkDeptParams = new List<SqlParameter>
            {
                new SqlParameter("@COMP_CODE", globals.PubCompCode),
                new SqlParameter("@YEAR_CODE", globals.PubFYearCode),
                new SqlParameter("@USER_CODE", parsedUserId)
            };

            DataTable deptResult = await _dbHelper.ExecuteQueryAsync(checkDeptSql, checkDeptParams);

            var deptList = new List<string>();
            foreach (DataRow row in deptResult.Rows)
            {
                deptList.Add(row["DEPT_CODE"].ToString());
            }
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@CompCode", globals.PubCompCode),
                new SqlParameter("@YearCode", globals.PubFYearCode),
                new SqlParameter("@UserCode", parsedUserId)
            };
            var userDetails = await _dbHelper.GetListFromStoredProcedureAsync<UserMenuDetail>("GetUserMenuDetails", parameters);
            var response = new
            {
                Departments = deptList,
                UserMenuDetails = userDetails
            };
            return Json(response);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitModules([FromBody] UserModuleSubmission submission)
        {
            if (submission == null || submission.UserID <= 0 || submission.UserMenuDetail == null || submission.UserMenuDetail.Count == 0)
            {
                return BadRequest("Invalid data.");
            }
            var globals = _globalVariableService.GetGlobalVariables();
            var userDetails = new List<UserMenuDetail>();
            var moduleCodes = string.Join(",", submission.UserMenuDetail.Select(u => u.MODULE_CODE));

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@CompCode", globals.PubCompCode),
                new SqlParameter("@YearCode", globals.PubFYearCode),
                new SqlParameter("@UserCode", submission.UserID),
                new SqlParameter("@ModuleCodes", moduleCodes)
            };
            var userDetail = await _dbHelper.GetListFromStoredProcedureAsync<UserMenuDetail>("GetUserMenuDetails", parameters);
            userDetails.AddRange(userDetail);
            return Json(userDetails);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitModulesData([FromBody] UserModuleSubmission submission)
        {
            if (submission == null || submission.UserID <= 0 || submission.UserMenuDetail == null || submission.UserMenuDetail.Count == 0)
            {
                return BadRequest("Invalid data submitted.");
            }

            var globals = _globalVariableService.GetGlobalVariables();
            int GetUserID = 0;
            string PubFYearCode = "";
            if (submission.copyPermission == 0)
            {
                GetUserID = submission.UserID;
            }
            else
            {
                GetUserID = submission.CopyOtherUser;
            }
            using (var connection = _dbConnection.GetErpConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // --- USER_DEPT Section ---
                        string checkDepartmentExistenceSql = @"
                          SELECT COUNT(*) FROM USER_DEPT 
                          WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";

                        var checkParametersDepartment = new List<SqlParameter>
                        {
                            new SqlParameter("@COMP_CODE", SqlDbType.NVarChar) { Value = globals.PubCompCode },
                            new SqlParameter("@YEAR_CODE", SqlDbType.NVarChar) { Value = globals.PubFYearCode },
                            //new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = submission.UserID }
                            new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = GetUserID }
                        };

                        var recordCountDept = await _dbHelper.ExecuteScalarAsynctran<int>(checkDepartmentExistenceSql, checkParametersDepartment, transaction);

                        if (recordCountDept > 0)
                        {
                            string deleteSql = @"
                        DELETE FROM USER_DEPT 
                        WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";

                            var deleteParamsDept = CloneParameters(checkParametersDepartment);
                            await _dbHelper.ExecuteQueryAsynctran(deleteSql, deleteParamsDept, transaction);
                        }

                        string deptInsertSql = @"INSERT INTO USER_DEPT (COMP_CODE, YEAR_CODE, USER_CODE, DEPT_CODE, UUSER, UDATE, AED, WSID, LIP, LID)
                        VALUES (@COMP_CODE, @YEAR_CODE, @USER_CODE, @DEPT_CODE,@UUSER, @UDATE, 'A', @WSID, @LIP, @LID)";
                        foreach (var deptCode in submission.DepartmentCheckCodes)
                        {
                            var deptParameters = new List<SqlParameter>
                            {
                                new SqlParameter("@COMP_CODE", SqlDbType.NVarChar) { Value = globals.PubCompCode },
                                new SqlParameter("@YEAR_CODE", SqlDbType.NVarChar) { Value = globals.PubFYearCode },
                                new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = GetUserID },
                                new SqlParameter("@DEPT_CODE", SqlDbType.NVarChar) { Value = deptCode },
                                new SqlParameter("@UUSER", SqlDbType.Int) { Value = GetUserID },
                                new SqlParameter("@UDATE", SqlDbType.DateTime) { Value = DateTime.Now },
                                new SqlParameter("@WSID", SqlDbType.NVarChar) { Value = globals.PubWorkStationID },
                                new SqlParameter("@LIP", SqlDbType.NVarChar) { Value = globals.PubLocalId },
                                new SqlParameter("@LID", SqlDbType.NVarChar) { Value = globals.PubUserName }
                            };
                            await _dbHelper.ExecuteQueryAsynctran(deptInsertSql, deptParameters, transaction);
                        }
                        // --- USER_MENU Section ---
                        string checkExistenceSql = @" SELECT COUNT(*) FROM USER_MENU WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";

                        var checkParametersMenu = new List<SqlParameter>
                        {
                            new SqlParameter("@COMP_CODE", SqlDbType.NVarChar) { Value = globals.PubCompCode },
                            new SqlParameter("@YEAR_CODE", SqlDbType.NVarChar) { Value = globals.PubFYearCode },
                            //new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = submission.UserID }
                            new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = GetUserID }
                        };

                        var recordCountMenu = await _dbHelper.ExecuteScalarAsynctran<int>(checkExistenceSql, checkParametersMenu, transaction);

                        if (recordCountMenu > 0)
                        {
                            string deleteSql = @"
                            DELETE FROM USER_MENU 
                            WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";
                            var deleteParamsMenu = CloneParameters(checkParametersMenu);
                            await _dbHelper.ExecuteQueryAsynctran(deleteSql, deleteParamsMenu, transaction);
                        }
                        string insertSql = @" INSERT INTO USER_MENU (COMP_CODE, YEAR_CODE, USER_CODE, MENU_CODE, MENU_NAME, _ACCESS, _ADD, _EDIT, _DELETE, _PRINT, 
                        _EXPORT, _MAIL, _APPROVAL, _DOCDETAIL, MODULE_CODE, UUSER, UDATE, AED, WSID, LIP, LID) VALUES (@COMP_CODE, @YEAR_CODE, @USER_CODE, @MENU_CODE, @MENU_NAME, @_ACCESS, @_ADD, 
                        @_EDIT, @_DELETE, @_PRINT, @_EXPORT, @_MAIL, @_APPROVAL, @_DOCDETAIL, @MODULE_CODE, @UUSER, @UDATE, 'A', @WSID, @LIP, @LID)";

                        foreach (var menu in submission.UserMenuDetail)
                        {
                            var insertParameters = new List<SqlParameter>
                            {
                                new SqlParameter("@COMP_CODE", SqlDbType.NVarChar) { Value = globals.PubCompCode },
                                new SqlParameter("@YEAR_CODE", SqlDbType.NVarChar) { Value = globals.PubFYearCode },
                                new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = GetUserID },
                                new SqlParameter("@MENU_CODE", SqlDbType.Int) { Value = menu.Code },
                                new SqlParameter("@MENU_NAME", SqlDbType.NVarChar) { Value = menu.Name },
                                new SqlParameter("@_ACCESS", SqlDbType.Bit) { Value = menu.Access },
                                new SqlParameter("@_ADD", SqlDbType.Bit) { Value = menu.Add },
                                new SqlParameter("@_EDIT", SqlDbType.Bit) { Value = menu.Edit },
                                new SqlParameter("@_DELETE", SqlDbType.Bit) { Value = menu.Delete },
                                new SqlParameter("@_PRINT", SqlDbType.Bit) { Value = menu.Print },
                                new SqlParameter("@_EXPORT", SqlDbType.Bit) { Value = menu.Export },
                                new SqlParameter("@_MAIL", SqlDbType.Bit) { Value = menu.Mail },
                                new SqlParameter("@_APPROVAL", SqlDbType.Bit) { Value = menu.Approval },
                                new SqlParameter("@_DOCDETAIL", SqlDbType.Bit) { Value = menu.DocDetail },
                                new SqlParameter("@MODULE_CODE", SqlDbType.Int) { Value = menu.MODULE_CODE },
                                new SqlParameter("@UUSER", SqlDbType.Int) { Value = GetUserID },
                                new SqlParameter("@UDATE", SqlDbType.DateTime) { Value = DateTime.Now },
                                new SqlParameter("@WSID", SqlDbType.NVarChar) { Value = globals.PubWorkStationID },
                                new SqlParameter("@LIP", SqlDbType.NVarChar) { Value = globals.PubLocalId },
                                new SqlParameter("@LID", SqlDbType.NVarChar) { Value = globals.PubUserName }
                            };

                            await _dbHelper.ExecuteQueryAsynctran(insertSql, insertParameters, transaction);
                        }
                        transaction.Commit();
                        return Json(new { success = true, message = "Permissions saved successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, "An error occurred: " + ex.Message);
                    }
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> UserCopytoOtherUser([FromBody] UserModuleSubmission submission)
        {
            if (submission == null || submission.UserID <= 0 || submission.UserMenuDetail == null || submission.UserMenuDetail.Count == 0)
            {
                return BadRequest("Invalid data submitted.");
            }

            var globals = _globalVariableService.GetGlobalVariables();
            int GetUserID = 0;
            string PubFYearCode = "";
            if (submission.copyPermission == 0)
            {
                GetUserID = submission.UserID;
            }
            else
            {
                GetUserID = submission.CopyOtherUser;
            }
            using (var connection = _dbConnection.GetErpConnection())
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // --- USER_DEPT Section ---
                        string checkDepartmentExistenceSql = @"
                          SELECT COUNT(*) FROM USER_DEPT 
                          WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";

                        var checkParametersDepartment = new List<SqlParameter>
                        {
                            new SqlParameter("@COMP_CODE", SqlDbType.NVarChar) { Value = globals.PubCompCode },
                            new SqlParameter("@YEAR_CODE", SqlDbType.NVarChar) { Value = submission.PubFYearCode },
                            //new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = submission.UserID }
                            new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = GetUserID }
                        };

                        var recordCountDept = await _dbHelper.ExecuteScalarAsynctran<int>(checkDepartmentExistenceSql, checkParametersDepartment, transaction);

                        if (recordCountDept > 0)
                        {
                            string deleteSql = @"
                        DELETE FROM USER_DEPT 
                        WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";

                            var deleteParamsDept = CloneParameters(checkParametersDepartment);
                            await _dbHelper.ExecuteQueryAsynctran(deleteSql, deleteParamsDept, transaction);
                        }

                        string deptInsertSql = @"INSERT INTO USER_DEPT (COMP_CODE, YEAR_CODE, USER_CODE, DEPT_CODE, UUSER, UDATE, AED, WSID, LIP, LID)
                        VALUES (@COMP_CODE, @YEAR_CODE, @USER_CODE, @DEPT_CODE,@UUSER, @UDATE, 'A', @WSID, @LIP, @LID)";
                        foreach (var deptCode in submission.DepartmentCheckCodes)
                        {
                            var deptParameters = new List<SqlParameter>
                            {
                                new SqlParameter("@COMP_CODE", SqlDbType.NVarChar) { Value = globals.PubCompCode },
                                new SqlParameter("@YEAR_CODE", SqlDbType.NVarChar) { Value = submission.PubFYearCode },
                                new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = GetUserID },
                                new SqlParameter("@DEPT_CODE", SqlDbType.NVarChar) { Value = deptCode },
                                new SqlParameter("@UUSER", SqlDbType.Int) { Value = GetUserID },
                                new SqlParameter("@UDATE", SqlDbType.DateTime) { Value = DateTime.Now },
                                new SqlParameter("@WSID", SqlDbType.NVarChar) { Value = globals.PubWorkStationID },
                                new SqlParameter("@LIP", SqlDbType.NVarChar) { Value = globals.PubLocalId },
                                new SqlParameter("@LID", SqlDbType.NVarChar) { Value = globals.PubUserName }
                            };
                            await _dbHelper.ExecuteQueryAsynctran(deptInsertSql, deptParameters, transaction);
                        }
                        // --- USER_MENU Section ---
                        string checkExistenceSql = @" SELECT COUNT(*) FROM USER_MENU WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";

                        var checkParametersMenu = new List<SqlParameter>
                        {
                            new SqlParameter("@COMP_CODE", SqlDbType.NVarChar) { Value = globals.PubCompCode },
                            new SqlParameter("@YEAR_CODE", SqlDbType.NVarChar) { Value = submission.PubFYearCode },
                            //new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = submission.UserID }
                            new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = GetUserID }
                        };

                        var recordCountMenu = await _dbHelper.ExecuteScalarAsynctran<int>(checkExistenceSql, checkParametersMenu, transaction);

                        if (recordCountMenu > 0)
                        {
                            string deleteSql = @"
                            DELETE FROM USER_MENU 
                            WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND USER_CODE = @USER_CODE";
                            var deleteParamsMenu = CloneParameters(checkParametersMenu);
                            await _dbHelper.ExecuteQueryAsynctran(deleteSql, deleteParamsMenu, transaction);
                        }
                        string insertSql = @" INSERT INTO USER_MENU (COMP_CODE, YEAR_CODE, USER_CODE, MENU_CODE, MENU_NAME, _ACCESS, _ADD, _EDIT, _DELETE, _PRINT, 
                        _EXPORT, _MAIL, _APPROVAL, _DOCDETAIL, MODULE_CODE, UUSER, UDATE, AED, WSID, LIP, LID) VALUES (@COMP_CODE, @YEAR_CODE, @USER_CODE, @MENU_CODE, @MENU_NAME, @_ACCESS, @_ADD, 
                        @_EDIT, @_DELETE, @_PRINT, @_EXPORT, @_MAIL, @_APPROVAL, @_DOCDETAIL, @MODULE_CODE, @UUSER, @UDATE, 'A', @WSID, @LIP, @LID)";

                        foreach (var menu in submission.UserMenuDetail)
                        {
                            var insertParameters = new List<SqlParameter>
                            {
                                new SqlParameter("@COMP_CODE", SqlDbType.NVarChar) { Value = globals.PubCompCode },
                                new SqlParameter("@YEAR_CODE", SqlDbType.NVarChar) { Value = submission.PubFYearCode },
                                new SqlParameter("@USER_CODE", SqlDbType.Int) { Value = GetUserID },
                                new SqlParameter("@MENU_CODE", SqlDbType.Int) { Value = menu.Code },
                                new SqlParameter("@MENU_NAME", SqlDbType.NVarChar) { Value = menu.Name },
                                new SqlParameter("@_ACCESS", SqlDbType.Bit) { Value = menu.Access },
                                new SqlParameter("@_ADD", SqlDbType.Bit) { Value = menu.Add },
                                new SqlParameter("@_EDIT", SqlDbType.Bit) { Value = menu.Edit },
                                new SqlParameter("@_DELETE", SqlDbType.Bit) { Value = menu.Delete },
                                new SqlParameter("@_PRINT", SqlDbType.Bit) { Value = menu.Print },
                                new SqlParameter("@_EXPORT", SqlDbType.Bit) { Value = menu.Export },
                                new SqlParameter("@_MAIL", SqlDbType.Bit) { Value = menu.Mail },
                                new SqlParameter("@_APPROVAL", SqlDbType.Bit) { Value = menu.Approval },
                                new SqlParameter("@_DOCDETAIL", SqlDbType.Bit) { Value = menu.DocDetail },
                                new SqlParameter("@MODULE_CODE", SqlDbType.Int) { Value = menu.MODULE_CODE },
                                new SqlParameter("@UUSER", SqlDbType.Int) { Value = GetUserID },
                                new SqlParameter("@UDATE", SqlDbType.DateTime) { Value = DateTime.Now },
                                new SqlParameter("@WSID", SqlDbType.NVarChar) { Value = globals.PubWorkStationID },
                                new SqlParameter("@LIP", SqlDbType.NVarChar) { Value = globals.PubLocalId },
                                new SqlParameter("@LID", SqlDbType.NVarChar) { Value = globals.PubUserName }
                            };

                            await _dbHelper.ExecuteQueryAsynctran(insertSql, insertParameters, transaction);
                        }
                        transaction.Commit();
                        return Json(new { success = true, message = "Update Permissions successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, "An error occurred: " + ex.Message);
                    }
                }
            }
        }
        private List<SqlParameter> CloneParameters(List<SqlParameter> originalParameters)
        {
            return originalParameters
                .Select(p => new SqlParameter(p.ParameterName, p.SqlDbType)
                {
                    Value = p.Value
                })
                .ToList();
        }
        [HttpGet]
        public IActionResult DownloadPermissionPdf(string permissionType)
        {
            if (string.IsNullOrEmpty(permissionType))
            {
                return BadRequest("Permission type is required.");
            }

            // Prepare SQL parameters
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@PermissionType", permissionType),
            };

            // Call SP and get data
            DataTable dt = _dbHelper.GetDataTableFromStoredProcedure("sp_GetPivotAccessRights", parameters);

            if (dt == null || dt.Rows.Count == 0)
            {
                return NotFound("No data found.");
            }

            // Generate PDF
            byte[] pdfBytes = GeneratePdf(dt);
            string timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            string fileName = $"PermissionReport_{permissionType}_{timestamp}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        public DataTable ExecuteStoredProcedure(string storedProcName, params SqlParameter[] parameters)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(storedProcName, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddRange(parameters);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
        public byte[] GeneratePdf(DataTable dataTable)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                iTextSharp.text.Document doc = new iTextSharp.text.Document(PageSize.A4.Rotate(), 20f, 20f, 20f, 20f);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                int maxColumnsPerPage = 10;
                int totalColumns = dataTable.Columns.Count;

                for (int colStart = 0; colStart < totalColumns; colStart += maxColumnsPerPage)
                {
                    int colEnd = Math.Min(colStart + maxColumnsPerPage, totalColumns);
                    var selectedColumns = dataTable.Columns.Cast<DataColumn>()
                        .Skip(colStart).Take(colEnd - colStart).ToList();

                    PdfPTable table = new PdfPTable(selectedColumns.Count);
                    table.WidthPercentage = 100;

                    // Header row
                    foreach (var column in selectedColumns)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(column.ColumnName, FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 6)));
                        cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }

                    // Data rows
                    foreach (DataRow row in dataTable.Rows)
                    {
                        foreach (var column in selectedColumns)
                        {
                            string value = row[column] != null ? row[column].ToString() : "";
                            PdfPCell cell = new PdfPCell(new Phrase(value, FontFactory.GetFont(FontFactory.HELVETICA, 6)));
                            cell.HorizontalAlignment = Element.ALIGN_LEFT;
                            table.AddCell(cell);
                        }
                    }
                    doc.Add(table);
                    doc.NewPage();
                }

                doc.Close();
                return ms.ToArray();
            }
        }
    }
}
