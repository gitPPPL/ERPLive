using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventroy.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryConsumptionListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly IInventroyConsumptionEntryListRepository _inventoryConsumptionEntryListRepository;
        public InventoryConsumptionListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService , IInventroyConsumptionEntryListRepository inventoryConsumptionEntryListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
            _inventoryConsumptionEntryListRepository = inventoryConsumptionEntryListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/Inventory/Transaction/InventoryConsumptionList/Index.cshtml");
        }

        //[HttpGet]
        //public IActionResult LoadListData(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        //{
        //    try
        //    {
        //        var globalVariables = _globalVariableService.GetGlobalVariables();
        //        int totalCount = 0;
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();

        //            using SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry",con);

        //            cmd.CommandType = CommandType.StoredProcedure;

        //            cmd.Parameters.AddWithValue("@YEAR_CODE",globalVariables.PubFYearCode);
        //            cmd.Parameters.AddWithValue("@COMP_CODE",globalVariables.PubCompCode);
        //            cmd.Parameters.AddWithValue("@BRANCH_CODE",globalVariables.PubBranchCode);
        //            cmd.Parameters.AddWithValue("@PageNo",pageNo);
        //            cmd.Parameters.AddWithValue("@PageSize",pageSize);
        //            cmd.Parameters.AddWithValue("@SearchTerm",searchTerm ?? "");
        //            cmd.Parameters.AddWithValue("@Action","ListData");
        //            using SqlDataReader reader = cmd.ExecuteReader();

        //            var list = new List<object>();

        //            while (reader.Read())
        //            {
        //                if (totalCount == 0 && reader["TotalCount"] != DBNull.Value)
        //                {
        //                    totalCount = Convert.ToInt32(reader["TotalCount"]);
        //                }

        //                list.Add(new
        //                {
        //                    vType = reader["V_TYPE"] == DBNull.Value? "" : reader["V_TYPE"].ToString(),
        //                    vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
        //                    vDate = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
        //                    docId = reader["DOC_ID"] == DBNull.Value ? "" : reader["DOC_ID"].ToString(),
        //                    empCode = reader["EMP_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["EMP_CODE"]),
        //                    empName = reader["EMP_NAME"] == DBNull.Value ? "" : reader["EMP_NAME"].ToString(),
        //                    remarks = reader["REMARKS"] == DBNull.Value ? "" : reader["REMARKS"].ToString()
        //                });
        //            }

        //            return Json(new { success = true, data = list, pageNo = pageNo, pageSize = pageSize, totalCount = totalCount, hasMore = (pageNo * pageSize) < totalCount });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> LoadListData(string searchTerm = "", int pageNo = 1,int pageSize = 20)
        {
            try
            {
                var result = await _inventoryConsumptionEntryListRepository.LoadListDataAsync(searchTerm, pageNo, pageSize);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = ex.Message});
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteData(string docId)
        {
            try
            {
                var result =await _inventoryConsumptionEntryListRepository.DeleteDataAsync(docId);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //[HttpPost]
        //public IActionResult DeleteData([FromBody] string docId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(docId))
        //        {
        //            return Json(new {success = false, message = "Document ID is required."});
        //        }

        //        var globalVariables = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();

        //            using (SqlTransaction tran = con.BeginTransaction())
        //            {
        //                try
        //                {
        //                    using (SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry", con, tran))
        //                    {
        //                        cmd.CommandType = CommandType.StoredProcedure;

        //                        cmd.Parameters.AddWithValue("@Action", "DeleteData");

        //                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode );
        //                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
        //                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
        //                        cmd.Parameters.AddWithValue("@DOC_ID", docId);

        //                        cmd.ExecuteNonQuery();
        //                    }

        //                    tran.Commit();

        //                    return Json(new {success = true, message = "Data deleted successfully."});
        //                }
        //                catch (Exception ex)
        //                {
        //                    tran.Rollback();

        //                    return Json(new {success = false, message = ex.Message});
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new {success = false, message = ex.Message});
        //    }
        //}

    }
}
