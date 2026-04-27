using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class BankMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public BankMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/FinancialAccounting/Master/BankMaster/Index.cshtml");
        }
        public IActionResult GetBankTypeList()
        {
            var types = new List<string>();
            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT TYPE FROM BANK_MAST WHERE TYPE <> '' AND TYPE IS NOT NULL ORDER BY TYPE", conn))
                {
                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            types.Add(reader["TYPE"].ToString());
                        }
                    }
                }
            }
            return Json(types);
        }
        [HttpPost]
        public async Task<IActionResult> SaveBank([FromBody] BANK_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            var result = await SaveOrUpdateBank(model, action);
            if (result.StartsWith("Error"))
            {
                return Json(new { success = false, message = result });
            }
            return Json(new { success = true, message = result });
        }
        public async Task<string> SaveOrUpdateBank(BANK_MAST bank, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    if (action == "INSERT")
                    {
                        using (SqlCommand checkCmd = new SqlCommand(
                            "SELECT COUNT(*) FROM BANK_MAST WHERE NAME = @NAME", con))
                        {
                            checkCmd.Parameters.AddWithValue("@NAME", bank.NAME ?? "");
                            await con.OpenAsync();
                            int exists = (int)await checkCmd.ExecuteScalarAsync();
                            con.Close();

                            if (exists > 0)
                                return "Error: Bank Name already exists.";
                        }
                    }
                    using (SqlCommand cmd = new SqlCommand("sp_BANK_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", bank.CODE);
                        cmd.Parameters.AddWithValue("@NAME", bank.NAME ?? "");
                        cmd.Parameters.AddWithValue("@SHORTNAME", bank.SHORTNAME ?? "");
                        cmd.Parameters.AddWithValue("@TYPE", bank.TYPE ?? "");
                        cmd.Parameters.AddWithValue("@REPL_CODE", bank.REPL_CODE);
                        cmd.Parameters.AddWithValue("@ACTIVE", bank.ACTIVE);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", bank.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        return action == "INSERT" ? "Bank saved successfully!" : "Update Bank successfully!";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
    }
}
 