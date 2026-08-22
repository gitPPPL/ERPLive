using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportPaymentListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IImportPaymentListRepository _importPaymentListRepository;
        public ImportPaymentListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IImportPaymentListRepository importPaymentListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _importPaymentListRepository = importPaymentListRepository; 
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportPaymentList/Index.cshtml");
        }

        //[HttpGet]
        //public JsonResult LoadListData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        //{
        //    try
        //    {
        //        var globalData = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        using (SqlCommand cmd = new SqlCommand("sp_ImportPaymentEntry", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            cmd.Parameters.AddWithValue("@COMP_CODE", globalData.PubCompCode);
        //            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalData.PubBranchCode);
        //            cmd.Parameters.AddWithValue("@YEAR_CODE", globalData.PubFYearCode);
        //            cmd.Parameters.AddWithValue("@Action", "LoadListData");
        //            cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm.Trim());
        //            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        //            cmd.Parameters.AddWithValue("@PageSize", pageSize);
        //            con.Open();

        //            var data = new List<object>();
        //            int totalCount = 0;

        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    totalCount = reader["TotalRecords"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalRecords"]);

        //                    data.Add(new
        //                    {
        //                        vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
        //                        vType = reader["V_TYPE"] == DBNull.Value ? "" : reader["V_TYPE"].ToString(),
        //                        vDate = reader["V_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["V_DATE"]),
        //                        docId = reader["DOC_ID"] == DBNull.Value ? "" : reader["DOC_ID"].ToString(),
        //                    });
        //                }
        //            }
        //            return Json(new { success = true, data = data, totalCount = totalCount });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error while loading Import Payment List.", data = new List<object>(), totalCount = 0 });
        //    }
        //}

        [HttpGet]
        public async Task<JsonResult> LoadListData(string searchTerm = "", int pageNumber = 1,int pageSize = 10)
        {
            try
            {
                var result = await _importPaymentListRepository.LoadListDataAsync(searchTerm, pageNumber, pageSize);

                return Json(new {success = true, data = result.Data, totalCount = result.TotalCount});

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, data = new List<object>(), totalCount = 0});
            }
        }

        //[HttpPost]
        //public JsonResult DeleteImportPaymentEntry(string docId)
        //{   
        //    try
        //    {
        //        var globalData = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        using (SqlCommand cmd = new SqlCommand("sp_ImportPaymentEntry", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            cmd.Parameters.AddWithValue("@COMP_CODE", globalData.PubCompCode);
        //            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalData.PubBranchCode);
        //            cmd.Parameters.AddWithValue("@YEAR_CODE", globalData.PubFYearCode);
        //            cmd.Parameters.AddWithValue("@Action", "DeleteRecord");
        //            cmd.Parameters.AddWithValue("@DOC_ID", docId);

        //            con.Open();
        //            cmd.ExecuteNonQuery();
        //        }

        //        return Json(new { success = true, message = "Import Payment Entry deleted successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<JsonResult> DeleteImportPaymentEntry(string docId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docId))
                {
                    return Json(new { success = false, message = "Document ID is required."});
                }

                var result = await _importPaymentListRepository.DeleteImportPaymentEntryAsync(docId);

                return Json(new { success = result.Success, message = result.Message});
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
