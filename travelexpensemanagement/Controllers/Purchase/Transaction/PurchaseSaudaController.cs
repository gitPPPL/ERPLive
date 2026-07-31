
using DocumentFormat.OpenXml.Presentation;
using iTextSharp.text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using StackExchange.Redis;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using UglyToad.PdfPig.Content;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseSaudaController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;

        public PurchaseSaudaController(DataBaseConnection dbConnection, GlobalValidationdate globalValidationdate, GlobalVariableService globalVariableService,
            DropdownService dropdownService, DbHelper dbHelper,
            ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
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
            return View("~/Views/Purchase/Transaction/PurchaseSauda/Index.cshtml");
        }

        public JsonResult GetVNo(String v_type = "PAUD" , string TableName = "SAUDA")
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

                    string lastV_NO_Query = "SELECT MAX(V_NO) FROM "+  TableName +" WHERE COMP_CODE = @CompCode AND YEAR_CODE = @YearCode  and BRANCH_CODE = @BRANCH_CODE and V_TYPE = @v_type ";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    lastVnoCmd.Parameters.AddWithValue("@TableName", TableName);
                    lastVnoCmd.Parameters.AddWithValue("@v_type", v_type);


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
        public JsonResult DDLPartyMast()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select a.Code ,a.NAME from SUBGROUP_MAST a where a.Active=1 and a.NATURE='Supplier' and " +
                    "a.comp_code="+ getdata.PubCompCode + " order by a.name asc ";

                var PartyList = _dropdownService.GetDropdownList(query);

                return Json(PartyList);
            }

        }
        public JsonResult DDLstatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE ";

                var DDLstatus = _dropdownService.GetDropdownList(query);

                return Json(DDLstatus);
            }

        }
        public JsonResult DDLCityMast()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE , NAME from CITY_MAST  where active = 1 ";
                var DDLCityMast = _dropdownService.GetDropdownList(query);
                return Json(DDLCityMast);
            }
        }
        public JsonResult DDLShipFrom()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT  a.Code, a.NAME FROM  SUBGROUP_MAST a WHERE  a.Active = 1 AND a.NATURE = 'Supplier' " +
                    "AND a.comp_code = "+   getdata.PubCompCode + " ORDER BY  a.name asc; ";

                var ShipFromList = _dropdownService.GetDropdownList(query);

                return Json(ShipFromList);
            }

        }
        public JsonResult DDLPurchaseThrough()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select Code , NAME from SALESEXECUTIVE_MAST  Where COMP_CODE = "+ getdata.PubCompCode + " and code<>0 and NAME <> ''  order by Name asc ";

                var PurchaseThroughList = _dropdownService.GetDropdownList(query);

                return Json(PurchaseThroughList);
            }

        }
        public JsonResult DDLItemMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT a.CODE, a.NAME FROM ITEM_MAST a " +
                               "LEFT JOIN ITEM_MGROUP b ON b.CODE = a.MGROUP_CODE AND b.COMP_CODE = a.COMP_CODE " +
                               "WHERE  b.MGROUP_TYPE IN ('RAW')  " +
                               "AND a.Active = 1 AND a.comp_code = "+ getdata.PubCompCode + " ORDER BY a.name asc;";

                var ItemMasterList = _dropdownService.GetDropdownList(query);

                return Json(ItemMasterList);
            }
        }
        public JsonResult DDLTaxRate()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = " Select Code,Name from TAX_MAST where TAX_TYPE='GST' Order by CODE asc   ";

                var TaxRateList = _dropdownService.GetDropdownList(query);

                return Json(TaxRateList);
            }
        }
        public JsonResult DDLPaymentTerm()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE, NAME  from PAYTERM_MAST where comp_code=" + getdata.PubCompCode + " ORDER BY NAME asc";

                var PaymenttermList = _dropdownService.GetDropdownList(query);

                return Json(PaymenttermList);
            }
        }
        public JsonResult DDLBrokerName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from SUBGROUP_MAST where comp_code= "+ getdata.PubCompCode +" and" +
                    " Active=1 and nature in ('Customer','Supplier','Broker') Order by Name asc";

                var BrokerNameList = _dropdownService.GetDropdownList(query);

                return Json(BrokerNameList);
            }
        }
        public JsonResult DDLDispatchForm()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from COUNTRY_MAST where Active=1  Order by Name asc";

                var DispatchFormList = _dropdownService.GetDropdownList(query);

                return Json(DispatchFormList);
            }
        }
        public JsonResult DDLDispatchItemMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT  a.CODE, LTRIM(RTRIM(a.NAME)) AS Shortname, b.mgroup_type FROM " +
                    " ITEM_MAST a LEFT JOIN  ITEM_MGROUP b  ON b.CODE = a.MGROUP_CODE  AND b.COMP_CODE = a.COMP_CODE WHERE " +
                    " a.Active = 1  AND a.comp_code = " + getdata.PubCompCode + " group by a.NAME ,a.code,b.mgroup_type order by a.NAME asc ";
                 
                var ItemList = _dropdownService.GetDropdownList(query);

                return Json(ItemList);
            }
        }

        public JsonResult GetDataByPartyCode(int PartyId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // -------------------- 1st QUERY (Supplier Master) --------------------
                string query1 = @" SELECT TOP 1 a.Code, a.NAME, a.ADD1, a.ADD2, a.ADD3, a.CITY_CODE,
                    b.NAME AS CityName, a.MOBILE,  c.Name AS Country, a.Pincode, a.gstin AS GST
                    FROM SUBGROUP_MAST a
                    LEFT JOIN CITY_MAST b ON a.CITY_CODE = b.CODE
                    LEFT JOIN COUNTRY_MAST c ON b.COUNTRY_CODE = c.CODE
                    WHERE a.Active = 1 
                    AND a.NATURE = 'Supplier'
                    AND a.comp_code = @CompCode
                    AND a.CODE = @PartyId;
                    ";

                object supplier = null;

                using (SqlCommand cmd = new SqlCommand(query1, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", PartyId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            supplier = new
                            {
                                Code = reader["Code"].ToString(),
                                Name = reader["NAME"].ToString(),
                                Address1 = reader["ADD1"].ToString(),
                                Address2 = reader["ADD2"].ToString(),
                                Address3 = reader["ADD3"].ToString(),
                                CityCode = reader["CITY_CODE"].ToString(),
                                CityName = reader["CityName"].ToString(),
                                Mobile = reader["MOBILE"].ToString(),
                                Country = reader["Country"].ToString(),
                                Pincode = reader["Pincode"].ToString(),
                                GST = reader["GST"].ToString()
                            };
                        }
                    }
                }

                // -------------------- 2nd QUERY (SAUDA Latest) --------------------
                string query2 = @" SELECT TOP 1 COALESCE(A.FRT_TERM, '') AS FRT_TERM, COALESCE(A.DEL_TERM, '') AS DEL_TERM,
                    B.NAME AS ITEM_NAME,  A.ITEM_CODE, A.ITEM_TYPE FROM SAUDA A  LEFT JOIN ITEM_MAST B ON A.ITEM_CODE = B.CODE
                    AND A.COMP_CODE = B.COMP_CODE
                    WHERE A.PARTY_CODE = @PartyId ORDER BY A.V_DATE DESC; ";

                object sauda = null;

                using (SqlCommand cmd = new SqlCommand(query2, con))
                {
                    cmd.Parameters.AddWithValue("@PartyId", PartyId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            sauda = new
                            {
                                FrtTerm = reader["FRT_TERM"].ToString(),
                                DelTerm = reader["DEL_TERM"].ToString(),
                                ItemName = reader["ITEM_NAME"].ToString(),
                                ItemCode = reader["ITEM_CODE"].ToString(),
                                ItemType = reader["ITEM_TYPE"].ToString()
                            };
                        }
                    }
                }


                string partermcode = GetText("select isnull(payterm_code,0) as  payterm_code from subgroup_mast where code=" + PartyId + " and comp_code= " + getdata.PubCompCode + "");


                // -------------------- FINAL RESPONSE --------------------
                return Json(new { Supplier = supplier, Sauda = sauda , partermcode = partermcode });
            }
        }

        public IActionResult SaveDispatchDetails([FromBody] PurchaseSauda_model model)
        {
            if (model == null || model.DispatchDelivery == null || model.DispatchDelivery.Count == 0)
            {
                return BadRequest("No dispatch delivery data provided.");
            }

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                try
                {
                    con.Open();
                              
                    using (var transaction = con.BeginTransaction())
                    {
                  
                        foreach (var dispatch in model.DispatchDelivery)
                        {
                            SaveDispatchDeliveryData(con, transaction, dispatch);
                        }
                                   
                        transaction.Commit();
                    }

                    return Ok(new { success = true, message = "Dispatch data saved successfully!" });
                }
                catch (Exception ex)
                {
         
                    return StatusCode(500, new { success = false, message = ex.Message });
                }
            }
        }        

        private void SaveDispatchDeliveryData(SqlConnection connection, SqlTransaction transaction, DispatchDeliveryPlaning dispatch)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using var conn = _dbConnection.GetErpConnection();

            conn.Open();

            string deletePRequest2Sql = @" DELETE FROM ORDER_DELPLAN  WHERE COMP_CODE = @CompCode 
                    AND V_NO = @VNo  AND BRANCH_CODE = @BranchCode  AND YEAR_CODE = @YearCode;";

            using (var deletePRequest2Cmd = conn.CreateCommand())
            {
                deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                deletePRequest2Cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                deletePRequest2Cmd.Parameters.AddWithValue("@VNo", dispatch.v_no);
                deletePRequest2Cmd.Parameters.AddWithValue("@BranchCode", 1);
                deletePRequest2Cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                deletePRequest2Cmd.Parameters.AddWithValue("@V_TYPE", "PAUD");

                deletePRequest2Cmd.ExecuteNonQuery();
            }

            conn.Close();


            using (var cmd = new SqlCommand("sp_PurchaseSauda", connection, transaction))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "SaveDataDispatch");
                cmd.Parameters.AddWithValue("@DOC_ID", "PAUD" + dispatch.v_no);  
                cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                cmd.Parameters.AddWithValue("@V_DATE", dispatch.V_DATE); 
                cmd.Parameters.AddWithValue("@V_TYPE", "PAUD");  
                cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);  
                cmd.Parameters.AddWithValue("@V_NO", dispatch.v_no);  
                cmd.Parameters.AddWithValue("@ITEM_NAME", dispatch.ItemName); 
                cmd.Parameters.AddWithValue("@ITEM_CODE", dispatch.ItemCode);
                cmd.Parameters.AddWithValue("@DELIVERY_DATE", dispatch.DeliveryDate);  
                cmd.Parameters.AddWithValue("@QTY", dispatch.Qty);  
                cmd.Parameters.AddWithValue("@BAL_QTY", 0); 
                cmd.Parameters.AddWithValue("@Remarks", dispatch.Remarks);  
                cmd.Parameters.AddWithValue("@UUSER", getdata.PubUserId);  
                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);  
                cmd.Parameters.AddWithValue("@EUSER", getdata.PubUserId); 
                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);  
                cmd.Parameters.AddWithValue("@AED", "A");  
                cmd.Parameters.AddWithValue("@WSID", getdata.PubWorkStationID); 
                cmd.Parameters.AddWithValue("@LIP", getdata.PubLocalId); 
                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                cmd.ExecuteNonQuery(); 
            }
        }
        [HttpPost]
        public IActionResult SavedData([FromBody] PurchaseSauda_model request)
        {
            if (request?.Header == null)
            {
             return Json(new { success = false, message = "Input model is null" });
            }

            var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";
            var result = SubmitRequest(request.Header, request.Document, action);
            return result == "Success" ? Json(new { success = true }) : Json(new { success = false, message = result });
        }

        private string SubmitRequest(PurchaseSauda_Header header,  List<DocumentAttachment> Attachments, string action)
        {
            try
            {
                Boolean isApprovalBody = false;
                Boolean isFinalApprovalBody = false;
                string DOC_APPROSTAGE = "";
                string APPROV_USER = "";
                string fappstatus = "";
                string fappRemark = "";
                var refflg = 0;

                var globalVaraible = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                string sql = "Select isnull(SMS,'') from SUBGROUP_MAST Where " + "Comp_code= "+ globalVaraible.PubCompCode  +" and Code= " +  header.PARTY_CODE + " ";
                string smsValue = "";
                using var cmd1 = new SqlCommand(sql, conn);
                cmd1.Parameters.AddWithValue("@CompCode", globalVaraible.PubCompCode);
                cmd1.Parameters.AddWithValue("@Code", header.PARTY_CODE);

                var result = cmd1.ExecuteScalar();
                if (result == null)
                {                   
                    return "SMS Number is blank of => " + header.PartyName  + "";
                }     


                DOC_APPROSTAGE = GetText("select 1 from DOC_APPROSTAGE where USER_CODE=" +  globalVaraible.PubUserId + " and DOC_CODE='PAUD' and" +
                " comp_code=" + globalVaraible.PubCompCode + " ");

                if (DOC_APPROSTAGE == "1")
                {
                    isApprovalBody = true;
                }

                APPROV_USER = GetText("select APPROV_USER from DOC_APPROSTAGE where USER_CODE=" + globalVaraible.PubUserId + " and " +
                "DOC_CODE='PAUD' and comp_code=" + globalVaraible.PubCompCode + " ");

                if(APPROV_USER == "FINAL")
                {
                    isFinalApprovalBody = true;
                }
     
                if(isFinalApprovalBody == true)
                {
                    fappstatus = "Approved";
                    fappRemark = "Document Approved.";
                }


                decimal minRate = 0, maxRate = 0;        
                        
                string query = @"
                SELECT TOP 1 MIN_RATE, MAX_RATE
                FROM MARKET_RATE1 a
                LEFT JOIN MARKET_RATE2 b 
                ON a.V_Type = b.V_Type 
                AND a.v_no = b.v_no 
                AND a.comp_code = b.comp_code 
                AND a.branch_code = b.branch_code 
                AND a.year_code = b.year_code
                WHERE a.Comp_code = @Comp_code
                AND a.faprov_status = 'Approved'
                AND b.Item_code = @ITEM_CODE
                AND a.eff_date >= DATEADD(DAY, -20, GETDATE())
                ORDER BY a.V_DATE DESC, a.V_no DESC";

                using var cmd2 = new SqlCommand(query, conn);

                cmd2.Parameters.AddWithValue("@Comp_code", globalVaraible.PubCompCode);
                cmd2.Parameters.AddWithValue("@ITEM_CODE", header.ITEM_CODE);

                using var reader = cmd2.ExecuteReader();
                {
                    if (reader.Read())
                    {
                        minRate = Convert.ToDecimal(reader["MIN_RATE"]);
                        maxRate = Convert.ToDecimal(reader["MAX_RATE"]);
                    }

                    if (header.RATE >= minRate && header.RATE <= maxRate)
                    {
                        fappstatus = "Approved";
                        fappRemark = "Document Approved.";
                    }
                }
                conn.Close();


                conn.Open();



                if (action == "UPDATE")
                {
                    _globalValidationdate.LogInsertUpdateDelete(destinationTable: "SAUDA", sourceTable: "SAUDA", transactionType: "Transaction",
                    codeVNo: header.V_NO.ToString(), vtype: "PAUD");
                }


                using (var cmd = new SqlCommand("sp_PurchaseSauda", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", header.action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@DOC_ID", ("PAUD") + header.V_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "PAUD");
                    cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                    cmd.Parameters.AddWithValue("@PARTY_TO", header.Delivery_From);
                    cmd.Parameters.AddWithValue("@ADD1", header.ADD1);
                    cmd.Parameters.AddWithValue("@ADD2", header.ADD2);
                    cmd.Parameters.AddWithValue("@ADD3", header.ADD3);
                    cmd.Parameters.AddWithValue("@REF_NO", header.REF_NO);
                    cmd.Parameters.AddWithValue("@CITY_CODE", header.CITY_CODE);
                    cmd.Parameters.AddWithValue("@PHONE", header.PHONE);
                    cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE);
                    cmd.Parameters.AddWithValue("@ITEM_CODE", header.ITEM_CODE);
                    cmd.Parameters.AddWithValue("@TRUCK_NO", header.TRUCK_NO);
                    cmd.Parameters.AddWithValue("@Waste_Per", header.WASTE_PER);
                    cmd.Parameters.AddWithValue("@HQTY", header.QTY);
                    cmd.Parameters.AddWithValue("@RATE", header.RATE);
                    cmd.Parameters.AddWithValue("@EXRATE", header.EXRATE);
                    cmd.Parameters.AddWithValue("@CURRENCY", header.CURRENCY);
                    cmd.Parameters.AddWithValue("@SHIP_TYPE", header.SHIP_TYPE);
                    cmd.Parameters.AddWithValue("@DISC_PER", header.DISC_PER);
                    cmd.Parameters.AddWithValue("@FRT_TERM", header.FRT_TERM);
                    cmd.Parameters.AddWithValue("@TAX_TERM", header.TAX_TERM);
                    cmd.Parameters.AddWithValue("@FRT_RATE", header.FRT_RATE);
                    cmd.Parameters.AddWithValue("@TAX_CODE", header.TAX_CODE);
                    cmd.Parameters.AddWithValue("@TAX_RATE", header.TAX_RATE);
                    cmd.Parameters.AddWithValue("@NET_RATE", header.NET_RATE);
                    cmd.Parameters.AddWithValue("@ONLY_NATURAL", header.ONLY_NATURAL);
                    cmd.Parameters.AddWithValue("@PAYTERM_CODE", header.PAYTERM_CODE);
                    cmd.Parameters.AddWithValue("@DEL_TERM", header.DEL_TERM);
                    cmd.Parameters.AddWithValue("@REMARK", header.REMARK);
                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", fappstatus);
                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", fappRemark);
                    cmd.Parameters.AddWithValue("@STATUS", header.STATUS);
                    cmd.Parameters.AddWithValue("@HOLD_PAY", header.HOLD_PAY);
                    cmd.Parameters.AddWithValue("@PINO", header.PINO);
                    cmd.Parameters.AddWithValue("@PIDATE", header.PIDATE);
                    cmd.Parameters.AddWithValue("@OFFERNO", header.OFFERNO);
                    cmd.Parameters.AddWithValue("@GRADE", header.GRADE);
                    cmd.Parameters.AddWithValue("@BROKER", header.BROKER);
                    cmd.Parameters.AddWithValue("@BROKER_RATE", header.BROKER_RATE);
                    cmd.Parameters.AddWithValue("@Offer_Rate", header.OfferRate);
               
                    cmd.Parameters.AddWithValue("@DELIVERY_TERMIMP", "");
                    cmd.Parameters.AddWithValue("@DISPATCH_FROM", header.DISPATCH_FROM);
                    cmd.Parameters.AddWithValue("@SHIP_CODE", header.SHIP_CODE);
                    cmd.Parameters.AddWithValue("@SHIP_FROM", header.SHIP_FROM);
                    cmd.Parameters.AddWithValue("@PACK_TYPE", header.PACK_TYPE);
                    cmd.Parameters.AddWithValue("@PAYMENT_STATUS", header.PAYMENT_STATUS);
                    cmd.Parameters.AddWithValue("@SBLC_DUEDATE", header.SBLC_DUEDATE);
                    cmd.Parameters.AddWithValue("@LC_DUEDATE", header.LC_DUEDATE);
                    cmd.Parameters.AddWithValue("@ITEM_REMARKS", header.ITEM_REMARKS);
                    cmd.Parameters.AddWithValue("@DEAL_THROUGH", header.DEAL_THROUGH);
                    cmd.Parameters.AddWithValue("@UUSER", globalVaraible.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", globalVaraible.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", globalVaraible.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVaraible.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.ExecuteNonQuery();
                }


         
                string deletePRequest2Sql = @"  DELETE FROM IMG_TABLE   WHERE COMP_CODE = @CompCode 
                    AND V_NO = @VNo  AND BRANCH_CODE = @BranchCode  AND YEAR_CODE = @YearCode;";

                using (var deletePRequest2Cmd = conn.CreateCommand())
                {
                    deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                    deletePRequest2Cmd.Parameters.AddWithValue("@CompCode", globalVaraible.PubCompCode);
                    deletePRequest2Cmd.Parameters.AddWithValue("@VNo", header.V_NO);
                    deletePRequest2Cmd.Parameters.AddWithValue("@BranchCode", globalVaraible.PubBranchCode);
                    deletePRequest2Cmd.Parameters.AddWithValue("@YearCode", globalVaraible.PubFYearCode);
                    deletePRequest2Cmd.ExecuteNonQuery();
                }


                foreach (var Attachment in Attachments)
                {
                    if (string.IsNullOrWhiteSpace(Attachment.FileName))
                        continue;

                    using var cmd3 = new SqlCommand("sp_PurchaseSauda", conn)
                    { CommandType = CommandType.StoredProcedure };
                    cmd3.Parameters.AddWithValue("@Action", header.action);
                    cmd3.Parameters.AddWithValue("@SaveAction", "Documnets");
                    cmd3.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                    cmd3.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                    cmd3.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "PAUD") + header.V_NO);
                    cmd3.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd3.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                    cmd3.Parameters.AddWithValue("@V_TYPE", "PAUD");
                    cmd3.Parameters.AddWithValue("@FILE_NAME", Attachment.FileName);
                    cmd3.Parameters.AddWithValue("@FILE_Path", "/attachments/Purchase/" + (Attachment.FileName ?? ""));
                    cmd3.Parameters.AddWithValue("@UUSER", globalVaraible.PubUserId);
                    cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd3.Parameters.AddWithValue("@EUSER", globalVaraible.PubUserId);
                    cmd3.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd3.Parameters.AddWithValue("@AED", "A");
                    cmd3.Parameters.AddWithValue("@WSID", globalVaraible.PubWorkStationID);
                    cmd3.Parameters.AddWithValue("@LIP", globalVaraible.PubLocalId);
                    cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd3.ExecuteNonQuery();
                }

                if (Attachments.Count > 0)
                {
                    string approval = GetText("SELECT 1  FROM approval_status WHERE user_Code = " + globalVaraible.PubUserId + " AND " +
                    "V_Type = 'PAUD' AND V_No = " + header.V_NO + "  AND  " +
                    "  COMP_CODE = " + globalVaraible.PubCompCode + "  AND Branch_Code = " + globalVaraible.PubBranchCode + "  AND Year_Code = " + globalVaraible.PubFYearCode + ";");

                    if (isFinalApprovalBody == true && approval != "")
                    {
                        string UpdateSql = @"UPDATE approval_status SET STATUS = 'CLOSE', LOSE_DATE = GETDATE(),  Approval_code = 8, Approval_remark = 'Approved',
                        remarks = 'Document Approved' WHERE V_Type = 'PAUD'  AND V_No = @V_No AND COMP_CODE = @COMP_CODE AND Branch_Code = @Branch_Code
                        AND Year_Code = @Year_Code;";

                        using (var updateCmd = new SqlCommand(UpdateSql, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@V_No", header.V_NO);              
                            updateCmd.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                            updateCmd.Parameters.AddWithValue("@Branch_Code", globalVaraible.PubBranchCode);
                            updateCmd.Parameters.AddWithValue("@Year_Code", globalVaraible.PubFYearCode);
                            updateCmd.ExecuteNonQuery();

                        }
                    }
                }


                return "Success";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("SAUDA", vdate, vtype, vno);
            return Ok(result);
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

        public JsonResult CheckOutherrised(int partycode)
        { 

            var globalVaraible = _globalVariableService.GetGlobalVariables();


            var ADD = GetText("select isnull(_ADD,0) as 'ADD' from user_menu where comp_code= " + globalVaraible.PubCompCode + " and MENU_CODE=83 and YEAR_CODE= " + globalVaraible.PubFYearCode + " and USER_CODE=" + globalVaraible.PubUserId + "");


            var Statetype = GetText("SELECT TOP 1 State_type  FROM State_Mast WHERE code = ( SELECT state_code FROM SUBGROUP_MAST  WHERE code = " + partycode + "   AND COMP_CODE = " + globalVaraible.PubCompCode + ");");



            if(globalVaraible.PubUserLevel != "1")
            {
                return new JsonResult(new { success = false , message = "You are not authorised to Create Purchase Order."  });
            }
            else
            {
                return new JsonResult(new { success = true, message = "You are not authorised to Create Purchase Order.", Statetype = Statetype });
            }




      


            return new JsonResult(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] PurchaseOrderDto model)
        {
            try
            {

                var globalVaraible = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                string sql = @"  SELECT Sauda_No FROM Order1  WHERE V_type = @VType   AND Sauda_Type = @SaudaType  AND Sauda_No = @SaudaNo   AND Comp_code = @CompCode";

                using var cmd2 = new SqlCommand(sql, conn);

                // IMPORTANT: ensure variables are actually assigned
                string vType = "RORD";
                string saudaType = "PAUD";

                cmd2.Parameters.AddWithValue("@VType", vType);
                cmd2.Parameters.AddWithValue("@SaudaType", saudaType);
                cmd2.Parameters.AddWithValue("@SaudaNo", model.SaudaNo ?? (object)DBNull.Value);
                cmd2.Parameters.AddWithValue("@CompCode", globalVaraible.PubCompCode);

                var result = cmd2.ExecuteScalar();

                if(result is not null)
                {
                    return Json(new { status = false, message = "Order already created of this Sauda." , validation = true });
                }


                int V_NO = 0;
                var jsonResult = GetVNo("RORD" , "order1") as JsonResult;
                dynamic data = jsonResult.Value;
                V_NO = Convert.ToInt32(data.V_NO);


                string docid = "RORD" + V_NO;


                decimal basicAmt = Math.Round((model.Qty ?? 0m) * (model.indrate ?? 0m), 2);
                string csgstper = GetText("select isnull(CGST_PER,0) from TAX_MAST where Code=" + model.taxrate + " ");
                string igstper = GetText("select isnull(IGST_PER,0) from TAX_MAST where Code=" + model.taxrate + " ");
                string vatper = GetText("select isnull(VAT_PER,0) from TAX_MAST where Code=" + model.taxrate + " ");

                decimal cgstPer = Convert.ToDecimal(csgstper);
                decimal igstPer = Convert.ToDecimal(igstper);
                decimal cvatper = Convert.ToDecimal(vatper);                            

                // CGST
                decimal cgstAmt = Math.Round(basicAmt * cgstPer / 100m, 2);

                // SGST (same as CGST)
                decimal sgstAmt = Math.Round(basicAmt * cgstPer / 100m, 2);   
                // IGST
                decimal igstAmt = Math.Round(basicAmt * igstPer / 100m, 2);

                decimal VAT_AMT = Math.Round(basicAmt * cvatper / 100m, 2);

                decimal netAmt = basicAmt + cgstAmt + sgstAmt + igstAmt + VAT_AMT;

                decimal LandRate = Math.Round(netAmt / (model.Qty ?? 1m), 2);   

                using var con = _dbConnection.GetErpConnection();        
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PurchaseSauda", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // ---------------- COMMON ----------------
                        cmd.Parameters.AddWithValue("@Action", "CreatePurcOrder");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVaraible.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVaraible.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVaraible.PubFYearCode);           
                        cmd.Parameters.AddWithValue("@V_NO", V_NO);           
                        cmd.Parameters.AddWithValue("@DOC_ID", docid);           
                        // ---------------- HEADER ----------------
                        cmd.Parameters.AddWithValue("@PARTY_CODE", model.PartyCode);
                        cmd.Parameters.AddWithValue("@BILL_ADD1", model.BillAdd1 ?? "");
                        cmd.Parameters.AddWithValue("@BILL_ADD2", model.BillAdd2 ?? "");
                        cmd.Parameters.AddWithValue("@BILL_ADD3", model.BillAdd3 ?? "");
                        cmd.Parameters.AddWithValue("@BILL_CITY", model.BillCity);
                        cmd.Parameters.AddWithValue("@BILL_PINCODE", model.BillPincode ?? "");
                        cmd.Parameters.AddWithValue("@BILL_GST", model.BillGst ?? "");
                        cmd.Parameters.AddWithValue("@SHIP_FROM", model.ShipFrom);
                        cmd.Parameters.AddWithValue("@SHIP_ADD1", model.ShipAdd1 ?? "");
                        cmd.Parameters.AddWithValue("@SHIP_ADD2", model.ShipAdd2 ?? "");
                        cmd.Parameters.AddWithValue("@SHIP_ADD3", model.ShipAdd3 ?? "");
                        cmd.Parameters.AddWithValue("@SHIP_CITY", model.ShipCity);
                        cmd.Parameters.AddWithValue("@SHIP_PINCODE", model.ShipPincode ?? "");
                        cmd.Parameters.AddWithValue("@SHIP_GST", model.ShipGst ?? "");
                        cmd.Parameters.AddWithValue("@SAUDA_TYPE", "PAUD");
                        cmd.Parameters.AddWithValue("@SAUDA_NO", model.SaudaNo);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", model.PlaceCode);
                        cmd.Parameters.AddWithValue("@PRICE_TYPE", model.PriceType ?? "");
                        cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", model.Currency ?? "");
                        // ---------------- QUANTITY / AMOUNT ----------------
                        cmd.Parameters.AddWithValue("@NOS", model.Nos);
                        cmd.Parameters.AddWithValue("@QTY", model.Qty);
                        cmd.Parameters.AddWithValue("@AMOUNT", basicAmt);
                        cmd.Parameters.AddWithValue("@PACK_AMT", 0);              
                        cmd.Parameters.AddWithValue("@CGST_AMT", cgstAmt);
                        cmd.Parameters.AddWithValue("@SGST_AMT", sgstAmt);
                        cmd.Parameters.AddWithValue("@IGST_AMT", igstAmt);
                        cmd.Parameters.AddWithValue("@VAT_AMT", VAT_AMT);
                        cmd.Parameters.AddWithValue("@TCS_PER", 0);
                        cmd.Parameters.AddWithValue("@TCS_AMT", 0);
                        cmd.Parameters.AddWithValue("@OTH_AMT", 0);
                        cmd.Parameters.AddWithValue("@NET_AMT", netAmt);
                        cmd.Parameters.AddWithValue("@DELIVERY_TERM", model.DeliveryTerm ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_REF", model.PartyRef ?? "");
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", "Approved");
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", "Auto Generated PO");
                        cmd.Parameters.AddWithValue("@PAYTERM_CODE", model.PayTermCode);
                        cmd.Parameters.AddWithValue("@REMARKS", model.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@DISC_AMT", 0);
                        cmd.Parameters.AddWithValue("@CDISC_AMT", 0);                                 
                        cmd.Parameters.AddWithValue("@STATUS", 1);

                        //---------------- ITEM DETAILS ----------------    
                        cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", model.ITEM_NAME);
                        cmd.Parameters.AddWithValue("@RATE", model.RATE);
                        cmd.Parameters.AddWithValue("@IMPORT_RATE", model.imprate);
                        cmd.Parameters.AddWithValue("@CALC_RATE", model.indrate);
                        cmd.Parameters.AddWithValue("@PACK_PER", 0);
                        cmd.Parameters.AddWithValue("@DISC_PER", 0);
                        cmd.Parameters.AddWithValue("@TAX_CODE", model.taxrate);
                        cmd.Parameters.AddWithValue("@CGST_PER", cgstPer);
                        cmd.Parameters.AddWithValue("@SGST_PER", cgstPer); ;
                        cmd.Parameters.AddWithValue("@IGST_PER", igstper);
                        cmd.Parameters.AddWithValue("@VAT_PER", vatper);
                        cmd.Parameters.AddWithValue("@CESS_PER", 0);
                        cmd.Parameters.AddWithValue("@CESS_AMT", 0);
                        cmd.Parameters.AddWithValue("@LAND_RATE", LandRate);
                       

                        // ---------------- AUDIT ----------------
                        cmd.Parameters.AddWithValue("@UUSER", globalVaraible.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVaraible.PubWorkStationID ?? "");
                        cmd.Parameters.AddWithValue("@LIP", globalVaraible.PubLocalId ?? "");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "");

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }

                }

                return Json(new { status = true, message = "Purchase Order created successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public class PurchaseOrderDto
        {
            public int? CompCode { get; set; }
            public int? BranchCode { get; set; }
            public int? YearCode { get; set; }
            public string? VType { get; set; }
            public int? VNo { get; set; }
            public string? DocId { get; set; }
            public int? PartyCode { get; set; }
            public string? BillAdd1 { get; set; }
            public string? BillAdd2 { get; set; }
            public string? BillAdd3 { get; set; }
            public int? BillCity { get; set; }
            public string? BillPincode { get; set; }
            public string? BillGst { get; set; }
            public int? ShipFrom { get; set; }
            public string? ShipAdd1 { get; set; }
            public string? ShipAdd2 { get; set; }
            public string? ShipAdd3 { get; set; }
            public int? ShipCity { get; set; }
            public string? ShipPincode { get; set; }
            public string? ShipGst { get; set; }
            public int? SaudaNo { get; set; }
            public int? PlaceCode { get; set; }
            public string? PriceType { get; set; }
            public string? Currency { get; set; }
            public decimal? Nos { get; set; }
            public decimal? Qty { get; set; }
            public decimal? RATE { get; set; }
            public decimal? Amount { get; set; }
            public decimal? PackAmt { get; set; }
            public decimal? DiscAmt { get; set; }
            public decimal? CgstAmt { get; set; }
            public decimal? SgstAmt { get; set; }
            public decimal? IgstAmt { get; set; }
            public decimal? TcsPer { get; set; }
            public decimal? TcsAmt { get; set; }
            public decimal? OtherAmt { get; set; }
            public decimal? NetAmt { get; set; }
            public decimal? IMPORT_CURRENCY { get; set; }
            public string?  DeliveryTerm { get; set; }
            public string? PartyRef { get; set; }
            public int? PayTermCode { get; set; }
            public string? Remarks { get; set; }
            public decimal? CDiscAmt { get; set; }
            public decimal? taxrate { get; set; }
            public decimal? BasicAmmount { get; set; }
            public decimal? indrate { get; set; }
            public decimal? imprate { get; set; }
            public string? IpAddress { get; set; }
 
            public int? ITEM_CODE { get; set; }
            public int? status { get; set; }
            public string? ITEM_NAME { get; set; }

        }

        public string Paymentterm(int partyCode)
        {
            var globalVaraible = _globalVariableService.GetGlobalVariables();

            string payterm_code = GetText("select isnull(payterm_code,0) from subgroup_mast where code=" + partyCode + " and comp_code=" + globalVaraible.PubCompCode + "");

            return payterm_code;
        }
        public string GetTaxRate(int taxrate)
        {
            var globalVaraible = _globalVariableService.GetGlobalVariables();

            string GetTaxRate = GetText("Select CGST_PER+SGST_PER+IGST_PER From TAX_MAST Where code=" + taxrate + "");

            return GetTaxRate ;
        }

        public JsonResult FinalUser(int v_no)
        {
            var globalVaraible = _globalVariableService.GetGlobalVariables();
            string  FinalUser = GetText("select APPROV_USER from DOC_APPROSTAGE where  USER_CODE = " + globalVaraible.PubUserId + "  ");

            using var con = _dbConnection.GetConDbConnection();

                    string imgQuery = @" SELECT IMGDATABASE_NAME  FROM COMP_MAST  WHERE CODE = @Code";

                    using var cmd = new SqlCommand(imgQuery, con);
                    cmd.Parameters.AddWithValue("@Code", globalVaraible.PubCompCode);

                    con.Open();
                    var result = cmd.ExecuteScalar();

            string modificationcount = GetText("select count(*) from  "+ result + ".dbo.SAUDA where V_NO = " + v_no + " and V_TYPE = 'PAUD' and COMP_CODE = " + globalVaraible.PubCompCode + "  and YEAR_CODE = " + globalVaraible.PubFYearCode + "  and BRANCH_CODE = " + globalVaraible.PubBranchCode + "  ");

            string CretePurchaseorder = GetText("select FAPROV_STATUS from SAUDA where V_NO = " + v_no + " and V_TYPE = 'PAUD' AND  FAPROV_STATUS = 'Approved'");

            return Json(new { FinalUser = FinalUser , modificationcount = modificationcount , CretePurchaseorder = CretePurchaseorder } );


        }

        public JsonResult CheackMail(int v_no)
        {
            var globalVaraible = _globalVariableService.GetGlobalVariables();

            string FAPROV_STATUS = GetText("select FAPROV_STATUS from SAUDA where FAPROV_STATUS='Approved' and V_TYPE='PAUD' and V_NO=" + v_no + " and " +
            "COMP_CODE=" + globalVaraible.PubCompCode + " and BRANCH_CODE="  + globalVaraible.PubBranchCode + " and YEAR_CODE=" + globalVaraible.PubFYearCode + " ");


            if(FAPROV_STATUS != "Approved")
            {
                return Json(new { status = false , message = "Document not approved, Mail not sent." });
            }
            else
            {
                return Json(new { status = true });
            }        
        }

        [HttpPost]
        public async Task<IActionResult> SendMail(int PartyCode , int vno, IFormFile file)
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

                string compname = GetText("Select COMP_NAME from COMP_MAST WHERE CODE= " + globalVaraible.PubCompCode);

                string mailBody = "Please find attached Purchase Contract/Order.<br><br><br>";
                mailBody += "Kindly send us acceptance mail of Purchase Contract/Order within 3 days, otherwise it will be deemed to be accepted.";
                mailBody += "<br><br>Regards,<br>" + compname + "<br>" + globalVaraible.Address1 + "<br>" + globalVaraible.Address2;

                return await GlobalSendMail("PAUD", vno, Mail,  mailBody, file ,  "" );
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GlobalSendMail(  string vtype,  int vno, string toEmail,  string body, IFormFile file,  string ccEmail = "")
        {
            try
            {
                string host = "";
                string user = "";
                string pass = "";
                int port = 0;
                bool ssl = true;

                var globalVaraible = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    string query = @"SELECT smtp_server, user_id, password, smtp_port, smtp_ussl 
                        FROM email_setting1
                        WHERE comp_code = @comp AND V_TYPE = @vtype";

                    using SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@comp", globalVaraible.PubCompCode);
                    cmd.Parameters.AddWithValue("@vtype", vtype);

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    if (!reader.Read())
                        return Json(new { success = false, message = "Email settings not found" });

                    host = reader["smtp_server"].ToString();
                    user = reader["user_id"].ToString();
                    pass = "Apple@213";
                    port = Convert.ToInt32(reader["smtp_port"]);
                    ssl = Convert.ToBoolean(reader["smtp_ussl"]);
                }

                using SmtpClient smtp = new SmtpClient(host)
                {
                    Port = port,
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = ssl
                };

                using MailMessage mail = new MailMessage
                {
                    From = new MailAddress(user),
                    Subject = vtype switch
                    {
                        "PAUD" => "Purchase Contract/Order",
                        "PORD" => $"Purchase Order No : {vno}",
                        "SAGT" or "SASI" => $"Invoice No : {vno}",
                        "BPMS" => "Confirmation of Balance",
                        "TASK" => "New Task Received",
                        "CLMT" => "Credit Limit Updation",
                        _ => $"Document No : {vno}"
                    },
                    Body = body,
                    IsBodyHtml = true
                };

                mail.To.Add(toEmail);

                if (!string.IsNullOrWhiteSpace(ccEmail))
                    mail.CC.Add(ccEmail);

                // 🔥 STEP 1: ADD ATTACHMENT (IMPORTANT PART)
                if (file != null && file.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);

                    var fileBytes = ms.ToArray();

                    mail.Attachments.Add(
                        new Attachment(
                            new MemoryStream(fileBytes),
                            file.FileName,
                            "application/pdf"
                        )
                    );
                }

                await smtp.SendMailAsync(mail);

                return Json(new { success = true, message = "Mail sent successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}

