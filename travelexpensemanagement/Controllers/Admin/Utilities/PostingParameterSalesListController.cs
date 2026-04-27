using iTextSharp.text.pdf.parser.clipper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class PostingParameterSalesListController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public PostingParameterSalesListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }
        
        public IActionResult Index()
        {
            ViewBag.CurrentMenu = "Posting Parameter Sales";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Admin/Utilities/PostingParameterSalesList/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPostingParameterSalesList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var companyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", companyCd},
                    {"@Action", "PostingParameterSalesEntryList" }
                };

                var fullList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PostingParameterEntry]", parameter);

                //string strqry = $@"
                //select DOC_TYPE,POST_TYPE,FORM_CODE,c.name Doctype,b.name Formname,
                // saleac.name Sales_Act,discac.name Disc_Act, 
                // cgstac.name CGST_Act,sgstac.name SGST_Act,igstac.name IGST_Act,frtac.name Freight_Act,rndoffac.name RoundOff_Act,insuac.name Insurance_Act, 
                // tdsac.name TDS_Act,tcsac.name TCS_Act,
                // qltydrac.name QltyDebit_Act,qcdrac.name QCDebit_Act,qtydrac.name QtyDebit_Act, 
                // rdiffac.name RateDiff_Act,cgstRCMac.name CGSTRCM_Act,sgstRCMac.name SGSTRCM_Act,igstRCMac.name IGSTRCM_Act,cessac.name Cess_Act,packac.name Pack_Act, 
                // billholdac.name BillHold_Act,
                // gstholdac.name GSTHold_Act,tds194qac.name TDS194Q_Act, 
                // a.form_code,a.v_type,
                // a.sale_ac,a.disc_ac,a.cgst_ac,a.sgst_ac,a.igst_ac,a.freight_ac,a.round_ac,a.insu_ac,a.tds_ac, 
                // a.tcs_ac,
                // a.qltdr_ac,a.qcdr_ac,a.qtydr_ac,a.rddr_ac,a.cgst_ac_rcm,a.sgst_ac_rcm,a.igst_ac_rcm,a.cess_ac,a.pack_ac,
                // a.billhold_ac,a.gsthold_ac,a.tds_194qac
                // from posting_mast a  
                //       left join form_mast b on a.form_code=b.code and a.comp_code=b.comp_code  
                //       left join doctype_mast c on a.v_type=c.code 
                //       left join subgroup_mast saleac on a.sale_ac=saleac.code and a.comp_code=saleac.comp_code 
                //       left join subgroup_mast discac on a.disc_ac=discac.code and a.comp_code=discac.comp_code 
                //       left join subgroup_mast cgstac on a.cgst_ac=cgstac.code and a.comp_code=cgstac.comp_code 
                //       left join subgroup_mast sgstac on a.sgst_ac=sgstac.code and a.comp_code=sgstac.comp_code 
                //       left join subgroup_mast igstac on a.igst_ac=igstac.code and a.comp_code=igstac.comp_code 
                //       left join subgroup_mast frtac on a.freight_ac=frtac.code and a.comp_code=frtac.comp_code 
                //       left join subgroup_mast rndoffac on a.round_ac=rndoffac.code and a.comp_code=rndoffac.comp_code 
                //       left join subgroup_mast insuac on a.insu_ac=insuac.code and a.comp_code=insuac.comp_code 
                //       left join subgroup_mast tdsac on a.tds_ac=tdsac.code and a.comp_code=tdsac.comp_code 
                //       left join subgroup_mast tcsac on a.tcs_ac=tcsac.code and a.comp_code=tcsac.comp_code 
                //       left join subgroup_mast qltydrac on a.qltdr_ac=qltydrac.code and a.comp_code=qltydrac.comp_code 
                //       left join subgroup_mast qcdrac on a.qcdr_ac=qcdrac.code and a.comp_code=qcdrac.comp_code 
                //       left join subgroup_mast qtydrac on a.qtydr_ac=qtydrac.code and a.comp_code=qtydrac.comp_code 
                //       left join subgroup_mast rdiffac on a.rddr_ac=rdiffac.code and a.comp_code=rdiffac.comp_code 
                //       left join subgroup_mast cgstRCMac on a.cgst_ac_rcm=cgstRCMac.code and a.comp_code=cgstRCMac.comp_code 
                //       left join subgroup_mast sgstRCMac on a.sgst_ac_rcm=sgstRCMac.code and a.comp_code=sgstRCMac.comp_code 
                //       left join subgroup_mast igstRCMac on a.igst_ac_rcm=igstRCMac.code and a.comp_code=igstRCMac.comp_code 
                //       left join subgroup_mast cessac on a.cess_ac=cessac.code and a.comp_code=cessac.comp_code 
                //       left join subgroup_mast packac on a.pack_ac=packac.code and a.comp_code=packac.comp_code 
                //       left join subgroup_mast billholdac on a.billhold_ac=billholdac.code and a.comp_code=billholdac.comp_code 
                //       left join subgroup_mast gstholdac on a.gsthold_ac=gstholdac.code and a.comp_code=gstholdac.comp_code 
                //       left join subgroup_mast tds194qac on a.tds_194qac=tds194qac.code and a.comp_code=tds194qac.comp_code 
                //       where a.comp_code= {companyCd} and isnull(a.FORM_CODE, 0) > 0 and isnull(POST_TYPE, '')<>''  order by DOCTYPE
                //";
                //var fullList = await _dbHelper.GetJsonDataAsync(strqry);
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    fullList = fullList
                        .Where(x =>
                        {
                            var dict = (IDictionary<string, object>)x;
                            string[] searchableKeys = { "Doctype" };
                            return searchableKeys.Any(key =>
                                dict.ContainsKey(key) &&
                                dict[key]?.ToString().ToLower().Contains(searchTerm) == true
                            );
                        })
                        .ToList();
                }
                var totalCount = fullList.Count;
                var pagedList = fullList
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new { status = true, data = pagedList, totalCount });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePostingParameterSalesEntry(string vType, string postType, int formCode)
        {
            try
            {
                
                var userSession = _globalValue.GetGlobalVariables();               
                using (var con = _dbcontext.GetErpConnection())
                {
                    try
                    {
                        string query = "DELETE FROM POSTING_MAST WHERE COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND POST_TYPE=@POST_TYPE  and FORM_CODE=@FORM_CODE AND V_TYPE=@V_TYPE";
                        using (var cmd = new SqlCommand(query, con))
                        {                           
                            cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = userSession.PubCompCode;
                            cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = 1;
                            cmd.Parameters.Add("@POST_TYPE", SqlDbType.VarChar, 50).Value = postType;
                            cmd.Parameters.Add("@FORM_CODE", SqlDbType.VarChar, 50).Value = formCode;
                            cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar, 50).Value = vType;
                            await con.OpenAsync();
                            await cmd.ExecuteNonQueryAsync();
                        }
                        return Json(new { status = true, data = "Data deleted successfully" });
                    }
                    catch (Exception ex)
                    {
                        return Json(new { status = false, message = $"Delete failed: {ex.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPostingParameterSalesEntryDetails(string vType, string postType, int formCode)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                if (string.IsNullOrEmpty(vType))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@BRANCH_CODE", 1},
                    {"@V_TYPE",  vType},
                    {"@POST_TYPE", postType},
                    {"@FORM_CODE", formCode},
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PostingParameterEntry]", parameter);
                return Json(new { status = true, data = entryDetailList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportAllDocs()
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PostingParameterEntry]", parameter);

                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }



    }
}
