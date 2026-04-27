using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Gate_Entry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class VisitorEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public VisitorEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService , GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }
        public IActionResult Index()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = globalVar.PubBranchCode;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/GateEntry/Transaction/VisitorEntry/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GenerateVNo()
        {
            string newV_NO = "00001";
            string vType = "VISI";

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

                    string lastV_NO_Query = "SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 FROM VISITOR WHERE V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";

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

        public IActionResult GetEmpList()
        {
            string query = "SELECT CODE,NAME FROM EMP_MAST WHERE ACTIVE=1 AND NAME<>'' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpPost]
        public async Task<IActionResult> SaveVisitorEntry([FromBody] VisitorWrapper data)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var model = data.Visitor;

            if (model == null)
                return Json(new { success = false, message = "Invalid data" });

            // ================= DOC_ID =================
            model.V_TYPE = "VISI";
            model.DOC_ID = model.V_TYPE + model.V_NO;

            string action = IsDuplicateVisitorEntry(model.DOC_ID) ? "UPDATE" : "INSERT";

            try
            {
                // ================= IMAGE HANDLING =================
                if (data.Image != null && data.Image.IsRemoved == true)
                {
                    // 🔥 USER WANTS TO DELETE IMAGE
                    model.IMG_FILE = null;
                    model.FILE_NAME = "";
                }
                else if (data.Image != null && !string.IsNullOrEmpty(data.Image.Base64Content))
                {
                    // NEW IMAGE
                    string base64 = data.Image.Base64Content;

                    var parts = base64.Split(',');
                    if (parts.Length == 2)
                        base64 = parts[1];

                    model.IMG_FILE = Convert.FromBase64String(base64);
                    model.FILE_NAME = $"{model.V_NO}_{data.Image.FileName}";
                }
                else if (action == "UPDATE")
                {
                    // KEEP OLD IMAGE
                    using (SqlConnection con = _dbConnection.GetErpConnection())
                    {
                        await con.OpenAsync();

                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT IMG_FILE, FILE_NAME FROM VISITOR WHERE DOC_ID = @docId", con))
                        {
                            cmd.Parameters.AddWithValue("@docId", model.DOC_ID);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    model.IMG_FILE = reader["IMG_FILE"] as byte[];
                                    model.FILE_NAME = reader["FILE_NAME"]?.ToString();
                                }
                            }
                        }
                    }
                }
                else
                {
                    model.IMG_FILE = null;
                    model.FILE_NAME = "";
                }

                // ================= SAVE =================
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                        cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? DateTime.Now);
                        cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID);

                        //cmd.Parameters.AddWithValue("@SLIP_NO", model.SLIP_NO ?? "");
                        cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
                        cmd.Parameters.AddWithValue("@ORGANIZATION", model.ORGANIZATION ?? "");
                        cmd.Parameters.AddWithValue("@ADDRESS", model.ADDRESS ?? "");

                        cmd.Parameters.AddWithValue("@MEET_CODE", model.MEET_CODE ?? 0);
                        cmd.Parameters.AddWithValue("@MEET_NAME", model.MEET_NAME ?? "");

                        cmd.Parameters.AddWithValue("@IN_TIME", model.IN_TIME ?? "");
                        cmd.Parameters.AddWithValue("@OUT_DATE", model.OUT_DATE);
                        cmd.Parameters.AddWithValue("@OUT_TIME", model.OUT_TIME ?? "");

                        cmd.Parameters.AddWithValue("@PURPOSE", model.PURPOSE ?? "");
                        cmd.Parameters.AddWithValue("@MOBILE_NO", model.MOBILE_NO ?? "");
                        cmd.Parameters.AddWithValue("@VEHICLE_NO", model.VEHICLE_NO ?? "");
                        cmd.Parameters.AddWithValue("@MATERIAL", model.MATERIAL ?? "");

                        cmd.Parameters.AddWithValue("@CARD_NO", model.CARD_NO ?? "");
                        cmd.Parameters.AddWithValue("@CARD_CODE", model.CARD_CODE ?? 0);

                        cmd.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary, -1)
                            .Value = model.IMG_FILE ?? (object)DBNull.Value;

                        cmd.Parameters.AddWithValue("@FILE_NAME", model.FILE_NAME ?? "");
                        cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? "");

                        // audit
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        await cmd.ExecuteNonQueryAsync();

                        //_logService.InsertLog("VISITOR", "Visitor Entry", "TRANSACTION", action, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);
                    }
                }
                return Json(new
                {
                    success = true,
                    message = action == "INSERT"
                        ? "Visitor Saved Successfully"
                        : "Visitor Updated Successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool IsDuplicateVisitorEntry(string docId)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM VISITOR WHERE DOC_ID = @docId", con))
                {
                    cmd.Parameters.AddWithValue("@docId", docId);

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        [HttpPost]
        public JsonResult DeleteVisitorEntry(string docId)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Visitor  deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVisitorByMobile(string mobileNo)
        {
            if (string.IsNullOrEmpty(mobileNo))
                return Json(new { success = false });

            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "GETBYMOBILE");
                    cmd.Parameters.AddWithValue("@MOBILE_NO", mobileNo);

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return Json(new
                            {
                                success = true,
                                data = new
                                {
                                    name = reader["NAME"]?.ToString(),
                                    address = reader["ADDRESS"]?.ToString(),
                                    organization = reader["ORGANIZATION"]?.ToString(),
                                    purpose = reader["PURPOSE"]?.ToString(),
                                    meet_CODE = reader["MEET_CODE"]?.ToString(),
                                    meet_NAME = reader["MEET_NAME"]?.ToString(),
                                    vehicle_NO = reader["VEHICLE_NO"]?.ToString(),
                                    material = reader["MATERIAL"]?.ToString(),
                                    remarks = reader["REMARKS"]?.ToString()
                                }
                            });
                        }
                    }
                }
            }

            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("VISITOR", vdate, vtype, vno);
            return Ok(result);
        }

        public async Task<IActionResult> PrintSlip(int vNo, string vType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "PRINT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (!reader.Read())
                            return Json(new { success = false, message = "No Data Found" });

                        string companyName = reader["COMPANY_NAME"]?.ToString();
                        string compAddress1 = reader["COMP_ADDRESS1"]?.ToString();
                        string compAddress2 = reader["COMP_ADDRESS2"]?.ToString();

                        string vNoVal = reader["V_NO"]?.ToString();
                        string date = Convert.ToDateTime(reader["V_DATE"]).ToString("dd-MMM-yyyy");
                        string name = reader["NAME"]?.ToString();
                        string org = reader["ORGANIZATION"]?.ToString();
                        string address = reader["ADDRESS"]?.ToString();
                        string mobile = reader["MOBILE_NO"]?.ToString();
                        string vehicle = reader["VEHICLE_NO"]?.ToString();
                        string material = reader["MATERIAL"]?.ToString();
                        string meet = reader["MEET_NAME"]?.ToString();
                        string purpose = reader["PURPOSE"]?.ToString();
                        string remarks = reader["REMARKS"]?.ToString();
                        string card = reader["CARD_NO"]?.ToString();
                        string inTime = reader["IN_TIME"]?.ToString();
                        string outTime = reader["OUT_TIME"]?.ToString();
                        string desg = reader["DESG_NAME"]?.ToString();
                        string imageBase64 = reader["IMG_FILE"] != DBNull.Value
                            ? Convert.ToBase64String((byte[])reader["IMG_FILE"])
                            : "";
                        byte[] compLogoBytes = reader["COMP_LOGO"] != DBNull.Value
                        ? (byte[])reader["COMP_LOGO"]
                        : null;

                        string companyLogoBase64 = compLogoBytes != null
                            ? Convert.ToBase64String(compLogoBytes)
                            : "";
                        string html = $@"
                            <!DOCTYPE html>
                            <html>
                            <head>
                            <title>Visitor Slip</title>

                            <style>
                            @media print {{
                                @page {{
                                    margin: 0;
                                }}

                                body {{
                                    margin: 0;
                                }}

                                /* 🔥 THIS HIDES URL / DATE HEADER FOOTER */
                                @page {{
                                    size: auto;
                                }}
                            }}
                            @@media print {{
                                @@page {{ margin:0; }}
                                body {{ margin:0; }}
                                .print-btn {{ display:none; }}
                            }}

                            body {{
                                font-family: Arial;
                                margin:0;
                            }}

                            .print-container {{
                                width:90%;
                                max-width: 780px; 
                                margin:30px auto;
    
                                border-top:2px solid #000;
                                border-right:2px solid #000;
                                border-bottom:2px solid #000;
                                border-left:2px solid #000;

                                padding:15px;
                                box-sizing: border-box;
                            }}

                            .header {{
                                display:flex;
                                align-items:center;
                                border-bottom:2px solid #000;
                            }}

                            .company-details {{
                                flex:1;
                                text-align:center;
                            }}

                            .company-details h2 {{
                                margin:0;
                                font-size:20px;
                            }}

                            .title-bar {{
                                display:flex;
                                justify-content:space-between;
                                border-bottom:2px solid #000;
                                padding:5px;
                                font-weight:bold;
                            }}

                            .main {{
                                display:flex;
                                border-bottom:2px solid #000;
                            }}

                            .left {{
                                width:70%;
                                padding:5px;
                            }}

                            .right {{
                                width:30%;
                                border-left:2px solid #000;
                                text-align:center;
                            }}

                            .field {{
                                display:flex;
                                margin:4px 0;
                                font-size:14px;
                            }}

                            .label {{
                                width:150px;
                                font-weight:bold;
                            }}

                            .photo-box {{
                                width:120px;
                                height:130px;
                                border:2px solid #000;
                                margin:10px auto;
                            }}

                            .photo-box img {{
                                width:100%;
                                height:100%;
                                object-fit:cover;
                            }}

                            .footer {{
                                display:flex;
                                height:80px;
                            }}

                           .footer div {{flex:1;
                            border-right:2px solid #000;
                            text-align:center;
                            font-weight:bold;

                            display:flex;              
                            flex-direction:column;     /* vertical stack */
                            justify-content:flex-end;  /* neeche align */
                            padding-bottom:8px;        /* border se gap */
                          }}

                            .footer div:last-child {{
                                border-right:none;
                            }}
                            </style>

                            </head>

                            <body onload='window.print(); window.onafterprint=function(){{window.close();}}'>

                            <div class='print-container'>

                                <!-- HEADER -->
                                <div class='header' style='display:flex; align-items:center; border-bottom:2px solid #000;'>

                                    <div style='width:120px; text-align:center;'>
                                        {(string.IsNullOrEmpty(companyLogoBase64)
                                            ? ""
                                            : $"<img src='data:image/png;base64,{companyLogoBase64}' style='height:70px;' />")}
                                    </div>

                                    <div class='company-details'>
                                        <h2>{companyName}</h2>
                                        <div>{compAddress1}</div>
                                        <div>{compAddress2}</div>
                                    </div>

                                </div>

                                <!-- TITLE -->
                                <div class='title-bar'>
                                    <div>Serial No : {vNoVal}</div>
                                    <div>VISITOR SLIP</div>
                                    <div>Date : {date}</div>
                                </div>

                                <!-- BODY -->
                                <div class='main'>

                                    <div class='left'>

                                        <div class='field'><div class='label'>Visitor Name :</div>{name}</div>
                                        <div class='field'><div class='label'>Organisation :</div>{org}</div>
                                        <div class='field'><div class='label'>Address :</div>{address}</div>
                                        <div class='field'><div class='label'>Mobile :</div>{mobile}</div>
                                        <div class='field'><div class='label'>Vehicle :</div>{vehicle}</div>
                                        <div class='field'><div class='label'>Material :</div>{material}</div>
                                        <div class='field'><div class='label'>Meet :</div>{meet}</div>
                                        <div class='field'><div class='label'>Purpose :</div>{purpose}</div>
                                        <div class='field'><div class='label'>Remarks :</div>{remarks}</div>
                                        <div class='field'><div class='label'>Card No :</div>{card}</div>

                                    </div>

                                    <div class='right'>

                                        <div class='photo-box'>
                                            {(string.IsNullOrEmpty(imageBase64)
                                                        ? ""
                                                        : $"<img src='data:image/jpeg;base64,{imageBase64}' />")}
                                        </div>

                                        <div>In Time : {inTime}</div>
                                        <div>Out Time : {outTime}</div>

                                    </div>
                                </div>

                                <!-- FOOTER -->
                                <div class='footer'>
                                    <div>Visitor Sign</div>

                                    <div>
                                        <div margin-top:4px;>{meet}</div>
                                        {(string.IsNullOrEmpty(desg) ? "" :
                                        $"<span style='display:block; font-size:11px;'>({desg})</span>")}
                                    </div>

                                    <div>Security Sign</div>
                                </div>

                            </div>
                            </body>
                            </html>";

                        return Content(html, "text/html");
                    }
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVisitorPrint(int vNo, string vType, string docId)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "PRINT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                                return Json(new { success = false });

                            await reader.ReadAsync();

                            var result = new
                            {
                                vNo = reader["V_NO"]?.ToString(),
                                date = Convert.ToDateTime(reader["V_DATE"]).ToString("dd-MMM-yyyy"),
                                name = reader["NAME"]?.ToString(),
                                org = reader["ORGANIZATION"]?.ToString(),
                                address = reader["ADDRESS"]?.ToString(),
                                mobile = reader["MOBILE_NO"]?.ToString(),
                                vehicle = reader["VEHICLE_NO"]?.ToString(),
                                material = reader["MATERIAL"]?.ToString(),
                                meet = reader["MEET_NAME"]?.ToString(),
                                purpose = reader["PURPOSE"]?.ToString(),
                                remarks = reader["REMARKS"]?.ToString(),
                                card = reader["CARD_NO"]?.ToString(),
                                inTime = reader["IN_TIME"]?.ToString(),
                                outTime = reader["OUT_TIME"]?.ToString(),
                                image = reader["IMG_FILE"] != DBNull.Value
                                    ? Convert.ToBase64String((byte[])reader["IMG_FILE"])
                                    : null,
                                companyName = reader["COMPANY_NAME"]?.ToString(),
                                compAddress1 = reader["COMP_ADDRESS1"]?.ToString(),
                                compAddress2 = reader["COMP_ADDRESS2"]?.ToString(),
                            };

                            return Json(new { success = true, data = result });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
 