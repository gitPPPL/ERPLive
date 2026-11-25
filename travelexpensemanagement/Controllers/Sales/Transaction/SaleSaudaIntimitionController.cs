using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SaleSaudaIntimitionController : Controller
    {



        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly ISalesInVoice _salesInVoice;

        public SaleSaudaIntimitionController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
       ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, ISalesInVoice salesInVoice)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _salesInVoice = salesInVoice;
        }

        public async Task<IActionResult> Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            var loadGeneralSetting = await _globalVariableService.LoadGeneralSetting();
            ViewBag.LoadGeneralSetting = loadGeneralSetting;
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/Sales/Transaction/SaleSaudaIntimition/Index.cshtml");

        }

        public List<SaleSaudaIntimitionModel> GetSaleSaudaIntimitionData()
        {
            var list = new List<SaleSaudaIntimitionModel>();

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SaleSaudaIntimition", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@ServerDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Action", "ShowData");

                        con.Open();

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                list.Add(new SaleSaudaIntimitionModel
                                {
                                    V_DATE = dr["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(dr["V_DATE"]),
                                    V_TYPE = dr["V_TYPE"]?.ToString(),
                                    V_NO = dr["V_NO"] == DBNull.Value ? null : Convert.ToInt32(dr["V_NO"]),
                                    PARTYNAME = dr["PARTYNAME"]?.ToString(),
                                    ITEMNAME = dr["ITEMNAME"]?.ToString(),
                                    QTY = dr["QTY"] == DBNull.Value ? null : Convert.ToDecimal(dr["QTY"]),
                                    RATE = dr["RATE"] == DBNull.Value ? null : Convert.ToDecimal(dr["RATE"]),
                                    REMARK = dr["REMARK"]?.ToString(),
                                    DOC_ID = dr["DOC_ID"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching Sale Sauda Intimation data: " + ex.Message, ex);
            }

            return list;
        }

        [HttpGet]
        public JsonResult GetData()
        {
            try
            {
                var data = GetSaleSaudaIntimitionData();
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class SaleSaudaIntimitionModel
        {
            public DateTime? V_DATE { get; set; }
            public string? V_TYPE { get; set; }
            public int? V_NO { get; set; }
            public string? PARTYNAME { get; set; }
            public string? ITEMNAME { get; set; }
            public decimal? QTY { get; set; }
            public decimal? RATE { get; set; }
            public string? REMARK { get; set; }
            public string? DOC_ID { get; set; }
        }

        public JsonResult GetSaleSaudaIntimitionData(int V_NO, string V_TYPE)
        {
            var list = new List<SaleSaudaIntimitionModel>();

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SaleSaudaIntimition", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", V_TYPE);
                        cmd.Parameters.AddWithValue("@ServerDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Action", "UpdateData");

                        con.Open();

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, data = list, message = "Priority updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, data = list, message = ex.Message });
            }
        }

        public JsonResult UPDATEDATA(int V_NO, string V_TYPE)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SaleSaudaIntimition", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", V_TYPE);
                        cmd.Parameters.AddWithValue("@Action", "UpdateData");

                        con.Open();

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return Json(new { success = true, message = "Priority updated successfully." });



                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error updating Sale Sauda Intimation: " + ex.Message
                });
            }
        }


    }
}
