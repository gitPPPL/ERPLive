using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Loom_Process.LoomPPMMeterEntry;

namespace travelexpensemanagement.Controllers.Production.LoomProcess
{
    public class LoomPPMMeterEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public LoomPPMMeterEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
     ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Production/LoomProcess/LoomPPMMeterEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult Place()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select code, name from PLACE_MAST where COMP_CODE=" + getData.PubCompCode + "order by code";
            var place = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = place });
        }

        [HttpGet]
        public IActionResult Superwisor()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = @"SELECT CODE AS Value, LTRIM(RTRIM(NAME)) + SPACE(30 - LEN(LTRIM(RTRIM(NAME)))) + ' | ' + CAST(CODE AS VARCHAR) AS Text FROM EMP_MAST 
                              WHERE RESIGN_DATE IS NULL AND COMP_CODE = " + getData.PubCompCode + @" AND ACTIVE = 1 ORDER BY NAME";
            var place = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = place });
        }

        [HttpGet]
        public IActionResult block()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT DISTINCT BLOCK AS Value,BLOCK AS Text FROM Machine_Mast WHERE Comp_code = 2 AND Type = 'Loom' AND BLOCK IS NOT NULL ORDER BY BLOCK";
            var block = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = block });
        }

        [HttpGet]
        public IActionResult loomDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select code ,name from MACHINE_MAST where COMP_CODE=2 and active=1 and type='Loom' order by name";
            var loom = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = loom });
        }

        [HttpGet]
        public IActionResult MeshDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select code, name from MESH_MAST where COMP_CODE=1 order by code";
            var mesh = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = mesh });
        }

        [HttpGet]
        public IActionResult ColorDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select code, name from COLOR_MAST where COMP_CODE=1 and active=1 order by code";
            var mesh = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = mesh });
        }

        [HttpGet]
        public IActionResult itemNameDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();

            string query = @"
                            SELECT 
                                a.Code,
                                a.Shortname AS ItemName,
                                d.NAME AS PType,
                                a.INCH AS Width,
                                f.NAME AS Gram,
                                c.NAME AS Color,
                                a.PTYPE_CODE,
                                a.COLOR_CODE
                            FROM item_mast a
                            INNER JOIN ITEM_MGROUP b ON a.mGROUP_CODE = b.CODE 
                                AND b.COMP_CODE = 1 
                                AND b.MGROUP_TYPE IN ('Finish')
                            LEFT JOIN COLOR_MAST c ON a.COLOR_CODE = c.CODE AND c.COMP_CODE = 1
                            LEFT JOIN ITEMPTYPE_MAST d ON a.PTYPE_CODE = d.CODE AND d.COMP_CODE = 1
                            LEFT JOIN ITEMGRAM_MAST f ON a.GRAM_CODE = f.CODE AND f.COMP_CODE = 1
                            WHERE a.Active = 1 AND a.COMP_CODE = 1
                            ORDER BY a.Shortname";

            var data = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            data.Add(new
                            {
                                code = dr["Code"].ToString(),
                                itemName = dr["ItemName"].ToString(),
                                pType = dr["PType"].ToString(),
                                width = dr["Width"].ToString(),
                                gram = dr["Gram"].ToString(),
                                //color = dr["Color"].ToString(),
                                //pTypeCode = dr["PTYPE_CODE"].ToString(),
                                //colorCode = dr["COLOR_CODE"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(new { success = true, data });
        }

        public JsonResult GenerateVNo()
        {
            string newV_NO = "00001";
            string vType = "LMPM";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Year Prefix
                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                    string lastV_NO_Query = "select isnull(max(CAST(RIGHT(V_NO,5) AS INT)),0)+1 from PROD1_PPMMETER where V_TYPE = @V_TYPE and COMP_CODE = @COMP_CODE and BRANCH_CODE = @BRANCH_CODE and YEAR_CODE = @YEAR_CODE";

                    SqlCommand cmd = new SqlCommand(lastV_NO_Query, con);

                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                    object result = cmd.ExecuteScalar();

                    int nextNo = Convert.ToInt32(result);

                    newV_NO = prefixYR + nextNo.ToString("D5");
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error generating V_NO: " + ex.Message });
            }

            return Json(new { v_NO = newV_NO, v_TYPE = vType });
        }
        
        [HttpPost]
        public IActionResult SaveAndUpdateData([FromBody] LoomPPMMeterEntry model)
        {
    
            if (model == null)
            {
                return BadRequest("Model binding failed");
            }

            var globalVariable = _globalVariableService.GetGlobalVariables();
            string vType = "LMPM";

            bool isUpdate = !string.IsNullOrEmpty(model.DOC_ID);
            string docId = model.DOC_ID;

            if (!isUpdate)
            {
                docId = vType + model.V_NO;
            }

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();

                    try
                    {
                        SqlCommand cmd = new SqlCommand("sp_LoomPPM_MeterEntry", con, trans);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMP_CODE", model.EMP_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT ?? (object)DBNull.Value);
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
                            SqlCommand cmdItem = new SqlCommand("sp_LoomPPM_MeterEntry", con, trans);
                            cmdItem.CommandType = CommandType.StoredProcedure;
                            cmdItem.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmdItem.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmdItem.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmdItem.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmdItem.Parameters.AddWithValue("@V_TYPE", vType);
                            cmdItem.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                            cmdItem.Parameters.AddWithValue("@DOC_ID", docId);
                            cmdItem.Parameters.AddWithValue("@FSHIFT", model.SHIFT ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@FPLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@LOOM_CODE", item.LOOM_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@LOOM_TYPE", item.LOOM_TYPE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@FEMP_CODE", item.FEMP_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@ITEM_CODE", item.ITEM_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@PTYPE_NAME", item.PTYPE_NAME ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@WIDTH", item.WIDTH ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@GRAM", item.GRAM ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@MESH", item.MESH ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@MESH_CODE", item.MESH_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@COLOR_CODE", item.COLOR_CODE ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@COLOR_NAME", item.COLOR_NAME ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@DNR", item.DNR ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@OPRD", item.OPRD ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@CLRD", item.CLRD ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@PRDN", item.PRDN ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@PPM", item.PPM ?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@FREMARKS", item.FREMARKS?? (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@READING_TIME", item.READING_TIME ?? (object)DBNull.Value);

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
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult loadDataOnEdit(string docId)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new LoomPPMMeterEntry();
            var items = new List<Item>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_LoomPPM_MeterEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@Action", "LoadEdit");

                        con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            /* ================= HEADER ================= */
                            if (reader.Read())
                            {
                                model = new LoomPPMMeterEntry
                                {
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                    V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                    SHIFT = reader["SHIFT"]?.ToString(),
                                    PLACE_CODE = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : null,
                                    EMP_CODE = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : null,
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
                                        DOC_ID = reader["DOC_ID"]?.ToString(),
                                        V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                        LOOM_CODE = reader["LOOM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LOOM_CODE"]) : (int?)null,
                                        LOOM_TYPE = reader["LOOM_TYPE"]?.ToString(),
                                        FEMP_CODE = reader["FEMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["FEMP_CODE"]) : (int?)null,
                                        ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : (int?)null,
                                        PTYPE_CODE = reader["PTYPE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PTYPE_CODE"]) : (int?)null,
                                        PTYPE_NAME = reader["PTYPE_NAME"]?.ToString(),
                                        WIDTH = reader["WIDTH"] != DBNull.Value ? Convert.ToDecimal(reader["WIDTH"]) : (decimal?)null,
                                        GRAM = reader["GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["GRAM"]) : (decimal?)null,
                                        MESH = reader["MESH"]?.ToString(),
                                        MESH_CODE = reader["MESH_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MESH_CODE"]) : (int?)null,
                                        COLOR_CODE = reader["COLOR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COLOR_CODE"]) : (int?)null,
                                        COLOR_NAME = reader["COLOR_NAME"]?.ToString(),
                                        DNR = reader["DNR"] != DBNull.Value ? Convert.ToDecimal(reader["DNR"]) : (decimal?)null,
                                        OPRD = reader["OPRD"] != DBNull.Value ? Convert.ToDecimal(reader["OPRD"]) : (decimal?)null,
                                        CLRD = reader["CLRD"] != DBNull.Value ? Convert.ToDecimal(reader["CLRD"]) : (decimal?)null,
                                        PRDN = reader["PRDN"] != DBNull.Value ? Convert.ToDecimal(reader["PRDN"]) : (decimal?)null,
                                        PPM = reader["PPM"] != DBNull.Value ? Convert.ToInt32(reader["PPM"]) : (int?)null,
                                        FREMARKS = reader["FREMARKS"].ToString(),
                                        READING_TIME = reader["READING_TIME"] != DBNull.Value ? Convert.ToDateTime(reader["READING_TIME"]) : (DateTime?)null,
                                    });
                                }
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

        [HttpGet]
        public IActionResult ImportData(string block, int compCode, int branchCode, int yearCode)
        {
            var list = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT a.LOOM_CODE,  b.NAME AS LOOM_NAME, a.EMP_CODE, c.NAME AS EMP_NAME, d.SHORTNAME AS ITEM_NAME, a.PTYPE_NAME,
                a.WIDTH, a.GRAM, a.MESH, a.COLOR_NAME, a.DNR FROM PROD2 a LEFT JOIN MACHINE_MAST b ON a.LOOM_CODE = b.CODE AND b.COMP_CODE = a.COMP_CODE
                LEFT JOIN EMP_MAST c ON a.EMP_CODE = c.CODE AND c.COMP_CODE = a.COMP_CODE LEFT JOIN ITEM_MAST d ON a.ITEM_CODE = d.CODE AND d.COMP_CODE = a.COMP_CODE
                WHERE b.Block = @Block AND a.COMP_CODE = @CompCode AND a.BRANCH_CODE = @BranchCode AND a.YEAR_CODE = @YearCode ORDER BY b.NAME";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Block", block);
                cmd.Parameters.AddWithValue("@CompCode", compCode);
                cmd.Parameters.AddWithValue("@BranchCode", branchCode);
                cmd.Parameters.AddWithValue("@YearCode", yearCode);

                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new
                    {
                        loomCode = reader["LOOM_CODE"],
                        loomName = reader["LOOM_NAME"],
                        empCode = reader["EMP_CODE"],
                        empName = reader["EMP_NAME"],
                        itemName = reader["ITEM_NAME"],
                        type = reader["PTYPE_NAME"],
                        width = reader["WIDTH"],
                        gram = reader["GRAM"],
                        mesh = reader["MESH"],
                        color = reader["COLOR_NAME"],
                        dnr = reader["DNR"]
                    });
                }
            }

            return Json(new { success = true, data = list });
        }

    }
}
