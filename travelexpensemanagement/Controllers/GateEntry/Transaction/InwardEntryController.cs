
using Microsoft.AspNetCore.Mvc;

using Microsoft.Data.SqlClient;

using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;



namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class InwardEntryController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public InwardEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/GateEntry/Transaction/InwardEntry/Index.cshtml");
        }
        public JsonResult GetVNo(string Vtype)
        {
            string newV_NO = "00000";

            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    // Get PREFIXYR from YEAR_MAST table
                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    using (SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con))
                    {
                        prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                        // Fetch last V_NO from GATE1
                        string lastV_NO_Query = @"
                                SELECT MAX(CAST(V_NO AS INT)) 
                                FROM GATE1 
                                WHERE COMP_CODE = @CompCode 
                                AND YEAR_CODE = @YearCode 
                                AND BRANCH_CODE = @BranchCode 
                                AND V_TYPE = @Vtype";

                        using (SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con))
                        {
                            lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                            lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                            lastVnoCmd.Parameters.AddWithValue("@BranchCode", 1);
                            lastVnoCmd.Parameters.AddWithValue("@Vtype", Vtype);

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
                }
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
                string query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('GateInward') order by Name ";

                var VtypeList = _dropdownService.GetDropdownList(query);

                return Json(VtypeList);
            }

        }

        public JsonResult DDlParty()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE, name from SUBGROUP_MAST where Nature in ('Customer','Supplier','Broker','Staff') and COMP_CODE = "+  getdata.PubCompCode + "    AND ACTIVE=1  and name <> '' order by name ";

                var Partylist = _dropdownService.GetDropdownList(query);

                return Json(Partylist);
            }

        }

        public JsonResult DDlShipFrom()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE, name from SUBGROUP_MAST where Nature in ('Customer','Supplier','Broker','Staff') and COMP_CODE ="+  getdata.PubCompCode + " AND ACTIVE=1 and name <> ''    order by name ";

                var ShipFromList = _dropdownService.GetDropdownList(query);

                return Json(ShipFromList);
            }

        }
        public JsonResult DDDocStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document'   and Name <> ''  Order by CODE";

                var DocStatusList = _dropdownService.GetDropdownList(query);

                return Json(DocStatusList);
            }

        }

        public JsonResult DDlPartycity()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from City_mast  Where Name <> ''  Order by name";

                var PartyCitylist = _dropdownService.GetDropdownList(query);

                return Json(PartyCitylist);
            }

        }

        [HttpGet]

        public JsonResult DDlTransitNo(string v_type, int v_no, int partycode, DateTime ExpiryDate)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();
            var date = ExpiryDate.Date;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string query = @"
                    SELECT V_No
                    FROM WAYBILL1
                    WHERE V_TYPE = 'TRIN'
                    AND V_No NOT IN (
                    SELECT TRANSIT_NO
                    FROM GATE1
                    WHERE V_TYPE = @V_Type
                    AND V_No = @V_No
                    AND TRANSIT_NO <> 0
                    AND COMP_CODE = @CompCode
                    AND BRANCH_CODE = 1
                    )
                    AND PARTY_CODE = @PartyCode
                    AND Status = 1
                    AND COMP_CODE = @CompCode
                    AND BRANCH_CODE = 1
                    AND EXPIRY_DATE IS NOT NULL
                    AND EXPIRY_DATE >= @ExpiryDate
                    ORDER BY V_No;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@V_Type", (object)v_type ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@V_No", v_no);
                    cmd.Parameters.AddWithValue("@PartyCode", partycode);
                    cmd.Parameters.AddWithValue("@ExpiryDate", ExpiryDate.Date);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                value = reader["V_No"].ToString(),
                                text = reader["V_No"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(dataList);
        }

        public JsonResult GetDataByPartyCode(int PartyId , int addressid )
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string query = @"
                                SELECT  a.Add1, a.Add2, a.Add3, a.GSTIN, a.City_Code, b.Name AS State, a.Pincode , c.NAME as cityName ,   a.PAN
                                FROM  Subgroup_Address a
                                LEFT JOIN STATE_MAST b ON a.STATE_CODE = b.code
                                LEFT JOIN CITY_MAST c ON a.CITY_CODE = c.code
                                WHERE   a.comp_code = @CompCode AND a.Code =@PartyId and   a.Address_Id = @Address_Id ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", PartyId);
                    cmd.Parameters.AddWithValue("@Address_Id", addressid);


                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                Add1 = reader["Add1"].ToString(),
                                Add2 = reader["Add2"].ToString(),
                                Add3 = reader["Add3"].ToString(),
                                GSTIN = reader["GSTIN"].ToString(),
                                City_Code = reader["City_Code"].ToString(),
                                State = reader["State"].ToString(),
                                Pincode = reader["Pincode"].ToString(),
                                cityName = reader["cityName"].ToString(),
                                PAN = reader["PAN"].ToString()


                            });
                        }
                    }
                }
            }

            return Json(dataList);
        }

        public JsonResult fetchShipFromAdd(int ShipFromID)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string query = @"
                SELECT CONCAT(A.ADD1, ' ', A.ADD2, ' ', A.ADD3) AS FullAddress 
                FROM SUBGROUP_MAST A
                WHERE Nature IN ('Customer','Supplier','Broker','Staff') 
                AND COMP_CODE = @CompCode 
                AND A.ACTIVE = 1 
                AND A.code = @ShipFromID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@ShipFromID", ShipFromID);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                Address = reader["FullAddress"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(dataList);
        }


        [HttpPost]

        public IActionResult SavedData([FromBody]  InwardEntryModel request)
        {
             if (request?.Header == null)
            {
                // Log the error and return a response if the Header is null

                return Json(new { success = false, message = "Input model is null" });
            }


            var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";
            var result = SubmitRequest(request.Header, request.Deatils, action);

            return result == "Success"
                ? Json(new { success = true })
                : Json(new { success = false, message = result });
        }

        private string SubmitRequest(InwardEntry_Header header, List<Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                // Delete from PREQUEST2
                string deletePRequest2Sql = @"
                    DELETE FROM GATE2 
                    WHERE COMP_CODE = @CompCode 
                    AND V_NO = @VNo 
                    AND BRANCH_CODE = @BranchCode 
                    AND YEAR_CODE = @YearCode;";

                using (var deletePRequest2Cmd = conn.CreateCommand())
                {
                    deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                    deletePRequest2Cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                    deletePRequest2Cmd.Parameters.AddWithValue("@VNo", header.V_NO);
                    deletePRequest2Cmd.Parameters.AddWithValue("@BranchCode", 1);
                    deletePRequest2Cmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                    deletePRequest2Cmd.ExecuteNonQuery();
                }

                conn.Close();
                          
                conn.Open();

                using (var cmd = new SqlCommand("sp_InwardEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE) + header.V_NO);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                    cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                    cmd.Parameters.AddWithValue("@V_TIME", header.V_TIME);
                    cmd.Parameters.AddWithValue("@R_DATE", header.R_DATE);
                    cmd.Parameters.AddWithValue("@R_TIME", header.R_TIME);
                    cmd.Parameters.AddWithValue("@DISP_PLAN_NO", header.DISP_PLAN_NO);
                    cmd.Parameters.AddWithValue("@DISP_PLAN_TYPE", header.DISP_PLAN_TYPE);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                    cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", header.PARTY_ADDRESSID);

                    cmd.Parameters.AddWithValue("@BILL_NO", header.BILL_NO);
                    cmd.Parameters.AddWithValue("@BILL_DATE", header.BILL_DATE);
                    cmd.Parameters.AddWithValue("@BILL_AMT", header.BILL_AMT);
                    cmd.Parameters.AddWithValue("@CHALL_NO", header.CHALL_NO);
                    cmd.Parameters.AddWithValue("@CHALL_DATE", header.CHALL_DATE);
                    cmd.Parameters.AddWithValue("@TRUCK_NO", header.TRUCK_NO);

                    cmd.Parameters.AddWithValue("@TRANSPORT_CODE", header.TRANSPORT_CODE);
                    cmd.Parameters.AddWithValue("@DRIVER_NAME", header.DRIVER_NAME);
                    cmd.Parameters.AddWithValue("@DRIVER_NO", header.DRIVER_NO);
                    cmd.Parameters.AddWithValue("@EWB_DATE", header.EWB_DATE);
                    cmd.Parameters.AddWithValue("@EWB_EXPDATE", header.EWB_EXPDATE);

                    cmd.Parameters.AddWithValue("@EWB_INVNO", header.EWB_INVNO);
                    cmd.Parameters.AddWithValue("@EWB_INVAMT", header.EWB_INVAMT);
                    cmd.Parameters.AddWithValue("@PARTY_WBSLIPNO", header.PARTY_WBSLIPNO);
                    cmd.Parameters.AddWithValue("@PARTY_WBGRWT", header.PARTY_WBGRWT);
                    cmd.Parameters.AddWithValue("@PARTY_WBTRWT", header.PARTY_WBTRWT);
                    cmd.Parameters.AddWithValue("@PARTY_WBTIME", header.PARTY_WBTIME);
                    cmd.Parameters.AddWithValue("@PARTY_EWBCITY", header.PARTY_EWBCITY);


                    cmd.Parameters.AddWithValue("@TRANSIT_NO", header.TRANSIT_NO);
                    cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO);

                    cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS);
                    cmd.Parameters.AddWithValue("@ADD1", header.Add1);
                    cmd.Parameters.AddWithValue("@ADD2", header.Add2);
                    cmd.Parameters.AddWithValue("@ADD3", header.Add3);

                    cmd.Parameters.AddWithValue("@PARTY_CITY", header.PARTY_CITY);
                    cmd.Parameters.AddWithValue("@PARTY_GST", header.PARTY_GST);
                    cmd.Parameters.AddWithValue("@PARTY_PINCODE", header.PARTY_PINCODE);
                    cmd.Parameters.AddWithValue("@SHIP_PARTY", header.SHIP_PARTY);
                    cmd.Parameters.AddWithValue("@SHIP_BILLNO", header.SHIP_BILLNO);
                    cmd.Parameters.AddWithValue("@SHIP_BILLDATE", header.SHIP_BILLDATE);

                    cmd.Parameters.AddWithValue("@RETURN_TYPE", header.RETURN_TYPE);
                

                    cmd.Parameters.AddWithValue("@GR_NO", header.GR_NO);
                    cmd.Parameters.AddWithValue("@GR_DATE", header.GR_DATE);

                    cmd.Parameters.AddWithValue("@RC_NO", header.RC_NO);
                    cmd.Parameters.AddWithValue("@DL_NO", header.DL_NO);
                    cmd.Parameters.AddWithValue("@INSU_NO", header.INSU_NO);
                    cmd.Parameters.AddWithValue("@PAN_NO", header.PAN_NO);

                    cmd.Parameters.AddWithValue("@STATUS", header.STATUS);

                    cmd.Parameters.AddWithValue("@INSU_EXPDT", header.INSU_EXPDT);
                    cmd.Parameters.AddWithValue("@DL_EXPDT", header.DL_EXPDT);
               
                    cmd.Parameters.AddWithValue("@FAPROV_STATUS", header.FAPROV_STATUS);
                    cmd.Parameters.AddWithValue("@CONTAINER_NO", header.CONTAINER_NO);

                    cmd.Parameters.AddWithValue("@FAPROV_REMARKS", "");
                
                    cmd.Parameters.AddWithValue("@ACTIVE", header.ACTIVE);
                    cmd.Parameters.AddWithValue("@Out_Date", header.OUT_DATE);

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

                foreach (var Details in details)
                {
                    if (string.IsNullOrWhiteSpace(Details.ITEM_NAME))
                        continue;

                    using var cmd3 = new SqlCommand("sp_InwardEntry", conn) { CommandType = CommandType.StoredProcedure };
                    cmd3.Parameters.AddWithValue("@Action", "INSERT");
                    cmd3.Parameters.AddWithValue("@SaveAction", "Details");
                    cmd3.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd3.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd3.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd3.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                    cmd3.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd3.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                    cmd3.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE ?? "PAUD") + header.V_NO);
                    cmd3.Parameters.AddWithValue("@TRF_TYPE", "");
                    cmd3.Parameters.AddWithValue("@TRF_NO", "");
                    cmd3.Parameters.AddWithValue("@ITEM_CODE", Details.ITEM_CODE);
                    cmd3.Parameters.AddWithValue("@ITEM_NAME", Details.ITEM_NAME);
                    cmd3.Parameters.AddWithValue("@DEPT_CODE", Details.DEPT_CODE);
                    cmd3.Parameters.AddWithValue("@NOS", Details.NOS);
                    cmd3.Parameters.AddWithValue("@QTY", Details.QTY);
                    cmd3.Parameters.AddWithValue("@UOM_CODE", Details.UOM_CODE);
                    cmd3.Parameters.AddWithValue("@UOM_NAME", Details.UOM_NAME);
                    cmd3.Parameters.AddWithValue("@EMPTY", Details.EMPTY);
                    cmd3.Parameters.AddWithValue("@REMARKS", Details.REMARKS);
                    cmd3.Parameters.AddWithValue("@REF_TYPE", Details.REF_TYPE);
                    cmd3.Parameters.AddWithValue("@REF_NO", Details.REF_NO);
                    cmd3.Parameters.AddWithValue("@MRN_TYPE", Details.MRN_TYPE);
                    cmd3.Parameters.AddWithValue("@MRN_NO", Details.MRN_NO);
                    cmd3.Parameters.AddWithValue("@STATUS", Details.STATUS);
                    cmd3.Parameters.AddWithValue("@ADJ_QTY", Details.ADJ_QTY);
                    cmd3.Parameters.AddWithValue("@BALANCEQTY", Details.BALANCEQTY);

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


        public JsonResult fetchSelectedAddress(int PartyId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"
                SELECT DISTINCT  address_id AS code, add1 AS name  FROM  SUBGROUP_ADDRESS 
                WHERE  code = " + PartyId + " AND COMP_CODE = " + getdata.PubCompCode  + "    and ADD1 <> ''  ORDER BY  ADDRESS_ID;";

                var selectAddList = _dropdownService.GetDropdownList(query);

                return Json(selectAddList);
            }

        }





    }
}
