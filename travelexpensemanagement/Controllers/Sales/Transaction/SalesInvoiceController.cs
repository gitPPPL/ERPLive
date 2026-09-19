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


        public SalesInvoiceController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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


        public JsonResult DDlDoNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select V_TYPE,V_NO,Bill_Name,VEHICLE_NO,format(v_date,'dd/MM/yyyy') from DO1 where V_TYPE='DOGT' and isnull(Ref_no,0)=0 order by V_no";
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

                    string query = $@"select V_TYPE,ltrim(rtrim(a.V_NO))'V_no' from {TableName} a where a.V_DATE=CONVERT(smalldatetime, {V_DATE}, 103) and a.V_TYPE in ({packtyp}) and a.COMP_CODE= {getdata.PubCompCode} and a.BRANCH_CODE= {getdata.PubBranchCode}
                    and a.YEAR_CODE={getdata.PubFYearCode} and (a.PARTY_CODE={PARTY_CODE} or a.PARTY_CODE=(select main_code from SUBGROUP_MAST where COMP_CODE=a.COMP_CODE and 
                    code=a.PARTY_CODE and ACTIVE=1 group by MAIN_CODE)){filtercnd} ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }


        public JsonResult DDlSaudaNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {                    
                string query = $@" select V_TYPE,ltrim(rtrim(V_NO))'V_no' ,Rate 'Rate',PARTY_CODE,isnull(DEFECTIVE_GOODS,0)DEFECTIVE_GOODS,isnull(OFFERNO,'')OFFERNO,b.name payterm from 
                SAUDA a Left join Payterm_mast b on a.PAYTERM_CODE=b.code and a.comp_code=b.comp_code where V_TYPE='SAUD' and FAPROV_STATUS='Approved' and Status=1 and
                a.COMP_CODE= {getdata.PubCompCode} and BRANCH_CODE={getdata.PubBranchCode} ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }




    }
}
