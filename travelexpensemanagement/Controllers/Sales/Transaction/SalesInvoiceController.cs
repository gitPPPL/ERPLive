using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
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


    }
}