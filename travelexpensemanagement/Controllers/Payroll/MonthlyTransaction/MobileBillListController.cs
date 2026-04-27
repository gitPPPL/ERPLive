using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.MobileBillEntry;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class MobileBillListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;

        public MobileBillListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/MonthlyTransaction/MobileBillList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetAllDocs(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var dataList = new List<MobileBillList>();
            int totalCount = 0;
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())

                using (SqlCommand cmd = new SqlCommand("usp_MobileBillEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    // Required parameters                  
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_type", "MBIL");
                    cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Read paged data
                        int rowindex = 0;
                        while (reader.Read())
                        {
                            dataList.Add(new MobileBillList
                            {
                                DocId = reader["DOC_ID"] != DBNull.Value ? Convert.ToString(reader["DOC_ID"]) : (string?)null,
                                DocNo = reader["V_NO"] != DBNull.Value ? Convert.ToString(reader["V_NO"]) : "",
                                DocDate = reader["V_DATE"]?.ToString(),
                                BillAmount = reader["BILL_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["BILL_AMT"]) : 0,
                                DeductAmount = reader["DEDUCT_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DEDUCT_AMT"]) : 0,
                                Dr_bill_name = reader["DR_AC_NAME"]?.ToString(),
                                Cr_bill_name = reader["CR_AC_NAME"]?.ToString()
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
            return Json(new { data = dataList, totalCount = totalCount });
        }


        //DeleteDocByCode

        [HttpPost]
        public IActionResult DeleteDocByCode(string DocNo)
        {
            try
            {
                // Add logic for deleting a document by code here.
                // For now, returning a placeholder success response.

                string vtype = "MBIL";
                var globalVar = _globalVariableService.GetGlobalVariables();

                string deleteQuery = @"DELETE FROM PAY_MOBILE1 WHERE COMP_CODE = @CompCode AND V_TYPE = @VType AND V_NO = @VNo
                                       AND YEAR_CODE = @YearCode 
                                       DELETE FROM PAY_MOBILE2 WHERE COMP_CODE = @CompCode AND V_TYPE = @VType AND V_NO = @VNo
                                       AND YEAR_CODE = @YearCode ";

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
