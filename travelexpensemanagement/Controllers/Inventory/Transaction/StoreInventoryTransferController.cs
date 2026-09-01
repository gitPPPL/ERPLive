using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Implementations.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventroy.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class StoreInventoryTransferController : Controller
    {
        private readonly DbHelper _dbHelper;            
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IStoreInventoryTransferRepository _storeInventoryTransferRepository;

        public StoreInventoryTransferController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IStoreInventoryTransferRepository storeInventoryTransferRepository )
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _storeInventoryTransferRepository = storeInventoryTransferRepository;
        }

        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.GlobalVariables = globalVar;
            return View("~/Views/Inventory/Transaction/StoreInventoryTransfer/Index.cshtml");
        }

        [HttpGet]
        public IActionResult DocType()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT Code, Name from DOCTYPE_MAST WHERE DOCTYPE IN ('StoreInvTransfer')";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public JsonResult GenerateVNo(string vType)
        {
            string newV_NO = "00001";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string prefixYRQuery = @"SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";

                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string query = @"
                    SELECT ISNULL(MAX(CAST(RIGHT(CAST(V_NO AS VARCHAR(20)), 5) AS INT)), 0) + 1 FROM ISSUE1 WHERE V_TYPE = @VType AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@VType", vType);
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    int nextNo = Convert.ToInt32(cmd.ExecuteScalar());

                    newV_NO = prefixYR + nextNo.ToString("D5");
                }

                return Json(new
                {
                    v_NO = newV_NO,
                    v_TYPE = vType
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetDropdown(string type)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            string query = "";

            switch (type)
            {
                case "Status":
                    query = $@"Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                    break;

                case "Department":
                    query = $@"Select Code,Name from ITEMDEPT_MAST where comp_code= {globalVariables.PubCompCode} and TRAN_TYPE='Production' Order by Name";
                    break;
            }

            var dropdownList = _dropdownService.GetDropdownList(query);

            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult GetItemList()
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                string query = @"
                    SELECT 
                        a.NAME AS [item name],
                        a.CODE AS [icode],
                        b.NAME AS [unit],
                        b.CODE AS [ucode]
                    FROM ITEM_MAST a
                    LEFT JOIN ITEMUNIT_MAST b 
                        ON a.UNIT_CODE = b.CODE
                        AND b.COMP_CODE = @COMP_CODE
                    INNER JOIN ITEM_MGROUP c 
                        ON a.MGROUP_CODE = c.CODE
                        AND c.COMP_CODE = @COMP_CODE
                        AND c.MGROUP_TYPE IN ('Fuel')
                    WHERE 
                        a.COMP_CODE = @COMP_CODE
                        AND a.ACTIVE = 1
                    GROUP BY 
                        a.NAME,
                        a.CODE,
                        b.NAME,
                        b.CODE
                    ORDER BY 
                        a.NAME";

                var itemList = new List<object>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            itemList.Add(new
                            {
                                itemName = reader["item name"] == DBNull.Value ? "" : reader["item name"].ToString(),
                                icode = reader["icode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["icode"]),
                                unit = reader["unit"] == DBNull.Value ? "" : reader["unit"].ToString(),
                                ucode = reader["ucode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ucode"])
                            });
                        }
                    }
                }

                return Json(new {success = true, data = itemList});
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = ex.Message});
            }
        }

        [HttpGet]
        public IActionResult GetDepartment()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"select code, name from ITEMDEPT_MAST where TRAN_TYPE='Production' and COMP_CODE=1 order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetMakeList(int itemCode)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();
            int compCode = Convert.ToInt32(globalVariables.PubCompCode);
            string query = $@"
            SELECT
                a.MAKE_CODE AS [Mcode],
                b.NAME AS [Make]
            FROM ITEM_MAKE a
            LEFT JOIN ITEMMAKE_MAST b
                ON a.MAKE_CODE = b.CODE
                AND b.COMP_CODE = {compCode}
            WHERE a.ITEM_CODE = {itemCode}
              AND a.COMP_CODE = {compCode}
            ORDER BY b.NAME";

            var makeList = _dropdownService.GetDropdownList(query);

            return Json(makeList);
        }

        [HttpGet]
        public IActionResult GetUnitDetails(int itemCode)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"
                    SELECT 
                        iu.Name AS unitName,
                        iu.Code AS unitCode
                        FROM ITEM_MAST im
                        INNER JOIN ITEMUNIT_MAST iu
                            ON im.UNIT_CODE = iu.CODE
                            AND im.COMP_CODE = iu.COMP_CODE
                    WHERE im.CODE = @ItemCode
                      AND im.COMP_CODE = @CompCode";

                    using SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@CompCode", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    var unitDetails = new List<object>();

                    while (reader.Read())
                    {
                        unitDetails.Add(new
                        {
                            unitName = reader["unitName"] == DBNull.Value ? "" : reader["unitName"].ToString(),
                            unitCode = reader["unitCode"] == DBNull.Value ? "" : reader["unitCode"].ToString()
                        });
                    }

                    return Json(unitDetails);
                }
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

        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateData([FromBody] StoreInventoryTransferModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { success = false, message = "Invalid data." });
                }

                var result = await _storeInventoryTransferRepository.SaveAndUpdateDataAsync(model);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> LoadEditData(string docId)
        {
            var result = await _storeInventoryTransferRepository.LoadEditDataAsync(docId);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetLinkData(DateTime vDate)
        {
            try
            {
                if (vDate == default)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid date."
                    });
                }

                var result = await _storeInventoryTransferRepository.GetLinkDataAsync(vDate);

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

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("ISSUE1", vdate, vtype, vno);
            return Ok(result);
        }

    }   
}
