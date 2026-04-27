
using AngleSharp.Dom;
using iText.StyledXmlParser.Jsoup.Select;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using System;
using System.Data;
using System.Net.Http;
using System.Text.RegularExpressions;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using static travelexpensemanagement.Models.Sale.Sale_TransportRateQuatation_Model;
namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class TransportRateQuotationController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TransportRateQuotationController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          DropdownService dropdownService, DbHelper dbHelper,
          ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public IActionResult Index()
        {
            TempData["LoginDate"] = _globalVariableService.GetGlobalVariables().PubLoginDate;
            return View("~/Views/Sales/Transaction/TransportRateQuotation/Index.cshtml");
        }
        public JsonResult GetVNo()
        {
            string newV_NO = "00000";
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {

                    con.Open();
                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";
                    string lastV_NO_Query = "select max(V_no) from TRANSPORT_QT1 where V_TYPE=@V_TYPE and COMP_CODE= @CompCode and BRANCH_CODE= @BRANCH_CODE and YEAR_CODE= @YearCode  ";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", "TRQT");
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    object result = lastVnoCmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        int lastV_NO = Convert.ToInt32(result);
                        newV_NO = (lastV_NO + 1).ToString("D5");
                    }
                    else
                    {
                        newV_NO = prefixYR + "00001";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }
        public JsonResult DDlDo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT    V_no, BILL_NAME FROM  DO1 a " +
                               "LEFT JOIN  City_mast b  ON a.SHIP_CITY = b.code " +
                               "WHERE a.COMP_CODE = " + getdata.PubCompCode + " AND a.BRANCH_CODE = " + getdata.PubBranchCode + " AND a.Year_code = " + getdata.PubFYearCode + "; ";

       

                var DDlDo = _dropdownService.GetDropdownList(query);
                return Json(DDlDo);
            }
        }

        public JsonResult DDlBillto()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                //string query = "SELECT    BILL_CODE,   BILL_NAME FROM  DO1 a " +
                //               "LEFT JOIN  City_mast b  ON a.SHIP_CITY = b.code " +
                //               "WHERE a.COMP_CODE = " + getdata.PubCompCode + " AND a.BRANCH_CODE = " + getdata.PubBranchCode + " AND a.Year_code = " + getdata.PubFYearCode + "; ";

                string query = "SELECT    a.CODE, LTRIM(RTRIM(a.NAME)) AS NAME  FROM SUBGROUP_MAST a " +
                               "WHERE a.COMP_CODE = " + getdata.PubCompCode + ";";

                var DDlDo = _dropdownService.GetDropdownList(query);
                return Json(DDlDo);
            }
        }
        public JsonResult DDlCityName()
        {
               using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE ,NAME  from CITY_MAST where ACTIVE =1  ";


                var DDlCityName = _dropdownService.GetDropdownList(query);
                return Json(DDlCityName);
            }


        }
        public JsonResult DDlTransPortName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
              //  string query = "select CODE,NAME from TRANSPORT_MAST where comp_code= "+ getdata.PubCompCode +" and Active=1 order by  NAME asc";
                string query = "select top 1 CODE,NAME from TRANSPORT_MAST  where Active=1 and code = 39 order by  NAME asc";


                var DDlTransPortName = _dropdownService.GetDropdownList(query);
                return Json(DDlTransPortName);
            }


        }
        public JsonResult GetDatabyDo(int Dono, int Vno)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            object result;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                try
                {
                    con.Open();
                    string checkQuery = @" SELECT TOP 1 V_NO   FROM TRANSPORT_QT1   WHERE DO_NO = @Dono AND V_NO <> @Vno AND COMP_CODE = @CompCode";
                    using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Dono", Dono);
                        cmd.Parameters.AddWithValue("@Vno", Vno);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        var existingVno = cmd.ExecuteScalar();
                        if (existingVno != null)
                        {
                            return Json(new { success = false, message = "DO already exists in another Voucher No. " + Vno });
                        }
                    }

                
                    string dataQuery = @"  
                        SELECT a.V_TYPE, a.V_NO, a.BILL_CODE, a.BILL_NAME, a.SHIP_ADD1, a.SHIP_ADD2, a.SHIP_ADD3, b.code, 
                        b.Name AS City, a.FAPROV_STATUS
                        FROM DO1 a 
                        LEFT JOIN City_mast b ON a.SHIP_CITY = b.code
                        WHERE a.V_NO = @Dono AND a.COMP_CODE = @CompCode AND a.BRANCH_CODE = @BranchCode AND a.YEAR_CODE = @YearCode";

                    using (SqlCommand cmd = new SqlCommand(dataQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Dono", Dono);
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result = new
                                {
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    V_NO = reader["V_NO"]?.ToString(),
                                    BILL_CODE = reader["BILL_CODE"]?.ToString(),
                                    BILL_NAME = reader["BILL_NAME"]?.ToString(),
                                    SHIP_ADD1 = reader["SHIP_ADD1"]?.ToString(),
                                    SHIP_ADD2 = reader["SHIP_ADD2"]?.ToString(),
                                    SHIP_ADD3 = reader["SHIP_ADD3"]?.ToString(),
                                    code = reader["code"] != DBNull.Value ? Convert.ToInt32(reader["code"]) : 0
                                };
                            }
                            else
                            {
                                result = new { Message = "No data found." };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result = new { Error = ex.Message };
                }
            }

            // If no errors, return success along with the data
            return Json(new { success = true, data = result, message = "" });
        }

        public JsonResult GetFillData()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var result = new object();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"Select Code, Name from Transport_mast 
                         Where Sale_group='Sale' 
                         and comp_code = @compCode
                         and Active = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    // Add parameterized query to prevent SQL injection
                    cmd.Parameters.AddWithValue("@compCode", getdata.PubCompCode);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                var rows = new List<object>(); // List to store multiple rows

                                while (reader.Read()) // Loop through all rows
                                {
                                    rows.Add(new
                                    {
                                        Name = reader["Name"].ToString(),
                                        Code = reader["Code"] != DBNull.Value ? Convert.ToInt32(reader["Code"]) : 0
                                    });
                                }

                                result = rows; // Return list of rows
                            }
                            else
                            {
                                result = new { Message = "No data found." };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result = new { Error = ex.Message };
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            return Json(result);
        }
        [HttpPost]
        public IActionResult SavedData([FromBody] Sale_TransportRateQuatation_Request request)
        {
            if (request?.Header == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";
            var result = SubmitRequest(request.Header, request.Detail, action);

            return result == "Success"
                ? Json(new { success = true })
                : Json(new { success = false, message = result });
        }
        private string SubmitRequest(sale_TransportRate_Header Header, List<sale_TransportRate_Detail> Detail, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();
                string deletePRequest2Sql = @"
                    DELETE FROM TRANSPORT_QT2 
                    Where V_TYPE=@V_TYPE and V_NO=@V_NO and comp_code=@comp_code and year_code=@year_code and branch_code=@branch_code";

                using (var deletePRequest2Cmd = conn.CreateCommand())
                {
                    deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                    deletePRequest2Cmd.Parameters.AddWithValue("@comp_code", g.PubCompCode);
                    deletePRequest2Cmd.Parameters.AddWithValue("@V_NO", Header.V_NO);
                    deletePRequest2Cmd.Parameters.AddWithValue("@branch_code", g.PubBranchCode);
                    deletePRequest2Cmd.Parameters.AddWithValue("@year_code", g.PubFYearCode);
                    deletePRequest2Cmd.Parameters.AddWithValue("@V_TYPE", "TRQT");
                    deletePRequest2Cmd.ExecuteNonQuery();
                }
                conn.Close();

                conn.Open();

                using (var cmd = new SqlCommand("sp_TransportRateQuatation", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", Header.action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@DOC_ID", ("TRQT") + Header.V_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "TRQT");
                    cmd.Parameters.AddWithValue("@V_NO", Header.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", Header.V_DATE);                       
                    cmd.Parameters.AddWithValue("@DO_TYPE", Header.DO_TYPE);   
                    cmd.Parameters.AddWithValue("@DO_NO", Header.DO_NO);   
                    cmd.Parameters.AddWithValue("@QTY", Header.QTY);   
                    cmd.Parameters.AddWithValue("@BILL_CODE", Header.BILL_CODE);   
                    cmd.Parameters.AddWithValue("@BILL_NAME", Header.BILL_NAME);   
                    cmd.Parameters.AddWithValue("@SHIP_ADD1", Header.SHIP_ADD1);   
                    cmd.Parameters.AddWithValue("@SHIP_ADD2", Header.SHIP_ADD2);   
                    cmd.Parameters.AddWithValue("@SHIP_ADD3", Header.SHIP_ADD3);   
                    cmd.Parameters.AddWithValue("@SHIP_CITY", Header.SHIP_CITY);   
                    cmd.Parameters.AddWithValue("@REMARKS", Header.REMARKS);   
                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", Header.FAPROV_REMARKS);   
                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", Header.FAPROV_STATUS);   
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.ExecuteNonQuery();
                }

                foreach (var detail in Detail)
                {
                    if (string.IsNullOrWhiteSpace(detail.TRANSPORT_NAME))
                        continue;
                    
                    using var cmd3 = new SqlCommand("sp_TransportRateQuatation", conn) { CommandType = CommandType.StoredProcedure };
                    cmd3.Parameters.AddWithValue("@Action", Header.action);
                    cmd3.Parameters.AddWithValue("@SaveAction", "Details");
                    cmd3.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd3.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd3.Parameters.AddWithValue("@DOC_ID", ("TRQT") + Header.V_NO);
                    cmd3.Parameters.AddWithValue("@V_NO", Header.V_NO);
                    cmd3.Parameters.AddWithValue("@V_DATE", Header.V_DATE.ToString("yyyy-MM-dd"));
                    cmd3.Parameters.AddWithValue("@V_TYPE", "TRQT");    
                    cmd3.Parameters.AddWithValue("@TRANSPORT_CODE", detail.TRANSPORT_CODE);    
                    cmd3.Parameters.AddWithValue("@TRANSPORT_NAME", detail.TRANSPORT_NAME);    
                    cmd3.Parameters.AddWithValue("@RATE", detail.RATE);    
                    cmd3.Parameters.AddWithValue("@OUR_RATE", detail.OUR_RATE);    
                    cmd3.Parameters.AddWithValue("@TRUCK_NO", detail.TRUCK_NO);    
                    cmd3.Parameters.AddWithValue("@GRNO", detail.GRNO);    
                    cmd3.Parameters.AddWithValue("@GRDATE", detail.GRDATE.Value.ToString("yyyy-MM-dd"));    
                    cmd3.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd3.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd3.Parameters.AddWithValue("@AED", "A");
                    cmd3.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd3.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd3.ExecuteNonQuery();
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }


        [HttpPost]
        public async Task<JsonResult> SMSSend(string mobileno, string smsMatter, string smstemplate)
        {
            try
            {
               
                if (string.IsNullOrEmpty(mobileno) || !Regex.IsMatch(mobileno, @"^\d{10}$"))
                {
                    return Json(new { success = false, message = "Invalid mobile number. It must be 10 digits." });
                }
                
                string PubWhatsupTokenId = string.Empty;
                string PubWhatsupInstantId = string.Empty;
                               
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string sql = "SELECT WHATSUP_TOKENID, WHATSUP_INSTANTID FROM SYS_SMS";

                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        try
                        {
                            con.Open();
                            using (SqlDataReader reader = await cmd.ExecuteReaderAsync()) 
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        PubWhatsupTokenId = reader["WHATSUP_TOKENID"].ToString();
                                        PubWhatsupInstantId = reader["WHATSUP_INSTANTID"].ToString();
                                    }
                                }
                                else
                                {
                                    return Json(new { success = false, message = "No data found for SMS configuration." });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            return Json(new { success = false, message = "Database error: " + ex.Message });
                        }
                        finally
                        {
                            con.Close();
                        }
                    }
                }
                               
                if (string.IsNullOrEmpty(PubWhatsupTokenId) || string.IsNullOrEmpty(PubWhatsupInstantId))
                {
                    return Json(new { success = false, message = "Invalid SMS configuration (Token or Instance ID missing)." });
                }

               
                string formattedMobileNo = "91" + mobileno.Trim();
                string encodedMessage = Uri.EscapeDataString(smsMatter);
                string encodedMobileNo = Uri.EscapeDataString(formattedMobileNo);

               
                string URL = $"https://ziper.io/api/send.php?number={encodedMobileNo}&type=text&message={encodedMessage}&instance_id={PubWhatsupInstantId}&access_token={PubWhatsupTokenId}";

               
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.PostAsync(URL, null);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();
                        return Json(new { success = true, message = "SMS sent successfully! Response: " + responseText });
                    }
                    else
                    {
                        return Json(new { success = false, message = $"Request failed: {response.StatusCode} - {response.ReasonPhrase}" });
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                return Json(new { success = false, message = "Network error: " + httpEx.Message });
            }
            catch (TimeoutException timeoutEx)
            {
                return Json(new { success = false, message = "Request timeout: " + timeoutEx.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while sending the SMS: " + ex.Message });
            }
        }




    }
}
