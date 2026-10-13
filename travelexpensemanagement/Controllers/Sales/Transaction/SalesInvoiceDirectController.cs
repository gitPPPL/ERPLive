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
using static travelexpensemanagement.Controllers.Sales.Transaction.SalesInvoiceController;

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
        private readonly ISalesInvoiceDirect _salesInvoiceDirect;
        public SalesInvoiceDirectController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
        ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, ISalesInvoiceDirect salesInvoiceDirect)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;       
            _salesInvoiceDirect = salesInvoiceDirect;
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
        public JsonResult DDlProdType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select Distinct SALE_GROUP as code  , SALE_GROUP as name from ITEM_GROUP where comp_code={getdata.PubCompCode} and SALE_GROUP='Other' order by SALE_GROUP";
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
                string query = $@"select ltrim(rtrim(V_NO))'V_no', V_TYPE  from purchase2 where COMP_CODE ={getdata.PubCompCode} and BRANCH_CODE ={getdata.PubBranchCode} and year_code={getdata.PubFYearCode} and V_type='RMTD' group by V_TYPE,v_no order by V_no ";
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
        public JsonResult cmbPaymentTerm()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select CODE,NAME from PAYTERM_MAST where Comp_code={getdata.PubCompCode}  Order by name";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
            }
        }
        public JsonResult cmbProductName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"Select ltrim(rtrim(a.name)) as itemname ,a.CODE from item_mast a  
                left join Item_Mgroup b on a.Mgroup_code=b.code and a.comp_code=b.comp_code where a.ACTIVE=1 and a.comp_code={getdata.PubCompCode}";

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
        public JsonResult cmbTransportName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = $@"select code,ltrim(rtrim(name))'Name',TDS_PER from TRANSPORT_MAST where Active=1 and comp_code={getdata.PubCompCode}";
                var data = _dropdownService.GetDropdownList(query);
                return Json(data);
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

        [HttpPost]
        public async Task<JsonResult> SavedData([FromBody] SalesInvoiceDirectModel request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, status = "Error", message = "Input model is null" });
            }

            var action = string.Equals(request.Header.action, "INSERT", StringComparison.OrdinalIgnoreCase) ? "INSERT" : "UPDATE";

            var result = await _salesInvoiceDirect.SubmitRequest(request.Header, request.Details, action);

            return Json(new { success = result.Status == "Success", status = result.Status, message = result.Message });
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

        public JsonResult GETDatabyDoNO(int DoNo, string DOType)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_SalesInvoiceDirect", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "DONO");
                    cmd.Parameters.AddWithValue("@V_TYPE", DOType);
                    cmd.Parameters.AddWithValue("@V_NO", DoNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var partyData = new
                            {
                                V_TYPE = reader["V_TYPE"],
                                V_NO = reader["V_NO"],
                                V_DATE = reader["V_DATE"],
                                DOC_ID = reader["DOC_ID"],
                                BILL_CODE = reader["BILL_CODE"],
                                BILL_NAME = reader["BILL_NAME"],
                                BILL_ADD1 = reader["BILL_ADD1"],
                                BILL_ADD2 = reader["BILL_ADD2"],
                                BILL_ADD3 = reader["BILL_ADD3"],
                                BILL_CITY = reader["BILL_CITY"],
                                BILL_GST = reader["BILL_GST"],
                                BILL_PINCODE = reader["BILL_PINCODE"],
                                BILL_CITY_NAME = reader["BILL CITY"],
                                SHIP_CODE = reader["SHIP_CODE"],
                                SHIP_NAME = reader["SHIP_NAME"],
                                SHIP_ADD1 = reader["SHIP_ADD1"],
                                SHIP_ADD2 = reader["SHIP_ADD2"],
                                SHIP_ADD3 = reader["SHIP_ADD3"],
                                SHIP_CITY = reader["SHIP_CITY"],
                                SHIP_GST = reader["SHIP_GST"],
                                SHIP_PINCODE = reader["SHIP_PINCODE"],
                                SHIP_CITY_NAME = reader["SHIP CITY"],
                                TAX_CODE = reader["TAX_CODE"],
                                TAX_NAME = reader["TAX_NAME"],
                                ITEM_TYPE = reader["ITEM_TYPE"],
                                AMOUNT = reader["AMOUNT"],
                                PACK_AMT = reader["PACK_AMT"],
                                TCS_PER = reader["TCS_PER"],
                                TCS_AMT = reader["TCS_AMT"],
                                NAMOUNT = reader["NAMOUNT"],
                                CGST_AMT = reader["CGST_AMT"],
                                SGST_AMT = reader["SGST_AMT"],
                                IGST_AMT = reader["IGST_AMT"],
                                CESS_PER = reader["CESS_PER"],
                                CESS_AMT = reader["CESS_AMT"],
                                TOT_NOS = reader["TOT_NOS"],
                                VEHICLE_NO = reader["VEHICLE_NO"],
                                REMARKS = reader["REMARKS"],
                                DISC_AMT = reader["DISC_AMT"],
                                CDISC_AMT = reader["CDISC_AMT"],
                                FORM_CODE = reader["form_Code"],
                                SAUDA_TYPE = reader["sauda_type"],
                                SAUDA_NO = reader["sauda_no"],
                                Supply_Type = reader["Supply_Type"],
                                TRAN_TYPE = reader["TRAN_TYPE"]
                            };

                            return Json(partyData);
                        }
                    }
                }
            }

            return Json(null);
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
                        return Json(new { success = false, status = "INVALID_DO", message = "Invalid DO Number." });
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
                            return Json(new { success = false, status = "NOT_ACTIVE", message = "DO Number not Active." });
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


                    return Json(new { success = true, status = "SUCCESS", message = "Out Allowed.", doNo = dno });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, status = "ERROR", message = ex.Message });
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

                string mailBody = "Please find attached invoice(s) for product dispatches.<br><br><br>";
                mailBody += "<br><br>Regards,<br>"
                 + compname + "<br>"
                 + globalVaraible.Address1 + "<br>"
                 + globalVaraible.Address2;

                return await _globalValidationdate.GlobalSendMail(v_type, vno, Mail, mailBody, file, "sale1", "");
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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
        public JsonResult PrintValidation([FromBody] PrintValidationRequest request)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                int exit = Convert.ToInt32(GetText(@$"
                SELECT COUNT(*)
                FROM Ledger2
                WHERE V_type = '{request.V_TYPE}'
                AND V_NO = {request.V_NO}
                AND COMP_CODE = {getdata.PubCompCode}
                AND BRANCH_CODE = {getdata.PubBranchCode}
                AND Year_Code = {getdata.PubFYearCode}
                ") is string result && int.TryParse(result, out int count) ? count : 0);

                if ((request.FrtAmt > 0 || request.TdsAmt > 0) && exit == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Voucher not posted of VType: {request.V_TYPE} and VNo: {request.V_NO}, Warning! Report not display"
                    });
                }

                string result1 = GetText(@$"
                SELECT COUNT(*)
                FROM Ledger2
                WHERE V_type = '{request.V_TYPE}'
                AND V_NO = {request.V_NO}
                AND COMP_CODE = {getdata.PubCompCode}
                AND BRANCH_CODE = {getdata.PubBranchCode}
                AND Year_Code = {getdata.PubFYearCode}");

                int ledgerCount = 0;
                int.TryParse(result1, out ledgerCount);

                if (request.V_TYPE != "SACH"
                && request.V_TYPE != "SAJI"
                && ledgerCount == 0)
                {
                return Json(new { success = false, message = $"Voucher not posted of VType: {request.V_TYPE} and VNo: {request.V_NO}. Warning! Report not display" });
                }

                string truncatequery = @$"Delete from tempInvoice where comp_code={getdata.PubCompCode} and Branch_code={getdata.PubBranchCode}";
                using (SqlCommand cmd = new SqlCommand(truncatequery, con))
                {
                    cmd.ExecuteNonQuery();
                }
              
                    string sql = @$"
                    INSERT INTO tempInvoice  SELECT   a.COMP_CODE, a.BRANCH_CODE, a.V_TYPE, a.V_NO, 1, 'Original for Buyer', b.IRN,
                    b.QR_IMAGE  FROM SALE1 a
                    LEFT JOIN QRImage_Path b ON a.V_NO = b.V_NO AND a.V_TYPE = b.V_TYPE AND a.COMP_CODE = b.COMP_CODE AND a.BRANCH_CODE = b.BRANCH_CODE  AND a.YEAR_CODE = b.YEAR_CODE
                    WHERE a.COMP_CODE = {getdata.PubCompCode}  AND a.BRANCH_CODE = {getdata.PubBranchCode}  AND a.V_TYPE = '{request.V_TYPE}' AND a.V_NO = {request.V_NO}

                    UNION ALL

                    SELECT   a.COMP_CODE,  a.BRANCH_CODE, a.V_TYPE, a.V_NO, 2, 'Duplicate for Transporter',  b.IRN,  b.QR_IMAGE
                    FROM SALE1 a  LEFT JOIN QRImage_Path b ON a.V_NO = b.V_NO AND a.V_TYPE = b.V_TYPE AND a.COMP_CODE = b.COMP_CODE
                    AND a.BRANCH_CODE = b.BRANCH_CODE AND a.YEAR_CODE = b.YEAR_CODE WHERE a.COMP_CODE = {getdata.PubCompCode}
                    AND a.BRANCH_CODE = {getdata.PubBranchCode} AND a.V_TYPE = '{request.V_TYPE}'  AND a.V_NO = {request.V_NO}

                    UNION ALL

                    SELECT  a.COMP_CODE, a.BRANCH_CODE,  a.V_TYPE,  a.V_NO,  3, 'Triplicate for Office', b.IRN, b.QR_IMAGE
                    FROM SALE1 a LEFT JOIN QRImage_Path b  ON a.V_NO = b.V_NO  AND a.V_TYPE = b.V_TYPE AND a.COMP_CODE = b.COMP_CODE
                    AND a.BRANCH_CODE = b.BRANCH_CODE  AND a.YEAR_CODE = b.YEAR_CODE  WHERE a.COMP_CODE = {getdata.PubCompCode}
                    AND a.BRANCH_CODE = {getdata.PubBranchCode} AND a.V_TYPE = '{request.V_TYPE}' AND a.V_NO = {request.V_NO}";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.ExecuteNonQuery();
                    }

                if (request.SaudaType != null && request.SaudaNo != null)
                {
                    sql = @$"Select isnull(pino,'') as pino from SAUDA Where V_type='{request.SaudaType}' and 
                    V_no={request.SaudaNo} and Comp_code={getdata.PubCompCode} and Branch_code={getdata.PubBranchCode}";
                    string pidt = GetText(sql);

                    sql = @$"Update SAUDA Set PIDATE='{pidt}' Where V_type='{request.SaudaType}' and 
                    V_no={request.SaudaNo} and Comp_code={getdata.PubCompCode}  and Branch_code={getdata.PubBranchCode}";
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }       

                return Json(new { success = true, message = "Print validation successful." });
            }
        }
        public class PrintValidationRequest
        {
            public string? V_TYPE { get; set; }
            public int? V_NO { get; set; }
            public string? SaudaType { get; set; }
            public int? SaudaNo { get; set; }
            public DateTime? V_Date { get; set; }
            public Decimal? FrtAmt { get; set; }
            public Decimal? TdsAmt { get; set; }   
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

    }
} 