using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesInvoiceController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly ISalesInVoice _salesInVoice;

        public SalesInvoiceController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
       ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, ISalesInVoice salesInVoice)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;    
            _salesInVoice = salesInVoice;
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

            return View("~/Views/Sales/Transaction/SalesInvoice/Index.cshtml");
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
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('SalesInvoice','SaleChallan') order by Name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlCURRENCY_MAST()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code , SHORTNAME from CURRENCY_MAST where active = 1 ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlLicType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select distinct LC_TYPE as no , LC_TYPE as name  from ADVLIC_MAST where Comp_code={getdata.PubCompCode} Order by LC_TYPE";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }        
        public JsonResult DDlLicNO(String TYPE)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select LC_NO,CONCAT(LC_NO,'  |  ',format(ISSUE_DATE,'dd/MM/yyyy'),'  |  ',format(EXPIRY_DATE,'dd/MM/yyyy'))LC
                from ADVLIC_MAST where Comp_code={getdata.PubCompCode} and LC_TYPE='{TYPE}' Order by ISSUE_DATE";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDLBank(String TYPE)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select code,NAME from BANK_MAST where ACTIVE =1 ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlTransPortMode()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from TRANSPORT_MODE where Name <> ''  order by Code ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }        
        public JsonResult DDlDoNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select V_NO , V_TYPE,Bill_Name,VEHICLE_NO,format(v_date,'dd/MM/yyyy') from DO1 where V_TYPE='DOGT' and isnull(Ref_no,0)=0 order by V_no";
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

                using (SqlCommand cmd = new SqlCommand("sp_SalesInvoice", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@COMP_CODE", SqlDbType.Int) .Value = getdata.PubCompCode;
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
                                ADD3 = reader["ADD3"],
                                CITY_CODE = reader["CITY_CODE"],
                                AGENT_CODE = reader["AGENT_CODE"],
                                agent_name = reader["agent_name"],
                                GSTIN = reader["GSTIN"],
                                Credit_days = reader["Credit_days"],
                                Drbal = reader["Drbal"],
                                crbal = reader["crbal"],
                                PINCODE = reader["PINCODE"],
                                Distance = reader["Distance"],
                                BILL_TYPE = reader["BILL_TYPE"],
                                discper = reader["discper"],
                                Payterm_code = reader["Payterm_code"],
                                State_Code = reader["State_Code"],
                                StateName = reader["StateName"],
                                Country_Code = reader["Country_Code"],
                                CountryName = reader["CountryName"]
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
        public JsonResult DDlPackNo(string v_type , string v_typetext, DateOnly V_DATE , int PARTY_CODE)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                string TableName = "PRODUCTION1";
                string packtyp = "";
                string filtercnd = "";

                if (v_type == "SAGT" || v_type == "SABS")
                {

                    if(v_typetext == "Flakes" || v_typetext == "Chips")
                    {
                        packtyp = "'SFIS','SFEI'";

                        if (getdata.PubCompCode == "1")
                        {
                            TableName = "PROD_SFG1";
                        }
                    }
                    else
                    {
                        packtyp = "'FPIS'";
                    }      

                }
                else if(v_type == "SACH")
                {
                    packtyp = "'FGIS','FGRC'";
                }
               else if(v_type == "SAJI")
                {
                    if (getdata.PubCompCode == "2" || getdata.PubCompCode == "5")
                    {
                        packtyp = "'FPJI','FFIS'";
                        filtercnd = " and MAC_TYPE in ('Packing Slip','Job Issue')";
                    }
                    else
                    {
                        packtyp = "'FFIS'";
                        filtercnd = " and MAC_TYPE='Job Issue'";
                    }
               }

                string query = $@"select top 10  ltrim(rtrim(a.V_NO))'V_no',V_TYPE from {TableName} a  ";








                //string query = $@"select V_TYPE,ltrim(rtrim(a.V_NO))'V_no' from {TableName} a 
                //where a.V_DATE=CONVERT(smalldatetime, {V_DATE}, 103) and a.V_TYPE in ({packtyp}) and a.COMP_CODE= {getdata.PubCompCode} and a.BRANCH_CODE= {getdata.PubBranchCode}
                //and a.YEAR_CODE={getdata.PubFYearCode} and (a.PARTY_CODE={PARTY_CODE} or a.PARTY_CODE=(select main_code from SUBGROUP_MAST where COMP_CODE=a.COMP_CODE and 
                //code=a.PARTY_CODE and ACTIVE=1 group by MAIN_CODE)){filtercnd} ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlSaudaNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@" select ltrim(rtrim(V_NO))'V_no',V_TYPE ,Rate ,PARTY_CODE,isnull(DEFECTIVE_GOODS,0)DEFECTIVE_GOODS,isnull(OFFERNO,'')OFFERNO,b.name payterm from 
                SAUDA a Left join Payterm_mast b on a.PAYTERM_CODE=b.code and a.comp_code=b.comp_code where V_TYPE='SAUD' and FAPROV_STATUS='Approved' and Status=1 and
                a.COMP_CODE= {getdata.PubCompCode} and BRANCH_CODE={getdata.PubBranchCode} ";

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
                                Rate = reader["Rate"],
                                PARTY_CODE = reader["PARTY_CODE"],
                                DEFECTIVE_GOODS = reader["DEFECTIVE_GOODS"],
                                OFFERNO = reader["OFFERNO"],
                                payterm = reader["payterm"]
                        
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
                string query = $@"select ltrim(rtrim(V_NO))'V_no' , V_TYPE  from WB1 where V_TYPE ='KANT' AND COMP_CODE =1 and BRANCH_CODE =1 and YEAR_CODE =9  order by V_NO";
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
                string query = $@" Select b.CODE , ltrim(rtrim(b.name)) 'itemname' from item_mast b where b.ACTIVE=1 and b.comp_code={getdata.PubCompCode } 
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
        [HttpPost]
        public async Task<JsonResult> SavedData([FromBody] SalesInvoiceModel request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, status = "Error", message = "Input model is null" });
            }

            var action = string.Equals(request.Header.action, "INSERT", StringComparison.OrdinalIgnoreCase) ? "INSERT" : "UPDATE";

            var result = await _salesInVoice.SubmitRequest(request.Header, request.Details, action);

            return Json(new { success = result.Status == "Success", status = result.Status, message = result.Message });
        }

        public JsonResult Outallowed(string vType, string vNo)
        {
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    string refNo = $"{vType}{vNo.Trim()}";

                    string dno = "";

                    string query = @" SELECT CONCAT(V_Type, V_No)  FROM DO1 WHERE CONCAT(Ref_Type, Ref_No) = @RefNo  AND Comp_Code = @CompCode
                    AND Branch_Code = @BranchCode AND Year_Code = @YearCode";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@RefNo", refNo);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            dno = result.ToString()?.Trim() ?? "";
                        }
                    }

                    if (string.IsNullOrWhiteSpace(dno) || dno.Length < 13)
                    {
                        return Json(new  { success = false, status = "INVALID_DO", message = "Invalid DO Number." });
                    }

                    string gateCheckQuery = @" SELECT COUNT(1)  FROM Gate1  WHERE CONCAT(Disp_Plan_type, Disp_Plan_No) = @DNo
                        AND Comp_Code = @CompCode  AND Branch_Code = @BranchCode AND Year_Code = @YearCode";

                    using (SqlCommand cmd = new SqlCommand(gateCheckQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DNo", dno);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count == 0)
                        {
                            return Json(new { success = false, status = "INVALID_DO", message = "Invalid DO Number." });
                        }
                    }


                    string activeCheckQuery = @"  SELECT COUNT(1)  FROM Gate1  WHERE CONCAT(Disp_Plan_type, Disp_Plan_No) = @DNo
                        AND INOUT_ACTIVE = 'Yes'  AND Comp_Code = @CompCode  AND Branch_Code = @BranchCode AND Year_Code = @YearCode";

                    using (SqlCommand cmd = new SqlCommand(activeCheckQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DNo", dno);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count == 0)
                        {
                            return Json(new  {  success = false, status = "NOT_ACTIVE",  message = "DO Number not Active." });
                        }
                    }

                    string updateQuery = @" UPDATE Gate1 SET OUT_ALLOWED = 'Yes', OUT_ALLOWEDBY = @UserId
                        WHERE CONCAT(Disp_Plan_type, Disp_Plan_No) = @DNo  AND Comp_Code = @CompCode  AND Branch_Code = @BranchCode  AND Year_Code = @YearCode";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@UserId", getdata.PubUserId);
                        cmd.Parameters.AddWithValue("@DNo", dno);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                        cmd.ExecuteNonQuery();
                    }


                    return Json(new { success = true, status = "SUCCESS",  message = "Out Allowed.", doNo = dno });
                }
            }
            catch (Exception ex)
            {
                return Json(new {  success = false, status = "ERROR",  message = ex.Message  });
            }
        }

        public JsonResult CalRate(int Itemcode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                    string query = $@"select isnull(b.REPORT_TYPE,'')REPORT_TYPE,isnull(a.Sale_Rate,0)Sale_Rate,isnull(a.Taxable_Rate,0)Taxable_Rate,
                    isnull(a.Net_Wt,0)Net_Wt,isnull(a.Packing_Wt,0)Packing_Wt,isnull(a.Packing_nos,0)Packing_nos from Item_mast a 
                    left join ITEM_MGROUP b on a.MGROUP_CODE=b.code and a.comp_code=b.comp_code
                    where a.comp_code={getdata.PubCompCode} and a.code={Itemcode} order by REPORT_TYPE";

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

                                REPORT_TYPE = reader["REPORT_TYPE"],
                                Sale_Rate = reader["Sale_Rate"],
                                Taxable_Rate = reader["Taxable_Rate"],
                                Net_Wt = reader["Net_Wt"],
                                Packing_Wt = reader["Packing_Wt"],
                                Packing_nos = reader["Packing_nos"]
             
                            });
                        }
                    }
                }

                return Json(partyList);
            }
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

        public JsonResult Multipleaddress(int partycode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string sql = @" SELECT COUNT(*)   FROM Subgroup_Address WHERE comp_code = @CompCode   AND Code = @PartyCode";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyCode", partycode);

                    con.Open();

                    int addctr = Convert.ToInt32(cmd.ExecuteScalar());

                    if (addctr > 1)
                    {
                        return Json(new {  success = false,  multipleAddress = true, message = "This party has multiple addresses. For Export, multiple addresses are not allowed in the same Ledger."
                        });
                    }

                    return Json(new { success = true,  multipleAddress = false, message = "" });
                }
            }
        }
        [HttpPost]
        public JsonResult PrintValidation([FromBody] PrintValidationRequest request)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            string message1 = "";
            string message2 = "";
            string message3 = "";
            string message4 = "";
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();


                if(request == null)
                {
                    return Json(new { success = false, message = "Request Showing Null", ReportName = "", godownAdd = "" });
                }

                // GST Tax Validation
                string query = $@" SELECT 1 FROM Sale2 WHERE Tax_Code IN ( SELECT code FROM TAX_MAST WHERE TAX_TYPE = 'GST' AND T_TYPE NOT IN ('Import')
                    ) AND CGST_AMT + SGST_AMT + IGST_AMT = 0 AND V_TYPE = '{request.V_TYPE}'  AND V_NO = {request.V_NO} AND Comp_code = {getdata.PubCompCode}
                    AND Branch_code = {getdata.PubBranchCode} AND Year_Code = {getdata.PubFYearCode}";

                if (IsExist(query))
                {
                    if (getdata.PubUserLevel != "1")
                    {
                        return Json(new { success = false, message = "ERROR! Please check Tax not calculated in Invoice." });
                    }
                }


                if ((request.FrtAmt ?? 0) > 0 || (request.TdsAmt ?? 0) > 0)
                {
                    string ledgerQuery = $@"
                        SELECT 1 FROM Ledger2 WHERE V_type = '{request.V_TYPE}' AND V_NO = {request.V_NO} AND COMP_CODE = {getdata.PubCompCode}
                        AND BRANCH_CODE = {getdata.PubBranchCode} AND Year_Code = {getdata.PubFYearCode}";

                    if (!IsExist(ledgerQuery))
                    {
                        // return Json(new { success = false, message = $"Voucher not posted of VType:{request.V_TYPE} and VNo:{request.V_NO}. Warning! Draft Report will display." });

                        message1 = $"Voucher not posted of VType:{request.V_TYPE} and VNo:{request.V_NO}. Warning! Draft Report will display.";

                    }
                }


                if (request.V_TYPE != "SACH" && request.V_TYPE != "SAJI")
                {
                    string ledgerQuery = $@"  SELECT 1  FROM Ledger2  WHERE V_type = '{request.V_TYPE}'  AND V_NO = {request.V_NO}
                    AND COMP_CODE = {getdata.PubCompCode}  AND BRANCH_CODE = {getdata.PubBranchCode}  AND Year_Code = {getdata.PubFYearCode}";

                    if (!IsExist(ledgerQuery))
                    {
                        //  return Json(new { success = true, warning = true, message = $"Voucher not posted of VType:{request.V_TYPE} and VNo:{request.V_NO}. Warning! Draft Report will display." });


                        message2 = $"Voucher not posted of VType:{request.V_TYPE} and VNo:{request.V_NO}. Warning! Draft Report will display.";


                    }
                }

                // Export Detail Validation
                if (request.V_TYPE == "SAGT" && (request.ExRate ?? 0) > 0)
                {
                    string exportQuery = $@"  SELECT CHA,  FORWARDER,  SHIPLINE,   ETAPOL_DATE,  ETAPOD_DATE,  FRT_ACTUAL  FROM SAUDA_EXPORT  WHERE V_type = 'SAGT'
                    AND V_no = {request.V_NO} AND Comp_code = {getdata.PubCompCode}  AND Branch_code = {getdata.PubBranchCode}";

                    using (SqlCommand cmd = new SqlCommand(exportQuery, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                decimal cha = reader["CHA"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CHA"]);

                                decimal forwarder = reader["FORWARDER"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["FORWARDER"]);

                                decimal shipline = reader["SHIPLINE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SHIPLINE"]);

                                bool etaPolEmpty = reader["ETAPOL_DATE"] == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(reader["ETAPOL_DATE"]));

                                bool etaPodEmpty = reader["ETAPOD_DATE"] == DBNull.Value || string.IsNullOrWhiteSpace(Convert.ToString(reader["ETAPOD_DATE"]));

                                if (cha == 0 && forwarder == 0 && shipline == 0 && etaPolEmpty && etaPodEmpty)
                                {


                                    //return Json(new
                                    //{
                                    //    success = true,
                                    //    warning = true,
                                    //    message = $"Please entry necessary fields in Export Detail " +
                                    //    $"(like Actual Freight, CHA, Forwarder, Shipline, " +
                                    //    $"ETA POL Date, ETA POD Date) of Invoice No:{request.V_NO}"
                                    //});

                                    message3 = $"Please entry necessary fields in Export Detail " +
                                        $"(like Actual Freight, CHA, Forwarder, Shipline, " +
                                        $"ETA POL Date, ETA POD Date) of Invoice No:{request.V_NO}";




                                }
                            }
                            else
                            {

                                //return Json(new
                                //{
                                //    success = true,
                                //    warning = true,
                                //    message = $"Export Detail not feeded " +
                                //    $"(like Actual Freight, CHA, Forwarder, Shipline, " +
                                //    $"ETA POL Date, ETA POD Date) in Invoice No:{request.V_NO}"
                                //});


                                message4 = $"Export Detail not feeded " +
                                    $"(like Actual Freight, CHA, Forwarder, Shipline, " +
                                    $"ETA POL Date, ETA POD Date) in Invoice No:{request.V_NO}";


                            }
                        }
                    }
                }


                string Sql = $@" DELETE FROM tempInvoice WHERE comp_code = {getdata.PubCompCode} AND Branch_code = {getdata.PubBranchCode}";

                using (SqlCommand cmd = new SqlCommand(Sql, con))
                {
                    cmd.ExecuteNonQuery();
                }


                string insertSql = $@"Insert into tempInvoice Select a.comp_code,a.Branch_code,a.V_type,a.v_no,1 ,'Original for Buyer',b.IRN,b.QR_IMAGE 
                    from sale1 a left join QRImage_Path b on a.v_no=b.v_no and a.V_TYPE=b.V_TYPE and a.COMP_CODE=b.COMP_CODE and a.BRANCH_CODE=b.BRANCH_CODE 
                    and a.YEAR_CODE=b.YEAR_CODE where a.comp_code={getdata.PubCompCode} and a.branch_code={getdata.PubBranchCode} and a.v_type='{request.V_TYPE}' and a.v_no= {request.V_NO}
                    union all
                    Select a.comp_code,a.Branch_code,a.V_type,a.v_no,2 ,'Duplicate for Transporter',b.IRN,b.QR_IMAGE from sale1 a
                    left join QRImage_Path b on a.v_no=b.v_no and a.V_TYPE=b.V_TYPE and a.COMP_CODE=b.COMP_CODE and a.BRANCH_CODE=b.BRANCH_CODE and a.YEAR_CODE=b.YEAR_CODE
                    where a.comp_code={getdata.PubCompCode}  and a.branch_code={getdata.PubBranchCode}  and a.v_type='{request.V_TYPE}' and a.v_no={request.V_NO}
                    union all
                    Select a.comp_code,a.Branch_code,a.V_type,a.v_no,3 ,'Triplicate for Office',b.IRN,b.QR_IMAGE from sale1 a 
                    left join QRImage_Path b on a.v_no=b.v_no and a.V_TYPE=b.V_TYPE and a.COMP_CODE=b.COMP_CODE and a.BRANCH_CODE=b.BRANCH_CODE and 
                    a.YEAR_CODE=b.YEAR_CODE where a.comp_code= {getdata.PubCompCode} and a.branch_code={getdata.PubBranchCode}  and a.v_type='{request.V_TYPE}' and a.v_no={request.V_NO} ";

                using (SqlCommand cmd = new SqlCommand(insertSql, con))
                {
                    cmd.ExecuteNonQuery();
                }


                if (request.V_TYPE != "SAJI")
                {
                    if ((request.CGSTAmt ?? 0) + (request.SGSTAmt ?? 0) + (request.IGSTAMT ?? 0) == 0)
                    {

                        int FYearcode = Convert.ToInt32(getdata.PubFYearCode);

                        DateTime pubFYStartDate = Convert.ToDateTime(GetText($@"select START_DATE from dbo.YEAR_MAST where code={FYearcode}"));
                        DateTime pubFYEndDate = Convert.ToDateTime(GetText($@"select END_DATE from dbo.YEAR_MAST where code={FYearcode}"));

                        string query1 = @" SELECT LUT_NO, LUT_VALIDITY FROM LUT_MAST  WHERE Comp_code = @CompCode AND LUT_DATE >= @pubFYStartDate
                                AND LUT_DATE < DATEADD(DAY, 1, @pubFYEndDate)";

                        using (SqlCommand cmd = new SqlCommand(query1, con))
                        {
                            cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = getdata.PubCompCode;
                            cmd.Parameters.Add("@pubFYStartDate", SqlDbType.SmallDateTime).Value = pubFYStartDate;
                            cmd.Parameters.Add("@pubFYEndDate", SqlDbType.SmallDateTime).Value = pubFYEndDate;
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    request.LutNo = reader["LUT_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["LUT_NO"]);

                                    request.LutDate = reader["LUT_VALIDITY"] == DBNull.Value ? null :
                                        DateOnly.FromDateTime(Convert.ToDateTime(reader["LUT_VALIDITY"]));
                                }
                            }
                        }


                        if (request.LutNo != 0 && request.LutDate.HasValue)
                        {
                            string UpdateQuery = $@"Update SALE1 Set LUT_NO= {request.LutNo}, LUT_DATE='{request.LutDate}' 
                                     Where V_type='{request.V_TYPE}' and V_no={request.V_NO} and Comp_code={getdata.PubCompCode} and Branch_code={getdata.PubBranchCode} and Year_Code={getdata.PubFYearCode}";

                            using (SqlCommand cmd = new SqlCommand(UpdateQuery, con))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }



                string truncatequery = "Truncate table tempContainerDetail";
                using (SqlCommand cmd = new SqlCommand(truncatequery, con))
                {
                    cmd.ExecuteNonQuery();
                }


                if (request.exportPrint == true)
                {
                    string cqry = "";
                    string dqry = "";

                    string vewqry = "", tblname1 = "Production1", tblname2 = "Production2";

                    if (request.WithoutBag == 1)
                    {
                        cqry = "sum(b.QTY)";
                        dqry = "b.pack_Qty+b.tare_qty,b.QTY";
                    }
                    else
                    {
                        cqry = "sum(b.GROSS_QTY)";
                        dqry = "b.pack_Qty,b.GROSS_QTY";
                    }

                    if (request.PackType == "SFIS" || request.PackType == "SFEI")
                    {
                        if (getdata.PubCompCode == "1")
                        {
                            tblname1 = "Prod_SFG1";
                            tblname2 = "Prod_SFG2";
                        }

                        vewqry = @$" Insert into tempContainerDetail (COMP_CODE,SI_TYPE,SI_NO,V_TYPE,V_NO,CONTAINER_NO,GROSS_WT,NET_WT,NOS,LINESEAL_NO,CUSTOMSEAL_NO)
                                Select a.COMP_CODE,'{request.V_TYPE}',{request.V_NO},a.V_type,a.V_no,CONTAINER_NO,iif(sum(b.pack_Qty)+sum(b.tare_Qty)>0,sum(b.GROSS_QTY+b.pack_qty),
                                sum(b.GROSS_QTY)),{cqry},count(*),LINESEAL_NO,a.CUSTOMSEAL_NO from  {tblname1}  a left join  {tblname2} b on a.V_TYPE=b.V_TYPE
                                and a.v_no=b.V_no and a.COMP_CODE=b.COMP_CODE 
                                and a.BRANCH_CODE=b.BRANCH_CODE and a.YEAR_CODE=b.YEAR_CODE 
                                where a.V_TYPE='{request.PackType}' and a.V_no in ({request.packNos}) and a.comp_code= {getdata.PubCompCode} and 
                                a.Branch_code= {getdata.PubBranchCode}and a.Year_code={getdata.PubFYearCode} group by a.COMP_CODE,a.V_type,a.V_no,
                                CONTAINER_NO,LINESEAL_NO,a.CUSTOMSEAL_NO order by CONTAINER_NO ";

                        using (SqlCommand cmd = new SqlCommand(vewqry, con))
                        {
                            cmd.ExecuteNonQuery();
                        }


                    }
                    else if (request.PackType == "FPIS")
                    {
                        vewqry = @$"Insert into tempContainerDetail (COMP_CODE,SI_TYPE,SI_NO,V_TYPE,V_NO,CONTAINER_NO,GROSS_WT,NET_WT,NOS,LINESEAL_NO,CUSTOMSEAL_NO)
                                Select a.COMP_CODE,'{request.V_TYPE}',{request.V_NO},a.V_type,a.V_no,CONTAINER_NO,sum(b.GROSS_QTY),sum(b.QTY),count(*),
                                LINESEAL_NO,a.CUSTOMSEAL_NO from {tblname1} a left join  {tblname2}  b on a.V_TYPE=b.V_TYPE and a.v_no=b.V_no and a.COMP_CODE=b.COMP_CODE 
                                and a.BRANCH_CODE=b.BRANCH_CODE and a.YEAR_CODE=b.YEAR_CODE 
                                where a.V_TYPE='FPIS' and a.V_no in ({request.packNos}) and a.comp_code={getdata.PubCompCode} and a.Branch_code={getdata.PubBranchCode} and
                                a.Year_code={getdata.PubFYearCode} group by a.COMP_CODE,a.V_type,a.V_no,CONTAINER_NO,LINESEAL_NO,a.CUSTOMSEAL_NO order by CONTAINER_NO";

                        using (SqlCommand cmd = new SqlCommand(vewqry, con))
                        {
                            cmd.ExecuteNonQuery();
                        }

                    }


                    if (request.ci == true)
                    {
                        if (request.citype == "Bank")
                        {
                            request.ReportName = "INVOICE_EXPORTCOMMBank";
                        }
                        else if (request.citype == "Custom")
                        {
                            request.ReportName = "INVOICE_EXPORTCOMMCustom";
                        }
                        else
                        {
                            request.ReportName = "INVOICE_EXPORTCOMM";
                        }
                    }
                    else if (request.si == true)
                    {
                        request.ReportName = "INVOICE_EXPORTSHIPINST";
                    }
                    else if (request.lc == true)
                    {
                        request.ReportName = "INVOICE_EXPORTLC";
                    }
                    else if (getdata.PubCompCode == "1")
                    {
                        request.ReportName = "INVOICE_EXPORTPPPL";
                    }
                    else if (request.V_TYPE == "SACH" || request.V_TYPE == "SAJI")
                    {
                        if (request.V_TYPE == "SAJI")
                        {
                            request.ReportName = "INVOICE_JobworkSale";
                        }
                        else
                        {
                            request.ReportName = "INVOICE_ChallanSale";
                        }
                    }
                    else
                    {
                        if (getdata.PubCompCode == "2")
                        {
                            request.ReportName = "INVOICE_GST1PLPLQR_DETAIL";
                        }
                        else if (getdata.PubCompCode == "5")
                        {
                            request.ReportName = "INVOICE_GST1SALASARQR_DETAIL";
                        }
                        else if (getdata.PubCompCode == "4")
                        {
                            request.ReportName = "INVOICE_GST1PEPLQR_DETAIL";
                        }
                        else if (getdata.PubCompCode == "7")
                        {
                            if (request.cbDetail == 1)
                            {
                                request.ReportName = "INVOICE_GST1KQRNew";
                            }
                            else
                            {
                                request.ReportName = "INVOICE_GST1KQRNewSumm";
                            }
                        }
                        else if (getdata.PubCompCode == "8")
                        {
                            request.ReportName = "INVOICE_GST1QR_SCTPL";
                        }
                        else
                        {
                            if (request.PackType == "Flakes" && request.cbDetail == 1)
                            {
                                request.ReportName = "INVOICE_GST1QRSumm";
                            }
                            else
                            {
                                request.ReportName = "INVOICE_GST1QR";
                            }
                        }
                    }

                }
                else
                {

                    if(getdata.PubCompCode == "2")
                    {
                        request.ReportName = "INVOICE_GST1PLPLQR_DETAIL";
                    }
                    else if(getdata.PubCompCode == "5")
                    {
                        request.ReportName = "INVOICE_GST1SALASARQR_DETAIL";
                    }
                    else if(getdata.PubCompCode == "4")
                    {
                        request.ReportName = "INVOICE_GST1PEPLQR_DETAIL";
                    }
                    else if(getdata.PubCompCode == "7")
                    {

                        if(request.cbDetail == 1)
                        {
                            request.ReportName = "INVOICE_GST1KQRNew";

                        }
                        else
                        {
                            request.ReportName = "INVOICE_GST1KQRNewSumm";
                        }
                       
                    }
                    else if(getdata.PubCompCode == "8")
                    {
                        request.ReportName = "INVOICE_GST1QR_SCTPL";
                    }
                    else
                    {
                        if(request.V_TYPE == "Flakes" && request.cbDetail == 1)
                        {
                            request.ReportName = "INVOICE_GST1QRSumm";
                        }
                        else
                        {
                            request.ReportName = "INVOICE_GST1QR";
                        }
                    }

                }

                string pino = GetText(@$"Select isnull(pino,'') from SAUDA Where 
                V_type='{request.V_TYPE}' and  V_no= {request.V_NO} and Comp_code= {getdata.PubCompCode} and Branch_code={getdata.PubBranchCode} ");


                if(pino != "")
                {
                    string pidt = GetText(@$"Select format(V_DATE,'dd/MM/yyyy') From SALE1 Where Concat(V_type,V_no)= '{pino}' and Comp_code={getdata.PubCompCode}  and Branch_code={getdata.PubBranchCode}");

                    string query1 = $@"Update SAUDA Set PIDATE='{pidt}' Where   V_type='{request.SaudaType}' and  V_no={request.SaudaNo} and Comp_code= {getdata.PubCompCode} and 
                    Branch_code={getdata.PubBranchCode}";
                    using (SqlCommand cmd = new SqlCommand(query1, con))
                    {
                        cmd.ExecuteNonQuery();
                    }

                }



                request.godownAdd = GetText(@$"Select isnull(COMP_NAME,'')+', '+ isnull(ADDRESS,'')+', '+ isnull(ADDRESS2,'') as adress from GODOWN_MAST where CODE= {request.godownNo} and COMP_CODE={getdata.PubCompCode}");




                return Json(new { success = true, message = "Print validation successful.", ReportName = request.ReportName , godownAdd = request.godownAdd , message1 = message1,message2 = message2 ,
                message3 = message3 , message4 = message4
                
                
                
                });
            }
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
                    public  string? PackType { get; set; }
                    public  string? packNos { get; set; }
                    public  Boolean? exportPrint { get; set; }
                    public  Boolean? ci { get; set; }
                    public  Boolean? si { get; set; }
                    public  Boolean? lc { get; set; }
                    public  int? WithoutBag { get; set; }
                    public  int? cbDetail { get; set; }
                    public string? ReportName { get; set; }
                    public string? citype { get; set; }
                    public string? SaudaType { get; set; }
                    public string? godownAdd { get; set; }
                    public int? SaudaNo { get; set; }                              
                    public string? godownNo{ get; set; }
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

        public bool IsExist(string query)
        {
            try
            {
                using var conn = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand(query, conn);

                conn.Open();

                using var reader = cmd.ExecuteReader();

                return reader.Read();
            }
            catch (Exception ex)
            {

                return false;
            }
        }

        [HttpPost]
        public JsonResult GetPackingSlipPrintValidation([FromBody] PrintValidationRequest request)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            string sql = "";
            string vtyp = "";
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                if (request == null)
                {
                    return Json(new { success = false, message = "Request Showing Null", ReportName = "", godownAdd = "" });
                }

                string truncatequery = "Truncate table tempContainerDetail";
                using (SqlCommand cmd = new SqlCommand(truncatequery, con))
                {
                    cmd.ExecuteNonQuery();
                }


                string cqry = "";
                string dqry = "";
                string tblname1 = "Production1";
                string tblname2 = "Production2";

                if(request.WithoutBag == 1)
                {
                    cqry = "Sum(b.QTY)";
                    dqry = "isnull(b.pack_Qty,0) + isnull(b.Tare_Qty,0),b.Qty";
                }
                else
                {
                    cqry = "Sum(b.Gross_Qty)";
                    dqry = "b.Pack_qty ,b.Gross_Qty";
                }

                if(request.PackType == "SFIS" || request.PackType == "SFEI")
                {
                    if(getdata.PubCompCode == "1")
                    {
                        tblname1 = "Prod_sfg1";
                        tblname2 = "Prod_sfg2";
                    }

                       sql = @$"Insert into tempContainerDetail (COMP_CODE,SI_TYPE,SI_NO,V_TYPE,V_NO,CONTAINER_NO,GROSS_WT,NET_WT,NOS,LINESEAL_NO,CUSTOMSEAL_NO)
                        Select a.COMP_CODE,'{request.V_TYPE}',{request.V_NO},a.V_type,a.V_no,CONTAINER_NO,iif(sum(b.pack_Qty)>0,sum(b.GROSS_QTY+b.pack_qty),
                        sum(b.GROSS_QTY)), {cqry},count(*),LINESEAL_NO,a.CUSTOMSEAL_NO from {tblname1} a left join {tblname2} b on a.V_TYPE=b.V_TYPE and 
                         a.v_no=b.V_no and a.COMP_CODE=b.COMP_CODE and a.BRANCH_CODE=b.BRANCH_CODE and a.YEAR_CODE=b.YEAR_CODE 
                        where a.V_TYPE= '{request.PackType}' and a.V_no in ({request.packNos}) and a.comp_code={getdata.PubCompCode} 
                        and a.Branch_code={getdata.PubBranchCode} and a.Year_code= {getdata.PubFYearCode} 
                        group by a.COMP_CODE,a.V_type,a.V_no,CONTAINER_NO,LINESEAL_NO,a.CUSTOMSEAL_NO order by CONTAINER_NO";


                        using (SqlCommand cmd = new SqlCommand(sql, con))
                        {
                            cmd.ExecuteNonQuery();
                        }
                  
                }
                else if( request.PackType == "FPIS")
                {
                    sql = @$" Insert into tempContainerDetail (COMP_CODE,SI_TYPE,SI_NO,V_TYPE,V_NO,CONTAINER_NO,GROSS_WT,NET_WT,NOS,LINESEAL_NO,CUSTOMSEAL_NO)
                          Select a.COMP_CODE,'{request.V_TYPE}',{request.V_NO},a.V_type,a.V_no,CONTAINER_NO,sum(b.GROSS_QTY),sum(b.QTY),count(*),LINESEAL_NO,
                          a.CUSTOMSEAL_NO from {tblname1} a left join {tblname2} b on a.V_TYPE=b.V_TYPE and a.v_no=b.V_no and a.COMP_CODE=b.COMP_CODE and
                          a.BRANCH_CODE=b.BRANCH_CODE and a.YEAR_CODE=b.YEAR_CODE 
                          where a.V_TYPE= '{request.PackType}' and a.V_no in ('{request.packNos}') and a.comp_code={getdata.PubCompCode} and a.Branch_code={getdata.PubBranchCode} 
                          and a.Year_code={getdata.PubFYearCode} group by a.COMP_CODE,a.V_type,a.V_no,CONTAINER_NO,LINESEAL_NO,a.CUSTOMSEAL_NO order by CONTAINER_NO";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                sql = "Delete from tempExportPackingSlip";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.ExecuteNonQuery();
                }


                if (request.ProdType == "Flakes")
                {
                    vtyp = "SFIS";
                }
                else if(request.ProdType == "PSF" || request.ProdType == "Finish" || request.ProdType == "Fabric" || request.ProdType == "Sacks")
                {
                    vtyp = "FPIS";
                }
                else if (request.ProdType == "Chips")
                {
                    vtyp = "SFEI";
                }

                if(request.rowscount > 0)
                {
                    int dataLength = 0;
                    sql = @$" SELECT COUNT(DISTINCT V_NO) FROM {tblname2} WHERE V_TYPE = '{vtyp}'  AND V_NO IN ({request.packNos})  AND Comp_code = {getdata.PubCompCode} AND Branch_code = {getdata.PubBranchCode}";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                         dataLength = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                        if(dataLength > 0 )
                        {

                            sql = @$" Insert into tempExportPackingSlip(COMP_CODE,BRANCH_CODE,YEAR_CODE,SI_TYPE,SI_NO,SI_DATE,V_TYPE,V_NO,V_DATE,CONTAINER_NO,VEHICLE_NO,
                            LINESEAL_NO,CUSTOMSEAL_NO,ITEM_CODE,ITEM_NAME,GROSS_QTY,TARE_QTY,NET_QTY) Select a.COMP_CODE,a.BRANCH_CODE,a.YEAR_CODE,
                            '{request.V_TYPE}',{request.V_NO},'{request.V_Date}',a.V_TYPE,a.V_NO,a.V_DATE,a.CONTAINER_NO,a.VEHICLE_NO,a.LINESEAL_NO,CUSTOMSEAL_NO,ITEM_CODE,
                            '',iif(b.pack_Qty>0,b.GROSS_QTY+b.pack_qty,b.GROSS_QTY),{dqry} from {tblname1}  a  Left join {tblname2} 
                            b on a.V_TYPE=b.V_TYPE and a.V_NO=b.V_no and a.COMP_CODE=b.COMP_CODE and a.BRANCH_CODE=b.BRANCH_CODE and a.YEAR_CODE=b.YEAR_CODE where a.V_TYPE='{vtyp}'
                            and a.v_no in ( {request.packNos} ) and a.Comp_code={getdata.PubCompCode} and a.Branch_code={getdata.PubBranchCode}";


                            using (SqlCommand cmd = new SqlCommand(sql, con))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            sql = @$"update tempExportPackingSlip set ITEM_NAME=(iif(isnull(b.Print_name,'')<>'',b.Print_name,b.Name)),hsn_Code=b.hsn_Code from Item_mast b Where tempExportPackingSlip.Item_Code=b.code and tempExportPackingSlip.comp_Code=b.comp_Code";
                            using (SqlCommand cmd = new SqlCommand(sql, con))
                            {
                                cmd.ExecuteNonQuery();
                            }

                        }
      
                    sql = "Select top 1* from tempExportPackingSlip";

                    int dataLengthCount = 0;

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        dataLengthCount = Convert.ToInt32(cmd.ExecuteScalar());

                        if(dataLengthCount > 0)
                        {
                            return Json(new { success = false, message = "Packing Slip Not Found"});
                        }
                    }
                }
                return Json(new { success = true, message = "Print validation successful." });
            }
        }



        [HttpGet]
        public JsonResult GetAddressData(int PartyCode, int AddressId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" SELECT  a.ADDRESS_ID, a.Add1, a.Add2, a.Add3, a.GSTIN,  a.City_Code,  d.Code AS CountryCode,
                a.Pincode, s.Code AS SCode FROM Subgroup_Address a
                LEFT JOIN STATE_MAST b   ON a.STATE_CODE = b.Code
                LEFT JOIN CITY_MAST c  ON a.CITY_CODE = c.Code
                LEFT JOIN STATE_MAST s  ON s.Code = c.State_code
                LEFT JOIN Country_MAST d   ON c.Country_CODE = d.Code
                WHERE   a.comp_code = @CompCode  AND a.Code = @PartyCode AND a.Address_Id = @AddressId; ";

                var AddressDataList = new List<object>();

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyCode", PartyCode);
                    cmd.Parameters.AddWithValue("@AddressId", AddressId);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AddressDataList.Add(new
                            {
                                ADDRESS_ID = reader["ADDRESS_ID"],
                                Add1 = reader["Add1"],
                                Add2 = reader["Add2"],
                                Add3 = reader["Add3"],
                                GSTIN = reader["GSTIN"],
                                City_Code = reader["City_Code"],
                                CountryCode = reader["CountryCode"],
                                Pincode = reader["Pincode"],
                                SCode = reader["SCode"]

                            });
                        }
                    }
                }

                return Json(AddressDataList);
            }
        }









    }
} 