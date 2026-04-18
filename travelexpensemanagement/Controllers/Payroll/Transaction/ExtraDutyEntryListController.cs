using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class ExtraDutyEntryListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public ExtraDutyEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/Transaction/ExtraDutyEntryList/Index.cshtml");
        }

        //usp_ExtraDutyEntry

        [HttpGet]
        public IActionResult GetAllDocs(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var dataList = new List<ExtraDutyViewModelList>();
            int totalCount = 0;
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())

                using (SqlCommand cmd = new SqlCommand("usp_ExtraDutyEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    // Required parameters                  
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YearCode",  globalVar.PubFYearCode );
                    cmd.Parameters.AddWithValue("@V_type", "GTED");
                    cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Read paged data
                        int rowindex = 0;
                        while (reader.Read())
                        {
                            dataList.Add(new ExtraDutyViewModelList
                            {
                                Vtype = reader["V_TYPE"] != DBNull.Value ? Convert.ToString(reader["V_TYPE"]) : (string?)null,
                                Vno = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                Vdate = reader["V_DATE"]?.ToString()
                            });

                            if (rowindex == 0) // Assuming total count is same for all rows in the current page
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;

                        }
                        //// Move to next result set for total count
                        //if (reader.NextResult() && reader.Read())
                        //{
                        //    totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
            return Json(new { data = dataList ,  totalCount=  totalCount });
        }


        //DeleteDocByCode

        [HttpPost]
        public IActionResult DeleteDocByCode(string DocNo)
        {
            try
            {
                // Add logic for deleting a document by code here.
                // For now, returning a placeholder success response.

                string vtype = "GTED";
                var globalVar = _globalVariableService.GetGlobalVariables();

                string deleteQuery = @"DELETE FROM PAY_GATEPASS WHERE COMP_CODE = @CompCode AND V_TYPE = @VType AND V_NO = @VNo
                                        AND YEAR_CODE = @YearCode";

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@VType", vtype);
                        cmd.Parameters.AddWithValue("@VNo", DocNo);
                        cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }
                
                return Json(new { success = true, message = "Document deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }
    }
}
