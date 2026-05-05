
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class OutwardEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IOutwardEntryRepository _outwardEntryRepository;
        private readonly GlobalValidationdate _globalValidationdate;
        public OutwardEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper,
        ModuleService.ModuleService moduleService  , IOutwardEntryRepository outwardEntryRepository, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _outwardEntryRepository = outwardEntryRepository;
            _globalValidationdate = globalValidationdate;
        }
        public IActionResult Index()
        {
            TempData["LoginDate"] = _globalVariableService.GetGlobalVariables().PubLoginDate;
            TempData["PubUserLevel"] = _globalVariableService.GetGlobalVariables().PubUserLevel;
            TempData["CompCode"] = _globalVariableService.GetGlobalVariables().PubCompCode;
            return View("~/Views/GateEntry/Transaction/OutwardEntry/Index.cshtml");
        }
        public JsonResult GetVNo(string Vtype , string Tablename)
        {
          string   newV_NO = _outwardEntryRepository.GetVNoAsync(Vtype, Tablename).GetAwaiter().GetResult();
          return Json(new { V_NO = newV_NO });
        }
        public JsonResult DDlVType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('GateOutward') order by Name ";
                var VtypeList = _dropdownService.GetDropdownList(query);
                return Json(VtypeList);
            }

        }
        public JsonResult fetchPartyAdd(int PartyId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select address_id code,add1 name from SUBGROUP_ADDRESS where code= " + PartyId + "  and COMP_CODE=" + getdata.PubCompCode + " order by ADDRESS_ID";
                var PartyAddList = _dropdownService.GetDropdownList(query);
                return Json(PartyAddList);
            }

        }
        public JsonResult DDlParty()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select a.CODE ,a.name from SUBGROUP_MAST a where  A.ACTIVE=1 order by a.name asc";                 
                var PartyList = _dropdownService.GetDropdownList(query);
                return Json(PartyList);
            }
        }
        public JsonResult GetDataByPartyandAddressidCode(int PartyId, int addressid)
        {        
            var dataList = new List<object>();
            dataList = _outwardEntryRepository.GetDataByPartyandAddressidCodeAsync(PartyId, addressid);
            return Json(dataList);
        }
        public JsonResult GetDataByPartyCode(int PartyId)
        {
            var dataList = new List<object>();
            dataList = _outwardEntryRepository.GetDataByPartyCodeAsync(PartyId);
            return Json(dataList);
        }
        public JsonResult DDLItemMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT  a.CODE, a.NAME AS Shortname, b.mgroup_type FROM  ITEM_MAST a\r\nLEFT JOIN  ITEM_MGROUP b  ON b.CODE = a.MGROUP_CODE  AND b.COMP_CODE = a.COMP_CODE\r\nWHERE  a.Active = 1  AND a.comp_code = 1 group by a.NAME ,a.code,b.mgroup_type order by a.NAME asc";
                var ItemList = _dropdownService.GetDropdownList(query);
                return Json(ItemList);
            }
        }
        public JsonResult DDLDeptMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select b.CODE , b.name  from ITEMDEPT_MAST b where B.ACTIVE=1 AND b.comp_code= " + getdata.PubCompCode  +"";
                var DeptList = _dropdownService.GetDropdownList(query);
                return Json(DeptList);
            }
        }
        public JsonResult DDLUnit()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())            {
                string query = "Select  b.CODE , b.name  from ITEMUNIT_MAST b where B.ACTIVE=1 AND b.comp_code=" + getdata.PubCompCode + "";
                var UnitList = _dropdownService.GetDropdownList(query);
                return Json(UnitList);
            }
        }
        [HttpPost]
        public IActionResult SavedData([FromBody] OutWordEntryModel request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }
            var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";
            var result = _outwardEntryRepository.SaveOutwardEntry(request.Header, request.detailsOutwardEntry, action);
            return result == "Success" ? Json(new { success = true }) : Json(new { success = false, message = result });
        }
        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("Gate1", vdate, vtype, vno);
            return Ok(result);
        }
    }
}
