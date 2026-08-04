using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Ocsp;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class ImportExportDocAttachmentListController : Controller
    {

        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;

        public ImportExportDocAttachmentListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/ImportExportDocAttachmentList/Index.cshtml");
        }


        public JsonResult cmbPartyName()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"Select CODE,NAME from SUBGROUP_MAST where comp_code="  + getdata.PubCompCode + " and  Active=1 order by name ";
                var cmbPartyName = _dropdownService.GetDropdownList(query);
                return Json(cmbPartyName);
            }
        }



        public JsonResult cmbLocation()
        {
            var getdata = _globalValue.GetGlobalVariables();
            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                string query = @"Select CODE,NAME from CITY_MAST where Active=1 order by name ";
                var cmbLocation = _dropdownService.GetDropdownList(query);
                return Json(cmbLocation);
            }

        }





        [HttpGet]
        public async Task<object> GetViewData(DateTime FromDate, DateTime ToDate , string V_TYPE)
        {
            var gv = _globalValue.GetGlobalVariables();
            var dataList = new List<object>();
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_ImportExportDocAttachmentList", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "Viewdata");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", V_TYPE);
                        cmd.Parameters.Add("@FromDate", SqlDbType.SmallDateTime).Value = FromDate;
                        cmd.Parameters.Add("@ToDate", SqlDbType.SmallDateTime).Value = ToDate;

                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                dataList.Add(new
                                {
   
                                });
                            }
                        }
                    }
                }

                return new { success = true, data = dataList };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }












    }
}
