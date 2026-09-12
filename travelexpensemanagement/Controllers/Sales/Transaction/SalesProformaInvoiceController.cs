using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sale.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    [SessionAuthorize]


    public class SalesProformaInvoiceController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly ISalesProformaInvoice _salesProformaInvoiceRepository;



        public SalesProformaInvoiceController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
          ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate ,ISalesProformaInvoice salesProformaInvoice )
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _salesProformaInvoiceRepository = salesProformaInvoice;
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
            return View("~/Views/Sales/Transaction/SalesProformaInvoice/Index.cshtml");
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
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('ProformaInvoice','SalesQuotation') order by Name ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult DDlCURRENCY_MAST()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE, SHORTNAME from CURRENCY_MAST where ACTIVE=1";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbPaymentTerm()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select CODE, NAME  from PAYTERM_MAST where comp_code={getdata.PubCompCode} ORDER BY NAME";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbSoldBy()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select code,name from SALESEXECUTIVE_MAST where COMP_CODE ={getdata.PubCompCode} and active=1 order by name";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbPartyName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                    SELECT  a.CODE ,LTRIM(RTRIM(a.NAME)) AS p_name,  a.ADD1 ,  a.ADD2 ,  a.ADD3 ,
                    a.CITY_CODE ,   c.NAME AS C_name,  a.AGENT_CODE ,  d.NAME AS agent_name,
                    a.GSTIN,  e.CODE AS CountryCode,  e.NAME AS CountryN,  a.Pincode, s.Code AS SCode,  s.Name AS StateName
                    FROM SUBGROUP_MAST a
                    LEFT JOIN CITY_MAST c  ON c.CODE = a.CITY_CODE
                    LEFT JOIN STATE_MAST s  ON s.CODE = c.STATE_CODE
                    LEFT JOIN Country_MAST e ON e.CODE = c.Country_CODE
                    LEFT JOIN SUBGROUP_MAST d  ON d.CODE = a.AGENT_CODE
                    AND d.NATURE = 'Broker' AND d.COMP_CODE = a.COMP_CODE  WHERE a.COMP_CODE = @CompCode  order by a.NAME  ";

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
                                Code = reader["CODE"],
                                p_name = reader["p_name"],
                                Add1 = reader["ADD1"],
                                Add2 = reader["ADD2"],
                                Add3 = reader["ADD3"],
                                C_code = reader["CITY_CODE"],
                                C_name = reader["C_name"],
                                agent_code = reader["AGENT_CODE"],
                                agent_name = reader["agent_name"],
                                GSTIN = reader["GSTIN"],
                                CountryCode = reader["CountryCode"],
                                CountryN = reader["CountryN"],
                                Pincode = reader["Pincode"],
                                SCode = reader["SCode"],
                                StateName = reader["StateName"]
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
                string query = $@"select CODE, NAME from CITY_MAST where ACTIVE = 1 ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbCOUNTRY_MAST()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select CODE, NAME from COUNTRY_MAST where ACTIVE = 1 ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbAddress()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select code , ADD1 From SUBGROUP_ADDRESS where COMP_CODE = {getdata.PubCompCode} ";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbSaleTh()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select code,ltrim(rtrim(name))'Name' from SUBGROUP_MAST where NATURE like 'Broker' and COMP_CODE ={getdata.PubCompCode} and active=1 ";
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
        public JsonResult cmbProductName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" SELECT  a.CODE, LTRIM(RTRIM(a.SHORTNAME)) AS Shortname,  b.mgroup_type,  a.Packing_Wt,  a.HSN_CODE
                        FROM ITEM_MAST a
                        LEFT JOIN ITEM_MGROUP b  ON b.CODE = a.MGROUP_CODE  AND b.COMP_CODE = a.COMP_CODE
                        WHERE a.Active = 1 AND a.comp_code = @CompCode; ";

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
                                Shortname = reader["Shortname"],
                                mgroup_type = reader["mgroup_type"],
                                Packing_Wt = reader["Packing_Wt"],
                                HSN_CODE = reader["HSN_CODE"]
                   
                            });
                        }
                    }
                }

                return Json(partyList);
            }
        }        
        public JsonResult cmbTransport()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select code,ltrim(rtrim(name))'Name',TDS_PER from TRANSPORT_MAST where comp_code={getdata.PubCompCode} ORDER BY name";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }

        [HttpPost]
        public async Task<JsonResult> SavedData([FromBody] SalesProformaInvoice_Model request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, status = "Error", message = "Input model is null" });
            }

            var action = string.Equals(request.Header.action, "INSERT", StringComparison.OrdinalIgnoreCase) ? "INSERT" : "UPDATE";

            var result = await _salesProformaInvoiceRepository.SubmitRequest(request.Header, request.Details, action);

            return Json(new { success = result.Status == "Success", status = result.Status, message = result.Message });
        }

    }
}
