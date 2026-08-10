using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.AddAttachmentService;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.LoomMonitoring;

namespace travelexpensemanagement.Controllers.LoomMonitoringSystem
{
    public class LoomMonitoringSystemEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly FileHelper _filehelper;
        public LoomMonitoringSystemEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
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
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
            //return View("~/Views/LoomMonitoringSystem/LoomMonitoringSystemEntry/Index.cshtml");
            return View("~/Views/Production/Transaction/LoomMonitoringSystemEntry/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetLoomProductionInfo(string sCode, string gCode, DateTime? sdate)
        {
            DateTime onlyDate = sdate?.Date ?? DateTime.MinValue;
            var resultList = new List<LoomProductionInfo>();

            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    // Load shift order
                    var shiftsOrder = new List<string>();
                    string shiftQuery = @" SELECT SHIFT, MIN(CODE) AS MinCode FROM [SHIFT_MAST] GROUP BY SHIFT ORDER BY MinCode";

                    using (SqlCommand cmdShift = new SqlCommand(shiftQuery, conn))
                    using (SqlDataReader reader = cmdShift.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var shift = reader["SHIFT"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(shift))
                            {
                                shiftsOrder.Add(shift);
                            }
                        }
                    }
                    // Query to fetch loom data
                    string query = @"
                SELECT 
                    M.CODE AS LOOM_CODE, 
                    M.NAME AS MACHINE_NAME, 
                    P.EMP_CODE, 
                    E.NAME AS EMP_NAME, 
                    P.ITEM_CODE, 
                    I.NAME AS ITEM_NAME, 
                    P.DNR AS DENIER, 
                    P.MESH_CODE,
                    MS.NAME AS MESH_NAME,
                    P.SCH_SHIFT, 
                    P.V_DATE AS LAST_PROD_DATE,
                    P.V_NO,
                    P.V_TYPE,
                    P.DOC_ID
                FROM  MACHINE_MAST M
                OUTER APPLY (
                    SELECT TOP 1    
                        PR.EMP_CODE, 
                        PR.ITEM_CODE, 
                        PR.DNR, 
                        PR.MESH_CODE, 
                        PR.SCH_SHIFT, 
                        PR.V_DATE,
                        PR.V_NO,
                        PR.V_TYPE,
                        PR.DOC_ID
                    FROM  [LOOM_ALLOC] PR 
                    WHERE PR.LOOM_CODE = M.CODE 
                      AND PR.SCH_SHIFT = @Shift
                      AND PR.V_DATE = @SDate
                    ORDER BY PR.V_DATE DESC
                ) P
                LEFT JOIN  EMP_MAST E ON P.EMP_CODE = E.CODE
                LEFT JOIN  ITEM_MAST I ON P.ITEM_CODE = I.CODE
                LEFT JOIN  MESH_MAST MS ON P.MESH_CODE = MS.CODE
                WHERE M.BLOCK = @GroupCode 
                ORDER BY M.CODE; ";

                    List<LoomProductionInfo> GetDataForShift(string shift, DateTime date)
                    {
                        var list = new List<LoomProductionInfo>();
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Shift", shift);
                            cmd.Parameters.AddWithValue("@GroupCode", gCode);
                            cmd.Parameters.AddWithValue("@SDate", date);

                            using (var rdr = cmd.ExecuteReader())
                            {
                                while (rdr.Read())
                                {
                                    list.Add(new LoomProductionInfo
                                    {
                                        LOOM_CODE = rdr["LOOM_CODE"]?.ToString(),
                                        MACHINE_NAME = rdr["MACHINE_NAME"]?.ToString(),
                                        EMP_CODE = rdr["EMP_CODE"]?.ToString(),
                                        EMP_NAME = rdr["EMP_NAME"]?.ToString(),
                                        ITEM_CODE = rdr["ITEM_CODE"]?.ToString(),
                                        ITEM_NAME = rdr["ITEM_NAME"]?.ToString(),
                                        DENIER = rdr["DENIER"]?.ToString(),
                                        MESH_CODE = rdr["MESH_CODE"]?.ToString(),
                                        MESH_NAME = rdr["MESH_NAME"]?.ToString(),
                                        SCH_SHIFT = rdr["SCH_SHIFT"]?.ToString(),
                                        LAST_PROD_DATE = rdr["LAST_PROD_DATE"] != DBNull.Value
                                            ? Convert.ToDateTime(rdr["LAST_PROD_DATE"])
                                            : (DateTime?)null,
                                        V_NO = int.TryParse(rdr["V_NO"]?.ToString(), out int vno) ? vno : 0,
                                        V_TYPE = rdr["V_TYPE"]?.ToString(),
                                        DOC_ID = rdr["DOC_ID"]?.ToString()
                                    });
                                }
                            }
                        }
                        return list;
                    }

