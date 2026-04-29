using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.PortableExecutable;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class CourierTrackingEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CourierTrackingEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/GateEntry/Transaction/CourierTrackingEntry/Index.cshtml");
        }
        public JsonResult GetddlDocType()
        {
            string query = $@"Select Code,Name from DOCTYPE_MAST where CODE in ('CTIN','CTOT') order by Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetDocNo(string docType, string docName)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = @"SELECT ISNULL(MAX(V_no), 0) + 1 AS NextVNo FROM COURIER_TRACKING WHERE V_TYPE = @V_TYPE 
                AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                var parameters = new[]
                {
                    new SqlParameter("@CompCode", globalVar.PubCompCode),
                    new SqlParameter("@BranchCode", globalVar.PubBranchCode),
                    new SqlParameter("@YearCode", globalVar.PubFYearCode),
                    new SqlParameter("@V_TYPE", docType)
                };
                int nextVNo = 1;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddRange(parameters);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            nextVNo = Convert.ToInt32(result);
                        }
                    }
                }
                return Json(new { success = true, nextVNo = nextVNo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public JsonResult GetddlPartyName()     
        {

            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT DISTINCT Code, Name FROM (SELECT Code, Name FROM SUBGROUP_MAST WHERE Comp_code = {globalVar.PubCompCode}
            AND Nature NOT IN ('CASH', 'BANK', 'OTHERS') AND Name IS NOT NULL AND Name <> '' AND Code IS NOT NULL AND Code <> '0' UNION ALL  SELECT Party_Code AS Code, Party_Name AS Name
            FROM COURIER_TRACKING WHERE Comp_code = {globalVar.PubCompCode} AND Party_Name IS NOT NULL AND Party_Name <> '' AND Party_Code IS NOT NULL AND Party_Code <> '0') x
            ORDER BY Name;";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlCity()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Distinct Code,name from(Select Code, Name from City_MAST Union all Select City_Code, City_Name from COURIER_TRACKING Where Comp_code= {globalVar.PubCompCode} and City_Name<>'' and City_Code<>'0')x Order by NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlCourierName()
        {
            string query = $@"SELECT DISTINCT COURIER_NAME AS Value, COURIER_NAME AS Text FROM COURIER_TRACKING WHERE COURIER_NAME IS NOT NULL AND COURIER_NAME <> '' ORDER BY COURIER_NAME";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlReceivedBy()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select code,ltrim(rtrim(CODE))+ space(10- LEN (ltrim(rtrim(CODE))))+'|'+SPACE(5)+CAST (NAME as varchar )'NAME' 
            from EMP_MAST Where RESIGN_DATE IS NULL and Comp_code={globalVar.PubCompCode} Order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        public JsonResult GetddlPurpose()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"Select Distinct  Purpose AS Value, Purpose AS Text from COURIER_TRACKING where PURPOSE<>''  Order by Purpose";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlVType()
        {
            string query = $@"Select Code,Name from DOCTYPE_MAST where CODE in ('CTIN','CTOT') order by Name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        public JsonResult GetddlParty()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT DISTINCT Code, Name FROM (SELECT Code, Name FROM SUBGROUP_MAST WHERE Comp_code = {globalVar.PubCompCode}
            AND Nature NOT IN ('CASH', 'BANK', 'OTHERS') AND Name IS NOT NULL AND Name <> '' AND Code IS NOT NULL AND Code <> '0' UNION ALL  SELECT Party_Code AS Code, Party_Name AS Name
            FROM COURIER_TRACKING WHERE Comp_code = {globalVar.PubCompCode} AND Party_Name IS NOT NULL AND Party_Name <> '' AND Party_Code IS NOT NULL AND Party_Code <> '0') x
            ORDER BY Name;";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }
        [HttpPost]
        public JsonResult SaveCourierData([FromBody] CourierTrackingModel model)
        {
            try
            {
                string message = "";
                var globalVar = _globalVariableService.GetGlobalVariables();
                if (model.ACTION?.ToUpper() == "INSERT")
                {
                    using (SqlConnection con = _dbConnection.GetErpConnection())
                    {
                        SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con);
                        cmd.CommandType = CommandType.StoredProcedure;
                        var docID = model.DocType + model.DocNo;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.DocType ?? "");
                        cmd.Parameters.AddWithValue("@V_NO", model.DocNo ?? "");
                        cmd.Parameters.AddWithValue("@V_DATE", model.DocDate);
                        cmd.Parameters.AddWithValue("@DOC_ID", docID);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", model.PartyName ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@CITY_CODE", model.City ?? "");
                        cmd.Parameters.AddWithValue("@CITY_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@COURIER_NAME", model.CourierName ?? "");
                        cmd.Parameters.AddWithValue("@DOCKET_NO", model.DocketNo ?? "");
                        cmd.Parameters.AddWithValue("@RECD_BY", model.ReceivedBy ?? "");
                        cmd.Parameters.AddWithValue("@PURPOSE", model.Purpose ?? "");
                        cmd.Parameters.AddWithValue("@WEIGHT", string.IsNullOrWhiteSpace(model.Weight) ? (object)DBNull.Value : Convert.ToDouble(model.Weight));
                        cmd.Parameters.AddWithValue("@REMARKS", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@Action", "INSERT");

                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }

                    message = "Record inserted successfully.";
                }
                else if (model.ACTION?.ToUpper() == "UPDATE")
                {

                    using (SqlConnection con = _dbConnection.GetErpConnection())
                    {
                        SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.DocType ?? "");
                        cmd.Parameters.AddWithValue("@V_NO", model.V_No);
                        cmd.Parameters.AddWithValue("@V_DATE", model.DocDate);
                        cmd.Parameters.AddWithValue("@DOC_ID", model.DocNo);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", model.PartyName ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@CITY_CODE", model.City ?? "");
                        cmd.Parameters.AddWithValue("@CITY_NAME", DBNull.Value);
                        cmd.Parameters.AddWithValue("@COURIER_NAME", model.CourierName ?? "");
                        cmd.Parameters.AddWithValue("@DOCKET_NO", model.DocketNo ?? "");
                        cmd.Parameters.AddWithValue("@RECD_BY", model.ReceivedBy ?? "");
                        cmd.Parameters.AddWithValue("@PURPOSE", model.Purpose ?? "");
                        cmd.Parameters.AddWithValue("@WEIGHT", string.IsNullOrWhiteSpace(model.Weight) ? (object)DBNull.Value : Convert.ToDouble(model.Weight));
                        cmd.Parameters.AddWithValue("@REMARKS", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);              
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);                    
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);             
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@Action", "UPDATE");

                        con.Open();
                        cmd.ExecuteNonQuery();
                        con.Close();
                    }
                    message = "Record Update successfully.";
                }
                else
                {
                    message = "Unknown action.";
                }
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult GetCourierDataList([FromBody] CodeRequest request)
        {
            GetCourierTrackingModel model = null;
            var global = _globalVariableService.GetGlobalVariables();
            var docid = request.docType + request.docNo;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetID");
                    cmd.Parameters.AddWithValue("@DOC_ID", docid); // V_NO = the doc code like CTIN252600208
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);

                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            model = new GetCourierTrackingModel
                            {
                                VType = rdr["V_TYPE"]?.ToString(),
                                DocDate = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]).ToString("dd/MM/yyyy") : null,
                                DocNo = rdr["DOC_ID"]?.ToString(), 
                                PartyName = rdr["PARTY_CODE"]?.ToString(),
                                City = rdr["CITY_CODE"]?.ToString(),
                                CourierName = rdr["COURIER_NAME"]?.ToString(),
                                DocketNo = rdr["DOCKET_NO"]?.ToString(),
                                ReceivedBy = rdr["RECD_BY"]?.ToString(),
                                Purpose = rdr["PURPOSE"]?.ToString(),
                                Weight = rdr["WEIGHT"]?.ToString(),
                                Remarks = rdr["REMARKS"]?.ToString()
                            };
                        }
                    }
                }
            }

            if (model == null)
            {
                return NotFound(new { message = "No courier data found for the given code." });
            }

            return Json(model);
        }
        public class CodeRequest
        {
            public string docNo { get; set; }
            public string docType { get; set; }
        }

    }
}
