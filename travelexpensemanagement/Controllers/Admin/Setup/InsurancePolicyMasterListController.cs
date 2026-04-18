using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    [SessionAuthorize]
    public class InsurancePolicyMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public InsurancePolicyMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Insurance Policy Master";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel(); // FIX: use this directly

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Setup/InsurancePolicyMasterList/Index.cshtml", model);
        }

        [HttpGet]
        public IActionResult GetAllInsurancePolicies(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var policyList = new List<INSU_MAST>();
            int totalCount = 0;
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsuranceMast", conn)) // Replace with your actual stored procedure name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", 1); // Replace with actual logic for company code
                        cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        // Required unused parameters set as DBNull
                        cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@DESCRIPTION", DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_ADD", DBNull.Value);
                        cmd.Parameters.AddWithValue("@POLICY_AMT", DBNull.Value);
                        cmd.Parameters.AddWithValue("@ENTRY_DATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EFF_DATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EXP_DATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                        cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                        cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                        cmd.Parameters.AddWithValue("@LID", DBNull.Value);

                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            try
                            {
                                while (reader.Read())
                                {
                                    policyList.Add(new INSU_MAST
                                    {
                                        CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                        NAME = reader["NAME"]?.ToString() ?? "",
                                        DESCRIPTION = reader["DESCRIPTION"]?.ToString() ?? "",
                                        COMP_NAME = reader["COMP_NAME"]?.ToString() ?? "",
                                        ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                        // Add additional fields if required in your UI
                                    });
                                }

                                // Move to next result set and read total count
                                if (reader.NextResult() && reader.Read())
                                {
                                    totalCount = Convert.ToInt32(reader["TotalCount"]);
                                }
                            }
                            catch (Exception ex)
                            {
                                return Json(new { error = "Error reading data", message = ex.Message });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "An error occurred while processing your request", message = ex.Message });
            }

            return Json(new { policies = policyList, totalCount });
        }
        public INSU_MAST GetInsurancePolicyByCode(int code)
        {
            INSU_MAST policy = null;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsuranceMast", con)) // Replace with actual SP name
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            policy = new INSU_MAST
                            {
                                CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                NAME = rdr["NAME"]?.ToString(),
                                DESCRIPTION = rdr["DESCRIPTION"]?.ToString(),
                                COMP_NAME = rdr["COMP_NAME"]?.ToString(),
                                COMP_ADD = rdr["COMP_ADD"]?.ToString(),
                                POLICY_AMT = rdr["POLICY_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["POLICY_AMT"]) : 0,
                                ENTRY_DATE = rdr["ENTRY_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["ENTRY_DATE"]) : (DateTime?)null,
                                EFF_DATE = rdr["EFF_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EFF_DATE"]) : (DateTime?)null,
                                EXP_DATE = rdr["EXP_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EXP_DATE"]) : (DateTime?)null,
                                ACTIVE = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0
                            };
                        }
                        else
                        {
                            // Optional: log if no data is found
                            Console.WriteLine("No insurance policy found for the given code.");
                        }
                    }
                }
            }

            return policy;
        }

        public IActionResult ExportAllDocs()
        {
            var dataList = new List<INSU_MASTExportDto>();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = @"SELECT Code, Name, DESCRIPTION, COMP_NAME AS CompanyName, ACTIVE FROM INSU_MAST";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dataList.Add(new INSU_MASTExportDto
                                {
                                    Code = reader["Code"]?.ToString(),
                                    Name = reader["Name"]?.ToString(),
                                    Description = reader["DESCRIPTION"]?.ToString(),
                                    CompanyName = reader["CompanyName"]?.ToString(),
                                    Active = reader["ACTIVE"] != DBNull.Value && Convert.ToBoolean(reader["ACTIVE"])
                                });
                            }
                        }
                    }
                }

                return Json(dataList);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "An error occurred while exporting insurance data.",
                    error = ex.Message
                });
            }
        }
        public JsonResult DocDetailsCode(string docCode)
        {
            var docDetails = new List<INSU_MASTDetailDto>();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = @"SELECT DISTINCT da.Code, um.USER_NAME AS UUser, da.UDATE, ume.USER_NAME AS EUSER, da.EDATE, da.WSID, da.LIP, da.LID   FROM INSU_MAST da
                LEFT JOIN CONDATABASE..USER_MAST um ON da.UUSER = um.CODE LEFT JOIN CONDATABASE..USER_MAST ume ON da.EUSER = ume.CODE
                WHERE da.Code = @Code";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Code", docCode);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                docDetails.Add(new INSU_MASTDetailDto
                                {
                                    DOC_CODE = reader["Code"]?.ToString(),
                                    UUser = reader["UUser"]?.ToString(),
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : (DateTime?)null,
                                    EUSER = reader["EUSER"]?.ToString(),
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : (DateTime?)null,
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
                return Json(new { success = false, message = "Error retrieving document details", error = ex.Message });
            }
        }


    }
}
