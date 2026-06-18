
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Metadata;
using System.Text.Json;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
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

                    string lastV_NO_Query = "SELECT MAX(V_NO) FROM SAUDA WHERE COMP_CODE = @CompCode AND YEAR_CODE = @YearCode  and SAUDA.BRANCH_CODE = @BRANCH_CODE and V_TYPE = 'PAUD'   ";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

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

                // -------------------- FINAL RESPONSE --------------------
                return Json(new { Supplier = supplier, Sauda = sauda });
            }
        }

        public JsonResult GetDataByRefNo(int RefNo)
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            object resultData = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("sp_PurchaseSauda", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@V_NO", RefNo);
                    cmd.Parameters.AddWithValue("@Action", "RefNoData");
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resultData = new
                            {
                                Disc = reader["DISC"],
                                VType = reader["V_TYPE"],
                                VNo = reader["V_no"],
                                VDate = reader["V_date"],
                                PartyCode = reader["P_code"],
                                PartyName = reader["P_name"],
                                Address1 = reader["Add1"],
                                Address2 = reader["Add2"],
                                Address3 = reader["Add3"],
                                CityCode = reader["C_code"],
                                CityName = reader["C_name"],
                                Phone = reader["Phone"],
                                DeliveryTo = reader["Delivery_to"],
                                ItemCode = reader["I_code"],
                                ItemName = reader["I_name"],
                                Qty = reader["Qty"],
                                Rate = reader["Rate"],
                                TaxRate = reader["Tax_rate"],
                                Truck = reader["Truck"],
                                Type = reader["Type"],

                                NetRate = reader["Net_rate"],
                                Remark = reader["Remark"],
                                DealThrough = reader["DealThrough"],
                                DEAL_THROUGHCode = reader["DEAL_THROUGHCode"],
                                COUNTRY_CODE = reader["COUNTRY_CODE"],
                                F_rate = reader["F_rate"],
                                F_term = reader["F_term"],
                                T_term = reader["T_term"],
                                PAYTERM_CODE = reader["PAYTERM_CODE"],
                                d_term = reader["d_term"]
                            };
                        }
                    }
                }
            }

            return Json(new { Refnodata = resultData });
        }








        public JsonResult DDLRefREqNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select V_No as 'No' , V_No as 'Text' from SAUDA  Order by V_NO";

                var ItemList = _dropdownService.GetDropdownList(query);

                return Json(ItemList);
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
                var globalVaraible = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();


                string sql = "Select isnull(SMS,'') from SUBGROUP_MAST Where " +
                    "Comp_code= "+ globalVaraible.PubCompCode  +" and Code= " +  header.PARTY_CODE + " ";


                string smsValue = "";

                using var cmd1 = new SqlCommand(sql, conn);
                cmd1.Parameters.AddWithValue("@CompCode", globalVaraible.PubCompCode);
                cmd1.Parameters.AddWithValue("@Code", header.PARTY_CODE);

                var result = cmd1.ExecuteScalar();
                if (result == null)
                {
                   
                    return "SMS Number is blank of => " + header.PartyName  + "";
                }

             
                conn.Close();

        
                conn.Open();

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
                    cmd.Parameters.AddWithValue("@QTY", header.QTY);
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
                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", "");
                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", "");
                    cmd.Parameters.AddWithValue("@STATUS", header.STATUS);
                    cmd.Parameters.AddWithValue("@HOLD_PAY", header.HOLD_PAY);
                    cmd.Parameters.AddWithValue("@PINO", header.PINO);
                    cmd.Parameters.AddWithValue("@PIDATE", header.PIDATE);
                    cmd.Parameters.AddWithValue("@OFFERNO", header.OFFERNO);
                    cmd.Parameters.AddWithValue("@GRADE", header.GRADE);
                    cmd.Parameters.AddWithValue("@BROKER", header.BROKER);
                    cmd.Parameters.AddWithValue("@BROKER_RATE", header.BROKER_RATE);
               
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
                    cmd3.Parameters.AddWithValue("@FILE_Path", "/attachments/pan/" + (Attachment.FileName ?? ""));
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

    }
}
