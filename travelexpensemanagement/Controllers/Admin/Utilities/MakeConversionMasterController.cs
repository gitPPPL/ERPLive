using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Utilities;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class MakeConversionMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public MakeConversionMasterController(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            DropdownService dropdownService,
            DbHelper dbHelper,
            travelexpensemanagement.ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/MakeConversionMaster/Index.cshtml");
        }
        public JsonResult ddlMakeType()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"
                SELECT DISTINCT MAKE_TYPE AS value, MAKE_TYPE AS text
                FROM PAY_LOOMINCENTIVERATE_MAST
                WHERE COMP_CODE = {globalVar.PubCompCode}
                ORDER BY MAKE_TYPE;";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        public JsonResult GetddlRunNo()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" SELECT DISTINCT RUN_NO AS value, RUN_NO AS text FROM PAY_LOOMINCENTIVERATE_MAST
                WHERE COMP_CODE = {globalVar.PubCompCode} ORDER BY RUN_NO;";
            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }
        [HttpPost]
        public JsonResult SaveMakeConversion([FromBody] MakeConversionRequest request)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                if (request == null)
                    return Json(new { success = false, message = "Request object is null." });

                if (string.IsNullOrEmpty(request.MAKE_TYPE) || string.IsNullOrEmpty(request.RUN_NO))
                    return Json(new { success = false, message = "Make Type and Run No are required." });

                if (request.Records == null || request.Records.Count == 0)
                    return Json(new { success = false, message = "No data to save." });

                string checkQuery = @" SELECT COUNT(*) FROM PAY_LOOMINCENTIVERATE_MAST WHERE COMP_CODE = @COMP_CODE AND MAKE_TYPE = @MAKE_TYPE AND RUN_NO = @RUN_NO";
                var checkParams = new List<SqlParameter>
                {
                    new SqlParameter("@COMP_CODE", globalVar.PubCompCode),
                    new SqlParameter("@MAKE_TYPE", request.MAKE_TYPE),
                    new SqlParameter("@RUN_NO", request.RUN_NO)
                };
                int exists = Convert.ToInt32(ExecuteScalar(checkQuery, checkParams));
                if (exists > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Record already exists."
                    });
                }
                string deleteQuery = @"DELETE FROM PAY_LOOMINCENTIVERATE_MAST WHERE COMP_CODE = @COMP_CODE AND MAKE_TYPE = @MAKE_TYPE AND RUN_NO = @RUN_NO";
                ExecuteNonQuery(deleteQuery, checkParams);

                string snoQuery = @"SELECT ISNULL(MAX(SNO), 0) + 1 FROM PAY_LOOMINCENTIVERATE_MAST";
                int nextSno = Convert.ToInt32(ExecuteScalar(snoQuery));

                int rowIndex = 0;
                foreach (var item in request.Records)
                {
                    rowIndex++;
                    try
                    {
                        decimal.TryParse(item.BaseProduction?.ToString(), out decimal baseProduction);
                        decimal.TryParse(item.Production?.ToString(), out decimal production);
                        decimal.TryParse(item.Per?.ToString(), out decimal per);

                        string insertQuery = @" INSERT INTO PAY_LOOMINCENTIVERATE_MAST (COMP_CODE, MAKE_TYPE, RUN_NO, BASE_PRODUCTION, PRODUCTION, PER, FLG, 
                        UUSER, UDATE, AED, WSID, LIP, LID, SNO) VALUES (@COMP_CODE, @MAKE_TYPE, @RUN_NO, @BASE_PRODUCTION, @PRODUCTION, 
                        @PER, @FLG, @UUSER, GETDATE(), 'A', @WSID, @LIP, @LID, @SNO)";

                        var insertParams = new List<SqlParameter>
                        {
                            new SqlParameter("@COMP_CODE", globalVar.PubCompCode),
                            new SqlParameter("@MAKE_TYPE", request.MAKE_TYPE),
                            new SqlParameter("@RUN_NO", request.RUN_NO),
                            new SqlParameter("@BASE_PRODUCTION", baseProduction == 0 ? (object)DBNull.Value : baseProduction),
                            new SqlParameter("@PRODUCTION", production == 0 ? (object)DBNull.Value : production),
                            new SqlParameter("@PER", per == 0 ? (object)DBNull.Value : per),
                            new SqlParameter("@FLG", item.Flg == null ? 0 : Convert.ToInt32(item.Flg)),
                            new SqlParameter("@UUSER", globalVar.PubUserId),
                            new SqlParameter("@WSID", globalVar.PubWorkStationID),
                            new SqlParameter("@LIP", globalVar.PubLocalId),
                            new SqlParameter("@LID", Environment.MachineName),
                            new SqlParameter("@SNO", nextSno)
                        };
                        ExecuteNonQuery(insertQuery, insertParams);
                        nextSno++;
                    }
                    catch (Exception innerEx)
                    {
                        return Json(new { success = false, message = $"Error inserting record #{rowIndex}: {innerEx.Message}" });
                    }
                }
                return Json(new { success = true, message = "Data saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Outer Error: " + ex.Message });
            }
        }
        public int ExecuteNonQuery(string query, List<SqlParameter> parameters)
        {
            using (var conn = _dbConnection.GetErpConnection())
            {
                if (conn == null)
                    throw new Exception("Database connection is null. Check GetErpConnection().");
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(param ?? throw new ArgumentNullException(nameof(param)));
                        }
                    }
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected;
                }
            }
        }
        public object ExecuteScalar(string query, List<SqlParameter> parameters = null)
        {
            using (var conn = _dbConnection.GetErpConnection())
            {
                if (conn == null)
                    throw new Exception("Database connection is null. Check GetErpConnection().");

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(param);
                        }
                    }
                    if (conn.State != ConnectionState.Open)
                        conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }
        [HttpPost]
        public JsonResult GetID([FromBody] GetRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid request" });
            }
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
            string query = @" SELECT MAKE_TYPE, RUN_NO, BASE_PRODUCTION, PRODUCTION, PER, REPORT_FLG FROM PAY_LOOMINCENTIVERATE_MAST
            WHERE COMP_CODE = @COMP_CODE AND MAKE_TYPE = @MAKE_TYPE AND RUN_NO = @RUN_NO AND SNO = @SNO";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@COMP_CODE", globalVar.PubCompCode),
                    new SqlParameter("@MAKE_TYPE", request.MakeType),
                    new SqlParameter("@RUN_NO", request.RunNo),
                    new SqlParameter("@SNO", request.Sno)
                };

                DataTable dt = ExecuteDataTable(query, parameters);

                if (dt.Rows.Count == 0)
                {
                    return Json(new { success = false, message = "No record found" });
                }
                var row = dt.Rows[0];
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        MAKE_TYPE = row["MAKE_TYPE"]?.ToString(),
                        RUN_NO = Convert.ToInt32(row["RUN_NO"] == DBNull.Value ? 0 : row["RUN_NO"]),
                        BASE_PRODUCTION = Convert.ToDecimal(row["BASE_PRODUCTION"] == DBNull.Value ? 0 : row["BASE_PRODUCTION"]),
                        PRODUCTION = Convert.ToDecimal(row["PRODUCTION"] == DBNull.Value ? 0 : row["PRODUCTION"]),
                        PER = Convert.ToDecimal(row["PER"] == DBNull.Value ? 0 : row["PER"]),
                        REPORT_FLG = Convert.ToInt32(row["REPORT_FLG"] == DBNull.Value ? 0 : row["REPORT_FLG"])
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public DataTable ExecuteDataTable(string query, List<SqlParameter> parameters)
        {
            DataTable dt = new DataTable();

            using (var conn = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;

                if (parameters != null)
                {
                    foreach (var p in parameters)
                    {
                        cmd.Parameters.Add(p);
                    }
                }
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            return dt;
        }
    }
}
