using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Spire.Doc.Pages;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Inventroy.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryConsumptionEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        private readonly IInventoryConsumptionEntryRepository _inventoryConsumptionEntryRepository;
        
        public InventoryConsumptionEntryController(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, IInventoryConsumptionEntryRepository inventoryConsumptionEntryRepository)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
            _inventoryConsumptionEntryRepository = inventoryConsumptionEntryRepository;
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
            return View("~/Views/Inventory/Transaction/InventoryConsumptionEntry/Index.cshtml");
        }
        
        [HttpGet]
        public IActionResult DocType()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT Code, Name FROM DOCTYPE_MAST WHERE DOCTYPE IN ('GoodsIssue') order by name";
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
                case "Place":
                    query = $@"SELECT Code, Name 
                       FROM PLACE_MAST 
                       WHERE Comp_Code = {globalVariables.PubCompCode} 
                       ORDER BY Code";
                break;

                case "HOD":
                    query = $@"select distinct b.code,b.name as EMP_NAME from EMP_MAST b where type in ('STAFF') and b.RESIGN_DATE is null and b.COMP_CODE= {globalVariables.PubCompCode}
                    order by name";
                break;
            }

            var dropdownList = _dropdownService.GetDropdownList(query);

            return Json(dropdownList);
        }

        [HttpGet]
        public IActionResult GetComplainNo()
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"
                    SELECT 
                        a.V_Type,
                        a.V_No AS VNo,
                        b.Name AS DeptName,
                        c.Name AS Fault,
                        d.Name AS Machine,
                        FORMAT(a.V_Date, 'dd/MM/yyyy') AS ComplainDate
                    FROM PM_MAINTENANCEPLAN a

                    LEFT JOIN ItemDept_Mast b 
                        ON a.Dept_code = b.Code 
                        AND a.Comp_code = b.Comp_code

                    LEFT JOIN Falt_Mast c 
                        ON a.Fault_code = c.Code 
                        AND a.Comp_code = c.Comp_code

                    LEFT JOIN Machine_Mast d 
                        ON a.Mach_code = d.Code 
                        AND a.Comp_code = d.Comp_code

                    WHERE a.Comp_code = @CompCode
                      AND a.Branch_code = @BranchCode
                      AND a.Year_code = @YearCode
                      AND a.V_Type = 'PMCP'

                    ORDER BY a.V_No DESC";

                    using SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@CompCode", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", globalVariables.PubFYearCode);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    var complainNumbers = new List<object>();

                    while (reader.Read())
                    {
                        complainNumbers.Add(new
                        {
                            V_Type = reader["V_Type"].ToString(),
                            VNo = reader["VNo"].ToString(),
                            DeptName = reader["DeptName"].ToString(),
                            Fault = reader["Fault"].ToString(),
                            Machine = reader["Machine"].ToString(),
                            ComplainDate = reader["ComplainDate"].ToString()
                        });
                    }

                    return Json(complainNumbers);
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

        [HttpGet]   
        public IActionResult SearchItems(string search = "", int page = 1)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                const int pageSize = 110;
                int offset = (page - 1) * pageSize;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"
                        SELECT
                            ITEM_MAST.NAME,
                            ITEM_MAST.CODE,
                            ITEM_MAST.UNIT_NAME,
                            ITEM_MAST.UNIT_CODE,
                            (CONVERT(VARCHAR, ITEM_MAST.CODE) 
                                + RTRIM(LTRIM(ITEM_MAST.NAME))) AS CODENM,
                            ITEM_MAST.CATLOG,
                            ITEM_MAST.DRAWING,
                            STKBAL.QTY AS CurStk

                        FROM ITEM_MAST

                        LEFT JOIN tmpStockBalance STKBAL
                            ON ITEM_MAST.CODE = STKBAL.ITEM_CODE
                            AND ITEM_MAST.COMP_CODE = STKBAL.COMP_CODE

                        INNER JOIN ITEM_MGROUP
                            ON ITEM_MGROUP.CODE = ITEM_MAST.MGROUP_CODE
                            AND ITEM_MGROUP.COMP_CODE = ITEM_MAST.COMP_CODE

                        WHERE
                            ITEM_MGROUP.MGROUP_TYPE IN ('Store', 'Fuel')
                            AND ITEM_MAST.COMP_CODE = @COMP_CODE
                            AND ITEM_MAST.ACTIVE = 1
                            AND
                            (
                                @SEARCH = ''
                                OR ITEM_MAST.NAME LIKE '%' + @SEARCH + '%'
                                OR CONVERT(VARCHAR, ITEM_MAST.CODE) LIKE '%' + @SEARCH + '%'
                            )

                        ORDER BY ITEM_MAST.NAME

                        OFFSET @OFFSET ROWS
                        FETCH NEXT @PAGE_SIZE ROWS ONLY";

                    using SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@SEARCH", search ?? "");
                    cmd.Parameters.AddWithValue("@OFFSET", offset);
                    cmd.Parameters.AddWithValue("@PAGE_SIZE", pageSize);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    var items = new List<object>();

                    while (reader.Read())
                    {
                        items.Add(new
                        {
                            name = reader["NAME"] == DBNull.Value ? "": reader["NAME"].ToString(),
                            code = reader["CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CODE"]),
                            unitName = reader["UNIT_NAME"] == DBNull.Value ? "" : reader["UNIT_NAME"].ToString(),
                            unitCode = reader["UNIT_CODE"] == DBNull.Value ? "" : reader["UNIT_CODE"].ToString(),
                            codeNm = reader["CODENM"] == DBNull.Value ? "" : reader["CODENM"].ToString(),
                            catlog = reader["CATLOG"] == DBNull.Value ? "" : reader["CATLOG"].ToString(),
                            drawing = reader["DRAWING"] == DBNull.Value ? "" : reader["DRAWING"].ToString(),
                            curStk = reader["CurStk"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CurStk"])
                        });
                    }

                    return Json(new {items = items, hasMore = items.Count == pageSize });
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

        [HttpGet]
        public IActionResult GetItemByCode(int itemCode)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"
                    SELECT TOP 1
                        ITEM_MAST.NAME,
                        ITEM_MAST.CODE,
                        ITEM_MAST.UNIT_NAME,
                        ITEM_MAST.UNIT_CODE,
                        (CONVERT(VARCHAR, ITEM_MAST.CODE) 
                            + RTRIM(LTRIM(ITEM_MAST.NAME))) AS CODENM,
                        ITEM_MAST.CATLOG,
                        ITEM_MAST.DRAWING,
                        STKBAL.QTY AS CurStk
                    FROM ITEM_MAST
                    LEFT JOIN tmpStockBalance STKBAL
                        ON ITEM_MAST.CODE = STKBAL.ITEM_CODE
                        AND ITEM_MAST.COMP_CODE = STKBAL.COMP_CODE
                    INNER JOIN ITEM_MGROUP
                        ON ITEM_MGROUP.CODE = ITEM_MAST.MGROUP_CODE
                        AND ITEM_MGROUP.COMP_CODE = ITEM_MAST.COMP_CODE
                    WHERE
                        ITEM_MAST.CODE = @ITEM_CODE
                        AND ITEM_MGROUP.MGROUP_TYPE IN ('Store', 'Fuel')
                        AND ITEM_MAST.COMP_CODE = @COMP_CODE
                        AND ITEM_MAST.ACTIVE = 1";

                    using SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE",globalVariables.PubCompCode);
                    using SqlDataReader reader = cmd.ExecuteReader();

                    if (!reader.Read())
                    {
                        return Json(new {success = false, message = "Invalid Item Code."});
                    }

                    var item = new
                    {
                        name = reader["NAME"] == DBNull.Value ? "" : reader["NAME"].ToString(),
                        code = reader["CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CODE"]),
                        unitName = reader["UNIT_NAME"] == DBNull.Value ? "" : reader["UNIT_NAME"].ToString(),
                        unitCode = reader["UNIT_CODE"] == DBNull.Value ? "" : reader["UNIT_CODE"].ToString(),
                        codeNm = reader["CODENM"] == DBNull.Value ? "" : reader["CODENM"].ToString(),
                        catlog = reader["CATLOG"] == DBNull.Value ? "" : reader["CATLOG"].ToString(),
                        drawing = reader["DRAWING"] == DBNull.Value ? "" : reader["DRAWING"].ToString(),
                        curStk = reader["CurStk"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CurStk"])
                    };

                    return Json(new {success = true,item = item});
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

        [HttpGet]
        public IActionResult GetDepartmentDetails()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"select name,code from ITEMDEPT_MAST where TRAN_TYPE='STORE' and COMP_CODE= {globalVaribales.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetMachineDetails()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"select name,code from MACHINE_MAST where TYPE='STORE' and ACTIVE=1 AND COMP_CODE= {globalVaribales.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetCostCatDetails()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"select name,code,COSTCODE from COSTCAT_MAST where ACTIVE=1 and COMP_CODE= {globalVaribales.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetCostSubCatDetails()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"select name,code,COSTCODE from COSTSUBCAT_MAST where ACTIVE=1 AND COMP_CODE= {globalVaribales.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetCostCenterDetails()
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            string query = $@"select name,code,COSTCODE from COSTCENTER_MAST where ACTIVE=1 AND COMP_CODE= {globalVaribales.PubCompCode} order by name";
            var moduleList = _dropdownService.GetDropdownList(query);
            return Json(moduleList);
        }

        [HttpGet]
        public IActionResult GetMakeDetails(int itemCode)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string query = @"
                    SELECT 
                        b.NAME AS Make,
                        a.MAKE_CODE AS Mcode
                    FROM ITEM_MAKE a
                    LEFT JOIN ITEMMAKE_MAST b 
                        ON a.MAKE_CODE = b.CODE
                        AND b.COMP_CODE = @CompCode
                    WHERE a.ITEM_CODE = @ItemCode
                      AND a.COMP_CODE = @CompCode
                    ORDER BY b.NAME";

                    using SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@CompCode", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@ItemCode", itemCode);

                    using SqlDataReader reader = cmd.ExecuteReader();

                    var makeDetails = new List<object>();

                    while (reader.Read())
                    {
                        makeDetails.Add(new
                        {
                            make = reader["Make"] == DBNull.Value ? "" : reader["Make"].ToString(),
                            mcode = reader["Mcode"] == DBNull.Value ? "" : reader["Mcode"].ToString()
                        });
                    }

                    return Json(makeDetails);
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

        //[HttpPost]
        //public IActionResult SaveAndUpdateData([FromBody] InventoryConsumptionEntryModel model)
        //{
        //    var globalVariable = _globalVariableService.GetGlobalVariables();
        //    var docId = model.V_TYPE + model.V_NO;

        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        con.Open();

        //        using (SqlTransaction tran = con.BeginTransaction())
        //        {
        //            try
        //            {
        //                if (!ValidateConsumptionStock(con, tran, model, out string validationMessage))
        //                {
        //                    tran.Rollback();
        //                    return Json(new { success = false, message = validationMessage });
        //                }

        //                bool isUpdate;

        //                using (SqlCommand checkCmd = new SqlCommand(@"
        //             SELECT COUNT(1)
        //             FROM ISSUE1
        //             WHERE YEAR_CODE = @YEAR_CODE
        //               AND COMP_CODE = @COMP_CODE
        //               AND BRANCH_CODE = @BRANCH_CODE
        //               AND DOC_ID = @DOC_ID", con, tran))
        //                {
        //                    checkCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
        //                    checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
        //                    checkCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
        //                    checkCmd.Parameters.AddWithValue("@DOC_ID", docId);
        //                    isUpdate = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
        //                }

        //                using (SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry", con, tran))
        //                {
        //                    cmd.CommandType = CommandType.StoredProcedure;

        //                    cmd.Parameters.AddWithValue("@Action", isUpdate ? "UpdateHeader" : "InsertHeader");
        
        //                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
        //                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
        //                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);          
        //                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)model.V_TYPE ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
        //                    cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@SHIFT", (object?)model.SHIFT ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@SLIP_NO", (object?)model.SLIP_NO ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@PLACE_CODE", (object?)model.PLACE_CODE ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@EMP_CODE", (object?)model.EMP_CODE ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@REMARKS", (object?)model.REMARKS ?? DBNull.Value);
        //                    if (isUpdate)
        //                    {
        //                        cmd.Parameters.AddWithValue("@EUSER",globalVariable.PubUserId);
        //                    }
        //                    else
        //                    {
        //                        cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
        //                    }
        //                    cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
        //                    cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
        //                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

        //                    cmd.ExecuteNonQuery();
        //                }

        //                if (isUpdate)
        //                {
        //                    // ---------------------------------------------
        //                    // Delete old footer
        //                    // ---------------------------------------------
        //                    using (SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry", con, tran))
        //                    {
        //                        cmd.CommandType = CommandType.StoredProcedure;

        //                        cmd.Parameters.AddWithValue( "@Action", "DeleteFooter");
        //                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
        //                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
        //                        cmd.Parameters.AddWithValue("@BRANCH_CODE",globalVariable.PubBranchCode);
        //                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
        //                        //cmd.ExecuteNonQuery();

        //                        var deletedRows = Convert.ToInt32(cmd.ExecuteScalar());

        //                        Console.WriteLine("Deleted Footer Rows: " + deletedRows);

        //                    }
        //                }

        //                if (model.InventryConsumptionFooter != null && model.InventryConsumptionFooter.Count > 0)
        //                {
        //                    foreach (var item in model.InventryConsumptionFooter)
        //                    {
        //                        using (SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry", con, tran))
        //                        {
        //                            cmd.CommandType = CommandType.StoredProcedure;

        //                            cmd.Parameters.AddWithValue("@Action", "InsertFooter");

        //                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
        //                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
        //                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
        //                            cmd.Parameters.AddWithValue("@V_TYPE", (object?)model.V_TYPE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@DOC_ID", docId);
        //                            cmd.Parameters.AddWithValue("@SHIFT", (object?)model.SHIFT ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@ITEM_NAME", (object?)item.ITEM_NAME ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@MAKE_CODE", (object?)item.MAKE_CODE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@UOM_CODE", (object?)item.UOM_CODE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@UOM_NAME", (object?)item.UOM_NAME ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@TO_DEPT", (object?)item.TO_DEPT ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@NOS", (object?)item.NOS ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@QTY", (object?)item.QTY ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@RATE", (object?)item.RATE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@AMOUNT", (object?)item.AMOUNT ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@LAND_RATE", (object?)item.LAND_RATE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@LAND_AMT", (object?)item.LAND_AMT ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@KANTA_TYPE", (object?)item.KANTA_TYPE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@KANTA_NO", (object?)item.KANTA_NO ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@REQ_TYPE", (object?)item.REQ_TYPE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@REQ_NO", (object?)item.REQ_NO ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@MACH_CODE", (object?)item.MACH_CODE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@FREMARKS", (object?)item.FREMARKS ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@COSTCAT_CODE", (object?)item.COSTCAT_CODE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@COSTSCAT_CODE", (object?)item.COSTSCAT_CODE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@COSTCENTER_CODE", (object?)item.COSTCENTER_CODE ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
        //                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
        //                            cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
        //                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

        //                            cmd.ExecuteNonQuery();
        //                        }
        //                    }
        //                }
        //                tran.Commit();

        //                return Json(new {success = true, message = isUpdate ? "Data updated successfully." : "Data saved successfully."});
        //            }
        //            catch (Exception ex)
        //            {
        //                tran.Rollback();
        //                return Json(new { success = false, message = ex.Message });
        //            }
        //        }
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SaveAndUpdateData([FromBody] InventoryConsumptionEntryModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new {success = false, message = "Invalid data."});
                }

                var result =await _inventoryConsumptionEntryRepository.SaveAndUpdateDataAsync(model);

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new {success = false, message = ex.Message});
            }
        }

        //[HttpGet]
        //public IActionResult LoadEditData(string docId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(docId))
        //        {
        //            return Json(new {success = false, message = "Document Id is required."});
        //        }

        //        var globalVariables = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();

        //            using SqlCommand cmd = new SqlCommand("sp_ConsumptionEntry",con);
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            cmd.Parameters.AddWithValue("@YEAR_CODE",globalVariables.PubFYearCode);
        //            cmd.Parameters.AddWithValue("@COMP_CODE",globalVariables.PubCompCode);
        //            cmd.Parameters.AddWithValue("@BRANCH_CODE",globalVariables.PubBranchCode);
        //            cmd.Parameters.AddWithValue("@DOC_ID",docId);
        //            cmd.Parameters.AddWithValue("@Action","LoadEditData");

        //            using SqlDataReader reader = cmd.ExecuteReader();

        //            // =========================
        //            // HEADER
        //            // =========================

        //            var header = new Dictionary<string, object?>();

        //            if (reader.Read())
        //            {
        //                for (int i = 0; i < reader.FieldCount; i++)
        //                {
        //                    header[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        //                }
        //            }

        //            // =========================
        //            // FOOTER
        //            // =========================

        //            var footer = new List<Dictionary<string, object?>>();

        //            if (reader.NextResult())
        //            {
        //                while (reader.Read())
        //                {
        //                    var row = new Dictionary<string, object?>();

        //                    for (int i = 0; i < reader.FieldCount; i++)
        //                    {
        //                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        //                    }

        //                    footer.Add(row);
        //                }
        //            }

        //            return Json(new {success = true, header = header, footer = footer});
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new {success = false, message = ex.Message});
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> LoadEditData(string docId)
        {
            var result =await _inventoryConsumptionEntryRepository.LoadEditDataAsync(docId);
            return Json(result);
        }

        //[HttpGet]
        //public IActionResult GetCapitalItemData(DateTime vDate)
        //{
        //    try
        //    {
        //        var globalVariables = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();

        //            string query = @"
        //            SELECT
        //                DOC_ID,
        //                V_DATE AS [Date],
        //                ITEM_NAME AS [ItemName],
        //                NET_WGT AS [Qty],
        //                TO_NAME AS [ToPlace],
        //                WEIGHT AS [GrossWt],
        //                TARE_WGT AS [TareWt],
        //                Remarks
        //            FROM WB2
        //            WHERE V_TYPE = 'KSOT'
        //              AND COMP_CODE = @CompCode
        //              AND BRANCH_CODE = @BranchCode
        //              AND YEAR_CODE = @YearCode
        //              AND V_DATE = @VDate
        //            ORDER BY V_DATE DESC, V_NO DESC";

        //            using SqlCommand cmd = new SqlCommand(query, con);

        //            cmd.Parameters.AddWithValue("@CompCode", globalVariables.PubCompCode);
        //            cmd.Parameters.AddWithValue("@BranchCode", globalVariables.PubBranchCode);
        //            cmd.Parameters.AddWithValue("@YearCode", globalVariables.PubFYearCode);
        //            cmd.Parameters.AddWithValue("@VDate", vDate.Date);

        //            using SqlDataReader reader = cmd.ExecuteReader();

        //            var result = new List<object>();

        //            while (reader.Read())
        //            {
        //                result.Add(new
        //                {
        //                    docId = reader["DOC_ID"]?.ToString(),
        //                    date = reader["Date"] == DBNull.Value ? "" : Convert.ToDateTime(reader["Date"]).ToString("dd-MM-yyyy"),
        //                    itemName = reader["ItemName"]?.ToString(),
        //                    qty = reader["Qty"] == DBNull.Value ? "" : reader["Qty"],
        //                    toPlace = reader["ToPlace"]?.ToString(),
        //                    grossWt = reader["GrossWt"] == DBNull.Value ? "" : reader["GrossWt"],
        //                    tareWt = reader["TareWt"] == DBNull.Value ? "" : reader["TareWt"],
        //                    remarks = reader["Remarks"]?.ToString()
        //                });
        //            }

        //            return Json(result);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> GetCapitalItemData(DateTime vDate)
        {
            var result =await _inventoryConsumptionEntryRepository.GetCapitalItemDataAsync(vDate);
            return Json(result);
        }

        //[HttpGet]
        //public IActionResult GetCopyFromData(string vType, int placeCode, DateTime vDate)
        //{
        //    try
        //    {
        //        if (vType == "BFIS")
        //        {
        //            return Json(new { success = true, data = new List<object>() });
        //        }

        //        if (placeCode <= 0)
        //        {
        //            return Json(new { success = false, message = "Please select Place first." });
        //        }

        //        var globalVariables = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();

        //            string query = @"
        //                SELECT 
        //                    a.DOC_ID AS ReqID,
        //                    a.ITEM_CODE AS ICode,
        //                    a.ITEM_NAME AS ItemName,
        //                    c.UNIT_NAME AS Unit,
        //                    a.Nos,
        //                    a.Qty,
        //                    d.NAME AS Department,
        //                    e.NAME AS Machine,
        //                    a.TO_DEPT,
        //                    a.MACH_CODE,
        //                    a.MAKE_CODE,
        //                    a.V_TYPE AS ReqType,
        //                    a.V_NO AS ReqNo,
        //                    c.UNIT_CODE AS Ucode,
        //                    m.NAME AS Make,
        //                    a.Remarks,
        //                    i.Remarks AS Remarks2,
        //                    i.EMP_CODE,
        //                    f.NAME AS empname,
        //                    i.Place_code,
        //                    c1.Name AS CostCat,
        //                    c2.Name AS CostsubCat,
        //                    c3.Name AS Costcenter,
        //                    c.COSTCAT_CODE,
        //                    c.COSTSCAT_CODE,
        //                    c.COSTCENTER_CODE
        //                FROM ISSUE2 a

        //                LEFT JOIN ISSUE1 i 
        //                    ON a.V_TYPE = i.V_TYPE
        //                    AND a.V_NO = i.V_NO
        //                    AND a.COMP_CODE = i.COMP_CODE
        //                    AND a.BRANCH_CODE = i.BRANCH_CODE
        //                    AND a.YEAR_CODE = i.YEAR_CODE

        //                LEFT JOIN ISSUE2 b 
        //                    ON a.ITEM_CODE = b.ITEM_CODE
        //                    AND a.V_TYPE = b.REQ_TYPE
        //                    AND a.V_NO = b.REQ_NO
        //                    AND a.COMP_CODE = b.COMP_CODE
        //                    AND a.BRANCH_CODE = b.BRANCH_CODE
        //                    AND b.V_TYPE = 'SICO'

        //                LEFT JOIN ITEM_MAST c 
        //                    ON a.ITEM_CODE = c.CODE
        //                    AND a.COMP_CODE = c.COMP_CODE

        //                LEFT JOIN ITEMMAKE_MAST m 
        //                    ON a.MAKE_CODE = m.CODE
        //                    AND a.COMP_CODE = m.COMP_CODE

        //                LEFT JOIN ITEMDEPT_MAST d 
        //                    ON a.TO_DEPT = d.CODE
        //                    AND a.COMP_CODE = d.COMP_CODE
        //                    AND d.TRAN_TYPE = 'STORE'

        //                LEFT JOIN MACHINE_MAST e 
        //                    ON a.MACH_CODE = e.CODE
        //                    AND a.COMP_CODE = e.COMP_CODE

        //                LEFT JOIN EMP_MAST f 
        //                    ON i.EMP_CODE = f.CODE
        //                    AND i.COMP_CODE = f.COMP_CODE

        //                LEFT JOIN CostCat_Mast c1 
        //                    ON c.COSTCAT_CODE = c1.CODE
        //                    AND c.COMP_CODE = c1.COMP_CODE

        //                LEFT JOIN CostSubCat_Mast c2 
        //                    ON c.COSTSCAT_CODE = c2.CODE
        //                    AND c.COMP_CODE = c2.COMP_CODE

        //                LEFT JOIN COSTCENTER_MAST c3 
        //                    ON c.COSTCENTER_CODE = c3.CODE
        //                    AND c.COMP_CODE = c3.COMP_CODE

        //                WHERE a.V_TYPE = 'IRST'
        //                    AND ISNULL(b.REQ_NO, 0) = 0
        //                    AND CAST(a.V_DATE AS DATE) = @V_DATE
        //                    AND i.STATUS = 1
        //                    AND a.COMP_CODE = @COMP_CODE
        //                    AND a.BRANCH_CODE = @BRANCH_CODE
        //                    AND a.YEAR_CODE = @YEAR_CODE

        //                UNION ALL

        //                SELECT 
        //                    a.DOC_ID AS ReqID,
        //                    a.ITEM_CODE AS ICode,
        //                    a.ITEM_NAME AS ItemName,
        //                    c.UNIT_NAME AS Unit,
        //                    a.NOS,
        //                    a.QTY,
        //                    d.NAME AS Department,
        //                    e.NAME AS Machine,
        //                    a.TO_DEPT,
        //                    a.MACH_CODE,
        //                    a.MAKE_CODE,
        //                    a.V_TYPE AS ReqType,
        //                    a.V_NO AS ReqNo,
        //                    c.UNIT_CODE AS Ucode,
        //                    m.NAME AS Make,
        //                    a.Remarks,
        //                    i.Remarks AS Remarks2,
        //                    i.EMP_CODE,
        //                    f.NAME AS empname,
        //                    i.Place_code,
        //                    c1.Name AS CostCat,
        //                    c2.Name AS CostsubCat,
        //                    c3.Name AS Costcenter,
        //                    c.COSTCAT_CODE,
        //                    c.COSTSCAT_CODE,
        //                    c.COSTCENTER_CODE
        //                FROM TRF_REQUEST2 a

        //                LEFT JOIN TRF_REQUEST2 b 
        //                    ON a.ITEM_CODE = b.ITEM_CODE
        //                    AND a.V_TYPE = b.REQ_TYPE
        //                    AND a.V_NO = b.REQ_NO
        //                    AND a.COMP_CODE = b.COMP_CODE
        //                    AND a.BRANCH_CODE = b.BRANCH_CODE
        //                    AND b.V_TYPE = 'SICO'

        //                LEFT JOIN TRF_REQUEST1 i 
        //                    ON a.V_TYPE = i.V_TYPE
        //                    AND a.V_NO = i.V_NO
        //                    AND a.COMP_CODE = i.COMP_CODE
        //                    AND a.BRANCH_CODE = i.BRANCH_CODE
        //                    AND a.YEAR_CODE = i.YEAR_CODE

        //                LEFT JOIN ITEM_MAST c 
        //                    ON a.ITEM_CODE = c.CODE
        //                    AND a.COMP_CODE = c.COMP_CODE

        //                LEFT JOIN ITEMMAKE_MAST m 
        //                    ON a.MAKE_CODE = m.CODE
        //                    AND a.COMP_CODE = m.COMP_CODE

        //                LEFT JOIN ITEMDEPT_MAST d 
        //                    ON a.TO_DEPT = d.CODE
        //                    AND a.COMP_CODE = d.COMP_CODE
        //                    AND d.TRAN_TYPE = 'STORE'

        //                LEFT JOIN MACHINE_MAST e 
        //                    ON a.MACH_CODE = e.CODE
        //                    AND a.COMP_CODE = e.COMP_CODE

        //                LEFT JOIN EMP_MAST f 
        //                    ON i.EMP_CODE = f.CODE
        //                    AND i.COMP_CODE = f.COMP_CODE

        //                LEFT JOIN CostCat_Mast c1 
        //                    ON c.COSTCAT_CODE = c1.CODE
        //                    AND c.COMP_CODE = c1.COMP_CODE

        //                LEFT JOIN CostSubCat_Mast c2 
        //                    ON c.COSTSCAT_CODE = c2.CODE
        //                    AND c.COMP_CODE = c2.COMP_CODE

        //                LEFT JOIN COSTCENTER_MAST c3 
        //                    ON c.COSTCENTER_CODE = c3.CODE
        //                    AND c.COMP_CODE = c3.COMP_CODE

        //                WHERE a.V_TYPE = 'ITST'
        //                    AND b.REQ_TYPE IS NULL
        //                    AND CAST(a.V_DATE AS DATE) = @V_DATE
        //                    AND i.STATUS = 1
        //                    AND a.COMP_CODE = @COMP_CODE
        //                    AND a.BRANCH_CODE = @BRANCH_CODE
        //                    AND a.YEAR_CODE = @YEAR_CODE

        //                ORDER BY ReqType, ReqID;
        //            ";

        //            using SqlCommand cmd = new SqlCommand(query, con);

        //            cmd.Parameters.Add("@V_DATE", SqlDbType.Date).Value = vDate.Date;
        //            cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = globalVariables.PubCompCode;
        //            cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = globalVariables.PubBranchCode;
        //            cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = globalVariables.PubFYearCode;

        //            var result = new List<Dictionary<string, object>>();

        //            using SqlDataReader reader = cmd.ExecuteReader();

        //            while (reader.Read())
        //            {
        //                var row = new Dictionary<string, object>();

        //                for (int i = 0; i < reader.FieldCount; i++)
        //                {
        //                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        //                }

        //                result.Add(row);
        //            }

        //            return Json(new { success = true, data = result });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> GetCopyFromData(string vType, int placeCode,DateTime vDate)
        {
            var result =await _inventoryConsumptionEntryRepository.GetCopyFromDataAsync(vType, placeCode,vDate);
            return Json(result);
        }

        [HttpGet]
        public IActionResult fillCapitalItemData(int? billPassNo, int? mrnNo)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                int compCode = Convert.ToInt32(globalVariables.PubCompCode);
                int branchCode = Convert.ToInt32(globalVariables.PubBranchCode);
                int yearCode = Convert.ToInt32(globalVariables.PubFYearCode);

                int deptCode;

                if (compCode == 1 || compCode == 4)
                {
                    deptCode = 137;
                }
                else if (compCode == 2 || compCode == 5)
                {
                    deptCode = 132;
                }
                else if (compCode == 3)
                {
                    deptCode = 114;
                }
                else
                {
                    return BadRequest(new { success = false, message = "Capital department is not configured for this company." });
                }

                string vType;
                int vNo;

                if (billPassNo.HasValue && billPassNo.Value > 0)
                {
                    vType = "STPB";
                    vNo = billPassNo.Value;
                }
                else if (mrnNo.HasValue && mrnNo.Value > 0)
                {
                    vType = "SRPU";
                    vNo = mrnNo.Value;
                }
                else
                {
                    return BadRequest(new { success = false, message = "Bill Pass No. or MRN No. is required." });
                }
                var finalItems = new List<object>();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // First validate document
                    string validateQuery = @"
                    SELECT COUNT(1)
                    FROM Purchase1
                    WHERE Comp_Code = @CompCode
                      AND V_Type = @VType
                      AND V_No = @VNo";

                    using (SqlCommand validateCmd = new SqlCommand(validateQuery, con))
                    {
                        validateCmd.Parameters.AddWithValue("@CompCode", compCode);
                        validateCmd.Parameters.AddWithValue("@VType", vType);
                        validateCmd.Parameters.AddWithValue("@VNo", vNo);

                        int count = Convert.ToInt32(validateCmd.ExecuteScalar());

                        if (count == 0)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = vType == "STPB" ? "Bill Pass No. Wrong OR Not Exist." : "MRN No. Wrong OR Not Exist."
                            });
                        }
                    }

                    string query = @"
                        SELECT
                            a.Comp_Code,
                            a.Item_Code,
                            f.Name AS ITEM_NAME,
                            f.UNIT_NAME,
                            f.UNIT_CODE,
                            SUM(a.Recd_Qty) AS Recd_Qty
                        FROM Purchase2 a

                        LEFT JOIN Purchase1 b
                            ON a.V_Type = b.V_Type
                            AND a.V_No = b.V_No
                            AND a.Comp_Code = b.Comp_Code
                            AND a.Branch_Code = b.Branch_Code
                            AND a.Year_Code = b.Year_Code

                        LEFT JOIN SUBGROUP_MAST c
                            ON b.Debit_Ac = c.Code
                            AND b.Comp_Code = c.Comp_Code

                        LEFT JOIN MGROUP_MAST d
                            ON c.GROUP_CODE = d.Code
                            AND c.Comp_Code = d.Comp_Code

                        LEFT JOIN GR_MAST e
                            ON d.GR_CODE = e.Code
                            AND d.Comp_Code = e.Comp_Code

                        LEFT JOIN ITEM_MAST f
                            ON a.ITEM_CODE = f.Code
                            AND a.Comp_Code = f.Comp_Code

                        WHERE a.V_Type = @VType
                          AND a.Comp_Code = @CompCode
                          AND a.Branch_Code = @BranchCode
                          AND a.Year_Code = @YearCode
                          AND a.V_No = @VNo
                    ";

                    if (vType == "STPB")
                    {
                        query += " AND e.TYPE = 'Asset' ";
                    }

                    query += @"
                    GROUP BY
                        a.Comp_Code,
                        a.Item_Code,
                        f.Name,
                        f.UNIT_NAME,
                        f.UNIT_CODE

                    ORDER BY
                        a.Comp_Code,
                        f.Name";

                    var items = new List<object>();
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@VType", vType);
                        cmd.Parameters.AddWithValue("@CompCode", compCode);
                        cmd.Parameters.AddWithValue("@BranchCode", branchCode);
                        cmd.Parameters.AddWithValue("@YearCode", yearCode);
                        cmd.Parameters.AddWithValue("@VNo", vNo);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int itemCode = reader["Item_Code"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Item_Code"]);
                                decimal receivedQty = reader["Recd_Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Recd_Qty"]);
                                items.Add(new
                                {
                                    itemCode,
                                    itemName = reader["ITEM_NAME"] == DBNull.Value ? "" : reader["ITEM_NAME"].ToString(),
                                    unitName = reader["UNIT_NAME"] == DBNull.Value ? "" : reader["UNIT_NAME"].ToString(),
                                    unitCode = reader["UNIT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["UNIT_CODE"]),
                                    receivedQty
                                });
                            }
                        }

                        // Get consumed quantities from SICO

                        foreach (dynamic item in items)
                        {
                            string consumptionQuery = @"
                            SELECT ISNULL(SUM(Qty), 0)
                            FROM Issue2
                            WHERE V_Type = 'SICO'
                              AND Item_Code = @ItemCode
                              AND ISNULL(Qty, 0) > 0
                              AND ISNULL(TO_DEPT, 0) = @DeptCode
                              AND Comp_Code = @CompCode
                              AND Branch_Code = @BranchCode
                              AND Year_Code = @YearCode";

                            using (SqlCommand consumptionCmd = new SqlCommand(consumptionQuery, con))
                            {
                                consumptionCmd.Parameters.AddWithValue("@ItemCode", item.itemCode);
                                consumptionCmd.Parameters.AddWithValue("@DeptCode", deptCode);
                                consumptionCmd.Parameters.AddWithValue("@CompCode", compCode);
                                consumptionCmd.Parameters.AddWithValue("@BranchCode", branchCode);
                                consumptionCmd.Parameters.AddWithValue("@YearCode", yearCode);

                                decimal consumedQty = Convert.ToDecimal(
                                    consumptionCmd.ExecuteScalar()
                                );

                                decimal balanceQty = item.receivedQty - consumedQty;

                                if (balanceQty != 0)
                                {
                                    finalItems.Add(new
                                    {
                                        itemCode = item.itemCode,
                                        itemName = item.itemName,
                                        unitName = item.unitName,
                                        unitCode = item.unitCode,
                                        receivedQty = item.receivedQty,
                                        consumedQty = consumedQty,
                                        balanceQty = balanceQty,
                                        deptCode = deptCode,
                                        deptName = "Capital"
                                    });
                                }
                            }
                        }
                    }
                }
                return Ok(new
                {
                    success = true,
                    departmentCode = deptCode,
                    departmentName = "Capital",
                    items = finalItems
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
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
