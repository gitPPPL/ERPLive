using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.OtherUnused;
using travelexpensemanagement.Repositories.Interfaces.Sales.OtherUnused;

namespace travelexpensemanagement.Controllers.Sales.OtherUnused
{
    [SessionAuthorize]
    public class PackingSlipEntryController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DataBaseConnection _dbConnection;
        private readonly IPackingSlipEntryRepository _repo;
        public PackingSlipEntryController(GlobalVariableService globalVariableService, DbHelper dbHelper, GlobalValidationdate globalValidationdate,
            DataBaseConnection dbConnection, IPackingSlipEntryRepository repo)
        {
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _globalValidationdate = globalValidationdate;
            _dbConnection = dbConnection;
            _repo = repo;
        }
        public IActionResult Index()
        {
            return View("~/Views/Sales/OtherUnused/PackingSlipEntry/Index.cshtml");
        }

        // ======================DROPDOWN======================
        public async Task<IActionResult> GetDropdown(string type, string data = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "doctype":
                    qry = $@"Select Code as value, Name as text from DOCTYPE_MAST where CODE IN ('FPSL')";
                    break;

                case "docstatus":
                    qry = $@"Select Code as value, Name as text from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                    break;

                case "godown":
                    qry = $@"Select Code as value, Name as text from GODOWN_MAST where comp_code={gv.PubCompCode} order by SNO";
                    break;

                case "item":
                    qry = $@"Select a.code as value, a.SHORTNAME as text from ITEM_MAST a 
                            left join ITEM_MGROUP b on a.MGROUP_CODE=b.CODE and b.COMP_CODE={gv.PubCompCode}
                            where a.Active=1 and b.MGROUP_TYPE='Finish' and a.COMP_CODE={gv.PubCompCode} order by a.SHORTNAME";
                    break;

                case "do":
                    qry = $@"Select DOC_ID as value, V_TYPE, V_no as text, BILL_CODE, BILL_NAME from DO1 a
                            left join City_mast b on a.SHIP_CITY=b.code 
                            Where isnull(a.Ref_no,0)=0 and a.COMP_CODE={gv.PubCompCode} and a.BRANCH_CODE={gv.PubBranchCode}
                            order by a.V_no desc";
                    break;

                case "party":
                    qry = $@"select a.code as value, a.name as text, b.Drbal, b.crbal, a.CREDIT_LIMIT from SUBGROUP_MAST a 
                            Left Join vwLedger b on a.code = b.code and a.comp_code=b.comp_code
                            where a.nature in ('Customer','Broker') and a.COMP_CODE={gv.PubCompCode} and a.ACTIVE=1
                            order by a.Name";
                    break;

                case "machine":
                    qry = $@"Select Code as value, Name as text from MACHINE_MAST where COMP_CODE={gv.PubCompCode}";
                    break;

                case "tenacity":
                    qry = $@"select name as text, code as value from TENACITY_MAST where COMP_CODE={gv.PubCompCode} and active=1 order by code";
                    break;

                case "itemstatus":
                    qry = $@"select name as text, code as value from STATUS_MAST where COMP_CODE={gv.PubCompCode} and active=1 order by code";
                    break;

                case "footeritem":
                    qry = $@"Select a.Shortname as text, a.CODE as value, isnull(b.NAME,'') as unit, isnull(b.CODE,0) as ucode from item_mast a 
                            left join ITEMUNIT_MAST b on a.UNIT_CODE=b.CODE and b.comp_code={gv.PubCompCode}
                            inner join ITEM_MGROUP c on a.mGROUP_CODE=c.CODE and c.COMP_CODE={gv.PubCompCode} and c.MGROUP_TYPE in ('Finish','Waste','Finish Waste')
                            where a.Active=1 and a.comp_code={gv.PubCompCode}
                            group by a.Shortname,a.CODE,b.NAME,b.CODE order by a.Shortname";
                    break;

                case "place":
                    qry = $@"SELECT CODE as value, NAME as text FROM PLACE_MAST where comp_code = {gv.PubCompCode} order by code";
                    break;

                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }


        //======================V_NO======================
        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "PPRODUCTION1");
            return Json(new { status = true, V_NO = result });
        }


        //======================DO DETAILS======================
        [HttpGet]
        public async Task<IActionResult> GetDOData(string docId)
        {
            var response = await _repo.GetDOData(docId);
            return Json(response);
        }


        //======================FOOTER ITEM DETAILS======================
        [HttpGet]
        public IActionResult GetItemRowData(string vType, int itemCode, int rowIndex, string prevBaleNo, int placeCode)
        {
            var response = _repo.GetItemRowData(vType, itemCode, rowIndex, prevBaleNo, placeCode);
            return Json(response);
        }


        //======================VALIDATE DOCUMENT DATE======================
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("PPRODUCTION1", vdate, vtype, vno);
            return Ok(result);
        }


        //======================VALIDATIONS======================
        [HttpPost]
        public async Task<IActionResult> ValidateData([FromBody] PackingSlipValidationRequest data)
        {
            var response = await _repo.ValidateData(data);
            return Json(response);
        }
       

        //======================SAVE & UPDATE======================
        [HttpPost]
        public IActionResult SaveData([FromBody] PackingSlipEntryModel model)
        {
            var response = _repo.SaveData(model);
            return Json(new { status = response.status, message = response.message });
        }


        //======================EDIT======================
        [HttpGet]
        public IActionResult GetById(int id, string vtype)
        {
            var response = _repo.GetById(id, vtype);
            return Json(new { status = response.status, data = response.data, message = response.message});
        }


        //======================GLOBAL VARIABLES======================
        [HttpGet]
        public async Task<JsonResult> getGlobalValues()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();
                using var erpCon = _dbConnection.GetErpConnection();

                string databaseName;
                using (var connection = _dbConnection.GetErpConnection())
                {
                    databaseName = connection.Database; // Get the database name
                }

                var response = new
                {
                    compCode = gv.PubCompCode,
                    yearCode = gv.PubFYearCode,
                    branchCode = gv.PubBranchCode,
                    add1 = gv.Address1,
                    add2 = gv.Address2,
                    companyName = gv.CompanyName,
                    db = databaseName
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        //======================COPY FROM======================
        [HttpGet]
        public async Task<IActionResult> GetCopyFromList(int? itemCode, int? macCode, DateTime vDate)
        {
            var response = await _repo.GetCopyFromList(itemCode, macCode, vDate);
            return Json(new { success = response.status, data = response.data, message = response.message });
        }
    }
}
