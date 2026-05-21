using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.LaminationProcess;

namespace travelexpensemanagement.Controllers.Production.LaminationProcess
{
    public class RollLaminationEntryController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        public RollLaminationEntryController(GlobalVariableService globalVariableService, DataBaseConnection dbConnection, DropdownService dropdownService,
            DbHelper dbHelper)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            return View("~/Views/Production/LaminationProcess/RollLaminationEntry/Index.cshtml");
        }
        [HttpGet]
        public async Task<IActionResult> GetddlUnLaminatedItem()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"SELECT A.CODE as Code, A.SHORTNAME as Name FROM ITEM_MAST A LEFT JOIN ITEM_GROUP B ON A.GROUP_CODE = B.CODE AND A.COMP_CODE = B.COMP_CODE WHERE A.ACTIVE = 1 AND A.COMP_CODE = {compCode} AND B.GROUP_TYPE in ('Finish Fabric UnLam','Finish HD UnLam','Finish HDPE UnLam') ORDER BY A.SHORTNAME";
            var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { status = true, data = dataList });
        }
        [HttpGet]
        public async Task<IActionResult> GetddlLaminatedItem()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"SELECT A.CODE as Code, A.SHORTNAME as Name FROM ITEM_MAST A LEFT JOIN ITEM_GROUP B ON A.GROUP_CODE = B.CODE AND A.COMP_CODE = B.COMP_CODE WHERE A.ACTIVE = 1 AND A.COMP_CODE = {compCode} AND B.GROUP_TYPE in ('Finish Fabric Lam','Finish HD Lam','Finish HDPE Lam') ORDER BY A.SHORTNAME";
            var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { status = true, data = dataList });
        }
        [HttpGet]
        public async Task<JsonResult> GetPendingLaminationRecord(string filterText)        
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var PendingList = new List<RollLaminationPendingRecordModel>();
            string query = @"Select B.SHORTNAME as ItemName, A.BALE_NO as RollNo, A.MTR as Meter, A.GROSS_QTY as GrossWt, A.TARE_QTY as TareWt, 
                            A.QTY as NetWt, A.MAC_CODE as LoomNo, A.MAC_TYPE as LoomType from PRODUCTION2 A
                            LEFT JOIN ITEM_MAST B ON A.ITEM_CODE = B.CODE AND A.COMP_CODE = B.COMP_CODE
                            WHERE V_TYPE in ('FLIS','FPJI') AND A.COMP_CODE = @COMP_CODE AND A.BRANCH_CODE = @BRANCH_CODE AND A.YEAR_CODE = @YEAR_CODE";
            try
            {
                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = con;
                    if (!string.IsNullOrEmpty(filterText))
                    {
                        query += @" AND B.SHORTNAME like @filterText";
                        cmd.Parameters.AddWithValue("@filterText", "%" + filterText + "%");
                    }
                    query += @" Order BY B.SHORTNAME";
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.CommandText = query;
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            PendingList.Add(new RollLaminationPendingRecordModel
                            {
                                itemName = reader["ItemName"].ToString(),
                                rollNo = reader["RollNo"].ToString(),
                                meter = reader["Meter"] != DBNull.Value ? Convert.ToDecimal(reader["Meter"]) : (decimal?)null,
                                grossWt = reader["GrossWt"] != DBNull.Value ? Convert.ToDecimal(reader["GrossWt"]) : (decimal?)null,
                                tareWt = reader["TareWt"] != DBNull.Value ? Convert.ToDecimal(reader["TareWt"]) : (decimal?)null,
                                netWt = reader["NetWt"] != DBNull.Value ? Convert.ToDecimal(reader["NetWt"]) : (decimal?)null,
                                loomNo = reader["LoomNo"] != DBNull.Value ? Convert.ToInt32(reader["LoomNo"]) : (int?)null,
                                loomType = reader["LoomType"]?.ToString()
                            });
                        }
                    }
                    return Json(new { success = true, pendingList = PendingList });
                }
            }
            catch(Exception ex)
            {
                return Json(new { error = true, message = ex.Message }); 
            }
        }
        [HttpGet]
        public JsonResult GetVNo()
        {
            string newV_NO = "00000";
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
                    string lastV_NO_Query = "SELECT MAX(V_NO) FROM LAMINATION WHERE COMP_CODE = @CompCode AND YEAR_CODE = @YearCode  and BRANCH_CODE = @BRANCH_CODE and V_TYPE = 'RLAM'";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    object result = lastVnoCmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        int lastV_NO = Convert.ToInt32(result);
                        newV_NO = (lastV_NO + 1).ToString("D5");
                    }
                    else
                    {
                        newV_NO = prefixYR + "00001";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }
        [HttpGet]
        public JsonResult GetddlPlace()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"SELECT CODE, NAME FROM PLACE_MAST WHERE COMP_CODE = {compCode}";
            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }
        [HttpGet]
        public async Task<IActionResult> GetddlOrderNo()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $"Select V_no as Code, V_TYPE+cast(V_NO as varchar) as Name, V_Type from LAMINATION where V_Type ='RLAM' and COMP_CODE={globalVar.PubCompCode} and YEAR_CODE={globalVar.PubFYearCode} and BRANCH_CODE={globalVar.PubBranchCode} order by V_no";
            var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { status = true, data = dataList });
        }
        [HttpGet]
        public async Task<IActionResult> GetddlLoomNo(int placeCode)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"select Code, Name from MACHINE_MAST where COMP_CODE={compCode} and Type='Loom' and Place_Code={placeCode} order by name";
            var dataList = await _dbHelper.GetJsonDataAsync(query);     
            return Json(new { status = true, data = dataList });
        }
        [HttpGet]
        public async Task<IActionResult> GetddlMachineNo(int placeCode)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"select Code, Name from MACHINE_MAST where COMP_CODE={compCode} and Type='Lamination' and active > 0 and Place_Code={placeCode} order by name";
            var dataList = await _dbHelper.GetJsonDataAsync(query);     
            return Json(new { status = true, data = dataList });
        }
        [HttpGet]
        public JsonResult GetddlStatus()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $"select Code, Name from STATUS_MAST where COMP_CODE={compCode} and ACTIVE=1";
            var result = _dropdownService.GetDropdownList(query);
            return Json(result);
        }
        [HttpGet]
        public JsonResult GetItemSize(int itemCode)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            decimal ItemSize = 0m;
            try
            {
                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string query = @"select isnull(INCH,0) as ItemSize from ITEM_MAST where CODE=@ItemCode and active=1 and comp_code=@Comp_Code";
                    using(SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                        cmd.Parameters.AddWithValue("@Comp_Code", compCode);
                        ItemSize = (decimal)cmd.ExecuteScalar();
                    }
                    return Json(new { success = true, itemSize = ItemSize });
                }
            }
            catch(Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult SaveRollLamination([FromBody] RollLaminationEntryModel model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_RollLamination", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", "INSERT");
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                            cmd.Parameters.AddWithValue("@V_TYPE", "RLAM");
                            cmd.Parameters.AddWithValue("@V_NO", model.vNo);
                            cmd.Parameters.AddWithValue("@DOC_ID", "RLAM" + model.vNo);
                            cmd.Parameters.AddWithValue("@JOB", model.jobWork);
                            cmd.Parameters.AddWithValue("@SHIFT", model.shiftBefore);
                            cmd.Parameters.AddWithValue("@V_DATE", model.dateBefore);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", model.itemBefore);
                            cmd.Parameters.AddWithValue("@ITEM_NAME", model.itemNameBefore);
                            cmd.Parameters.AddWithValue("@ROLL_NO", model.rollNoBefore);
                            cmd.Parameters.AddWithValue("@MTR", model.meterBefore);
                            cmd.Parameters.AddWithValue("@GR_WT", model.grossBefore);
                            cmd.Parameters.AddWithValue("@TR_WT", model.tareBefore);
                            cmd.Parameters.AddWithValue("@NT_WT", model.netBefore);
                            cmd.Parameters.AddWithValue("@AV_WT", model.avgBefore);
                            cmd.Parameters.AddWithValue("@GRAM", model.gramBefore);
                            cmd.Parameters.AddWithValue("@PSIZE", model.sizeBefore);
                            cmd.Parameters.AddWithValue("@LOOM_CODE", model.loomNo);
                            cmd.Parameters.AddWithValue("@REMARKS", model.remarksBefore);
                            cmd.Parameters.AddWithValue("@PLACE_CODE", model.placeCode);
                            //cmd.Parameters.AddWithValue("@V_NO", model.pordNo);
                            cmd.Parameters.AddWithValue("@SHIFT_A", model.shiftAfter);
                            cmd.Parameters.AddWithValue("@V_DATE_A", model.dateAfter);
                            cmd.Parameters.AddWithValue("@ITEM_CODE_A", model.itemAfter);
                            cmd.Parameters.AddWithValue("@ITEM_NAME_A", model.itemNameAfter);
                            cmd.Parameters.AddWithValue("@ROLL_NO_A", model.rollNoAfter);
                            cmd.Parameters.AddWithValue("@MTR_A", model.meterAfter);
                            cmd.Parameters.AddWithValue("@LOT_NO_A", model.batchNo);
                            cmd.Parameters.AddWithValue("@GR_WT_A", model.grossAfter);
                            cmd.Parameters.AddWithValue("@TR_WT_A", model.tareAfter);
                            cmd.Parameters.AddWithValue("@NT_WT_A", model.netAfter);
                            cmd.Parameters.AddWithValue("@AV_WT_A", model.avgAfter);
                            cmd.Parameters.AddWithValue("@GRAM_A", model.gramAfter);
                            cmd.Parameters.AddWithValue("@PSIZE_A", model.sizeAfter);
                            cmd.Parameters.AddWithValue("@LAM_CODE", model.machineNo);
                            cmd.Parameters.AddWithValue("@STATUS_CODE_A", model.status);
                            cmd.Parameters.AddWithValue("@REMARKS_A", model.remarksAfter);
                           
                            cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                        return Json(new { success = true, message = "Inserted Successfully!" });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return Json(new { error = true, message = ex.Message });
                    }
                }
            }
        }
        [HttpGet]
        public JsonResult ValidatePendingRoll(string itemCode, string rollNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            bool IsExist = false;
            try
            {
                string query = @"select 1 from PRODUCTION2 A
                                LEFT JOIN ITEM_MAST B ON A.ITEM_CODE = B.CODE AND A.COMP_CODE = B.COMP_CODE
                                where V_TYPE in ('FLIS','FPJI') and A.COMP_CODE = @COMP_CODE and A.BRANCH_CODE = @BRANCH_CODE and A.YEAR_CODE = @YEAR_CODE
                                AND A.ITEM_CODE = @ITEM_CODE AND A.BALE_NO = @BALE_NO";
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using(SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@BALE_NO", rollNo);
                        var exists = cmd.ExecuteScalar();

                        if (exists != null)
                        {
                            IsExist = true;
                        }
                    }
                    return Json(new { success = true, isExist = IsExist });
                }
            }
            catch(Exception ex)
            {
                return Json(new {error = true, message = ex.Message});
            }
        }
        public JsonResult ValidateRollExist(string itemCode, string rollNo, int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            bool IsExist = false;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string query1 = @"select 1 from LAMINATION 
                                where V_TYPE='RLAM' and V_NO <> @VNo and COMP_CODE = @COMP_CODE and BRANCH_CODE = @BRANCH_CODE and 
                                YEAR_CODE = @YEAR_CODE AND ITEM_CODE = @ITEM_CODE AND ROLL_NO = @ROLL_NO";
                    using (SqlCommand cmd = new SqlCommand(query1, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@ROLL_NO", rollNo);
                        cmd.Parameters.AddWithValue("@VNo", vNo);
                        var exists = cmd.ExecuteScalar();

                        if (exists != null)
                        {
                            IsExist = true;
                        }
                    }
                    return Json(new { success = true, isExist = IsExist });
                }
            }
            catch(Exception ex)
            {
                return Json(new {error = true, message = ex.Message});
            }
        }
        [HttpGet]
        public JsonResult GetRollDetails(string itemCode, string rollNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var RollDetails = new RollRecordBeforeLaminationModel();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    string query1 = @"Select MTR, GROSS_QTY, TARE_QTY, qty, AVG_QTY, GRAM, MAC_CODE from PRODUCTION2 
                                     where V_TYPE in ('FLIS','FPJI') and isnull(LMRC_No,0)=0 and ITEM_CODE=@ITEM_CODE
                                     and BALE_NO=@ROLL_NO and COMP_CODE=@COMP_CODE and BRANCH_CODE=@BRANCH_CODE
                                     and YEAR_CODE=@YEAR_CODE";
                    using (SqlCommand cmd = new SqlCommand(query1, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@ROLL_NO", rollNo);
                        using(SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                RollDetails.meter = reader["MTR"] != DBNull.Value ? Convert.ToDecimal(reader["MTR"]) : (decimal?)null;
                                RollDetails.grossWt = reader["GROSS_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["GROSS_QTY"]) : (decimal?)null;
                                RollDetails.tareWt = reader["TARE_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["TARE_QTY"]) : (decimal?)null;
                                RollDetails.netWt = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]) : (decimal?)null;
                                RollDetails.avgWt = reader["AVG_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["AVG_QTY"]) : (decimal?)null;
                                RollDetails.gram = reader["GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["GRAM"]) : (decimal?)null;
                                RollDetails.loomNo = reader["MAC_CODE"] != DBNull.Value ? Convert.ToInt32(reader["MAC_CODE"]) : (int?)null;
                            }
                        }
                    }
                    return Json(new { success = true, rollDetails = RollDetails });
                }
            }
            catch(Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetBatchNo()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string BatchNo = "";
            try
            {
                if (compCode == "2")
                {
                    BatchNo = "PLPL";
                }
                else
                {
                    BatchNo = "";
                }
                    return Json(new { success = true, batchNo = BatchNo });
            }
            catch(Exception ex)
            {
                return Json(new { success = true, message = ex.Message });
            }
        }
        [HttpGet]
        public JsonResult GetRollDetailsByVno(int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var RollDetails = new RollLaminationEntryModel();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using(SqlCommand cmd = new SqlCommand("sp_RollLamination", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetByVno");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        using(SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                RollDetails.vNo = vNo;
                                RollDetails.jobWork = reader["JOB"] != DBNull.Value ? Convert.ToInt32(reader["JOB"]) : null;
                                RollDetails.shiftBefore = reader["SHIFT"]?.ToString();
                                RollDetails.dateBefore = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null;
                                RollDetails.itemBefore = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : null;
                                RollDetails.rollNoBefore = reader["ROLL_NO"]?.ToString();
                                RollDetails.meterBefore = reader["MTR"] != DBNull.Value ? Convert.ToInt32(reader["MTR"]) : null;
                                RollDetails.grossBefore = reader["GR_WT"] != DBNull.Value ? Convert.ToDecimal(reader["GR_WT"]) : null;
                                RollDetails.tareBefore = reader["TR_WT"] != DBNull.Value ? Convert.ToDecimal(reader["TR_WT"]) : null;
                                RollDetails.netBefore = reader["NT_WT"] != DBNull.Value ? Convert.ToDecimal(reader["NT_WT"]) : null;
                                RollDetails.avgBefore = reader["AV_WT"] != DBNull.Value ? Convert.ToDecimal(reader["AV_WT"]) : null;
                                RollDetails.gramBefore = reader["GRAM"] != DBNull.Value ? Convert.ToDecimal(reader["GRAM"]) : null;
                                RollDetails.sizeBefore = reader["PSIZE"] != DBNull.Value ? Convert.ToDecimal(reader["PSIZE"]) : null;
                                RollDetails.loomNo = reader["LOOM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LOOM_CODE"]) : null;
                                RollDetails.remarksBefore = reader["REMARKS"]?.ToString();
                                RollDetails.placeCode = reader["PLACE_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PLACE_CODE"]) : null;
                                RollDetails.shiftAfter = reader["SHIFT_A"]?.ToString();
                                RollDetails.dateAfter = reader["V_DATE_A"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE_A"]) : null;
                                RollDetails.itemAfter = reader["ITEM_CODE_A"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE_A"]) : null;
                                RollDetails.rollNoAfter = reader["ROLL_NO_A"]?.ToString();
                                RollDetails.meterAfter = reader["MTR_A"] != DBNull.Value ? Convert.ToInt32(reader["MTR_A"]) : null;
                                RollDetails.batchNo = reader["LOT_NO_A"]?.ToString();
                                RollDetails.grossAfter = reader["GR_WT_A"] != DBNull.Value ? Convert.ToDecimal(reader["GR_WT_A"]) : null;
                                RollDetails.tareAfter = reader["TR_WT_A"] != DBNull.Value ? Convert.ToDecimal(reader["TR_WT_A"]) : null;
                                RollDetails.netAfter = reader["NT_WT_A"] != DBNull.Value ? Convert.ToDecimal(reader["NT_WT_A"]) : null;
                                RollDetails.avgAfter = reader["AV_WT_A"] != DBNull.Value ? Convert.ToDecimal(reader["AV_WT_A"]) : null;
                                RollDetails.gramAfter = reader["GRAM_A"] != DBNull.Value ? Convert.ToDecimal(reader["GRAM_A"]) : null;
                                RollDetails.sizeAfter = reader["PSIZE_A"] != DBNull.Value ? Convert.ToDecimal(reader["PSIZE_A"]) : null;
                                RollDetails.machineNo = reader["LAM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LAM_CODE"]) : null;
                                RollDetails.status = reader["STATUS_CODE_A"] != DBNull.Value ? Convert.ToInt32(reader["STATUS_CODE_A"]) : null;
                                RollDetails.remarksAfter = reader["REMARKS_A"]?.ToString();
                            }
                        }
                    }
                    return Json(new { success = true, rollDetails = RollDetails });
                }
            }
            catch(Exception ex)
            {
                return Json(new { success = true, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpdateRollLamination([FromBody] RollLaminationEntryModel model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid data." });
            }
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_RollLamination", con, tran))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", "Update");
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                            cmd.Parameters.AddWithValue("@V_NO", model.vNo);
                            cmd.Parameters.AddWithValue("@JOB", model.jobWork);
                            cmd.Parameters.AddWithValue("@SHIFT", model.shiftBefore);
                            cmd.Parameters.AddWithValue("@V_DATE", model.dateBefore);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", model.itemBefore);
                            cmd.Parameters.AddWithValue("@ITEM_NAME", model.itemNameBefore);
                            cmd.Parameters.AddWithValue("@ROLL_NO", model.rollNoBefore);
                            cmd.Parameters.AddWithValue("@MTR", model.meterBefore);
                            cmd.Parameters.AddWithValue("@GR_WT", model.grossBefore);
                            cmd.Parameters.AddWithValue("@TR_WT", model.tareBefore);
                            cmd.Parameters.AddWithValue("@NT_WT", model.netBefore);
                            cmd.Parameters.AddWithValue("@AV_WT", model.avgBefore);
                            cmd.Parameters.AddWithValue("@GRAM", model.gramBefore);
                            cmd.Parameters.AddWithValue("@PSIZE", model.sizeBefore);
                            cmd.Parameters.AddWithValue("@LOOM_CODE", model.loomNo);
                            cmd.Parameters.AddWithValue("@REMARKS", model.remarksBefore);
                            cmd.Parameters.AddWithValue("@PLACE_CODE", model.placeCode);
                            cmd.Parameters.AddWithValue("@SHIFT_A", model.shiftAfter);
                            cmd.Parameters.AddWithValue("@V_DATE_A", model.dateAfter);
                            cmd.Parameters.AddWithValue("@ITEM_CODE_A", model.itemAfter);
                            cmd.Parameters.AddWithValue("@ITEM_NAME_A", model.itemNameAfter);
                            cmd.Parameters.AddWithValue("@ROLL_NO_A", model.rollNoAfter);
                            cmd.Parameters.AddWithValue("@MTR_A", model.meterAfter);
                            cmd.Parameters.AddWithValue("@LOT_NO_A", model.batchNo);
                            cmd.Parameters.AddWithValue("@GR_WT_A", model.grossAfter);
                            cmd.Parameters.AddWithValue("@TR_WT_A", model.tareAfter);
                            cmd.Parameters.AddWithValue("@NT_WT_A", model.netAfter);
                            cmd.Parameters.AddWithValue("@AV_WT_A", model.avgAfter);
                            cmd.Parameters.AddWithValue("@GRAM_A", model.gramAfter);
                            cmd.Parameters.AddWithValue("@PSIZE_A", model.sizeAfter);
                            cmd.Parameters.AddWithValue("@LAM_CODE", model.machineNo);
                            cmd.Parameters.AddWithValue("@STATUS_CODE_A", model.status);
                            cmd.Parameters.AddWithValue("@REMARKS_A", model.remarksAfter);

                            cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@AED", "E");
                            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.ExecuteNonQuery();
                        }
                        tran.Commit();
                        return Json(new { success = true, message = "Updated Successfully!" });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return Json(new { error = true, message = ex.Message });
                    }
                }
            }
        }
    }
}
