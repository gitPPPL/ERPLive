using iTextSharp.text.xml.xmp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Controllers.Inventory.Transaction
{
    public class InventoryTransferRequestController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public InventoryTransferRequestController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
        public IActionResult Index()
        {

            var globalVariables = _globalVariableService.GetGlobalVariables();
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            return View("~/Views/Inventory/Transaction/InventoryTransferRequest/Index.cshtml");
        }

        public JsonResult GetVNo(string Vtype, string Tablename = "ISSUE1")
        {
            string newV_NO = "00000";
            try
            {
                newV_NO = _globalValidationdate.GetVNo(Vtype, Tablename);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }

        public JsonResult DDlVType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE IN ('InventoryRequest') ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        public JsonResult DDlStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlPlace()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE,NAME FROM PLACE_MAST where comp_code=" + getdata.PubCompCode + " Order by NAME ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlHOD()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select distinct b.code,EMP_NAME from PAYGATE_HOD a left join EMP_MAST b on  a.EMP_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE where b.RESIGN_DATE is null and a.COMP_CODE=" + getdata.PubCompCode + " order by  EMP_NAME ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlDeptName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT distinct b.CODE,b.NAME FROM USER_DEPT a left join ITEMDEPT_MAST b on a.DEPT_CODE=b.CODE and a.comp_code=b.COMP_CODE " +
                "where a.user_code= " + getdata.PubUserId + " and a.comp_code=" + getdata.PubCompCode + " and b.TRAN_TYPE='Store' order by  b.NAME  ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlItemName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEM_MAST where comp_code=" + getdata.PubCompCode + " and active=1 order by name  ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlUnit()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select distinct unit_code,unit_name from ITEM_MAST where comp_code=" + getdata.PubCompCode + " and active=1 order by unit_name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDLItemmake()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEMMAKE_MAST where comp_code=" + getdata.PubCompCode + " and active=1  order by name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDLItemDapt()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,name from ITEMDEPT_MAST where comp_code=" + getdata.PubCompCode + " and active=1  order by name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult GetDataByItemcode(int ItemCode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var data = new object();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string sql = @"Select  c.NAME 'unit_name',c.CODE 'unit_code' from item_mast a 
                left join Item_Mgroup b on a.Mgroup_code=b.code  and a.comp_code=b.comp_code
                left outer join ITEMUNIT_MAST c on a.UNIT_CODE=c.CODE and c.comp_code=@CompCode
                where a.comp_code=@CompCode and a.active=1 and b.Mgroup_type in ('Store','Fuel')  and  a.CODE = @ItemCode   group by  c.NAME ,c.CODE  order by
                c.NAME ";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@ItemCode", ItemCode);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            data = new
                            {
                                unit_code = reader["unit_code"],
                                unit_name = reader["unit_name"]
                            };
                        }
                    }
                }
            }

            return Json(new
            {
                Data = data,
                Status = true
            });
        }
        public string GetText(string query)
        {
            try
            {
                using var con = _dbConnection.GetErpConnection();
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return reader[0].ToString();
                            }
                            else
                            {
                                return string.Empty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetText() Error: " + ex.Message);
                return string.Empty;
            }
        }

        [HttpPost]
        public async Task<JsonResult> SavedData([FromBody] InventoryTransferRequest_Model request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, status = "Error", message = "Input model is null" });
            }

            var action = string.Equals(request.Header.action, "INSERT", StringComparison.OrdinalIgnoreCase) ? "INSERT" : "UPDATE";

            var validationresult = await Validation(request.Header, request.Details, action);

            if (validationresult.Status == "Info")
            {
                return Json(new { status = validationresult.Status, message = validationresult.Message });
            }

            var result = await SubmitRequest(request.Header, request.Details, action);

            return Json(new { success = result.Status == "Success", status = result.Status, message = result.Message });
        }

        private async Task<(string Status, string Message)> SubmitRequest(InventoryTransferRequest_Header header, List<InventoryTransferRequest_Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                Boolean isApprovalBody = false;
                Boolean isFinalApprovalBody = false;
                string DOC_APPROSTAGE = "";
                string APPROV_USER = "";
                string fappstatus = "";
                string fappRemark = "";

                await conn.OpenAsync();

                if (action == "INSERT")
                {
                    var jsonResult = GetVNo(header.V_TYPE) as JsonResult;

                    dynamic data = jsonResult?.Value;

                    if (data == null || data.V_NO == null)
                    {
                        return ("Error", "Unable to generate voucher number.");
                    }

                    header.V_NO = Convert.ToInt32(data.V_NO);
                }



                DOC_APPROSTAGE = GetText("select 1 from DOC_APPROSTAGE where USER_CODE= " + g.PubUserId + " and DOC_CODE= '" + header.V_TYPE + "' and comp_code= " + g.PubCompCode + " ");

                if (DOC_APPROSTAGE == "1")
                {
                    isApprovalBody = true;
                }

                APPROV_USER = GetText("select APPROV_USER from DOC_APPROSTAGE where USER_CODE = " + g.PubUserId + " and DOC_CODE = '" + header.V_TYPE + "' and comp_code = " + g.PubCompCode + "");

                if (APPROV_USER == "FINAL")
                {
                    isFinalApprovalBody = true;
                }

                fappstatus = "Approved";
                fappRemark = "Document Approved.";

                if (isFinalApprovalBody == false)
                {
                    if (details != null && details.Count > 0)
                    {
                        foreach (var detail in details)
                        {

                            if (detail == null || detail.ITEM_CODE <= 0)
                                continue;

                            if (detail.LAND_AMT > 20000)
                            {
                                fappstatus = "";
                                fappRemark = "";
                            }
                        }

                    }

                }

                if (fappstatus == "")
                {
                    return ("False", "In this Document Item Amount is More then 20000/-, so in that case Approval is Required, Please Send Approval first");
                }



                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;

                using (var cmd = new SqlCommand("sp_InventoryTransferRequest", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                    cmd.Parameters.AddWithValue("@STATUS", header.STATUS);
                    cmd.Parameters.AddWithValue("@DEPT_CODE", header.DEPT_CODE);
                    cmd.Parameters.AddWithValue("@SHIFT", header.SHIFT);
                    cmd.Parameters.AddWithValue("@PLACE_CODE", header.PLACE_CODE);
                    cmd.Parameters.AddWithValue("@EMP_CODE", header.EMP_CODE);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fappRemark);
                    cmd.Parameters.AddWithValue("@REMARKS", (object?)header.REMARKS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;

                        using var cmd1 = new SqlCommand("sp_InventoryTransferRequest", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd1.Parameters.Add("@Action", SqlDbType.NVarChar, 25).Value = "BalAmtorQty";
                        cmd1.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;
                        cmd1.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = g.PubBranchCode;
                        cmd1.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = g.PubFYearCode;
                        cmd1.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                        cmd1.Parameters.Add("@V_NO", SqlDbType.Int).Value = header.V_NO;
                        cmd1.Parameters.Add("@ITEM_CODE", SqlDbType.Int).Value = detail.ITEM_CODE;
                        cmd1.Parameters.Add("@QTY", SqlDbType.Decimal).Value = detail.QTY;

                        using (var reader = await cmd1.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                detail.LAND_AMT = reader["LAND_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["LAND_AMT"]);
                            }
                        }

                        using var cmd = new SqlCommand("sp_InventoryTransferRequest", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "Details");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@SNO", detail.SNO);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", detail.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", (object?)detail.ITEM_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MAKE_CODE", (object?)detail.MAKE_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UOM_CODE", (object?)detail.UOM_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UOM_NAME", (object?)detail.UOM_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FROM_DEPT", (object?)detail.FROM_DEPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TO_DEPT", (object?)detail.TO_DEPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@MACH_CODE", (object?)detail.MAC_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NOS", (object?)detail.NOS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@QTY", (object?)detail.QTY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@RATE", (object?)detail.RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AMOUNT", (object?)detail.AMOUNT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LAND_AMT", (object?)detail.LAND_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", (object?)detail.REMARKS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return ("Success", "Data Save Successfully");
            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

        private async Task<(string Status, string Message)> Validation(InventoryTransferRequest_Header header, List<InventoryTransferRequest_Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();

                await conn.OpenAsync();

                if (details == null || details.Count == 0)
                    return ("Error", "No detail data found.");

                foreach (var detail in details)
                {
                    if (detail == null || detail.ITEM_CODE <= 0)
                        continue;

                    using var cmd = new SqlCommand("sp_InventoryTransferRequest", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 25).Value = "CURSTOCK";
                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = g.PubCompCode;
                    cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.Int).Value = g.PubBranchCode;
                    cmd.Parameters.Add("@YEAR_CODE", SqlDbType.Int).Value = g.PubFYearCode;
                    cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                    cmd.Parameters.Add("@V_NO", SqlDbType.Int).Value = header.V_NO;
                    cmd.Parameters.Add("@ITEM_CODE", SqlDbType.Int).Value = detail.ITEM_CODE;
                    cmd.Parameters.Add("@UUSER", SqlDbType.Int).Value = g.PubUserId;
                    cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                    cmd.Parameters.Add("@EUSER", SqlDbType.Int).Value = g.PubUserId;
                    cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                    cmd.Parameters.Add("@AED", SqlDbType.NVarChar, 1).Value = "A";
                    cmd.Parameters.Add("@WSID", SqlDbType.NVarChar, 100).Value = g.PubWorkStationID ?? (object)DBNull.Value;
                    cmd.Parameters.Add("@LIP", SqlDbType.NVarChar, 100).Value = g.PubLocalId ?? (object)DBNull.Value;
                    cmd.Parameters.Add("@LID", SqlDbType.NVarChar, 100).Value =
                        Environment.MachineName;

                    var result = await cmd.ExecuteScalarAsync();

                    decimal curStock = result == null || result == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(result);

                    if (detail.QTY > curStock)
                    {
                        return (
                            "Info",
                            $"Insufficient stock for Item Code {detail.ITEM_CODE}. " +
                            $"Available Stock: {curStock}, Requested Qty: {detail.QTY}"
                        );
                    }
                }


                return ("Success", "Validation successful.");
            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("ISSUE1", vdate, vtype, vno);
            return Ok(result);
        }

        public JsonResult CreateView(string V_TYPE , DateTime From_DATE , DateTime To_DATE , int DEPT_CODE)
        {
            try
            {
                var GlobalData = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand("sp_InventoryTransferRequest", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "Createview");
                cmd.Parameters.AddWithValue("@V_TYPE", V_TYPE);
                cmd.Parameters.AddWithValue("@COMP_CODE", GlobalData.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", GlobalData.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", GlobalData.PubFYearCode);
                cmd.Parameters.Add("@From_DATE", SqlDbType.SmallDateTime).Value = From_DATE;
                cmd.Parameters.Add("@To_DATE", SqlDbType.SmallDateTime).Value = To_DATE;
                cmd.Parameters.AddWithValue("@DEPT_CODE", DEPT_CODE);

                conn.Open();
                cmd.ExecuteNonQuery();

                return Json(new  { success = true, message = "View created successfully" });
            }
            catch (Exception error)
            {
                Console.WriteLine(error);

                return Json(new { success = false, message = error.Message });
            }
        }


    }
}
