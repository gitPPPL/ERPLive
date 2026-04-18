using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.MobileBillEntry;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class MobileBillEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public MobileBillEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/Payroll/MonthlyTransaction/MobileBillEntry/Index.cshtml");
        }


        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            try
            {
                var userSession = _globalVariableService.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = "1";
                var vType = V_type;
                var tableName = "PAY_MOBILE1";

                var yearParams = new Dictionary<string, object> { { "@YearCd", yearCode } };
                var vnoParams = new Dictionary<string, object>
                {
                    { "@COMP_CODE", companyCode },
                    { "@BRANCH_CODE", branchCode },
                    { "@YEAR_CODE", yearCode },
                    { "@V_TYPE", vType },
                    { "@TableName", tableName }
                };

                string nextVNo = await _dbHelper.GetExecuteScalarAsync<string>("sp_GetMaxVNo", vnoParams, isStoredProc: true);
                string year = await _dbHelper.GetExecuteScalarAsync<string>("SELECT dbo.fn_GetCurrentYear(@YearCd)", yearParams);
                var docId = (vType) + (year) + (nextVNo);
                var newVno = year + nextVNo;
                var docIdNoList = new { DocId = docId, VNo = newVno };
                return Json(new { status = true, data = docIdNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public IActionResult GetDocumentTypeList()
        {
            string query = "SELECT CODE, NAME FROM DOCTYPE_MAST(NOLOCK)  where doctype= 'PayMobile' AND CODE = 'MBIL' ORDER BY NAME ASC";
            var docTypeList = _dropdownService.GetDropdownList(query);
            return Json(new { status = "success", data = docTypeList });
        }

        [HttpGet]
        public IActionResult GetEmplist()
        {
            var g = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE, CONCAT(NAME , ' - ',CODE ) as NAME  FROM EMP_MAST(NOLOCK)  WHERE COMP_CODE  =  " + g.PubCompCode + " ORDER BY NAME ASC";
            var docTypeList = _dropdownService.GetDropdownList(query);
            return Json(new { status = "success", data = docTypeList });
        }

        [HttpGet]
        public IActionResult GetSubgroupAccount()
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                string query = " SELECT CODE,NAME FROM SUBGROUP_MAST(NOLOCK) WHERE COMP_CODE =" + g.PubCompCode + " AND ACTIVE=1 ORDER BY NAME ";
                var subAccountlist = _dropdownService.GetDropdownList(query);

                return new JsonResult(new { success = true, data = subAccountlist });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, Message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetMobBillEntryDatabyId(int Vno)
        {
            MobileEntryModel mbillEntry = new MobileEntryModel();
            mbillEntry.Details = new MobileEntryModel.details();

            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                string selectQuery = @"SELECT DOC_ID, DOC_ID AS [DOCUMENT ID], V_TYPE, DTY.NAME AS [DOCTYPE_NAME], V_NO, V_DATE,
                                   REMARK, BILL_AMT, DEDUCT_AMT, DRAC_BILL, SGDB.NAME AS [DR_BILL_NAME],
                                   CRAC_BILL, SGCB.NAME AS [CR_BILL_NAME], CGST_AMT, SGST_AMT, IGST_AMT,
                                   CGST_AC, SCGST.NAME AS [CGST_NAME], SGST_AC, SSGST.NAME AS [SGST_NAME],
                                   IGST_AC, SIGST.NAME AS [IGST_NAME], PM1.UUSER, PM1.UDATE
                            FROM PAY_MOBILE1 PM1(NOLOCK)
                            LEFT JOIN DOCTYPE_MAST DTY(NOLOCK) ON DTY.CODE = PM1.V_TYPE
                            LEFT JOIN SUBGROUP_MAST SGDB(NOLOCK) ON SGDB.CODE = PM1.DRAC_BILL AND PM1.COMP_CODE = SGDB.COMP_CODE
                            LEFT JOIN SUBGROUP_MAST SGCB(NOLOCK) ON SGCB.CODE = PM1.CRAC_BILL AND PM1.COMP_CODE = SGCB.COMP_CODE
                            LEFT JOIN SUBGROUP_MAST SCGST(NOLOCK) ON SCGST.CODE = PM1.CGST_AC AND PM1.COMP_CODE = SCGST.COMP_CODE
                            LEFT JOIN SUBGROUP_MAST SSGST(NOLOCK) ON SSGST.CODE = PM1.SGST_AC AND PM1.COMP_CODE = SSGST.COMP_CODE
                            LEFT JOIN SUBGROUP_MAST SIGST(NOLOCK) ON SIGST.CODE = PM1.IGST_AC AND PM1.COMP_CODE = SIGST.COMP_CODE
                            WHERE PM1.COMP_CODE = " + g.PubCompCode + @" 
                            AND PM1.BRANCH_CODE = 1
                            AND PM1.YEAR_CODE = " + g.PubFYearCode + @" 
                            AND PM1.V_NO = " + Vno + @"
                            ORDER BY PM1.V_NO, PM1.V_DATE";


                SqlConnection conn = _dbConnection.GetErpConnection();
                conn.Open();
                using (SqlCommand command = new SqlCommand(selectQuery, conn))
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = selectQuery;
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        mbillEntry.Details.header = new MobileEntryModel.Header()
                        {
                            VType = reader["V_TYPE"]?.ToString(),
                            VNo = reader["V_NO"] != DBNull.Value ? Convert.ToString(reader["V_NO"]) : "",
                            Vdate = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd") : "",
                            BillAmt = reader["BILL_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["BILL_AMT"]) : 0,
                            CgstAmt = reader["CGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["CGST_AMT"]) : 0,
                            SgstAmt = reader["SGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["SGST_AMT"]) : 0,
                            IgstAmt = reader["IGST_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["IGST_AMT"]) : 0,
                            DeductAmt = reader["DEDUCT_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DEDUCT_AMT"]) : 0,
                            BillDrAccount = reader["DRAC_BILL"] != DBNull.Value ? Convert.ToInt32(reader["DRAC_BILL"]) : 0,
                            BillCrAccount = reader["CRAC_BILL"] != DBNull.Value ? Convert.ToInt32(reader["CRAC_BILL"]) : 0,
                            CGSTAccount = reader["CGST_AC"] != DBNull.Value ? Convert.ToInt32(reader["CGST_AC"]) : 0,
                            SGSTAccount = reader["SGST_AC"] != DBNull.Value ? Convert.ToInt32(reader["SGST_AC"]) : 0,
                            IGSTAccount = reader["IGST_AC"] != DBNull.Value ? Convert.ToInt32(reader["IGST_AC"]) : 0,
                            Action = "UPDATE"
                        };
                    }
                }

                selectQuery = @" SELECT V_TYPE, V_NO, V_DATE, DOC_ID, EMP_CODE, EM.NAME AS [EMP_NAME],
                                MOBILE_NO, PM2.SNO, LIMIT, BILL_AMT, DEDUCT_AMT, REMARK, BILL_NAME,
                                DR_AC, SGDR.NAME AS [DR_AC_NAME], CR_AC, SGCR.NAME AS [CR_AC_NAME]
                                FROM PAY_MOBILE2 PM2(NOLOCK)
                                LEFT JOIN DOCTYPE_MAST DTY(NOLOCK) ON DTY.CODE = PM2.V_TYPE
                                LEFT JOIN EMP_MAST EM(NOLOCK) ON EM.CODE = PM2.EMP_CODE AND PM2.COMP_CODE = EM.COMP_CODE
                                LEFT JOIN SUBGROUP_MAST SGDR(NOLOCK) ON SGDR.CODE = PM2.DR_AC AND PM2.COMP_CODE = SGDR.COMP_CODE
                                LEFT JOIN SUBGROUP_MAST SGCR(NOLOCK) ON SGCR.CODE = PM2.CR_AC AND PM2.COMP_CODE = SGCR.COMP_CODE
                                WHERE PM2.COMP_CODE = " + g.PubCompCode + @" 
                                AND PM2.BRANCH_CODE = 1 
                                AND PM2.YEAR_CODE = " + g.PubFYearCode + @" 
                                AND PM2.V_NO = " + Vno + @"
                                ORDER BY PM2.MOBILE_NO";

                List<MobileEntryModel.tableRow> tableDataList = new List<MobileEntryModel.tableRow>();

                if (conn.State == ConnectionState.Open) { conn.Close(); }
                conn.Open();

                using (SqlCommand command = new SqlCommand(selectQuery, conn))
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = selectQuery;
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        tableDataList.Add(new MobileEntryModel.tableRow()
                        {
                            EmpCode = reader["EMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["EMP_CODE"]) : 0,
                            EmpName = reader["EMP_NAME"]?.ToString() ?? "",
                            MobNo = reader["MOBILE_NO"]?.ToString() ?? "",
                            Limit = reader["LIMIT"] != DBNull.Value ? Convert.ToDecimal(reader["LIMIT"]) : 0,
                            BillAmt = reader["BILL_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["BILL_AMT"]) : 0,
                            DeductAmt = reader["DEDUCT_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DEDUCT_AMT"]) : 0,
                            Name = reader["BILL_NAME"]?.ToString() ?? "",
                            DrAcName = reader["DR_AC"]?.ToString() ?? "",
                            CrAcName = reader["CR_AC"]?.ToString() ?? "",
                            Remarks = reader["REMARK"]?.ToString() ?? ""
                        });
                    }
                }

                mbillEntry.Details.TableRow = tableDataList;

                return Json(new { status = "success", data = mbillEntry.Details });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }

        }


        [HttpPost]
        public IActionResult SaveMobileEntryData([FromBody] MobileEntryModel mbillEntry)
        {
            try
            {
                if (mbillEntry == null)
                {
                    return new JsonResult(new { success = false, message = "No valid data." });
                }

                var g = _globalVariableService.GetGlobalVariables();
                bool pubIsSave = false;
                var header = mbillEntry.Details.header;
                var tablerow = mbillEntry.Details.TableRow;

                SqlTransaction tran = null;
                try
                {
                    //commitchanges(MyDGV1);
                    //pubArrControl = new object[,] { { txtVNo, "Sr No " }, { txtVDate, "Date " } };
                    //if (ISvalidControl(pubArrControl) == false)
                    //{
                    //    return;
                    //}

                    //if (txtVDate.Text == "  /  /")
                    //{
                    //    MessageBox.Show("Voucher Date can't blank", titleInformation, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    //    return;
                    //}

                    //CalBillAndDeductAmt();

                    //if (chkAccounts() == false)
                    //{
                    //    return;
                    //}

                    string DOCID = mbillEntry.Details.header.VType + mbillEntry.Details.header.VNo;

                    string AED;
                    int UUSER = 0;
                    DateTime UDATE = DateTime.Now;

                    SqlConnection con = _dbConnection.GetErpConnection();
                    con.Open();

                    if (con.State == ConnectionState.Open) con.Close();
                    con.Open();
                    tran = con.BeginTransaction();

                    string qry = "DELETE FROM PAY_MOBILE2 WHERE V_NO=@V_NO AND V_TYPE=@V_TYPE AND YEAR_CODE=@YEAR_CODE AND BRANCH_CODE=@BRANCH_CODE AND COMP_CODE=@COMP_CODE";
                    SqlCommand pubCmd = new SqlCommand(qry, con, tran);
                    pubCmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    pubCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    pubCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    pubCmd.Parameters.AddWithValue("@V_TYPE", header.VType);
                    pubCmd.Parameters.AddWithValue("@V_NO", header.VNo);
                    pubCmd.Parameters.AddWithValue("@V_DATE", header.Vdate);
                    pubCmd.Parameters.AddWithValue("@DOC_ID", DOCID);
                    pubCmd.ExecuteNonQuery();

                    if (header.Action == "UPDATE")
                    {
                        var dt1 = _dbHelper.ExecuteQueryAsync("SELECT UUSER, UDATE FROM PAY_MOBILE1 WHERE COMP_CODE=@COMP_CODE AND V_NO=@V_NO AND V_TYPE=@V_TYPE",
                            new List<SqlParameter>
                            {
                               new SqlParameter( "@COMP_CODE", g.PubCompCode ),
                               new SqlParameter( "@V_NO", Convert.ToInt32( header.VNo) ),
                               new SqlParameter( "@V_TYPE", header.VType )
                            }).GetAwaiter().GetResult();

                        if (dt1.Rows.Count > 0)
                        {
                            UUSER = dt1.Rows[0]["UUSER"] != DBNull.Value ? Convert.ToInt32(dt1.Rows[0]["UUSER"]) : 0;
                            UDATE = dt1.Rows[0]["UDATE"] != DBNull.Value ? Convert.ToDateTime(dt1.Rows[0]["UDATE"]) : DateTime.Now;
                        }
                    }

                    if (header.Action == "INSERT")
                    {
                        AED = "A";
                        //addsrno();
                        // "Document Already Present for this doc no. and doc type "

                        var dt = _dbHelper.ExecuteQueryAsync("SELECT COUNT(1) FROM PAY_MOBILE1 WHERE COMP_CODE=@COMP_CODE AND YEAR_CODE = @YEAR_CODE AND V_NO=@V_NO AND V_TYPE=@V_TYPE",
                            new List<SqlParameter>
                            {
                               new SqlParameter( "@COMP_CODE", g.PubCompCode ),
                               new SqlParameter( "@YEAR_CODE", g.PubFYearCode),
                               new SqlParameter( "@V_NO", Convert.ToInt32( header.VNo) ),
                               new SqlParameter( "@V_TYPE", header.VType )
                            }).GetAwaiter().GetResult();

                        if (dt.Rows.Count > 0 && Convert.ToString(dt.Rows[0].ItemArray[0]) != "0")
                        {
                            return new JsonResult(new { success = false, message = "current Vno already exist." });
                        }

                        qry = "INSERT INTO [PAY_MOBILE1] (V_TYPE, V_NO, V_DATE, DOC_ID, REMARK, BILL_AMT, DEDUCT_AMT, DRAC_BILL, CRAC_BILL, CGST_AMT, SGST_AMT, IGST_AMT, CGST_AC, SGST_AC, IGST_AC, COMP_CODE, BRANCH_CODE, YEAR_CODE, UUSER, UDATE,  AED, WSID, LIP, LID) " +
                            "VALUES (@V_TYPE, @V_NO, @V_DATE, @DOC_ID, @REMARK, @BILL_AMT, @DEDUCT_AMT, @DRAC_BILL, @CRAC_BILL, @CGST_AMT, @SGST_AMT, @IGST_AMT, @CGST_AC, @SGST_AC, @IGST_AC, @COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @UUSER, GETDATE(), @AED, @WSID, @LIP, @LID)";

                        pubCmd = new SqlCommand(qry, con, tran);
                        pubCmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        pubCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        pubCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        pubCmd.Parameters.AddWithValue("@V_TYPE", header.VType);
                        pubCmd.Parameters.AddWithValue("@V_NO", header.VNo);
                        pubCmd.Parameters.AddWithValue("@V_DATE", header.Vdate);
                        pubCmd.Parameters.AddWithValue("@DOC_ID", DOCID);
                        pubCmd.Parameters.AddWithValue("@REMARK", "");
                        pubCmd.Parameters.AddWithValue("@BILL_AMT", header.BillAmt);
                        pubCmd.Parameters.AddWithValue("@DEDUCT_AMT", header.DeductAmt);
                        pubCmd.Parameters.AddWithValue("@DRAC_BILL", header.BillDrAccount);
                        pubCmd.Parameters.AddWithValue("@CRAC_BILL", header.BillCrAccount);
                        pubCmd.Parameters.AddWithValue("@CGST_AMT", header.CgstAmt);
                        pubCmd.Parameters.AddWithValue("@SGST_AMT", header.SgstAmt);
                        pubCmd.Parameters.AddWithValue("@IGST_AMT", header.IgstAmt);
                        pubCmd.Parameters.AddWithValue("@CGST_AC", header.CGSTAccount);
                        pubCmd.Parameters.AddWithValue("@SGST_AC", header.SGSTAccount);
                        pubCmd.Parameters.AddWithValue("@IGST_AC", header.IGSTAccount);
                        pubCmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        pubCmd.Parameters.AddWithValue("@AED", "A");
                        pubCmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        pubCmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        pubCmd.Parameters.AddWithValue("@LID", g.PubLocalId);

                        if (pubCmd.ExecuteNonQuery() > 0)
                        {
                            pubIsSave = true;
                        }
                    }
                    else
                    {
                        AED = "E";
                        qry = "UPDATE PAY_MOBILE1 SET V_DATE=@V_DATE, REMARK=@REMARK, BILL_AMT=@BILL_AMT, DEDUCT_AMT=@DEDUCT_AMT, DRAC_BILL=@DRAC_BILL, CRAC_BILL=@CRAC_BILL, CGST_AMT=@CGST_AMT, SGST_AMT=@SGST_AMT, IGST_AMT=@IGST_AMT, CGST_AC=@CGST_AC, SGST_AC=@SGST_AC, IGST_AC=@IGST_AC, EUSER=@EUSER, EDATE= GETDATE() , AED=@AED, WSID=@WSID, LIP=@LIP, LID=@LID WHERE YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND V_TYPE=@V_TYPE AND V_NO=@V_NO AND DOC_ID=@DOC_ID";

                        pubCmd = new SqlCommand(qry, con, tran);
                        pubCmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        pubCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        pubCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        pubCmd.Parameters.AddWithValue("@V_TYPE", header.VType);
                        pubCmd.Parameters.AddWithValue("@V_NO", header.VNo);
                        pubCmd.Parameters.AddWithValue("@V_DATE", header.Vdate);
                        pubCmd.Parameters.AddWithValue("@DOC_ID", DOCID);
                        pubCmd.Parameters.AddWithValue("@REMARK", "");
                        pubCmd.Parameters.AddWithValue("@BILL_AMT", header.BillAmt);
                        pubCmd.Parameters.AddWithValue("@DEDUCT_AMT", header.DeductAmt);
                        pubCmd.Parameters.AddWithValue("@DRAC_BILL", header.BillDrAccount);
                        pubCmd.Parameters.AddWithValue("@CRAC_BILL", header.BillCrAccount);
                        pubCmd.Parameters.AddWithValue("@CGST_AMT", header.CgstAmt);
                        pubCmd.Parameters.AddWithValue("@SGST_AMT", header.SgstAmt);
                        pubCmd.Parameters.AddWithValue("@IGST_AMT", header.IgstAmt);
                        pubCmd.Parameters.AddWithValue("@CGST_AC", header.CGSTAccount);
                        pubCmd.Parameters.AddWithValue("@SGST_AC", header.SGSTAccount);
                        pubCmd.Parameters.AddWithValue("@IGST_AC", header.IGSTAccount);
                        pubCmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        pubCmd.Parameters.AddWithValue("@AED", "A");
                        pubCmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        pubCmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        pubCmd.Parameters.AddWithValue("@LID", g.PubLocalId);

                        if (pubCmd.ExecuteNonQuery() > 0)
                        {
                            pubIsSave = true;
                        }
                    }

                    // Save data into PAY_MOBILE2 table
                    foreach (var row in tablerow.Select((value, index) => new { value, index }))
                    {
                        int i = row.index;
                        var data = row.value;

                        //if (Convert.ToDecimal(dataNulltoEmpty(MyDGV1.Rows[i].Cells[5].Value)) == 0)
                        //{
                        //    MyDGV1.Rows[i].Cells[10].Value = 0;
                        //    MyDGV1.Rows[i].Cells[11].Value = 0;
                        //}

                        qry = "INSERT INTO [PAY_MOBILE2] (V_TYPE, V_NO, V_DATE, DOC_ID, EMP_CODE, MOBILE_NO, SNO, LIMIT, BILL_AMT, DEDUCT_AMT, REMARK, BILL_NAME, DR_AC, CR_AC, COMP_CODE, BRANCH_CODE, YEAR_CODE, UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID) " +
                              "VALUES (@V_TYPE, @V_NO, @V_DATE, @DOC_ID, @EMP_CODE, @MOBILE_NO, @SNO, @LIMIT, @BILL_AMT, @DEDUCT_AMT, @REMARK, @BILL_NAME, @DR_AC, @CR_AC, @COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @UUSER, @UDATE, @EUSER, @EDATE, @AED, @WSID, @LIP, @LID)";

                        pubCmd = new SqlCommand(qry, con, tran);

                        pubCmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        pubCmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        pubCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        pubCmd.Parameters.AddWithValue("@V_TYPE", header.VType);
                        pubCmd.Parameters.AddWithValue("@V_NO", header.VNo);
                        pubCmd.Parameters.AddWithValue("@V_DATE", header.Vdate);
                        pubCmd.Parameters.AddWithValue("@DOC_ID", DOCID);
                        pubCmd.Parameters.AddWithValue("@EMP_CODE", data.EmpCode);
                        pubCmd.Parameters.AddWithValue("@MOBILE_NO", data.MobNo);
                        pubCmd.Parameters.AddWithValue("@SNO", i + 1);
                        pubCmd.Parameters.AddWithValue("@LIMIT", data.Limit);
                        pubCmd.Parameters.AddWithValue("@BILL_AMT", Convert.ToDecimal(data.BillAmt));
                        pubCmd.Parameters.AddWithValue("@DEDUCT_AMT", Convert.ToDecimal(data.DeductAmt));
                        pubCmd.Parameters.AddWithValue("@REMARK", data.Remarks);
                        pubCmd.Parameters.AddWithValue("@BILL_NAME", data.Name);
                        pubCmd.Parameters.AddWithValue("@DR_AC", Convert.ToInt32("0" + data.DrAcName));
                        pubCmd.Parameters.AddWithValue("@CR_AC", Convert.ToInt32("0" + data.CrAcName));

                        if (AED == "A")
                        {
                            pubCmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                            pubCmd.Parameters.AddWithValue("@UDATE", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                            pubCmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                            pubCmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        }
                        else
                        {
                            pubCmd.Parameters.AddWithValue("@UUSER", UUSER);
                            pubCmd.Parameters.AddWithValue("@UDATE", UDATE.ToString("yyyy-MM-dd HH:mm"));
                            pubCmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                            pubCmd.Parameters.AddWithValue("@EDATE", DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        }

                        pubCmd.Parameters.AddWithValue("@AED", AED);
                        pubCmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        pubCmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        pubCmd.Parameters.AddWithValue("@LID", g.PubLocalId);

                        if (pubCmd.ExecuteNonQuery() > 0)
                        {
                            pubIsSave = true;
                        }
                    }

                    if (pubIsSave)
                    {
                        tran.Commit();
                    }
                    else
                    {
                        tran.Rollback();
                    }
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    return Json(new { success = false, message = ex.Message });
                }
                return Json(new { success = true, message = "Save sucess." });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }


        [HttpGet]
        public IActionResult GetCopyMobileList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var dataList = new List<object>();
            int totalCount = 0;
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())

                using (SqlCommand cmd = new SqlCommand("usp_CopyFromMbillEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    // Required parameters                  
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_type", "MBIL");
                    cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // Read paged data
                        int rowindex = 0;
                        while (reader.Read())
                        {
                            dataList.Add(new 
                            {
                                DocId = reader["DOC_ID"] != DBNull.Value ? Convert.ToString(reader["DOC_ID"]) : (string?)null,
                                DocNo = reader["V_NO"] != DBNull.Value ? Convert.ToString(reader["V_NO"]) : "",
                                DocDate = reader["V_DATE"]?.ToString(),
                                BillAmount = reader["BILL_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["BILL_AMT"]) : 0,
                                DeductAmount = reader["DEDUCT_AMT"] != DBNull.Value ? Convert.ToDecimal(reader["DEDUCT_AMT"]) : 0,
                                Dr_bill_name = reader["DR_AC_NAME"]?.ToString(),
                                Cr_bill_name = reader["CR_AC_NAME"]?.ToString(),
                                Remarks = reader["REMARK"]?.ToString(),
                                CgstAmt = reader["CGST_AMT"]?.ToString(),
                                SgstAmt = reader["SGST_AMT"]?.ToString(),
                                IgstAmt = reader["IGST_AMT"]?.ToString(),
                                CgstActName = reader["CGST_ACNAME"]?.ToString(),
                                SgstAcName = reader["SGST_ACNAME"]?.ToString(),
                                IgstAcName = reader["IGST_ACNAME"]?.ToString()
                            });
                            rowindex++;

                            if (rowindex == 0) // Assuming total count is same for all rows in the current page
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;

                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
            return Json(new { data = dataList, totalCount = totalCount });
        }



    }

}
