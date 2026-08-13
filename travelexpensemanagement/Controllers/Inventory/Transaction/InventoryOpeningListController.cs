using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryOpeningListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly IOutwardEntryListRepository _outwardEntryListRepository;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;


        public InventoryOpeningListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DbHelper dbHelper, ModuleService.ModuleService moduleService, IOutwardEntryListRepository outwardEntryListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _outwardEntryListRepository = outwardEntryListRepository;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Material Outward";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();
            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };

            return View("~/Views/Inventory/Transaction/InventoryOpeningList/Index.cshtml" , model);
        }


        [HttpGet]
        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            if (getvariabledata == null)
            {
                return Json(new { success = false, message = "Global variable data is null." });
            }

            int totalCount = 0;
            var headerList = new List<InventoryOpeningEntry_Header>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_InventoryOpening", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getvariabledata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getvariabledata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getvariabledata.PubBranchCode);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            headerList.Add(new InventoryOpeningEntry_Header
                            {
                           
                                NAME = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
                                YEAR_CODE = reader["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["YEAR_CODE"]) : 0,
                                COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                BRANCH_CODE = reader["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BRANCH_CODE"]) : 0,
                                V_TYPE = reader["Vouchertype"] != DBNull.Value ? reader["Vouchertype"].ToString() : string.Empty,
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                DOC_ID = reader["DOC_ID"] != DBNull.Value ? reader["DOC_ID"].ToString() : string.Empty,
                                SHIFT = reader["SHIFT"] != DBNull.Value ? reader["SHIFT"].ToString() : string.Empty,
                                SLIP_NO = reader["SLIP_NO"] != DBNull.Value ? reader["SLIP_NO"].ToString() : string.Empty,
                                PORD_TYPE = reader["PORD_TYPE"] != DBNull.Value ? reader["PORD_TYPE"].ToString() : string.Empty,
                                PORD_NO = reader["PORD_NO"] != DBNull.Value ? Convert.ToInt32(reader["PORD_NO"]) : 0,
                                PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : 0,
                                EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                                DEPT_CODE = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0,
                                REMARKS = reader["REMARKS"] != DBNull.Value ? reader["REMARKS"].ToString() : string.Empty,
                                CONS_TYPE = reader["CONS_TYPE"] != DBNull.Value ? reader["CONS_TYPE"].ToString() : string.Empty,
                                STATUS = reader["STATUS"] != DBNull.Value ? Convert.ToInt32(reader["STATUS"]) : 0,
                                AMOUNT = reader["STATUS"] != DBNull.Value ? Convert.ToDecimal(reader["STATUS"]) : 0,
                                PLAN_TYPE = reader["PLAN_TYPE"] != DBNull.Value ? reader["PLAN_TYPE"].ToString() : string.Empty,
                                PLAN_NO = reader["PLAN_NO"] != DBNull.Value ? Convert.ToInt32(reader["PLAN_NO"]) : 0,
                                FAPROV_STATUS = reader["FAPROV_STATUS"] != DBNull.Value ? reader["FAPROV_STATUS"].ToString() : string.Empty,
                                FAPROV_REMARKS = reader["FAPROV_REMARKS"] != DBNull.Value ? reader["FAPROV_REMARKS"].ToString() : string.Empty

                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data.", error = ex.Message });
            }


            return Json(new { success = true, lists = headerList, totalCount });
        }


    }
}
