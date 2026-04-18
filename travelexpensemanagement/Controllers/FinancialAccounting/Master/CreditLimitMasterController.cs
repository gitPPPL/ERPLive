using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Finance;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class CreditLimitMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CreditLimitMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/FinancialAccounting/Master/CreditLimitMaster/Index.cshtml");
        }

        public IActionResult GetPartyList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM SUBGROUP_MAST WHERE COMP_CODE='" + compCode + "' and NATURE='Customer' ORDER BY NAME ";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult GetGroupList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM MGROUP_MAST WHERE COMP_CODE='" + compCode + "' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }


        //public IActionResult GetGroupNameByPartyCode(int code)
        //{
        //    string groupName = string.Empty;
        //    int groupCode = 0;

        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        //            var OSAmt = "";
        //            var showCreditdebit = "";


        //            string query = @"SELECT TOP 1 GM.CODE AS GroupCode, GM.NAME AS GroupName FROM SUBGROUP_MAST SM
        //                            INNER JOIN 
        //                                MGROUP_MAST GM ON GM.CODE = SM.GROUP_CODE
        //                            WHERE 
        //                                SM.CODE = @PartyCode AND GM.COMP_CODE = @CompCode";

        //            using (SqlCommand cmd = new SqlCommand(query, con))
        //            {
        //                cmd.Parameters.AddWithValue("@PartyCode", code);
        //                cmd.Parameters.AddWithValue("@CompCode", compCode);

        //                con.Open();
        //                using (var reader = cmd.ExecuteReader())
        //                {
        //                    if (reader.Read())
        //                    {
        //                        groupCode = Convert.ToInt32(reader["GroupCode"]);
        //                        groupName = reader["GroupName"].ToString();
        //                    }
        //                }
        //            }
        //        }

        //        return Json(new { success = true, groupName, groupCode });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error fetching group name", error = ex.Message });
        //    }
        //}
        public IActionResult GetGroupNameByPartyCode(int code)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    var global = _globalVariableService.GetGlobalVariables();
                    var compCode = global.PubCompCode;
                    var branchCode = global.PubBranchCode;

                    string groupName = string.Empty;
                    int groupCode = 0;
                    int creditdays = 0;
                    int creditlimit = 0;
                    decimal OSAmount = 0;
                    string drcrStatus = "";

                    con.Open();

                    // 1️⃣ GET GROUP NAME, GROUP CODE, CREDIT DAYS, CREDIT LIMIT
                    string queryGroup = @"
                SELECT TOP 1 
                    GM.CODE AS GroupCode, 
                    GM.NAME AS GroupName, 
                    SM.CREDIT_DAYS AS CREDIT_DAYS, 
                    SM.CREDIT_LIMIT AS CREDIT_LIMIT
                FROM SUBGROUP_MAST SM
                INNER JOIN MGROUP_MAST GM 
                    ON GM.CODE = SM.GROUP_CODE 
                    AND GM.COMP_CODE = SM.COMP_CODE
                WHERE 
                    SM.CODE = @PartyCode 
                    AND SM.COMP_CODE = @CompCode";

                    using (SqlCommand cmd = new SqlCommand(queryGroup, con))
                    {
                        cmd.Parameters.AddWithValue("@PartyCode", code);
                        cmd.Parameters.AddWithValue("@CompCode", compCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                groupCode = Convert.ToInt32(reader["GroupCode"]);
                                groupName = reader["GroupName"].ToString();

                                creditdays = reader["CREDIT_DAYS"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["CREDIT_DAYS"]);

                                creditlimit = reader["CREDIT_LIMIT"] == DBNull.Value
                                    ? 0
                                    : Convert.ToInt32(reader["CREDIT_LIMIT"]);
                            }
                        }
                    }

                    // 2️⃣ GET DEBIT AMOUNT (dramt)
                    decimal dramt = 0;
                    string queryDR = @"
                SELECT SUM(Amt) FROM ledger2 
                WHERE 
                    DR_CODE = @PartyCode 
                    AND DR_CODE > 0 
                    AND COMP_CODE = @CompCode 
                    AND Branch_code = @BranchCode";

                    using (SqlCommand cmd = new SqlCommand(queryDR, con))
                    {
                        cmd.Parameters.AddWithValue("@PartyCode", code);
                        cmd.Parameters.AddWithValue("@CompCode", compCode);
                        cmd.Parameters.AddWithValue("@BranchCode", branchCode);

                        var drResult = cmd.ExecuteScalar();
                        dramt = drResult == DBNull.Value ? 0 : Convert.ToDecimal(drResult);
                    }

                    // 3️⃣ GET CREDIT AMOUNT (cramt)
                    decimal cramt = 0;
                    string queryCR = @"
                SELECT SUM(Amt) FROM ledger2 
                WHERE 
                    CR_CODE = @PartyCode 
                    AND CR_CODE > 0 
                    AND COMP_CODE = @CompCode 
                    AND Branch_code = @BranchCode";

                    using (SqlCommand cmd = new SqlCommand(queryCR, con))
                    {
                        cmd.Parameters.AddWithValue("@PartyCode", code);
                        cmd.Parameters.AddWithValue("@CompCode", compCode);
                        cmd.Parameters.AddWithValue("@BranchCode", branchCode);

                        var crResult = cmd.ExecuteScalar();
                        cramt = crResult == DBNull.Value ? 0 : Convert.ToDecimal(crResult);
                    }

                    // 4️⃣ OUTSTANDING AMOUNT
                    OSAmount = Math.Round(Math.Abs(dramt - cramt), 2);

                    // 5️⃣ Dr / Cr Status
                    drcrStatus = dramt > cramt ? "Dr" : "Cr";

                    // 6️⃣ RETURN JSON
                    return Json(new
                    {
                        success = true,
                        groupName,
                        groupCode,
                        creditdays,
                        creditlimit,
                        OSAmount,
                        drcrStatus
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching group details",
                    error = ex.Message
                });
            }
        }
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
                string lastV_NO_Query = "SELECT TOP 1 V_NO FROM CREDIT_LIMIT ORDER BY V_NO DESC";
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
        [HttpPost]
        public IActionResult SaveCreditLimit([FromBody] CREDIT_LIMIT model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            var result = SaveOrUpdateCreditLimit(model, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }
        public string SaveOrUpdateCreditLimit(CREDIT_LIMIT creditLimit, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            int v_NO = GetNextV_NO(globalVar.PubFYearCode);
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    // 🔍 CHECK DUPLICATE ONLY FOR INSERT
                    if (action == "INSERT")
                    {
                        string checkQuery = @" SELECT COUNT(*) FROM CREDIT_LIMIT WHERE PARTY_CODE = @PARTY_CODE AND EFF_FROM = @EFF_FROM AND COMP_CODE = @COMP_CODE";
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                        {
                            checkCmd.Parameters.AddWithValue("@PARTY_CODE", creditLimit.PARTY_CODE);
                            checkCmd.Parameters.AddWithValue("@EFF_FROM", creditLimit.EFF_FROM);
                            checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());

                            if (exists > 0)
                            {
                                return "Entry already exists for this PARTY and Date.";
                            }
                        }
                    }
                    // ⭐ IF NOT DUPLICATE → CONTINUE PROCEDURE
                    using (SqlCommand cmd = new SqlCommand("sp_CREDIT_LIMIT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                        if (action == "INSERT")
                            cmd.Parameters.AddWithValue("@V_NO", v_NO);
                        else
                            cmd.Parameters.AddWithValue("@V_NO", creditLimit.V_NO);

                        cmd.Parameters.AddWithValue("@V_TYPE", creditLimit.V_TYPE ?? "CLMT");
                        cmd.Parameters.AddWithValue("@DOC_ID", (creditLimit.V_TYPE ?? "CLMT") + v_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", creditLimit.V_DATE);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", creditLimit.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@GR_CODE", creditLimit.GR_CODE);
                        cmd.Parameters.AddWithValue("@CR_LIMIT", creditLimit.CR_LIMIT);
                        cmd.Parameters.AddWithValue("@CR_DAYS", creditLimit.CR_DAYS);
                        cmd.Parameters.AddWithValue("@EFF_FROM", creditLimit.EFF_FROM);
                        cmd.Parameters.AddWithValue("@REMARKS", creditLimit.REMARKS ?? "");
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", creditLimit.FAPROV_STATUS ?? "");
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", creditLimit.FAPROV_REMARKS ?? "");
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", creditLimit.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");
                        cmd.Parameters.AddWithValue("@OURCR_DAYS", creditLimit.OURCR_DAYS);

                        cmd.ExecuteNonQuery();
                    }

                    return "Success";
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
        //public string SaveOrUpdateCreditLimit(CREDIT_LIMIT creditLimit, string action)
        //{
        //    var globalVar = _globalVariableService.GetGlobalVariables();
        //    int v_NO = GetNextV_NO(globalVar.PubFYearCode);

        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_CREDIT_LIMIT", con)) 
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@Action", action);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
        //                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
        //                if (action == "INSERT")
        //                    cmd.Parameters.AddWithValue("@V_NO", v_NO);
        //                else
        //                    cmd.Parameters.AddWithValue("@V_NO", creditLimit.V_NO);

        //                cmd.Parameters.AddWithValue("@V_TYPE", creditLimit.V_TYPE ?? "CLMT");
        //                cmd.Parameters.AddWithValue("@DOC_ID", (creditLimit.V_TYPE ?? "CLMT") + v_NO);
        //                cmd.Parameters.AddWithValue("@V_DATE", creditLimit.V_DATE);
        //                cmd.Parameters.AddWithValue("@PARTY_CODE", creditLimit.PARTY_CODE);
        //                cmd.Parameters.AddWithValue("@GR_CODE", creditLimit.GR_CODE);
        //                cmd.Parameters.AddWithValue("@CR_LIMIT", creditLimit.CR_LIMIT);
        //                cmd.Parameters.AddWithValue("@CR_DAYS", creditLimit.CR_DAYS);
        //                cmd.Parameters.AddWithValue("@EFF_FROM", creditLimit.EFF_FROM);
        //                cmd.Parameters.AddWithValue("@REMARKS", creditLimit.REMARKS ?? "");
        //                cmd.Parameters.AddWithValue("@FAPROV_STATUS", creditLimit.FAPROV_STATUS ?? "");
        //                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", creditLimit.FAPROV_REMARKS ?? "");
        //                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@AED", creditLimit.AED ?? "A");
        //                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
        //                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
        //                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");
        //                cmd.Parameters.AddWithValue("@OURCR_DAYS", creditLimit.OURCR_DAYS);

        //                con.Open();
        //                cmd.ExecuteNonQuery();

        //                return "Success";
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return "Error: " + ex.Message;
        //    }
        //}

        [HttpPost]
        public JsonResult DeleteCreditLimitById(int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_CREDIT_LIMIT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
