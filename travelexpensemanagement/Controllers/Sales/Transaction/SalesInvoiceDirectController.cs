using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.ModuleService;
using travelexpensemanagement.Repositories.Implementations.Sale.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesInvoiceDirectController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public SalesInvoiceDirectController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
        ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, ISalesInVoice salesInVoice)
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
            return View("~/Views/Sales/Transaction/SalesInvoiceDirect/Index.cshtml");
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
                string query = "Select Code,Name from DOCTYPE_MAST where Code='SATD' order by Name";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlFormType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from FORM_MAST where comp_code=" + getdata.PubCompCode + " order by Name";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlMode()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from TRANSPORT_MODE order by Code";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlLoadParty()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select Code,Name from SUBGROUP_MAST where comp_code= {getdata.PubCompCode} and Active=1 order by Name";
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

                using (SqlCommand cmd = new SqlCommand("sp_SalesInvoiceDirect", con))
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
                                Add1 = reader["Add1"],
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
        public JsonResult cmbAddress(int partycode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select ADDRESS_ID , ADD1 From SUBGROUP_ADDRESS where COMP_CODE = {getdata.PubCompCode} and  CODE = {partycode}  ";
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


        public JsonResult cmbPurchaseNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select V_TYPE ,ltrim(rtrim(V_NO))'V_no' from purchase2 where COMP_CODE ={getdata.PubCompCode} and BRANCH_CODE ={getdata.PubBranchCode} and year_code={getdata.PubFYearCode} and V_type='RMTD' group by V_TYPE,v_no order by V_no ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbDocStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }




    }
} 