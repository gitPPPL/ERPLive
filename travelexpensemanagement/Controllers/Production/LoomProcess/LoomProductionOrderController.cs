using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Loom_Process.LoomProductionOrder;

namespace travelexpensemanagement.Controllers.Production.LoomProcess
{
    public class LoomProductionOrderController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IMemoryCache _cache;
        public LoomProductionOrderController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
     ModuleService.ModuleService moduleService, IMemoryCache cache)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _cache = cache;
        }
        public IActionResult Index()
        {
            return View("~/Views/Production/LoomProcess/LoomProductionOrder/Index.cshtml");
        }

        [HttpGet]
        public IActionResult ITEM()
        {
            var getData = _globalVariableService.GetGlobalVariables();

            string cacheKey = $"ITEM_DDL_{getData.PubCompCode}";

           
            if (!_cache.TryGetValue(cacheKey, out List<object> item))
            {
                Console.WriteLine("❌ CACHE MISS");
                string query = "select code, name from ITEM_MAST where COMP_CODE = " + getData.PubCompCode + " and active = 1";
                item = _dropdownService.GetDropdownList(query);
                var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30)).SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _cache.Set(cacheKey, item, cacheOptions);
            }
            else
            {
                Console.WriteLine("✅ CACHE HIT");
            }

                return Json(new { success = true, list = item });
        }

        [HttpGet]
        public IActionResult DocType()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select code,name from DOCTYPE_MAST WHERE code in ('LMPO')";
            var item = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = item });
        }
        
        [HttpGet]
        public IActionResult loom()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select code,name from machine_mast where comp_code=" + getData.PubCompCode + "and active=1 order by name";
            var loom = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = loom });
        }

        [HttpGet]
        public IActionResult itemNameDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();

            string cacheKey = $"ITEM_NAME_DDL_{getData.PubCompCode}";

            // Try to get from cache
            if (!_cache.TryGetValue(cacheKey, out List<object> data))
            {
                Console.WriteLine("❌ CACHE MISS");
                string query = @"
                        SELECT 
                            ITM.CODE, 
                            ITM.NAME, 
                            ITM.SIZE_CODE, 
                            ITS.NAME AS SIZE_NAME, 
                            ITM.GRAM_CODE, 
                            ITG.NAME AS GRAM_NAME, 
                            ITM.COLOR_CODE,
                            COLR.NAME AS COLOR_NAME,
                            ITM.PTYPE_CODE, 
                            ITP.NAME AS PTYPE_NAME 
                        FROM ITEM_MAST ITM 
                        LEFT JOIN ITEMSIZE_MAST ITS 
                            ON ITS.CODE = ITM.SIZE_CODE 
                            AND ITS.COMP_CODE = ITM.COMP_CODE
                        LEFT JOIN ITEMGRAM_MAST ITG 
                            ON ITG.CODE = ITM.GRAM_CODE 
                            AND ITG.COMP_CODE = ITM.COMP_CODE
                        LEFT JOIN COLOR_MAST COLR 
                            ON COLR.CODE = ITM.COLOR_CODE 
                            AND COLR.COMP_CODE = ITM.COMP_CODE
                        LEFT JOIN ITEMPTYPE_MAST ITP 
                            ON ITP.CODE = ITM.PTYPE_CODE 
                            AND ITP.COMP_CODE = ITM.COMP_CODE
                        WHERE ITM.COMP_CODE = " + getData.PubCompCode + @"
                        ORDER BY ITM.NAME";

                data = new List<object>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            data.Add(new
                            {
                                code = dr["CODE"].ToString(),
                                itemName = dr["NAME"].ToString(),

                                size = dr["SIZE_NAME"].ToString(),
                                gram = dr["GRAM_NAME"].ToString(),
                                color = dr["COLOR_NAME"].ToString(),
                                type = dr["PTYPE_NAME"].ToString(),

                                sizeCode = dr["SIZE_CODE"].ToString(),
                                gramCode = dr["GRAM_CODE"].ToString(),
                                colorCode = dr["COLOR_CODE"].ToString(),
                                typeCode = dr["PTYPE_CODE"].ToString()
                            });
                        }
                    }
                }
                // Store in cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(15))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5));

                _cache.Set(cacheKey, data, cacheOptions);
            }
            else
            {
                Console.WriteLine("✅ CACHE HIT");
            }

            return Json(new { success = true, list = data });
        }

        [HttpGet]
        public IActionResult Mesh()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE, NAME FROM MESH_MAST WHERE COMP_CODE ="+getData.PubCompCode + "ORDER BY NAME";
            var mesh = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = mesh });
        }

        public JsonResult GenerateVNo(string vType)
        {
            string newV_NO = "00001";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {

                    con.Open();

                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 from PROD_ORDER1 WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);

                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    lastVnoCmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                    object result = lastVnoCmd.ExecuteScalar();

                    int nextNo = Convert.ToInt32(result);

                    string runningPart = nextNo.ToString("D5");

                    newV_NO = prefixYR + runningPart;

                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { v_NO = newV_NO });
        }

        [HttpPost]
        public IActionResult SaveData([FromBody] LoomProductionOrder model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Model is null (binding failed)" });
            }

            var globalVariable= _globalVariableService.GetGlobalVariables();
            bool isUpdate = !string.IsNullOrEmpty(model.DOC_ID);
            string docId = model.DOC_ID;

            if (!isUpdate)
            {
                docId = model.V_TYPE + model.V_NO;
            }

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();
                    try
                    {
                        SqlCommand cmd = new SqlCommand("sp_Loom_ProductionOrder", con,trans);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@EFF_DATE", model.EFF_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@COMP_DATE", model.COMP_DATE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PROD_QTY", model.PROD_QTY ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@APPROX_MTR", model.APPROX_MTR ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@APPROX_KG", model.APPROX_KG ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NO_OF_LOOM", model.NO_OF_LOOM ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@Action", isUpdate ? "Update" : "InsertHeader");
                        cmd.ExecuteNonQuery();

                        foreach (var item in model.ItemList)
                        {
                            SqlCommand cmdItem = new SqlCommand("sp_Loom_ProductionOrder", con,trans);
                            cmdItem.CommandType = CommandType.StoredProcedure;
                            cmdItem.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmdItem.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmdItem.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmdItem.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmdItem.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            cmdItem.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                            cmdItem.Parameters.AddWithValue("@DOC_ID", docId);
                            cmdItem.Parameters.AddWithValue("@FITEM_CODE", item.FITEM_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@FITEM_NAME", item.FITEM_NAME ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@COLOR_CODE", item.COLOR_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@MITEM_NAME", item.MITEM_NAME ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@MITEM_CODE", item.MITEM_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@LOOM_CODE", item.LOOM_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@FEFF_DATE", item.FEFF_DATE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@EFF_SHIFT", item.EFF_SHIFT ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@SIZE_CODE", item.SIZE_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@PTYPE_CODE", item.PTYPE_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@GRAM_CODE", item.GRAM_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@MESH_CODE", item.MESH_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@STATUS", item.STATUS ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmdItem.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmdItem.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmdItem.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmdItem.Parameters.AddWithValue("@Action", "InsertFooter");

                            cmdItem.ExecuteNonQuery();

                        }
                        trans.Commit();
                        string message = isUpdate ? "Record Updated Successfully" : "Record Inserted Successfully";
                        return Json(new { success = true, message = message, isUpdate = isUpdate });
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new {success= false, message= ex.Message});
            }
        }

        [HttpGet]
        public IActionResult loadDataOnEdit(string docId)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new LoomProductionOrder();
            var items = new List<Item>();

            try 
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("sp_Loom_ProductionOrder", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@Action", "LoadDataOnEdit");

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        /* ================= HEADER ================= */
                        if (reader.Read())
                        {
                            model = new LoomProductionOrder
                            {
                                DOC_ID = reader["DOC_ID"]?.ToString(),
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                EFF_DATE = reader["EFF_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EFF_DATE"]) : (DateTime?)null,
                                COMP_DATE = reader["COMP_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["COMP_DATE"]) : (DateTime?)null,
                                ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : (int?)null,
                                PROD_QTY = reader["PROD_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["PROD_QTY"]) : (decimal?)null,
                                APPROX_MTR = reader["APPROX_MTR"] != DBNull.Value ? Convert.ToDecimal(reader["APPROX_MTR"]) : (decimal?)null,
                                APPROX_KG = reader["APPROX_KG"] != DBNull.Value ? Convert.ToDecimal(reader["APPROX_KG"]) : (decimal?)null,
                                NO_OF_LOOM = reader["NO_OF_LOOM"] != DBNull.Value ? Convert.ToInt32(reader["NO_OF_LOOM"]) : (int?)null,
                                REMARKS = reader["REMARKS"]?.ToString()
                            };
                        }

                        /* ================= DETAIL ================= */
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                items.Add(new Item
                                {
                                    FITEM_CODE = reader["FITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["FITEM_CODE"]) : (int?)null,
                                    FITEM_NAME = reader["FITEM_NAME"]?.ToString(),
                                    COLOR_CODE = reader["COLOR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COLOR_CODE"]) : (int?)null,
                                    MITEM_CODE = reader["MITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MITEM_CODE"]) : (int?)null,
                                    MITEM_NAME = reader["MITEM_NAME"]?.ToString(),
                                    LOOM_CODE = reader["LOOM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LOOM_CODE"]) : (int?)null,
                                    FEFF_DATE = reader["FEFF_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["FEFF_DATE"]) : (DateTime?)null,
                                    EFF_SHIFT = reader["EFF_SHIFT"]?.ToString(),
                                    SIZE_CODE = reader["SIZE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["SIZE_CODE"]) : (int?)null,
                                    PTYPE_CODE = reader["PTYPE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PTYPE_CODE"]) : (int?)null,
                                    GRAM_CODE = reader["GRAM_CODE"] != DBNull.Value ? Convert.ToDecimal(reader["GRAM_CODE"]) : (decimal?)null,
                                    MESH_CODE = reader["MESH_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MESH_CODE"]) : (int?)null,
                                    STATUS = reader["STATUS"]?.ToString()
                                });
                            }
                        }
                    }
                }
                return Json(new { success = true, header = model, items = items });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
