
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;


namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class TransitEntryController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public TransitEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/GateEntry/Transaction/TransitEntry/Index.cshtml");
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
                                FROM WAYBILL1 
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

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE,NAME FROM DOCTYPE_MAST  WHERE DOCTYPE='Transit' ";

                var VtypeList = _dropdownService.GetDropdownList(query);

                return Json(VtypeList);
            }

        }


        public JsonResult DDlParty(string VTypeId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "";
                if (VTypeId == "TRIN")
                {
                    query = "Select code,name from SUBGROUP_MAST  where Nature='Supplier' and COMP_CODE=" + getdata.PubCompCode +"  order by name";
                }

                else if (VTypeId == "TROT")
                {
                    query = "Select code,name from SUBGROUP_MAST  where Nature='Customer' and COMP_CODE=" + getdata.PubCompCode +"   order by name ";
                }
                else
                {
                    query = "Select code,name from SUBGROUP_MAST  where Nature in ('Supplier','Customer') and COMP_CODE=" + getdata.PubCompCode + " order by name ";
                }


                    var Partylist = _dropdownService.GetDropdownList(query);

                return Json(Partylist);
            }

        }

        public JsonResult DDlstatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                    var Partylist = _dropdownService.GetDropdownList(query);

                return Json(Partylist);
            }

        }
        public JsonResult fetchPartyGstinNo(int Partycode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string query = @"
                   select gstin from subgroup_mast where code=@Partycode and comp_code=@CompCode";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@Partycode", Partycode);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                gstin = reader["gstin"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(dataList);
        }



        [HttpPost]
        public IActionResult Savedata([FromBody] TransitEntryModel data)

        {
            if (data == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            string action = data.action == "INSERT" ? "INSERT" : "UPDATE";
            var result = Submitbtn(data, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }


        [HttpPost]
        private string Submitbtn(TransitEntryModel data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_TransitEntry", conn))
                    {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@Action", action);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                            cmd.Parameters.AddWithValue("@V_TYPE", data.V_TYPE);
                            cmd.Parameters.AddWithValue("@V_NO", data.V_NO);
                            cmd.Parameters.AddWithValue("@DOC_ID", data.V_TYPE + data.V_NO);
                            cmd.Parameters.AddWithValue("@FORM_NO", data.FORM_NO);
                            cmd.Parameters.AddWithValue("@FORM_DATE", data.FORM_DATE);
                            cmd.Parameters.AddWithValue("@EXPIRY_DATE", data.EXPIRY_DATE);
                            cmd.Parameters.AddWithValue("@PARTY_CODE", data.PARTY_CODE);
                            cmd.Parameters.AddWithValue("@PARTY_GSTIN", data.PARTY_GSTIN);
                            cmd.Parameters.AddWithValue("@OTHER_GSTIN", data.OTHER_GSTIN);
                            cmd.Parameters.AddWithValue("@NOS", data.NOS);
                            cmd.Parameters.AddWithValue("@BILL_NO", data.BILL_NO);
                            cmd.Parameters.AddWithValue("@BILL_DATE", data.BILL_DATE);
                            cmd.Parameters.AddWithValue("@GR_NO", data.GR_NO);
                            cmd.Parameters.AddWithValue("@GR_DATE", data.GR_DATE);
                            cmd.Parameters.AddWithValue("@TRUCK_NO", data.TRUCK_NO);
                            cmd.Parameters.AddWithValue("@TRANSPORT", data.TRANSPORT);
                            cmd.Parameters.AddWithValue("@ORD_TYPE", data.ORD_TYPE);
                            cmd.Parameters.AddWithValue("@ORD_NO", data.ORD_NO);
                            cmd.Parameters.AddWithValue("@HSN_CODE", data.HSN_CODE);
                            cmd.Parameters.AddWithValue("@ITEM_DESC", data.ITEM_DESC);
                            cmd.Parameters.AddWithValue("@BILL_AMT", data.BILL_AMT);
                            cmd.Parameters.AddWithValue("@SGST_AMT", data.SGST_AMT);
                            cmd.Parameters.AddWithValue("@CGST_AMT", data.CGST_AMT);
                            cmd.Parameters.AddWithValue("@IGST_AMT", data.IGST_AMT);
                            cmd.Parameters.AddWithValue("@CESS_AMT", data.CESS_AMT);
                            cmd.Parameters.AddWithValue("@CESS_NONADVOLAMT", data.CESS_NONADVOLAMT);
                            cmd.Parameters.AddWithValue("@OTHER_AMT", data.OTHER_AMT);
                            cmd.Parameters.AddWithValue("@TOTAL_AMT", data.TOTAL_AMT);
                            cmd.Parameters.AddWithValue("@STATUS", data.STATUS);
                            cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        int rowsInserted = cmd.ExecuteNonQuery();

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }


    }
}
