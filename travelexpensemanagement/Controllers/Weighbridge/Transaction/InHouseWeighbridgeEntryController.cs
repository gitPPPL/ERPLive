using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Weighbridge.Transaction;
using travelexpensemanagement.Repositories.Implementations.Weighbridge;
using travelexpensemanagement.Repositories.Interfaces.Weighbridge;

namespace travelexpensemanagement.Controllers.Weighbridge.Transaction
{
    public class InHouseWeighbridgeEntryController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IInHouseWeighbridgeEntryRepository _inHouseWeighbridgeEntryRepository;
        public InHouseWeighbridgeEntryController(DataBaseConnection dbcontext, DbHelper dbHelper,
        travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, GlobalVariableService globalValue,
        ModuleService.ModuleService moduleService, DataBaseConnection dbConnection, GlobalValidationdate globalValidationdate, IInHouseWeighbridgeEntryRepository inHouseWeighbridgeEntryRepository)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _dropdownService = dropdownService;
            _globalValidationdate = globalValidationdate;
            _dbConnection = dbConnection;
            _inHouseWeighbridgeEntryRepository = inHouseWeighbridgeEntryRepository;
        }
        public IActionResult Index()
        {
            return View("~/Views/Weighbridge/Transaction/InHouseWeighbridgeEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            try
            {
                var docIdNoList = _globalValidationdate.GetVNo(V_type, "WB1");
                return Json(new { status = true, data = docIdNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlaceMast()
        {
            try
            {
                var placelist = await _dbHelper.GetJsonDataAsync(@$" select  CODE , Name from ITEMDEPT_MAST  where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode}  group by b.name ,b.CODE  order by b.name  order by NAME ");
                return Json(new { status = true, data = placelist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public JsonResult GetTareSlipNo()
        {
            var usersession = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string sql = $@"select V_NO ,V_TYPE  from WB1 where V_TYPE='KINH' and WB_TYPE='Tare' and COMP_CODE={usersession.PubCompCode} and YEAR_CODE={usersession.PubFYearCode} and BRANCH_CODE={usersession.PubBranchCode} order by V_NO ";
                var GetTareSlipNo = _dropdownService.GetDropdownList(sql);
                return Json(GetTareSlipNo);
            }
        }

        [HttpGet]
        public JsonResult GetGrossSlipNo()
        {
            var usersession = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string sql = $@"  SELECT  V_NO ,V_TYPE  FROM WB1 WHERE V_TYPE = 'KINH' AND WB_TYPE = 'Gross' AND COMP_CODE = {usersession.PubCompCode}
                AND YEAR_CODE = {usersession.PubFYearCode}  AND BRANCH_CODE = {usersession.PubBranchCode} ORDER BY V_NO";
                var Partylist = _dropdownService.GetDropdownList(sql);
                return Json(Partylist);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeighBridgeBySlipNo(int SlipNo, string vType)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();

                var list = new List<object>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetWBEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@V_NO", SlipNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", usersession.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", usersession.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", usersession.PubBranchCode);
                        cmd.Parameters.AddWithValue("@Action", "GetInHouseWBridgeList");

                        await con.OpenAsync();

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new
                                {

                                    ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                                    WEIGHT = reader["WEIGHT"] != DBNull.Value ? Convert.ToDecimal(reader["WEIGHT"]) : 0,
                                    FROM_PLACE = reader["FROM_PLACE"] != DBNull.Value ? Convert.ToInt32(reader["FROM_PLACE"]) : 0,
                                    TO_PLACE = reader["TO_PLACE"] != DBNull.Value ? Convert.ToInt32(reader["TO_PLACE"]) : 0,
                                    Ref_no = reader["Ref_no"]?.ToString(),
                                    TYPE = reader["TYPE"]?.ToString(),
                                    WGT_DATE = reader["WGT_DATE"],
                                    WGT_TIME = reader["WGT_TIME"]?.ToString(),
                                    FROM_NAME = reader["FROM_NAME"]?.ToString(),
                                    TO_NAME = reader["TO_NAME"]?.ToString(),
                                    REMARKS = reader["REMARKS"]?.ToString(),
                                    STATUS = reader["STATUS"]?.ToString(),
                                    Ref_type = reader["Ref_type"]?.ToString(),
                                    VEHICLE_NO = reader["VEHICLE_NO"]?.ToString()

                                });
                            }
                        }
                    }
                }


                return Json(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWeighBridgeByGrossSlipNo(int SlipNo, string vType)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var list = new List<object>();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetWBEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@V_NO", SlipNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", usersession.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", usersession.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", usersession.PubBranchCode);
                        cmd.Parameters.AddWithValue("@Action", "GetWeighBridgeByGrossSlipNo");
                        await con.OpenAsync();
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                list.Add(new
                                {
                                    VEHICLE_NO = reader["VEHICLE_NO"]?.ToString(),
                                    ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                                    WEIGHT = reader["WEIGHT"] != DBNull.Value ? Convert.ToDecimal(reader["WEIGHT"]) : 0,
                                    WGT_DATE = reader["WGT_DATE"],
                                    WGT_TIME = reader["WGT_TIME"]?.ToString(),
                                    FROM_NAME = reader["FROM_NAME"]?.ToString(),
                                    TO_NAME = reader["TO_NAME"]?.ToString(),
                                    REMARKS = reader["REMARKS"]?.ToString()
                                });
                            }
                        }
                    }
                }

                return new JsonResult(new { status = true, data = list });
            }
            catch (Exception ex)
            {

                return new JsonResult(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='KantaInhouse' ");
                return Json(new { status = true, data = Doctype });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemList()
        {
            try
            {
                var itemlist = await _dbHelper.GetJsonDataAsync($@"select CODE, NAME,HSN_CODE,UNIT_NAME,UNIT_CODE from item_mast where COMP_CODE ={_globalValue.GetGlobalVariables().PubCompCode} order by NAME");
                return Json(new { status = true, data = itemlist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, messsage = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInHouseWeighBridgeById(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@DOC_ID", id},
                    {"@Action", "WBEntryHeaderData"}
                };
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@DOC_ID", id},
                    {"@Action", "WBEntryDetailData"}
                };

                var headerlist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter);
                var detaillist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetWBEntry]", parameter1);
                return Json(new { status = true, header = headerlist, detail = detaillist });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateInHouseWeighBridgeEntry([FromBody] WBEntryModel model)
        {
            if (model == null)
                return Json(new { status = false, message = "Data save failed." });
            try
            {
                var result = await _inHouseWeighbridgeEntryRepository.SaveOrUpdateInHouseWeighBridgeEntryasync(model);
                return result;
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("WB1", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(string searchTerm = null)
        {
            try
            {
                var fileBytes = await _inHouseWeighbridgeEntryRepository.ExportToExcel(searchTerm);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InHouseWeighbridgeEntry.xlsx");
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error exporting excel.",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToPdf(string searchTerm = null)
        {
            try
            {
                var fileBytes = await _inHouseWeighbridgeEntryRepository.ExportToPdf(searchTerm);
                return File(fileBytes, "application/pdf", "InHouseWeighbridgeEntry.pdf");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error exporting pdf.", error = ex.Message });
            }
        }

    }
}