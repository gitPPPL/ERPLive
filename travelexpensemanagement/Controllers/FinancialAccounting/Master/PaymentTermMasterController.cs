using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class PaymentTermMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public PaymentTermMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/FinancialAccounting/Master/PaymentTermMaster/Index.cshtml");
        }
        public IActionResult GetCreditTypeList()
        {
            var types = new List<string>();

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT CREDIT_TYPE FROM PAYTERM_MAST WHERE CREDIT_TYPE <> '' AND CREDIT_TYPE IS NOT NULL ORDER BY CREDIT_TYPE\r\n", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["CREDIT_TYPE"].ToString());
                        }
                    }
                }
            }

            return Json(types);
        }
        [HttpPost]
        public async Task<IActionResult> SavePaymentTerm([FromBody] PAYTERM_MAST model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid model." });

            string action = model.ACTION?.ToUpper() == "INSERT" ? "INSERT" : "UPDATE";
            var result = await SaveOrUpdatePaymentTermAsync(model, action);

            if (result == "Success")
                return Ok(new { success = true });
            else
                return BadRequest(new { success = false, message = result });
        }
        public async Task<string> SaveOrUpdatePaymentTermAsync(PAYTERM_MAST paymentTerm, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    int exists = 0;
                    // Check for duplicate NAME
                    using (SqlCommand checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM PAYTERM_MAST WHERE COMP_CODE = @COMP_CODE AND NAME = @NAME", con))
                    {
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@NAME", _dbHelper.Xnull(paymentTerm.NAME));

                        await con.OpenAsync();
                        exists = (int)await checkCmd.ExecuteScalarAsync();
                        await con.CloseAsync();
                    }
                    if (exists > 0 && action == "INSERT")
                    {
                        return "Name already exists!";
                    }
                    // Call stored procedure to insert/update
                    using (SqlCommand cmd = new SqlCommand("sp_PAYTERM_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", paymentTerm.CODE);
                        cmd.Parameters.AddWithValue("@NAME", paymentTerm.NAME ?? "");
                        cmd.Parameters.AddWithValue("@SHORTNAME", paymentTerm.SHORTNAME ?? "");
                        cmd.Parameters.AddWithValue("@DUEBASEON", paymentTerm.DUEBASEON ?? "");
                        cmd.Parameters.AddWithValue("@DAY_PLUS", paymentTerm.DAY_PLUS);
                        cmd.Parameters.AddWithValue("@TOLRENCEDAY", paymentTerm.TOLRENCEDAY);
                        cmd.Parameters.AddWithValue("@DAY_INT", paymentTerm.DAY_INT);
                        cmd.Parameters.AddWithValue("@CREDIT_TYPE", paymentTerm.CREDIT_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", paymentTerm.Active);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", paymentTerm.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        await con.CloseAsync();

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
        [HttpPost]
        public JsonResult DeletePaymentTermByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAYTERM_MAST", con)) // Replace with your SP name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting record", error = ex.Message });
            }
        }

    }
} 
 