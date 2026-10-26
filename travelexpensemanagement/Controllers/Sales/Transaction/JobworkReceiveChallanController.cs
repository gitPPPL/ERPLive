using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class JobworkReceiveChallanController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly IJobworkReceiveChallan _jobworkReceiveChallan;

        public JobworkReceiveChallanController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
       ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, IJobworkReceiveChallan jobworkReceiveChallan)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _jobworkReceiveChallan = jobworkReceiveChallan;
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

            return View("~/Views/Sales/Transaction/JobworkReceiveChallan/Index.cshtml");
        }

        public JsonResult GetVNo(string Vtype, string Tablename = "SALE1")
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
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('JobworkIssue') and Code<>'SASI' order by Name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult ddlGateNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"Select  V_NO ,V_Type from GATE1 where V_Type='INJB' and comp_code={getdata.PubCompCode} and Branch_Code={getdata.PubBranchCode} and Year_code>=4 order by V_TYpe,V_NO";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbPartyName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                var partyList = new List<object>();

                using (SqlCommand cmd = new SqlCommand("sp_JobworkReceiveChallan", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int).Value = getdata.PubCompCode;
                    cmd.Parameters.Add("@Action", SqlDbType.VarChar, 50).Value = "PartyMast";


                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            partyList.Add(new
                            {
                                CODE = reader["CODE"],
                                P_name = reader["P_name"],
                                ADD1 = reader["ADD1"],
                                ADD2 = reader["ADD2"],
                                ADD3 = reader["ADD2"],
                                CITY_CODE = reader["CITY_CODE"],
                                AGENT_CODE = reader["AGENT_CODE"],
                                agent_name = reader["agent_name"],
                                GSTIN = reader["GSTIN"],                          
                                PINCODE = reader["PINCODE"]
                           
                            });
                        }
                    }
                }

                return Json(partyList);
            }
        }
        public JsonResult cmbCityName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select CODE, NAME from CITY_MAST where ACTIVE = 1  order by NAME ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbSalesThrough()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select code,ltrim(rtrim(name))'Name'from SUBGROUP_MAST where NATURE like 'Broker' and Active=1 and COMP_CODE ={getdata.PubCompCode} ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbFormType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select Code,Name from FORM_MAST where comp_code={getdata.PubCompCode} order by Name";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbTaxType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" select code,ltrim(rtrim(name))'Name',CGST_PER,SGST_PER,IGST_PER,TDS_PER,TCS_PER,OTH_PER from TAX_MAST where ACTIVE=1 ORDER BY NAME ";

                var partyList = new List<object>();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            partyList.Add(new
                            {
                                code = reader["code"],
                                Name = reader["Name"],
                                CGST_PER = reader["CGST_PER"],
                                SGST_PER = reader["SGST_PER"],
                                IGST_PER = reader["IGST_PER"],
                                TDS_PER = reader["TDS_PER"],
                                TCS_PER = reader["TCS_PER"],
                                OTH_PER = reader["OTH_PER"]
                            });
                        }
                    }
                }

                return Json(partyList);
            }
        }
        public JsonResult DDlPackNo(string v_type ,  int PARTY_CODE)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string packtyp = "";
       

                if (v_type == "SAJR" )
                {

                    if (getdata.PubCompCode == "2" || getdata.PubCompCode == "5")
                    {
                        packtyp = "'FPJR'";
                        packtyp = "'FPDR'";
                    }
                    else
                    {
                        packtyp = "'FFRC'";
                    }

                }
                else
                {
                    packtyp = "'FPIS'";
                }





                string query = $@"select top 10  ltrim(rtrim(a.V_NO))'V_no' , V_TYPE from PRODUCTION1 a ";
      
                //string query = $@"
                //select   ltrim(rtrim(a.V_NO))'V_no'  ,   V_TYPE from PRODUCTION1 a where a.V_TYPE ={packtyp} and a.COMP_CODE= {getdata.PubCompCode} and a.BRANCH_CODE={getdata.PubBranchCode}
                //and a.YEAR_CODE={getdata.PubFYearCode} and (a.PARTY_CODE={PARTY_CODE} or a.PARTY_CODE=(select main_code from SUBGROUP_MAST where COMP_CODE=a.COMP_CODE and 
                //code=a.PARTY_CODE and ACTIVE=1 group by MAIN_CODE)) order by a.V_no ";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlSaudaNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@" select ltrim(rtrim(V_NO)) as V_NO , V_TYPE ,Rate ,PARTY_CODE from SAUDA 
                where V_TYPE='SAUD' and FAPROV_STATUS='Approved' and COMP_CODE=1 and BRANCH_CODE=1";

                var SaudaNolist = new List<object>();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SaudaNolist.Add(new
                            {
                                V_NO = reader["V_NO"],
                                V_TYPE = reader["V_TYPE"],
                                Rate = reader["Rate"],
                                PARTY_CODE = reader["PARTY_CODE"]                        

                            });
                        }
                    }
                }

                return Json(SaudaNolist);
            }
        }
        public JsonResult cmbAddress(int partycode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select ADDRESS_ID , ADD1 From SUBGROUP_ADDRESS where COMP_CODE = {getdata.PubCompCode} and CODE = {partycode}  ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlIssueNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@" select ltrim(rtrim(V_NO))'V_no' , V_TYPE ,cast(TOT_NET as float) as TOT_NET ,cast(tot_Nos as int) as tot_Nos from 
                SALE1 where V_type='SAJI' and COMP_CODE ={getdata.PubCompCode} and BRANCH_CODE ={getdata.PubBranchCode} and year_code={getdata.PubFYearCode} ";

                var SaudaNolist = new List<object>();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SaudaNolist.Add(new
                            {
                                V_no = reader["V_no"],
                                V_TYPE = reader["V_TYPE"],
                                TOT_NET = reader["TOT_NET"],
                                tot_Nos = reader["tot_Nos"]

                            });
                        }
                    }
                }

                return Json(SaudaNolist);
            }
        }
        public JsonResult cmbGodown()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select Code,Name from GODOWN_MAST where comp_code= {getdata.PubCompCode} and Active=1 order by SNO";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbProdType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select distinct  SALE_GROUP as no, SALE_GROUP from ITEM_GROUP where comp_code={getdata.PubCompCode}  and  SALE_GROUP <> ''  order by SALE_GROUP";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbWBNO()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select ltrim(rtrim(V_NO))'V_no' , V_TYPE  from WB1 where V_TYPE ='KANT' AND COMP_CODE ={getdata.PubCompCode} and BRANCH_CODE ={getdata.PubBranchCode} and Year_code>=4";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlDocStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlTransPort()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select code,ltrim(rtrim(name))'Name',TDS_PER from TRANSPORT_MAST where Active=1 and comp_code = {getdata.PubCompCode}";

                var SaudaNolist = new List<object>();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SaudaNolist.Add(new
                            {
                                code = reader["code"],
                                Name = reader["Name"],
                                TDS_PER = reader["TDS_PER"]
                            });
                        }
                    }
                }

                return Json(SaudaNolist);
            }
        }
        public JsonResult cmbProductName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@" Select b.CODE , ltrim(rtrim(b.name)) 'itemname' from item_mast b where b.ACTIVE=1 and b.comp_code={getdata.PubCompCode} 
                group by b.name ,b.CODE  order by b.name  ; ";

                var partyList = new List<object>();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            partyList.Add(new
                            {
                                CODE = reader["CODE"],
                                itemname = reader["itemname"]

                            });
                        }
                    }
                }

                return Json(partyList);
            }
        }
        public JsonResult DDlLoadParty()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select Code,Name from SUBGROUP_MAST where comp_code= {getdata.PubCompCode} and active=1 order by Name";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlWBParty()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select Code,Name from SUBGROUP_MAST  where comp_code={getdata.PubCompCode}  order by Name";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbReferenceNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select ltrim(rtrim(x.V_NO))'V_no' , x.V_TYPE  from 
                (Select V_type,ltrim(rtrim(V_NO))V_no from  SALE1
                where V_TYPE ='SAGT' and isnull(Status,0)<>2 AND COMP_CODE ={getdata.PubCompCode} and BRANCH_CODE ={getdata.PubBranchCode} and Year_code>=7 and year_Code<8 
                Union All Select V_type,ltrim(rtrim(V_NO)) from SALE1 where V_TYPE ='SAGT' and 
                isnull(Status,0)<>2 AND COMP_CODE ={getdata.PubCompCode} and BRANCH_CODE ={getdata.PubBranchCode} and Year_code>=7)x Where x.V_no is not null ";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbNatureofJW()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select distinct INSU_DETAIL as no ,INSU_DETAIL from SALE1 where V_type in ('SAJI','SAJR')  and isnull(INSU_DETAIL,'')<>'' and comp_code={getdata.PubCompCode}";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }        
        public JsonResult cmbTransactionThrough()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select code,ltrim(rtrim(name))'Name'from SUBGROUP_MAST where NATURE like 'Broker' and COMP_CODE = {getdata.PubCompCode} and active=1";

                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult CalRate(int Itemcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select HSN_CODE from Item_mast a  where a.comp_code={getdata.PubCompCode} and a.code={Itemcode}";

                var partyList = new List<object>();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            partyList.Add(new
                            {

                                HSN_CODE = reader["HSN_CODE"]                   

                            });
                        }
                    }
                }

                return Json(partyList);
            }
        }


        [HttpPost]
        public async Task<JsonResult> SavedData([FromBody] JobworkReceiveChallanModel request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, status = "Error", message = "Input model is null" });
            }

            var action = string.Equals(request.Header.action, "INSERT", StringComparison.OrdinalIgnoreCase) ? "INSERT" : "UPDATE";

            var result = await _jobworkReceiveChallan.SubmitRequest(request.Header, request.Details, action);

            return Json(new { success = result.Status == "Success", status = result.Status, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("SALE1", vdate, vtype, vno);
            return Ok(result);
        }


        public class PrintValidationRequest
        {
            public string? V_TYPE { get; set; }
            public int? V_NO { get; set; }
            public DateTime? V_Date { get; set; }


            public Decimal? FrtAmt { get; set; }
            public Decimal? TdsAmt { get; set; }
            public Decimal? ExRate { get; set; }
            public Decimal? CGSTAmt { get; set; }
            public Decimal? SGSTAmt { get; set; }
            public Decimal? IGSTAMT { get; set; }
            public int? LutNo { get; set; }
            public DateOnly? LutDate { get; set; }
            public string? PackType { get; set; }
            public string? packNos { get; set; }
            public Boolean? exportPrint { get; set; }
            public Boolean? ci { get; set; }
            public Boolean? si { get; set; }
            public Boolean? lc { get; set; }
            public int? WithoutBag { get; set; }
            public int? cbDetail { get; set; }
            public string? ReportName { get; set; }
            public string? citype { get; set; }
            public string? SaudaType { get; set; }
            public string? godownAdd { get; set; }
            public int? SaudaNo { get; set; }
            public string? godownNo { get; set; }
            public string? ProdType { get; set; }
            public int? godownType { get; set; }
            public int? rowscount { get; set; }


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
        public JsonResult PrintEsingValidation([FromBody] PrintValidationRequest request)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @$"select 1 from Ledger2 where V_type='{request.V_TYPE}' and V_NO={request.V_NO} and COMP_CODE={getdata.PubCompCode} and BRANCH_CODE={getdata.PubBranchCode} and Year_Code={getdata.PubFYearCode}";

                string Data = GetText(query);

                if(Data != "")
                {
                    string Message = @$"Voucher not posted of VType:{request.V_TYPE}{request.V_NO} and VNo:{request.V_NO}Warning! Report not display";
                    return Json(new { success = false, message = Message });
                }
                return Json(new { success = true, message = "Print validation successful." });

            }
        }



        [HttpPost]
        public async Task<IActionResult> SendMail(int PartyCode, int vno, string v_type, IFormFile file)
        {
            try
            {
                var globalVaraible = _globalVariableService.GetGlobalVariables();

                if (file == null)
                    return Json(new { success = false, message = "Report file missing" });

                using var ms = new MemoryStream();
                file.CopyTo(ms);

                byte[] pdfBytes = ms.ToArray();

                //string Mail = GetText("Select EMAIL from SUBGROUP_MAST WHERE CODE= " + PartyCode +
                //                      " AND COMP_CODE= " + globalVaraible.PubCompCode);

                string Mail = "sg256001@gmail.com";

                if (Mail == "")
                {
                    return Json(new { success = false, message = "Email address is blank for the selected party." });
                }

                string compname = GetText("Select NAME from COMP_MAST WHERE CODE= " + globalVaraible.PubCompCode);

                string mailBody = "Please find attached invoice(s) for product dispatches.<BR><BR><BR>Regards,<BR>" + globalVaraible.CompanyName + "<BR>";
             

                return await _globalValidationdate.GlobalSendMail(v_type, vno, Mail, mailBody, file, "sale1",  "");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}
