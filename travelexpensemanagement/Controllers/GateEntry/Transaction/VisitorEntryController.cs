using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Gate_Entry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    [SessionAuthorize]
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
        private readonly IVisitorRepository _visitorRepo;

        public VisitorEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
        ModuleService.ModuleService moduleService , GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, IVisitorRepository visitorRepo)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
            _visitorRepo = visitorRepo;
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
        public IActionResult GetEmpList()
        {
            string query = "SELECT CODE,NAME FROM EMP_MAST WHERE ACTIVE=1 AND NAME<>'' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public JsonResult GenerateVNo()
        {
            try
            {
                string vNo = _visitorRepo.GenerateVNo();
                return Json(new { v_NO = vNo, v_TYPE = "VISI" });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult SaveVisitorEntry([FromBody] VisitorWrapper data)
        {
            if (data?.Visitor == null)
                return Json(new { success = false, message = "Invalid data" });

            var model = data.Visitor;

            model.V_TYPE = "VISI";
            model.DOC_ID = model.V_TYPE + model.V_NO;

            string action = _visitorRepo.IsDuplicate(model.DOC_ID) ? "UPDATE" : "INSERT";

            try
            {
                // ================= IMAGE HANDLING =================

                if (data.Image != null && data.Image.IsRemoved)
                {
                    model.IMG_FILE = null;
                    model.FILE_NAME = "";
                }
                else if (data.Image != null && !string.IsNullOrEmpty(data.Image.Base64Content))
                {
                    var base64 = data.Image.Base64Content.Split(',').Last();
                    model.IMG_FILE = Convert.FromBase64String(base64);
                    model.FILE_NAME = $"{model.V_NO}_{data.Image.FileName}";
                }
                else if (action == "UPDATE")
                {
                    var oldData = _visitorRepo.GetVisitorImage(model.DOC_ID);

                    if (oldData != null)
                    {
                        model.IMG_FILE = oldData.IMG_FILE;
                        model.FILE_NAME = oldData.FILE_NAME;
                    }
                }

                // SAVE VIA REPO
                bool result = _visitorRepo.SaveUpdateVisitor(model, action);

                if (!result)
                    return Json(new { success = false, message = "Save failed" });

                // LOGGING
                _globalValidationdate.LogInsertUpdateDelete(
                    "VISITOR", "VISITOR", "Transaction",
                    model.V_NO.ToString(), model.V_TYPE
                );

                _logService.InsertLog("VISITOR", "Visitor Entry", "TRANSACTION", action, model.V_TYPE, model.V_NO.ToString(), model.V_DATE);

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

        [HttpPost]
        public JsonResult DeleteVisitorEntry(string docId)
        {
            string VType = docId.Substring(0, 4);
            string VNo = docId.Substring(4);

            try
            {
                bool result = _visitorRepo.DeleteVisitor(docId);

                if (result)
                {
                    _logService.InsertLog("VISITOR","Visitor Entry", "TRANSACTION", "DELETE", VType, VNo, null
                );

                    return Json(new { success = true, message = "Visitor deleted successfully." });
                }

                return Json(new { success = false, message = "Delete failed" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetVisitorByMobile(string mobileNo)
        {
            var result = _visitorRepo.GetVisitorByMobile(mobileNo);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("VISITOR", vdate, vtype, vno);
            Console.WriteLine("vdate: " + vdate);
            Console.WriteLine("today: " + DateTime.Today);
            Console.WriteLine("LoginDate: " + global.PubLoginDate.Date);
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

    }
}
 