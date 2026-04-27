using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.PlantMaintenance.Transaction;

namespace travelexpensemanagement.Controllers.PlantMaintenance.Transaction
{
    public class MaintenanceStatusController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public MaintenanceStatusController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/PlantMaintenance/Transaction/MaintenanceStatus/Index.cshtml");
        }

        [HttpGet]
        public IActionResult DocTypeDropdown()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select Code,Name From Doctype_Mast Where doctype='MaintenanceFollowUp'";
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
        }
        [HttpGet]
        public IActionResult SectionDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE,NAME  FROM ITEMDEPT_MAST WHERE COMP_CODE =1 ORDER BY NAME ";
            var section = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = section });
        }

        [HttpGet]
        public IActionResult PlaceDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE ,NAME FROM PLACE_MAST WHERE COMP_CODE =1 order by Name ";
            var place = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = place });
        }

        [HttpGet]
        public IActionResult ActivityDDl()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from PMACTIVITY_MAST  where COMP_CODE=1 order by name";
            var activity = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = activity });
        }

        [HttpGet]
        public IActionResult PMCheckListDDl()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select code,CHECKLIST_NAME 'Name' from PMCHECKLIST_MAST1 where COMP_CODE =1 order by name";
            var checkList = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = checkList });
        }

        [HttpGet]
        public IActionResult FrequencyDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select Code,name from PM_frequency_mast Order by code";
            var frequency = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = frequency });
        }

        [HttpGet]
        public IActionResult EmployeeDDL(string search)
        {
            string query = "SELECT TOP 200 CODE AS value, NAME AS text FROM EMP_MAST WHERE RESIGN_DATE IS NULL AND COMP_CODE = 1";

            if (!string.IsNullOrEmpty(search))
            {
                query += " AND NAME LIKE '%" + search + "%'";
            }

            query += " ORDER BY NAME";

            var employee = _dropdownService.GetDropdownList(query);

            return Json(new { success = true, data = employee });
        }

        [HttpGet]
        public IActionResult PlanNameDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select Code,Plan_Name Name From PM_PLAN_MAST Where Comp_code=1 and Branch_code=1 Order by Name ";
            var planName = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = planName });
        }

        [HttpGet]
        public IActionResult FaultDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select Code,Name From FALT_MAST Where Comp_code=1  Order by Name ";
            var fault = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = fault });
        }

        [HttpGet]
        public IActionResult PrepareByDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select Code,Name From Emp_Mast Where Type in ('Staff','Semi Staff') and Resign_Date is null and Comp_code=1 Order by Name";
            var prepareBy = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = prepareBy });
        }

        [HttpGet]
        public IActionResult MachineDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select Code,Name From Machine_Mast Where Comp_code=1 Order by Name";
            var machine = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = machine });
        }
        [HttpGet]
        public IActionResult CategoryDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE,NAME FROM PMCAT_MAST WHERE COMP_CODE =1 order by name ";
            var category = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = category });
        }

        [HttpGet]
        public IActionResult SearchItem(string search)
        {
            var getData = _globalVariableService.GetGlobalVariables();

            string query = "SELECT TOP 200 CODE, NAME FROM ITEM_MAST WHERE COMP_CODE = 1";

            if (!string.IsNullOrEmpty(search))
            {
                query += " AND NAME LIKE '%" + @search + "%'";
            }

            query += " ORDER BY NAME";

            var items = _dropdownService.GetDropdownList(query);

            return Json(items);
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

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)),0)+1 FROM PM_FOLLOWUPHEADER WHERE V_TYPE=@V_TYPE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);

                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    lastVnoCmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                    //object result = lastVnoCmd.ExecuteScalar();

                    //if (result != DBNull.Value && result != null)
                    //{
                    //    int lastV_NO = Convert.ToInt32(result);
                    //    newV_NO = lastV_NO.ToString("D5");
                    //}
                    //else
                    //{
                    //    newV_NO = prefixYR + "00001";
                    //}
                    int runningNo = Convert.ToInt32(lastVnoCmd.ExecuteScalar());

                    string runningFormatted = runningNo.ToString("D5");

                    newV_NO = prefixYR + runningFormatted;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }

        [HttpPost]
        public IActionResult SaveOrUpdateResult([FromBody] MaintenanceStatus model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            if (model == null)
            {
                return Json(new { success = false, error = "Model binding failed" });
            }

            try
            {
                bool isUpdate = !string.IsNullOrEmpty(model.DOC_ID); 
                string docId = model.DOC_ID;

                if (!isUpdate)
                {
                    docId = model.V_TYPE + model.V_NO; 
                }

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // ================= HEADER SAVE =================
                    SqlTransaction tran = con.BeginTransaction();
                    try
                    {
                        SqlCommand cmd = new SqlCommand("Sp_Maintenance_Status", con, tran);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);

                        cmd.Parameters.AddWithValue("@PLAN_TYPE", model.PLAN_TYPE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLAN_NO", model.PLAN_NO ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLAN_CODE", model.PLAN_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLAN_NAME", model.PLAN_NAME ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", model.ITEM_NAME ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PLACE_NAME", model.PLACE_NAME ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@DEPT_NAME", model.DEPT_NAME ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@CLDEPT_CODE", model.CLDEPT_CODE ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@CLOSE_REMARKS", model.CLOSE_REMARKS ?? (object)DBNull.Value);

                        cmd.Parameters.AddWithValue("@S_DATE", model.S_DATE);
                        cmd.Parameters.AddWithValue("@E_DATE", model.E_DATE);

                        cmd.Parameters.AddWithValue("@Active", 1);
                        cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.Parameters.AddWithValue("@Action", isUpdate ? "UpdateHeader" : "InsertHeader");

                        cmd.ExecuteNonQuery();

                        // ================= DELETE CHILD (ONLY UPDATE) =================
                        if (isUpdate)
                        {
                            SqlCommand cmdDel = new SqlCommand("Sp_Maintenance_Status", con, tran);
                            cmdDel.CommandType = CommandType.StoredProcedure;

                            cmdDel.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmdDel.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmdDel.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmdDel.Parameters.AddWithValue("@DOC_ID", docId);
                            cmdDel.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                            cmdDel.Parameters.AddWithValue("@Action", "DeleteChild");
                            cmdDel.ExecuteNonQuery();
                        }

                        // ================= ACTIVITY SAVE =================
                        if (model.ActivityList != null)
                        {
                            foreach (var act in model.ActivityList)
                            {

                                SqlCommand cmdAct = new SqlCommand("Sp_Maintenance_Status", con, tran);
                                cmdAct.CommandType = CommandType.StoredProcedure;

                                cmdAct.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmdAct.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                cmdAct.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                cmdAct.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                cmdAct.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmdAct.Parameters.AddWithValue("@DOC_ID", docId);
                                cmdAct.Parameters.AddWithValue("@V_DATE", model.V_DATE);

                                cmdAct.Parameters.AddWithValue("@ACT_CODE", act.ACT_CODE ?? (object)DBNull.Value);
                                cmdAct.Parameters.AddWithValue("@ACT_NAME", act.ACT_NAME ?? (object)DBNull.Value);

                                cmdAct.Parameters.AddWithValue("@CAT_CODE", act.CAT_CODE ?? (object)DBNull.Value);
                                cmdAct.Parameters.AddWithValue("@CAT_NAME", act.CAT_NAME ?? (object)DBNull.Value);

                                cmdAct.Parameters.AddWithValue("@CHK_CODE", act.CHK_CODE ?? (object)DBNull.Value);
                                cmdAct.Parameters.AddWithValue("@CHK_NAME", act.CHK_NAME ?? (object)DBNull.Value);

                                cmdAct.Parameters.AddWithValue("@FREQUENCY", act.FREQUENCY ?? (object)DBNull.Value);

                                cmdAct.Parameters.AddWithValue("@AS_DATE", act.AS_DATE);
                                cmdAct.Parameters.AddWithValue("@AE_DATE", act.AE_DATE);

                                cmdAct.Parameters.AddWithValue("@STATUS", act.STATUS ?? (object)DBNull.Value);
                                cmdAct.Parameters.AddWithValue("@AREMARKS", act.AREMARKS ?? (object)DBNull.Value);

                                cmdAct.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                cmdAct.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                                cmdAct.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                cmdAct.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                cmdAct.Parameters.AddWithValue("@LID", Environment.MachineName);

                                cmdAct.Parameters.AddWithValue("@Action", "InsertActivity");

                                cmdAct.ExecuteNonQuery();
                            }
                        }

                        // ================ SPARES SAVE =================
                        if (model.SparesList != null)
                        {
                            foreach (var sp in model.SparesList)
                            {

                                SqlCommand cmdSp = new SqlCommand("Sp_Maintenance_Status", con, tran);
                                cmdSp.CommandType = CommandType.StoredProcedure;

                                cmdSp.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmdSp.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                cmdSp.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                cmdSp.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                cmdSp.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmdSp.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                cmdSp.Parameters.AddWithValue("@DOC_ID", docId);

                                cmdSp.Parameters.AddWithValue("@SITEM_CODE", sp.SITEM_CODE ?? (object)DBNull.Value);
                                cmdSp.Parameters.AddWithValue("@SITEM_NAME", sp.SITEM_NAME ?? (object)DBNull.Value);

                                cmdSp.Parameters.AddWithValue("@QUANTITY", sp.QUANTITY ?? (object)DBNull.Value);
                                cmdSp.Parameters.AddWithValue("@SREMARKS", sp.SREMARKS ?? (object)DBNull.Value);

                                cmdSp.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                cmdSp.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                                cmdSp.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                cmdSp.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                cmdSp.Parameters.AddWithValue("@LID", Environment.MachineName);

                                cmdSp.Parameters.AddWithValue("@Action", "InsertSpares");

                                cmdSp.ExecuteNonQuery();
                            }
                        }

                        // ================= RESOURCE SAVE =================
                        if (model.FollowResource != null)
                        {
                            foreach (var rs in model.FollowResource)
                            {

                                SqlCommand cmdRs = new SqlCommand("Sp_Maintenance_Status", con, tran);
                                cmdRs.CommandType = CommandType.StoredProcedure;

                                cmdRs.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                cmdRs.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                cmdRs.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                cmdRs.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                cmdRs.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmdRs.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                cmdRs.Parameters.AddWithValue("@DOC_ID", docId);

                                cmdRs.Parameters.AddWithValue("@EMP_CODE", rs.EMP_CODE ?? (object)DBNull.Value);
                                cmdRs.Parameters.AddWithValue("@EMP_NAME", rs.EMP_NAME ?? (object)DBNull.Value);

                                cmdRs.Parameters.AddWithValue("@FS_DATE", rs.FS_DATE);
                                cmdRs.Parameters.AddWithValue("@FE_DATE", rs.FE_DATE);

                                cmdRs.Parameters.AddWithValue("@HOUR", rs.HOUR);
                                cmdRs.Parameters.AddWithValue("@FREMARKS", rs.FREMARKS ?? (object)DBNull.Value);

                                cmdRs.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                                cmdRs.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                                cmdRs.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                cmdRs.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                cmdRs.Parameters.AddWithValue("@LID", Environment.MachineName);

                                cmdRs.Parameters.AddWithValue("@Action", "InsertResource");

                                cmdRs.ExecuteNonQuery();
                            }
                        }
                        tran.Commit();
                    }
                    catch (Exception)
                    {
                        tran.Rollback();
                        throw; 
                    }
                }
                string message = isUpdate ? "Record Updated Successfully" : "Record Inserted Successfully";
                return Json(new { success = true, message =message, isUpdate = isUpdate });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult LoadListData(string docId)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new MaintenanceStatus();
            var activity = new List<Activity>();
            var spares = new List<Spares>();
            var resources = new List<FollowResource>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_Maintenance_Status ", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@V_DATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@Action", "GetDetails");

                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read()){
                        model = new MaintenanceStatus
                        {
                            DOC_ID = reader["DOC_ID"].ToString(),
                            V_TYPE = reader["V_TYPE"].ToString(),
                            V_NO = Convert.ToInt32(reader["V_NO"]),
                            V_DATE = reader["V_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["V_DATE"]),
                            PLAN_CODE = reader["PLAN_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PLAN_CODE"]),
                            PLAN_NAME = reader["PLAN_NAME"].ToString(),
                            PLAN_NO = reader["PLAN_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PLAN_NO"]),
                            PLACE_CODE = reader["PLACE_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PLACE_CODE"]),
                            PLACE_NAME = reader["PLACE_NAME"].ToString(),
                            ITEM_NAME = reader["ITEM_NAME"].ToString(),
                            DEPT_CODE = reader["DEPT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DEPT_CODE"]),
                            DEPT_NAME = reader["DEPT_NAME"].ToString(),
                            CLDEPT_CODE = reader["CLDEPT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CLDEPT_CODE"]),
                            CLOSE_REMARKS = reader["CLOSE_REMARKS"].ToString(),
                            S_DATE = reader["S_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["S_DATE"]),
                            E_DATE = reader["E_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["E_DATE"]),

                        };
                    };
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            activity.Add(new Activity
                            {
                                ACT_CODE = reader["ACT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ACT_CODE"]),
                                ACT_NAME = reader["ACT_NAME"].ToString(),
                                CAT_CODE = reader["CAT_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CAT_CODE"]),
                                CAT_NAME = reader["CAT_NAME"].ToString(),
                                CHK_CODE = reader["CHK_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CHK_CODE"]),
                                CHK_NAME = reader["CHK_NAME"].ToString(),
                                FREQUENCY = reader["FREQUENCY"].ToString(),
                                AS_DATE = reader["AS_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["AS_DATE"]),
                                AE_DATE = reader["AE_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["AE_DATE"]),
                                STATUS = reader["STATUS"].ToString(),
                                AREMARKS = reader["AREMARKS"].ToString()
                            });
                        }
                    };
                    // ===== SPARES =====
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            spares.Add(new Spares
                            {
                                SITEM_CODE = reader["SITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["SITEM_CODE"]),
                                SITEM_NAME = reader["SITEM_NAME"].ToString(),
                                QUANTITY = reader["QUANTITY"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QUANTITY"]),
                                SREMARKS = reader["SREMARKS"].ToString()
                            });
                        }
                    };
                    // ===== RESOURCES =====
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            resources.Add(new FollowResource
                            {
                                EMP_CODE = reader["EMP_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["EMP_CODE"]),
                                EMP_NAME = reader["EMP_NAME"].ToString(),
                                FS_DATE = reader["FS_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FS_DATE"]),
                                FE_DATE = reader["FE_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FE_DATE"]),
                                HOUR = reader["HOUR"].ToString(),
                                FREMARKS = reader["FREMARKS"].ToString()
                            });
                        }
                    }
                    return Json(new { success = true, header = model, activity = activity, spares = spares, resources = resources });
                }
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetCopyData(string vType)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("Sp_Maintenance_Status", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@Action", "GetCopyData");

                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    List<object> list = new List<object>();
                    while (reader.Read())
                    {
                        list.Add(new
                        {
                            VNo = reader["VNo"] == DBNull.Value ? 0 : Convert.ToInt32(reader["VNo"]),
                            VType = reader["VType"]?.ToString(),
                            VDate = reader["VDate"] == DBNull.Value ? "" : Convert.ToDateTime(reader["VDate"]).ToString("yyyy-MM-dd"),
                            MachineName = reader["Machine_Name"]?.ToString(),
                            Priority = reader["Priority"]?.ToString(),
                            FaultCode = reader["Fault_Code"]?.ToString(),
                            ComplaintDate = reader["Complaint_Date"] == DBNull.Value ? "" : Convert.ToDateTime(reader["Complaint_Date"]).ToString("yyyy-MM-dd"),
                            MachStop = reader["Mach_Stop"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Mach_Stop"]),
                            ToDept = reader["ToDept"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ToDept"]),
                            To_Dept = reader["To_Dept"]?.ToString(),
                            Mcode = reader["MCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["MCode"]),
                            Plancode = reader["Plancode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Plancode"]),
                            EDate = reader["E_Date"] == DBNull.Value ? "" : Convert.ToDateTime(reader["E_Date"]).ToString("yyyy-MM-dd"),
                            FromDept = reader["FROM_DEPT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["FROM_DEPT"]),
                            PriorityText = reader["PRIORITY"]?.ToString(),
                            COMPLAINT_REMARKS = reader["COMPLAINT_REMARKS"]?.ToString(),
                            Prepared_By = reader["PREPARED_BY"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PREPARED_BY"])
                        });
                    }
                    return Json(new { success = true, data = list });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false,  message =ex.Message });
            }
        }
    }
}
