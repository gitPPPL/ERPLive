using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseRequestModel;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    [SessionAuthorize]
    public class PurchaseRequestController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IPurchaseRequestRepository _IPRRepository;

        public PurchaseRequestController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DropdownService dropdownService, DbHelper dbHelper,
            ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, IPurchaseRequestRepository IPRRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _IPRRepository = IPRRepository;
        }

        public IActionResult Index()
        {
            var gv = _globalVariableService.GetGlobalVariables();

            ViewBag.UserLevel = gv.PubUserLevel;
            ViewBag.CompCode = gv.PubCompCode;
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database; // Get the database name
            }

            ViewBag.GlobalVariables = gv;
            ViewBag.DatabaseName = databaseName;
            return View("~/Views/Purchase/Transaction/PurchaseRequest/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetVNo()
        {
            var result = _globalValidationdate.GetVNo("STPI", "PREQUEST1");
            return Json(new { status = true, V_NO = result });
        }

        //============Check IsApprovalBody=============
        [HttpGet]
        public JsonResult CheckIsApprovalBody()
        {
            var result = _IPRRepository.CheckIsApprovalBody();
            return Json(new { exists = result.data });
        }

        //===========Check IsFinalApprovalBody============
        [HttpGet]
        public async Task<JsonResult> CheckIsFinalApprovalBody()
        {
            var result = await _IPRRepository.CheckIsFinalApprovalBodyAsync();
            return Json(new { success = result.status, exists = result.data, message = result.message });
        }

        public JsonResult GetDropdown(string type, int data = 0)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = "";

            switch (type)
            {
                case "Department":
                    
                    if (data == 1)
                    {
                        query = $@"
                        SELECT DISTINCT b.CODE, b.NAME FROM USER_DEPT a LEFT JOIN ITEMDEPT_MAST b ON a.DEPT_CODE = b.CODE 
                         WHERE   a.USER_CODE = {gv.PubUserId} AND a.COMP_CODE = '{gv.PubCompCode}' AND b.TRAN_TYPE = 'Store' ORDER BY b.NAME ASC";
                    }
                    else
                    {
                        query = $@"
                        SELECT DISTINCT b.CODE, b.NAME FROM USER_DEPT a LEFT JOIN ITEMDEPT_MAST b ON a.DEPT_CODE = b.CODE 
                         WHERE a.COMP_CODE = '{gv.PubCompCode}' AND b.TRAN_TYPE = 'Store' ORDER BY b.NAME ASC";
                    }
                        break;
                
                case "DocStatus":
                    query = $@"Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                    break;

                case "Place":
                    query = $@"
                        select CODE,NAME from PLACE_MAST  WHERE  COMP_CODE = {gv.PubCompCode}  AND  NAME <> ''  ORDER BY NAME asc";
                    break;

                //case "Requester":
                    //query = $@"
                    //    Select a.Code , a.Name from EMP_MAST a where a.RESIGN_DATE is null and a.COMP_CODE=  {gv.PubCompCode}  order by a.Name asc";
                    //break;

                case "PlaceUse":
                    query = $@"
                        select CODE , NAME  From MACHINE_MAST where COMP_CODE = {gv.PubCompCode} and NAME <> '' order by NAME asc  ";
                    break;

                case "Make":
                    query = $@"
                        SELECT distinct  a.MAKE_CODE as Code , b.name as Name FROM ITEM_MAKE a LEFT JOIN ITEMMAKE_MAST b ON a.MAKE_CODE = b.CODE AND
                        b.comp_code = a.comp_code where a.ITEM_CODE ={data} and b.COMP_CODE = {gv.PubCompCode}  order by  b.name asc ;";
                    break;
                
                case "Priority":
                    query = $@"select CODE, NAME from DOCSTATUS_MAST WHERE V_TYPE = 'Preority' order by CODE";
                    break;
                
                case "WorkType":
                    query = $@"select CODE, NAME from DOCSTATUS_MAST WHERE V_TYPE = 'WorkType' order by name";
                    break;
                
                case "ApprovalStatus":
                    query = $@"select CODE, NAME from DOCSTATUS_MAST WHERE V_TYPE = 'Approval' and CODE in (4,5,8) order by name";
                    break;
            }

            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetddlItems(int deptid = 0)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "";
            if (compCode == "1")
            {   
                query += $@"SELECT b.CODE, b.name, c.NAME as Unit, c.CODE as UCode FROM item_dept a 
                        LEFT JOIN item_mast b ON a.ITEM_CODE = b.code AND b.ACTIVE = 1 AND b.comp_code = a.COMP_CODE 
                        LEFT JOIN ITEMUNIT_MAST c ON b.UNIT_CODE = c.CODE AND c.comp_code = 1  
                        LEFT JOIN ITEM_MGROUP d ON b.MGROUP_CODE = d.CODE AND d.comp_code = a.COMP_CODE  
                        WHERE a.comp_code = {compCode}  AND d.mgroup_type IN ('Store', 'Fuel') AND  
                        a.DEPT_CODE  = {deptid} 
                        and b.NAME <> '' 
                        ORDER BY b.name  asc ";
            }
            else
            {
                query += $@"
                    Select a.CODE, a.name, c.NAME as Unit, c.CODE as UCode
                    from item_mast a 
                    left outer join ITEMUNIT_MAST c on a.UNIT_CODE=c.CODE and c.comp_code=a.comp_Code 
                    Left join ITEM_MGROUP d on a.MGROUP_CODE=d.CODE and a.comp_code=d.comp_code
                    where a.comp_code={compCode} and a.active=1 and d.mgroup_type in ('Store','Fuel') 
                    group by a.name ,a.CODE, c.NAME ,c.CODE,a.HSN_CODE,a.CATLOG  order by a.name";
            }
                var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = dataList });
        }
        [HttpGet]
        public async Task<JsonResult> GetPlanList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"SELECT a.V_no as PlanNo, a.V_Type as PlanType, b.NAME as deptName, c.NAME as FaltName, d.NAME as MachName, format(a.V_date,'dd/MM/yyyy') as ComplainDate
                          FROM PM_MAINTENANCEPLAN a 
                          left join ItemDept_Mast b on a.Dept_code=b.code and a.comp_code=b.comp_code
                          Left join Falt_Mast c on a.Fault_code=c.Code and a.comp_code=c.comp_code
                          Left join Machine_Mast d on a.mach_code=d.Code and a.comp_code=d.comp_code
                           where a.comp_code={gv.PubCompCode} and a.Branch_code= {gv.PubBranchCode}
                          and a.Year_code={gv.PubFYearCode} and V_type='PMCP'  order by a.V_no asc";
            var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = dataList });
        }
        [HttpGet]
        public async Task<JsonResult> GetRequesterList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = $@"
                            Select a.Code as Code, a.Name as Name, b.NAME as Designation from EMP_MAST a 
                            left join DESG_MAST b on b.CODE = a.DESG_CODE
                            where a.RESIGN_DATE is null and a.COMP_CODE = {gv.PubCompCode}  order by a.Name asc";
            var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = dataList });
        }

        //=============================================
        [HttpPost]
        public IActionResult SavedData([FromBody] PurchaseRequest_model request)
        {
            if (request?.Header == null)
                return Json(new { success = false, message = "Input model is null" });

            var result = _IPRRepository.SaveData(request);

            return result.status
                ? Json(new { success = true })
                : Json(new { success = false, message = result.message });
        }

        //===========================Methods For Validation=======
        [HttpGet]
        public async Task<IActionResult> GetPurchaseRequests(int itemCode, int deptCode, int vNo)
        {
            if (itemCode <= 0 || deptCode <= 0)
                return Json(new { success = false, message = "Invalid parameters" });

            var result = await _IPRRepository.GetPurchaseRequestsAsync(itemCode, deptCode, vNo);

            return Json(new { success = result.status, data = result.data, message = result.message});
        }

        [HttpGet]
        public async Task<IActionResult> GetItemMake(int itemCode, int makeCode)
        {
            if (itemCode <= 0 || makeCode <= 0)
                return Json(new { success = false, exists = false, message = "Invalid parameters" });

            var result = await _IPRRepository.GetItemMakeAsync(itemCode, makeCode);

            return Json(new {success = result.status, exists = result.data, message = result.message});
        }

        [HttpGet]
        public async Task<IActionResult> CheckMonthlyReq(int itemCode)
        {
            if (itemCode <= 0)
                return Json(new { success = false, message = "Invalid item code!" });

            var result = await _IPRRepository.CheckMonthlyReqAsync(itemCode);

            return Json(new { success = result.status, exists = result.data, message = result.message});
        }

        [HttpGet]
        public async Task<JsonResult> GetMaxRequestCount(int vNo, DateTime vDate)
        {
            if (vNo <= 0)
                return Json(new { success = false, isWithinLimit = false, message = "Invalid VNo" });

            var result = await _IPRRepository.GetMaxRequestCountAsync(vNo, vDate);

            return Json(new {success = result.status, isWithinLimit = result.data, message = result.message});
        }

        //===Check Modification Days
        [HttpGet]
        public JsonResult checkModificationDays(DateTime? vDate)
        {
            if (!vDate.HasValue)
            {
                return Json(new { success = false, message = "Doc Date is empty!!" });
            }
            var (allowed, message) = _globalValidationdate.CheckModificationDays(vDate.Value);
            return Json(new { success = true, isAllowed = allowed, message = message });
        }

        [HttpGet]
        public JsonResult GetApprovalStatus(int VNo)
        {
            if (VNo <= 0)
                return Json(new { Success = false, Message = "Invalid VNo" });

            var result = _IPRRepository.GetApprovalStatus(VNo);

            return Json(new {Success = result.status, FAPROV_STATUS = result.data});
        }

        [HttpGet]
        public IActionResult ValidateDepartmentAccess(int deptCode)
        {
            if (deptCode <= 0)
                return Json(new { success = false, exists = false });

            var result = _IPRRepository.ValidateDepartmentAccess(deptCode);

            return Json(new { success = result.status, exists = result.data });
        }


        //=============Modal Methods===============
            //=============Overall History==========
        [HttpGet]
        public JsonResult GetLastTenPurchaseRequest(List<int> itemCodes)
        {
            var result = _IPRRepository.GetLastTenPurchaseRequest(itemCodes);

            if(result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }
        
        [HttpGet]
        public JsonResult GetLastTenConsumptionDetails(List<int> itemCodes)
        {
            var result = _IPRRepository.GetLastTenConsumptionDetails(itemCodes);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult GetLastTenPurchaseHistory(List<int> itemCodes)
        {
            var result = _IPRRepository.GetLastTenPurchaseHistory(itemCodes);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult GetLastTenOrderHistory(List<int> itemCodes)
        {
            var result = _IPRRepository.GetLastTenOrderHistory(itemCodes);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

           //===================Row Wise History============
        [HttpGet]
        public JsonResult GetItemWisePurchaseRequest(int itemCode)
        {
            var result = _IPRRepository.GetItemWisePurchaseRequest(itemCode);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult GetItemWiseConsumptionHistory(int itemCode)
        {
            var result = _IPRRepository.GetItemWiseConsumptionHistory(itemCode);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult GetItemWisePurchaseOrderHistory(int itemCode)
        {
            var result = _IPRRepository.GetItemWisePurchaseOrderHistory(itemCode);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult GetItemWisePurchaseQuotationHistory(int itemCode)
        {
            var result = _IPRRepository.GetItemWisePurchaseQuotationHistory(itemCode);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult GetItemWisePurchaseReceiptHistory(int itemCode)
        {
            var result = _IPRRepository.GetItemWisePurchaseReceiptHistory(itemCode);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpGet]
        public JsonResult GetItemWisePurchaseHistory(int itemCode)
        {
            var result = _IPRRepository.GetItemWisePurchaseHistory(itemCode);
            if (result.data != null)
            {
                return Json(new { success = result.status, data = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = "STPI";
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("PREQUEST1", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();

                var parameters = new Dictionary<string, object>
                {
                    { "@YEAR_CODE", gv.PubFYearCode },
                    { "@COMP_CODE", gv.PubCompCode },
                    { "@BRANCH_CODE", gv.PubBranchCode },
                    { "@Action", "Excel" }
                };

                var fileBytes = _globalValidationdate.ExportToExcel("sp_PurchaseReq1", "Purchase Request", parameters);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"PurchaseRequest_{DateTime.Now:ddMMyyyy}.xlsx"
                );
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

        public class ItemAllDetailsDto
        {
            public decimal? Rate { get; set; }
            public decimal? PendingQty { get; set; }
            public decimal? Total_Qty { get; set; }
            public decimal? CurrentStocklist { get; set; }
            public decimal? AvgConsumption { get; set; }
            public string TECH_DESC { get; set; }
        }
        public JsonResult GetItemAllDetails(int itemCode, DateTime vDate)
        {
            if (itemCode <= 0)
            {
                return Json(new { success = false, message = "Invalid item code!" });
            }

            var model = new ItemAllDetailsDto();

            // 1. Rate
            var rateResult = _IPRRepository.GetApporxiateRate(itemCode);
                model.Rate = rateResult.data;

            // 2. Pending Qty
            var pendingResult = _IPRRepository.GetPendingQty(itemCode);
                model.PendingQty = pendingResult.data;

            // 3. Total Qty
            var totalResult = _IPRRepository.GetTotalQty(itemCode);
                model.Total_Qty = totalResult.data;

            // 4. TECH DESC
            var techResult = _IPRRepository.GetTECH_DESC(itemCode);
                model.TECH_DESC = techResult.data;

            // 5. Current Stock
            var cStock = _IPRRepository.GetCurrentStock(itemCode);
            model.CurrentStocklist = cStock.data;
           
            // 6. Avg Consumption
            var avgResult = _IPRRepository.GetAvgConsumption(itemCode, vDate);
                model.AvgConsumption = avgResult.data;

            return Json(new { success = true, data = model });
        }
        
        [HttpPost]
        public JsonResult PrintRequest([FromBody] PRPrintModel model)
        {
            if(model == null)
            {
                return Json(new { success = false, message = "Invalid data!" });
            }
            var result = _IPRRepository.PRPrintRequest(model);
            return Json(new { success = result.status, message = result.message, approvedBy = result.data });
        }

    }

}