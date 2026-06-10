using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class LoomFabricWidthEntryController : Controller
    {
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly ILoomFabricWidthEntryRepository _loomFabricWidthEntry;
        private readonly GlobalValidationdate _globalValidationdate;
        public LoomFabricWidthEntryController(DataBaseConnection dbcontext, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, ILoomFabricWidthEntryRepository loomFabricWidthEntry, GlobalValidationdate globalValidationdate)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _loomFabricWidthEntry = loomFabricWidthEntry;
            _globalValidationdate = globalValidationdate;
        }

        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbcontext.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVariables = _globalValue.GetGlobalVariables();

            ViewBag.GlobalVariables = globalVariables;
            return View("~/Views/QualityControl/Transaction/LoomFabricWidthEntry/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetMaxVNo()
        {
            try
            {
                var data = await _loomFabricWidthEntry.GetMaxVNoAsync();

                return Json(new
                {
                    status = true,
                    data = data
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemList()
        {
            try
            {
                var companyCode = _globalValue.GetGlobalVariables().PubCompCode;
                string query = $@"
                SELECT 
                    a.Code AS CODE,
                    a.Shortname AS NAME,
                    d.NAME AS PType,
                    ROUND(((ISNULL(e.INCH,0)+0.5)*25.4),0) AS Width,
                    f.NAME AS Gram,
                    c.NAME AS Color,
                    a.PTYPE_CODE,
                    a.COLOR_CODE
                FROM item_mast a
                INNER JOIN ITEM_MGROUP b 
                    ON a.mGROUP_CODE = b.CODE 
                   AND b.COMP_CODE = {companyCode} 
                   AND b.MGROUP_TYPE IN ('Finish')
                LEFT JOIN COLOR_MAST c 
                    ON a.COLOR_CODE = c.CODE 
                   AND c.COMP_CODE = {companyCode}
                LEFT JOIN ITEMPTYPE_MAST d 
                    ON a.PTYPE_CODE = d.CODE 
                   AND d.COMP_CODE = {companyCode}
                LEFT JOIN ITEMSIZE_MAST e 
                    ON a.SIZE_CODE = e.CODE 
                   AND e.COMP_CODE = {companyCode}
                LEFT JOIN ITEMGRAM_MAST f 
                    ON a.GRAM_CODE = f.CODE
                   AND f.COMP_CODE = {companyCode}
                WHERE a.Active = 1 
                  AND a.COMP_CODE = {companyCode}
                GROUP BY 
                    a.Shortname,
                    a.CODE,
                    a.COLOR_CODE,
                    c.NAME,
                    a.PTYPE_CODE,
                    d.NAME,
                    ROUND(((ISNULL(e.INCH,0)+0.5)*25.4),0),
                    f.NAME
                ORDER BY a.Shortname";

                var item = await _dbHelper.GetJsonDataAsync(query);

                return Json(new { status = true, data = item });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateLoomFabricWidthEntry([FromBody] LoomFabricEntryModel model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request." });

            var result = await _loomFabricWidthEntry.SaveOrUpdateLoomFabricEntryAsync(model);

            return Json(new
            {
                status = result.Status,
                message = result.Message
            });
        }

        [HttpGet]
        public IActionResult ImportWidth(string shift, int placeCode, DateTime vDate, string vTime)
        {
            var userSession = _globalValue.GetGlobalVariables();

            try
            {
                DateTime baseDateTime = vDate;

                if (!string.IsNullOrEmpty(vTime))
                {
                    TimeSpan ts = TimeSpan.Parse(vTime);
                    baseDateTime = vDate.Date.Add(ts);
                }

                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_GetLoomFabricEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@SHIFT", shift);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", placeCode);

                        //IMPORTANT FIX
                        cmd.Parameters.AddWithValue("@VDATE", vDate.Date);
                        cmd.Parameters.AddWithValue("@FROM_TIME", baseDateTime);

                        cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                        cmd.Parameters.AddWithValue("@Action", "ImportWidth");

                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);

                            var list = dt.AsEnumerable()
                                .Select(row => dt.Columns.Cast<DataColumn>()
                                .ToDictionary(col => col.ColumnName, col => row[col]))
                                .ToList();

                            return Json(new { status = true, data = list });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLastEntry()
        {
            try
            {
                var data = await _loomFabricWidthEntry.GetLastQCEntryAsync();

                if (data == null)
                {
                    return Json(new
                    {
                        status = false,
                        message = "No previous record found"
                    });
                }

                return Json(new
                {
                    status = true,
                    data = data
                });
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

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalValue.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("PROD1_QC", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpPost]
        public IActionResult PrintLoom(LoomFabricEntryModel model)
        {
            var globalValue = _globalValue.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_GetLoomFabricEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "LoomFabricReport");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalValue.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalValue.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalValue.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "LINS");

                    cmd.Parameters.AddWithValue("@VDATE", model.V_DATE);
                    cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT);
                    cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE);

                    con.Open();

                    cmd.ExecuteNonQuery();  

                    return Json(new
                    {
                        status = true,
                        message = "Temp table prepared successfully"
                    });
                }
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
    }
}
