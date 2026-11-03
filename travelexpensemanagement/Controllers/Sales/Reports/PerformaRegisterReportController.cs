using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Reports
{
    public class PerformaRegisterReportController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public PerformaRegisterReportController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
       ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public async Task<IActionResult> Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            var loadGeneralSetting = await _globalVariableService.LoadGeneralSetting();

            ViewBag.LoadGeneralSetting = loadGeneralSetting;

            string databaseName;

            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            return View("~/Views/Sales/Reports/PerformaRegisterReport/Index.cshtml");
        }

        public JsonResult DDlInsurancesName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
               string query = "select code,name from insu_mast where comp_code =" + getdata.PubCompCode + "  order by name;";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }


        public JsonResult Report(DateTime ToDate, DateTime FromDate, int InsuranceId)
        {
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // 1. Delete temporary data
                    string deleteQuery = @"
                    DELETE FROM TEMP_GST WHERE WSID = @WSID AND USERID = @USERID";

                    using (SqlCommand cmd = new SqlCommand(deleteQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@WSID", getdata.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@USERID", getdata.PubUserId);

                        cmd.ExecuteNonQuery();
                    }

                    // 2. Calculate previous insurance sales amount
                    string salesQuery = @"
                        SELECT SUM(ISNULL(SALE1.NAMOUNT, 0)) FROM SALE1 LEFT JOIN SALE3 ON SALE3.V_NO = SALE1.INSU_NO
                        AND SALE3.COMP_CODE = SALE1.COMP_CODE WHERE SALE3.INSU_CODE = @InsuranceId
                        AND SALE1.V_DATE < @FromDate AND ISNULL(SALE1.INSU_NO, 0) <> 0 AND SALE1.COMP_CODE = @CompCode
                        AND SALE1.BRANCH_CODE = @BranchCode AND SALE1.V_TYPE = 'SAGT'";

                    decimal PubRes1Str = 0;

                    using (SqlCommand cmd = new SqlCommand(salesQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@InsuranceId", InsuranceId);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);

                        object result = cmd.ExecuteScalar();

                        PubRes1Str = result != DBNull.Value && result != null
                            ? Convert.ToDecimal(result)
                            : 0;
                    }

                    // 3. Calculate insurance policy amount
                    string policyQuery = @"
                    SELECT SUM(ISNULL(POLICY_AMT, 0))  FROM INSU_MAST WHERE CODE = @InsuranceId
                    AND EFF_DATE <= @FromDate  AND COMP_CODE = @CompCode";

                    decimal PubRes3Str = 0;

                    using (SqlCommand cmd = new SqlCommand(policyQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@InsuranceId", InsuranceId);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        object result = cmd.ExecuteScalar();
                        PubRes3Str = result != DBNull.Value && result != null ? Convert.ToDecimal(result) : 0;
                    }

                    decimal OpeningBalance = PubRes3Str - PubRes1Str;

                    string insertQuery = @"
                    INSERT INTO TEMP_GST (WSID,USERID,COMP_CODE,V_TYPE,V_NO,V_DATE,ITEM_CODE,ITEM_NAME,CGST,SGST)
                    SELECT @WSID, @USERID, SALE3.COMP_CODE, SALE3.V_TYPE, SALE3.V_NO, SALE3.V_DATE, SALE3.INSU_CODE,
                    ('invoice no. from '  + CONVERT(VARCHAR, SALE3.F_BILL_NO) + ' to ' + CONVERT(VARCHAR, SALE3.T_BILL_NO)),
                    0, 0 FROM SALE3 WHERE SALE3.V_DATE >= @FromDate AND SALE3.V_DATE < DATEADD(DAY, 1, @ToDate)
                    AND ISNULL(SALE3.INSU_CODE, 0) <> 0 AND SALE3.COMP_CODE = @CompCode  AND SALE3.BRANCH_CODE = @BranchCode
                    AND SALE3.INSU_CODE = @InsuranceId";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@WSID", getdata.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@USERID", getdata.PubUserId);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate.Date);
                        cmd.Parameters.AddWithValue("@ToDate", ToDate.Date);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@InsuranceId", InsuranceId);

                        int insertedRows = cmd.ExecuteNonQuery();
                    }


                    // 5. Return result
                        return Json(new { success = true, PreviousSalesAmount = PubRes1Str, PolicyAmount = PubRes3Str, OpeningBalance = OpeningBalance, message = "Report generated successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new  {  success = false, message = ex.Message });
            }
        }

    }
}