                    // Get initial data
                    var currentData = GetDataForShift(sCode, onlyDate);
                    bool isAllEmpEmpty = currentData.All(r => string.IsNullOrWhiteSpace(r.EMP_CODE));

                    if (!isAllEmpEmpty)
                    {
                        resultList = currentData;
                    }
                    else
                    {
                        Dictionary<string, LoomProductionInfo> latestPerLoom = new();

                        int daysBack = 0;
                        const int maxDaysBack = 10;

                        while (daysBack < maxDaysBack && latestPerLoom.Count < currentData.Count)
                        {
                            DateTime searchDate = onlyDate.AddDays(-daysBack);

                            foreach (var shift in shiftsOrder)
                            {
                                var historicalData = GetDataForShift(shift, searchDate);

                                foreach (var rec in historicalData)
                                {
                                    if (string.IsNullOrWhiteSpace(rec.EMP_CODE)) continue;

                                    if (!latestPerLoom.ContainsKey(rec.LOOM_CODE))
                                    {
                                        latestPerLoom[rec.LOOM_CODE] = rec;
                                    }
                                }

                                if (latestPerLoom.Count == currentData.Count)
                                    break;
                            }

                            daysBack++;
                        }

                        if (currentData.Count == 0)
                        {
                            resultList = latestPerLoom.Values
                                .Select(r => new LoomProductionInfo
                                {
                                    LOOM_CODE = r.LOOM_CODE,
                                    MACHINE_NAME = r.MACHINE_NAME,
                                    EMP_CODE = "",       
                                    EMP_NAME = "",
                                    ITEM_CODE = r.ITEM_CODE,
                                    ITEM_NAME = r.ITEM_NAME,
                                    DENIER = r.DENIER,
                                    MESH_CODE = r.MESH_CODE,
                                    MESH_NAME = r.MESH_NAME,
                                    SCH_SHIFT = r.SCH_SHIFT,
                                    LAST_PROD_DATE = r.LAST_PROD_DATE,
                                    V_NO = r.V_NO,
                                    V_TYPE = r.V_TYPE,
                                    DOC_ID = r.DOC_ID
                                })
                                .ToList();
                        }
                        else
                        {
                            foreach (var rec in currentData)
                            {
                                if (latestPerLoom.TryGetValue(rec.LOOM_CODE, out var fallbackRec))
                                {
                                    if (string.IsNullOrWhiteSpace(rec.EMP_CODE))
                                    {
                                        resultList.Add(new LoomProductionInfo
                                        {
                                            LOOM_CODE = fallbackRec.LOOM_CODE,
                                            MACHINE_NAME = fallbackRec.MACHINE_NAME,
                                            EMP_CODE = "",      
                                            EMP_NAME = "",
                                            ITEM_CODE = fallbackRec.ITEM_CODE,
                                            ITEM_NAME = fallbackRec.ITEM_NAME,
                                            DENIER = fallbackRec.DENIER,
                                            MESH_CODE = fallbackRec.MESH_CODE,
                                            MESH_NAME = fallbackRec.MESH_NAME,
                                            SCH_SHIFT = fallbackRec.SCH_SHIFT,
                                            LAST_PROD_DATE = fallbackRec.LAST_PROD_DATE,
                                            V_NO = fallbackRec.V_NO,
                                            V_TYPE = fallbackRec.V_TYPE,
                                            DOC_ID = fallbackRec.DOC_ID
                                        });
                                    }
                                    else
                                    {
                                        rec.ITEM_NAME ??= fallbackRec.ITEM_NAME;
                                        rec.DENIER ??= fallbackRec.DENIER;
                                        rec.MESH_NAME ??= fallbackRec.MESH_NAME;

                                        resultList.Add(rec);
                                    }
                                }
                                else
                                {
                                    resultList.Add(rec);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching loom production info", error = ex.Message });
            }

            return Json(new { success = true, data = resultList });
        }
        public IActionResult GetHPInfo(string sCode, string gCode, DateTime? sdate, int stime)
        {
            DateTime onlyDate = DateTime.MinValue;
            var resultList = new List<LOOM_2HRS>();

            if (sdate.HasValue)
            {
                onlyDate = sdate.Value.Date;
            }
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = @"
                SELECT
                    M.CODE AS LOOM_CODE, 
                    M.NAME AS MACHINE_NAME,
                    ISNULL([CURRENT].LA_COMP_CODE, [PREV].LA_COMP_CODE) AS COMP_CODE,
                    ISNULL([CURRENT].LA_YEAR_CODE, [PREV].LA_YEAR_CODE) AS YEAR_CODE,
                    ISNULL([CURRENT].LA_BRANCH_CODE, [PREV].LA_BRANCH_CODE) AS BRANCH_CODE,
                    ISNULL([CURRENT].LA_V_DATE, [PREV].LA_V_DATE) AS LAST_PROD_DATE,
                    ISNULL([CURRENT].LA_V_NO, [PREV].LA_V_NO) AS V_NO,
                    ISNULL([CURRENT].LA_V_TYPE, [PREV].LA_V_TYPE) AS V_TYPE,
                    ISNULL([CURRENT].LA_DOC_ID, [PREV].LA_DOC_ID) AS DOC_ID,
                    CASE 
                        WHEN [CURRENT].OP_READING IS NOT NULL THEN [CURRENT].OP_READING
                        ELSE ISNULL([PREV].CL_READING, 0)
                    END AS OP_READING,
                    ISNULL([CURRENT].CL_READING, 0) AS CL_READING FROM  MACHINE_MAST M
                    OUTER APPLY (
                    SELECT TOP 1 
                        LA.COMP_CODE AS LA_COMP_CODE,
                        LA.BRANCH_CODE AS LA_BRANCH_CODE,
                        LA.YEAR_CODE AS LA_YEAR_CODE,
                        LA.V_DATE AS LA_V_DATE,
                        LA.V_NO AS LA_V_NO,
                        LA.V_TYPE AS LA_V_TYPE,
                        LA.SCH_SHIFT,
                        LA.DOC_ID AS LA_DOC_ID,
                        HR.OP_READING,
                        HR.CL_READING
                    FROM  LOOM_ALLOC LA
                    LEFT JOIN  LOOM_2HRS HR  
                        ON LA.LOOM_CODE = HR.LOOM_CODE
                       AND CAST(HR.READING_TIME AS DATE) = LA.V_DATE
                       AND DATEPART(HOUR, HR.READING_TIME) = @stime 
                    WHERE 
                        LA.LOOM_CODE = M.CODE
                        AND LA.SCH_SHIFT = @Shift
                        AND LA.V_DATE = @SDate
                ) AS [CURRENT]

                OUTER APPLY (
                    SELECT TOP 1 
                        LA.COMP_CODE AS LA_COMP_CODE,
                        LA.BRANCH_CODE AS LA_BRANCH_CODE,
                        LA.YEAR_CODE AS LA_YEAR_CODE,
                        LA.V_DATE AS LA_V_DATE,
                        LA.V_NO AS LA_V_NO,
                        LA.V_TYPE AS LA_V_TYPE,
                        LA.SCH_SHIFT,
                        LA.DOC_ID AS LA_DOC_ID,
                        HR.CL_READING
                    FROM  LOOM_ALLOC LA
                    LEFT JOIN  LOOM_2HRS HR  
                        ON LA.LOOM_CODE = HR.LOOM_CODE
                       AND HR.READING_TIME < DATEADD(HOUR, @stime, @SDate)
                       AND CAST(HR.READING_TIME AS DATE) = @SDate
                    WHERE 
                        LA.LOOM_CODE = M.CODE
                        AND LA.SCH_SHIFT = @Shift
                        AND LA.V_DATE = @SDate
                    ORDER BY HR.READING_TIME DESC
                ) AS [PREV]

                WHERE M.BLOCK = @GroupCode
                ORDER BY M.CODE;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Shift", sCode);
                        cmd.Parameters.AddWithValue("@GroupCode", gCode);
                        cmd.Parameters.AddWithValue("@SDate", onlyDate);
                        cmd.Parameters.AddWithValue("@stime", stime);
                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                resultList.Add(new LOOM_2HRS
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    YEAR_CODE = reader["YEAR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["YEAR_CODE"]) : 0,
                                    BRANCH_CODE = reader["BRANCH_CODE"] != DBNull.Value ? Convert.ToInt32(reader["BRANCH_CODE"]) : 0,
                                    LOOM_CODE = reader["LOOM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["LOOM_CODE"]) : 0, // assuming LOOM_CODE is string
                                    LOOM_NAME = reader["MACHINE_NAME"]?.ToString(),
                                    V_DATE = reader["LAST_PROD_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["LAST_PROD_DATE"]) : (DateTime?)null,
                                    V_NO = int.TryParse(reader["V_NO"]?.ToString(), out int vno) ? vno : 0,
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    OP_READING = reader["OP_READING"] != DBNull.Value ? Convert.ToInt32(reader["OP_READING"]) : (int?)null,
                                    CL_READING = reader["CL_READING"] != DBNull.Value ? Convert.ToInt32(reader["CL_READING"]) : (int?)null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching loom production info", error = ex.Message });
            }

            return Json(new { success = true, data = resultList });
        }

        [HttpGet]
        public IActionResult GetPPMInfo(string sCode, string gCode, DateTime? sdate)
        {
            DateTime onlyDate = DateTime.MinValue;
            var resultList = new List<LoomProductionInfo>();
            if (sdate.HasValue)
            {
                onlyDate = sdate.Value.Date;
            }
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = @"
                        SELECT M.CODE AS LOOM_CODE, M.NAME AS MACHINE_NAME, P.V_DATE AS LAST_PROD_DATE,
                        P.V_NO, P.V_TYPE, P.DOC_ID, p.PPM FROM  MACHINE_MAST M
                        OUTER APPLY (
                            SELECT TOP 1 PR.V_DATE, PR.V_NO, PR.V_TYPE, PR.DOC_ID, PR.PPM
                            FROM  [LOOM_ALLOC] PR WHERE PR.LOOM_CODE = M.CODE AND PR.SCH_SHIFT = @Shift
                            AND PR.V_DATE = @SDate ORDER BY PR.V_DATE DESC) P
                        WHERE M.BLOCK = @GroupCode ORDER BY M.CODE;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Shift", sCode);
                        cmd.Parameters.AddWithValue("@GroupCode", gCode);
                        cmd.Parameters.AddWithValue("@SDate", onlyDate);
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                resultList.Add(new LoomProductionInfo
                                {
                                    LOOM_CODE = reader["LOOM_CODE"]?.ToString(),
                                    MACHINE_NAME = reader["MACHINE_NAME"]?.ToString(),
                                    LAST_PROD_DATE = reader["LAST_PROD_DATE"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["LAST_PROD_DATE"])
                                        : (DateTime?)null,
                                    V_NO = int.TryParse(reader["V_NO"]?.ToString(), out int vno) ? vno : 0,
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    PPM = reader["PPM"] != DBNull.Value ? (int?)Convert.ToInt32(reader["PPM"]) : null,
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching loom production info", error = ex.Message });
            }
            return Json(new { success = true, data = resultList });
        }

        [HttpGet]
        public IActionResult GetWastageInfo(string sCode, string gCode, DateTime? sdate)
        {
            DateTime onlyDate = DateTime.MinValue;
            var resultList = new List<LoomProductionInfo>();
            if (sdate.HasValue)
            {
                onlyDate = sdate.Value.Date;
            }
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    string query = @"
                         SELECT 
                            M.CODE AS LOOM_CODE, 
                            M.NAME AS MACHINE_NAME,                  
                            P.V_DATE AS LAST_PROD_DATE,
                            P.V_NO,
                            P.V_TYPE,
                            P.DOC_ID,
                            p.WASTAGE
                        FROM  MACHINE_MAST M
                        OUTER APPLY (
                            SELECT TOP 1 
                                PR.V_DATE,
                                PR.V_NO,
                                PR.V_TYPE,
                                PR.DOC_ID,
                                PR.WASTAGE
                            FROM  [LOOM_ALLOC] PR 
                            WHERE PR.LOOM_CODE = M.CODE 
                              AND PR.SCH_SHIFT = @Shift
                              AND PR.V_DATE = @SDate
                            ORDER BY PR.V_DATE DESC
                        ) P
                        WHERE M.BLOCK = @GroupCode
                        ORDER BY M.CODE;
                        ";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Shift", sCode);
                        cmd.Parameters.AddWithValue("@GroupCode", gCode);
                        cmd.Parameters.AddWithValue("@SDate", onlyDate);

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                resultList.Add(new LoomProductionInfo
                                {
                                    LOOM_CODE = reader["LOOM_CODE"]?.ToString(),
                                    MACHINE_NAME = reader["MACHINE_NAME"]?.ToString(),
                                    LAST_PROD_DATE = reader["LAST_PROD_DATE"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["LAST_PROD_DATE"])
                                        : (DateTime?)null,
                                    V_NO = int.TryParse(reader["V_NO"]?.ToString(), out int vno) ? vno : 0,
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    DOC_ID = reader["DOC_ID"]?.ToString(),
                                    WASTAGE = reader["WASTAGE"] != DBNull.Value ? (int?)Convert.ToInt32(reader["WASTAGE"]) : null,
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching loom production info", error = ex.Message });
            }
            return Json(new { success = true, data = resultList });
        }

        public int GetNextV_NO(string yearCode)
        {
            string newV_NO = "00000";
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                // Execute query to get PREFIXYR
                string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = '" + yearCode + "'";
                SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                // Execute query to get last V_NO
                string lastV_NO_Query = "SELECT TOP 1 V_NO FROM  [LOOM_ALLOC] ORDER BY V_NO DESC";
                SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                string lastV_NO = lastVnoCmd.ExecuteScalar()?.ToString();

                int lastNumber = 0;
                if (!string.IsNullOrEmpty(lastV_NO) && lastV_NO.Length >= 9)
                {
                    string numericPart = lastV_NO.Substring(lastV_NO.Length - 5);
                    int.TryParse(numericPart, out lastNumber);
                }
                // Increment and format the new V_NO
                string newRunningNo = (lastNumber + 1).ToString("D5");
                newV_NO = prefixYR + newRunningNo;
            }
            return Convert.ToInt32(newV_NO);
        }

        public IActionResult loomNoList(int cCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE,NAME FROM  [MACHINE_MAST] WHERE ACTIVE=1 AND COMP_CODE='"+ gv.PubCompCode + "' AND TYPE = 'Store' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult shiftList(int cCode)
        {
            string query = "SELECT SHIFT, MIN(CODE) AS CODE FROM  [SHIFT_MAST] GROUP BY SHIFT";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public IActionResult GetBlocksList()
        {
            List<string> blocks = new List<string>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand(@"
                SELECT DISTINCT BLOCK 
                FROM  [MACHINE_MAST] 
                WHERE  TYPE='Loom' AND BLOCK IS NOT NULL AND LTRIM(RTRIM(BLOCK)) <> '' 
                ORDER BY BLOCK", con))
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                blocks.Add(reader["BLOCK"].ToString());
                            }
                        }
                    }
                }
                return Json(new { success = true, data = blocks });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving block list", error = ex.Message });
            }
        }
        public IActionResult meshList(int cCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE, NAME FROM  [MESH_MAST] WHERE COMP_CODE = '"+ gv.PubCompCode +"' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult empList(int cCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE, NAME FROM  [EMP_MAST] WHERE COMP_CODE = '"+ gv.PubCompCode + "' AND RESIGN_DATE IS NULL  ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult itemList(int cCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            //string query = "SELECT CODE,SHORTNAME FROM  [ITEM_MAST] WHERE COMP_CODE = '"+ gv.PubCompCode + "' AND GROUP_CODE IN (103,125,142) ORDER BY SHORTNAME";
            string query = @"SELECT CODE,SHORTNAME FROM ITEM_MAST WHERE COMP_CODE = '"+ gv.PubCompCode + "' AND SHORTNAME <> ''";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetEmpNameBycode(int cCode, int eCode)
        {
            string empName = string.Empty;
            //cCode = 5;
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT NAME FROM  [EMP_MAST] WHERE RESIGN_DATE IS NULL AND COMP_CODE = @COMP_CODE AND CODE = @CODE", con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", eCode);

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                empName = reader["NAME"].ToString();

                                return Json(new
                                {
                                    success = true,
                                    empName
                                });
                            }
                            else
                            {
                                return Json(new
                                {
                                    success = false,
                                    message = "No employee found against this emp code"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error retrieving employee name",
                    error = ex.Message
                });
            }
        }
        [HttpPost]
        public JsonResult SaveLoomProductionRecords([FromBody] List<LOOM_ALLOC> data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                if (data == null || data.Count == 0)
                {
                    return Json(new { success = false, message = "No data received" });
                }

                using (var connection = _dbConnection.GetErpConnection())
                {
                    connection.Open();

                    var firstRecord = data.First();
                    bool isUpdate = false;
                    int vNo;
                    string vType;
                    string docId;

                    // 🔍 Check for existing record
                    var checkExistenceCommand = new SqlCommand(@"
                SELECT TOP 1 V_NO, V_TYPE, DOC_ID 
                FROM  [LOOM_ALLOC]
                WHERE LOOM_CODE = @LOOM_CODE
                  AND SCH_SHIFT = @SCH_SHIFT
                  AND V_DATE = @V_DATE
                  AND YEAR_CODE = @YEAR_CODE
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE", connection);

                    checkExistenceCommand.Parameters.AddWithValue("@LOOM_CODE", firstRecord.LOOM_CODE ?? (object)DBNull.Value);
                    checkExistenceCommand.Parameters.AddWithValue("@SCH_SHIFT", firstRecord.SCH_SHIFT ?? (object)DBNull.Value);
                    checkExistenceCommand.Parameters.AddWithValue("@V_DATE", firstRecord.V_DATE ?? (object)DBNull.Value);
                    checkExistenceCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    checkExistenceCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    checkExistenceCommand.Parameters.AddWithValue("@BRANCH_CODE", 1);

                    using (var reader = checkExistenceCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isUpdate = true;
                            vNo = reader.GetInt32(reader.GetOrdinal("V_NO"));
                            vType = reader.GetString(reader.GetOrdinal("V_TYPE"));
                            docId = reader.GetString(reader.GetOrdinal("DOC_ID"));
                        }
                        else
                        {
                            vNo = GetNextV_NO(globalVar.PubFYearCode);
                            vType = "LMOP";
                            docId = vType + vNo;
                        }
                    }

                    if (isUpdate)
                    {
                        // 🔁 Delete old records
                        var deleteCommand = new SqlCommand(@"
                    DELETE FROM  [LOOM_ALLOC]
                    WHERE 
                      SCH_SHIFT = @SCH_SHIFT
                      AND V_DATE = @V_DATE
                      AND V_NO = @V_NO
                      AND V_TYPE = @V_TYPE
                      AND YEAR_CODE = @YEAR_CODE
                      AND COMP_CODE = @COMP_CODE
                      AND BRANCH_CODE = @BRANCH_CODE", connection);

                        deleteCommand.Parameters.AddWithValue("@LOOM_CODE", firstRecord.LOOM_CODE ?? (object)DBNull.Value);
                        deleteCommand.Parameters.AddWithValue("@SCH_SHIFT", firstRecord.SCH_SHIFT ?? (object)DBNull.Value);
                        deleteCommand.Parameters.AddWithValue("@V_DATE", firstRecord.V_DATE ?? (object)DBNull.Value);
                        deleteCommand.Parameters.AddWithValue("@V_NO", vNo);
                        deleteCommand.Parameters.AddWithValue("@V_TYPE", vType);
                        deleteCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        deleteCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        deleteCommand.Parameters.AddWithValue("@BRANCH_CODE", 1);

                        deleteCommand.ExecuteNonQuery();
                    }

                    // 🔁 Insert new records with SNO starting from 1
                    int sno = 1;

                    foreach (var record in data)
                    {
                        var insertCommand = new SqlCommand(@"
                    INSERT INTO  [LOOM_ALLOC] (
                        YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO,
                        DOC_ID, SCH_SHIFT, LOOM_CODE, ITEM_CODE, EMP_CODE,
                        MESH_CODE, DNR, UUSER, UDATE, EUSER,
                        EDATE, AED, WSID, LIP, LID, V_DATE, SNO
                    )
                    VALUES (
                        @YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO,
                        @DOC_ID, @SCH_SHIFT, @LOOM_CODE, @ITEM_CODE, @EMP_CODE,
                        @MESH_CODE, @DNR, @UUSER, @UDATE, @EUSER,
                        @EDATE, @AED, @WSID, @LIP, @LID, @V_DATE, @SNO
                    );", connection);

                        insertCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        insertCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        insertCommand.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        insertCommand.Parameters.AddWithValue("@V_TYPE", vType);
                        insertCommand.Parameters.AddWithValue("@V_NO", vNo);
                        insertCommand.Parameters.AddWithValue("@DOC_ID", docId);
                        insertCommand.Parameters.AddWithValue("@SCH_SHIFT", record.SCH_SHIFT ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@LOOM_CODE", record.LOOM_CODE ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@ITEM_CODE", record.ITEM_CODE ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@EMP_CODE", record.EMP_CODE ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@MESH_CODE", record.MESH_CODE ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@DNR", record.DNR ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        insertCommand.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        insertCommand.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        insertCommand.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@AED", record.AED ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        insertCommand.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        insertCommand.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");
                        insertCommand.Parameters.AddWithValue("@V_DATE", record.V_DATE ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@SNO", sno);

                        insertCommand.ExecuteNonQuery();
                        sno++; // increment SNO for next record
                    }
                    return Json(new
                    {
                        success = true,
                        message = isUpdate ? "Data updated successfully." : "Data inserted successfully.",
                        V_NO = vNo,
                        V_TYPE = vType,
                        DOC_ID = docId
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult SaveAndUpdateHrsProduction([FromBody] List<LOOM_2HRS> data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                if (data == null || data.Count == 0)
                {
                    return Json(new { success = false, message = "No data received" });
                }
                using (var connection = _dbConnection.GetErpConnection())
                {
                    connection.Open();

                    var firstRecord = data.First();
                    bool isUpdate = false;
                    int vNo;
                    string vType;
                    string docId;

                    // 🔍 Check for existing record
                    var checkExistenceCommand = new SqlCommand(@"
                    SELECT TOP 1 V_NO, V_TYPE, DOC_ID FROM  [LOOM_2HRS] WHERE LOOM_CODE = @LOOM_CODE
                    AND SHIFT = @SHIFT AND V_DATE = @V_DATE AND YEAR_CODE = @YEAR_CODE AND COMP_CODE = @COMP_CODE
                    AND BRANCH_CODE = @BRANCH_CODE AND DATEPART(HOUR, READING_TIME) = @HOUR", connection);

                    checkExistenceCommand.Parameters.AddWithValue("@LOOM_CODE", firstRecord.LOOM_CODE ?? (object)DBNull.Value);
                    checkExistenceCommand.Parameters.AddWithValue("@SHIFT", firstRecord.SHIFT ?? (object)DBNull.Value);
                    checkExistenceCommand.Parameters.AddWithValue("@V_DATE", firstRecord.V_DATE ?? (object)DBNull.Value);
                    checkExistenceCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    checkExistenceCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    checkExistenceCommand.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    checkExistenceCommand.Parameters.AddWithValue("@HOUR", firstRecord.READING_TIME.Value.Hour);

                    using (var reader = checkExistenceCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isUpdate = true;
                            vNo = reader.GetInt32(reader.GetOrdinal("V_NO"));
                            vType = reader.GetString(reader.GetOrdinal("V_TYPE"));
                            docId = reader.GetString(reader.GetOrdinal("DOC_ID"));
                        }
                        else
                        {
                            vNo = GetNextV_NO(globalVar.PubFYearCode);
                            vType = "LMOP";
                            docId = vType + vNo;
                        }
                    }

                    if (isUpdate)
                    {
                        // 🔁 Delete old records
                        var deleteCommand = new SqlCommand(@"
                        DELETE FROM  [LOOM_2HRS] WHERE SHIFT = @SHIFT AND V_DATE = @V_DATE AND V_NO = @V_NO
                        AND V_TYPE = @V_TYPE AND YEAR_CODE = @YEAR_CODE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE
                        AND DATEPART(HOUR, READING_TIME) = @HOUR", connection);

                        deleteCommand.Parameters.AddWithValue("@LOOM_CODE", firstRecord.LOOM_CODE ?? (object)DBNull.Value);
                        deleteCommand.Parameters.AddWithValue("@SHIFT", firstRecord.SHIFT ?? (object)DBNull.Value);
                        deleteCommand.Parameters.AddWithValue("@V_DATE", firstRecord.V_DATE ?? (object)DBNull.Value);
                        deleteCommand.Parameters.AddWithValue("@V_NO", vNo);
                        deleteCommand.Parameters.AddWithValue("@V_TYPE", vType);
                        deleteCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        deleteCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        deleteCommand.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        deleteCommand.Parameters.AddWithValue("@HOUR", firstRecord.READING_TIME.Value.Hour);

                        deleteCommand.ExecuteNonQuery();
                    }

                    // 🔁 Insert new records with SNO starting from 1
                    int srno = 1;

                    foreach (var record in data)
                    {
                        var insertCommand = new SqlCommand(@"
                     INSERT INTO  [LOOM_2HRS] (
                        YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO,
                        DOC_ID, SHIFT, LOOM_CODE,OP_READING,CL_READING
                        ,UUSER, UDATE, EUSER,
                        EDATE, AED, WSID, LIP, LID, V_DATE,SRNO,READING_TIME
                    )
                    VALUES (@YEAR_CODE, @COMP_CODE, @BRANCH_CODE, @V_TYPE, @V_NO,
                        @DOC_ID, @SHIFT, @LOOM_CODE,@OP_READING,@CL_READING,
                        @UUSER, @UDATE, @EUSER,
                        @EDATE, @AED, @WSID, @LIP, @LID, @V_DATE,@SRNO,@READING_TIME);", connection);

                        insertCommand.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        insertCommand.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        insertCommand.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        insertCommand.Parameters.AddWithValue("@V_TYPE", vType);
                        insertCommand.Parameters.AddWithValue("@V_NO", vNo);
                        insertCommand.Parameters.AddWithValue("@DOC_ID", docId);
                        insertCommand.Parameters.AddWithValue("@SHIFT", record.SHIFT ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@LOOM_CODE", record.LOOM_CODE ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@OP_READING", record.OP_READING ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@CL_READING", record.CL_READING ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        insertCommand.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        insertCommand.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        insertCommand.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@AED", record.AED ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        insertCommand.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        insertCommand.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");
                        insertCommand.Parameters.AddWithValue("@V_DATE", record.V_DATE ?? (object)DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@SRNO", srno);
                        insertCommand.Parameters.AddWithValue("@READING_TIME", record.READING_TIME);

                        insertCommand.ExecuteNonQuery();
                        srno++; // increment SNO for next record
                    }

                    return Json(new
                    {
                        success = true,
                        message = isUpdate ? "Data updated successfully." : "Data inserted successfully.",
                        V_NO = vNo,
                        V_TYPE = vType,
                        DOC_ID = docId
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult UpdateLoomProductionRecords([FromBody] List<LOOM_ALLOC> data)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    return Json(new { success = false, message = "No data received" });
                }

                var globalVar = _globalVariableService.GetGlobalVariables();

                using (var connection = _dbConnection.GetErpConnection())
                {
                    connection.Open();

                    foreach (var record in data)
                    {
                        var command = new SqlCommand(@"
                    WITH LatestRecord AS (
                        SELECT TOP 1 * 
                        FROM  PROD2 
                        WHERE LOOM_CODE = @LOOM_CODE
                        AND V_DATE = @V_DATE
                        ORDER BY V_DATE DESC
                    )
                    UPDATE LatestRecord
                    SET 
                        ITEM_CODE = @ITEM_CODE, 
                        EMP_CODE = @EMP_CODE,
                        MESH_CODE = @MESH_CODE,
                        DNR = @DNR,
                        UUSER = @UUSER,
                        UDATE = @UDATE,
                        EUSER = @EUSER,
                        EDATE = @EDATE,
                        AED = @AED,
                        WSID = @WSID,
                        LIP = @LIP,
                        LID = @LID
                ", connection);

                        command.Parameters.AddWithValue("@ITEM_CODE", (object)record.ITEM_CODE ?? DBNull.Value);
                        command.Parameters.AddWithValue("@EMP_CODE", (object)record.EMP_CODE ?? DBNull.Value);
                        command.Parameters.AddWithValue("@MESH_CODE", (object)record.MESH_CODE ?? DBNull.Value);
                        command.Parameters.AddWithValue("@DNR", (object)record.DNR ?? DBNull.Value);
                        command.Parameters.AddWithValue("@LOOM_CODE", (object)record.LOOM_CODE ?? DBNull.Value);
                        command.Parameters.AddWithValue("@V_DATE", (object)record.V_DATE ?? DBNull.Value);


                        // Audit fields
                        command.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        command.Parameters.AddWithValue("@UDATE", DBNull.Value);
                        command.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        command.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        command.Parameters.AddWithValue("@AED", 'E');
                        command.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        command.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        command.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

                        command.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult UpdatePpmBatch([FromBody] List<LOOM_ALLOC> data)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();
            try
            {
                if (data == null || data.Count == 0)
                    return Json(new { success = false, message = "No data received" });

                using (var connection = _dbConnection.GetErpConnection())
                {
                    connection.Open();

                    foreach (var record in data)
                    {
                        string updateQuery = @"
                    UPDATE  [LOOM_ALLOC]
                    SET PPM = @PPM,
                        EUSER = @EuserId,
                        UDATE = GETDATE()
                    WHERE LOOM_CODE = @LoomCode
                      AND V_NO = @VNo
                      AND V_TYPE = @VType
                      AND DOC_ID = @DocId
                      AND SCH_SHIFT = @Shift
                      AND V_DATE = @VDate";

                        using (var command = new SqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@PPM", (object)record.PPM ?? DBNull.Value);
                            command.Parameters.AddWithValue("@EuserId", globelVar.PubUserId);
                            command.Parameters.AddWithValue("@LoomCode", (object)record.LOOM_CODE ?? DBNull.Value);
                            command.Parameters.AddWithValue("@VNo", (object)record.V_NO ?? DBNull.Value);
                            command.Parameters.AddWithValue("@VType", (object)record.V_TYPE ?? DBNull.Value);
                            command.Parameters.AddWithValue("@DocId", (object)record.DOC_ID ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Shift", (object)record.SCH_SHIFT ?? DBNull.Value);
                            //command.Parameters.AddWithValue("@GroupCode", (object)record.GROUP_CODE ?? DBNull.Value);
                            command.Parameters.AddWithValue("@VDate", (object)record.V_DATE ?? DBNull.Value);

                            command.ExecuteNonQuery();
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateWastageBatch([FromBody] List<LOOM_ALLOC> data)
        {
            var globelVar = _globalVariableService.GetGlobalVariables();
            try
            {
                if (data == null || data.Count == 0)
                    return Json(new { success = false, message = "No data received" });

                using (var connection = _dbConnection.GetErpConnection())
                {
                    connection.Open();

                    foreach (var record in data)
                    {
                        string updateQuery = @"
                    UPDATE  [LOOM_ALLOC]
                    SET WASTAGE = @WASTAGE,
                        EUSER = @EuserId,
                        UDATE = GETDATE()
                    WHERE LOOM_CODE = @LoomCode
                      AND V_NO = @VNo
                      AND V_TYPE = @VType
                      AND DOC_ID = @DocId
                      AND SCH_SHIFT = @Shift
                      AND V_DATE = @VDate";

                        using (var command = new SqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("@WASTAGE", (object)record.WASTAGE ?? DBNull.Value);
                            command.Parameters.AddWithValue("@EuserId", globelVar.PubUserId);
                            command.Parameters.AddWithValue("@LoomCode", (object)record.LOOM_CODE ?? DBNull.Value);
                            command.Parameters.AddWithValue("@VNo", (object)record.V_NO ?? DBNull.Value);
                            command.Parameters.AddWithValue("@VType", (object)record.V_TYPE ?? DBNull.Value);
                            command.Parameters.AddWithValue("@DocId", (object)record.DOC_ID ?? DBNull.Value);
                            command.Parameters.AddWithValue("@Shift", (object)record.SCH_SHIFT ?? DBNull.Value);
                            //command.Parameters.AddWithValue("@GroupCode", (object)record.GROUP_CODE ?? DBNull.Value);
                            command.Parameters.AddWithValue("@VDate", (object)record.V_DATE ?? DBNull.Value);

                            command.ExecuteNonQuery();
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
