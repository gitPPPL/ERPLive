using iTextSharp.text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;


namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    public class MiscConsumptionEntryListController : Controller
    {


        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public MiscConsumptionEntryListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;

        }

        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/MiscConsumptionEntryList/Index.cshtml");
        }


        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getvariabledata = _globalVariableService.GetGlobalVariables();

            if (getvariabledata == null)
            {
                return Json(new { success = false, message = "Global variable data is null." });
            }

            int totalCount = 0;
            var headerList = new List<MiscConsumptionEntry_Header>();
            var detailsList = new List<Details>();

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_MiscConsumptionEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getvariabledata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getvariabledata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        // First result set: Header data
                        while (reader.Read())
                        {
                            headerList.Add(new MiscConsumptionEntry_Header
                            {
                                V_TYPE = reader["Vtype"]?.ToString(),
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["Voucherdate"] != DBNull.Value ? Convert.ToDateTime(reader["Voucherdate"]) : DateTime.MinValue,
                                PARTY_NAME = reader["PartyName"]?.ToString(),
                                DOC_ID = reader["DOC_ID"]?.ToString(),
                                VtypeCode = reader["vCode"]?.ToString()

                            });
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data.", error = ex.Message });
            }

            return Json(new { success = true, headers = headerList, details = detailsList, totalCount });
        }




        [HttpPost]
        public IActionResult GetDataByCode([FromForm] int rowId, [FromForm] string vtype)

        {
            var GetGlobalCode = _globalVariableService.GetGlobalVariables();
            MiscConsumptionEntryModel wrapper = new MiscConsumptionEntryModel
            {
                Header = new MiscConsumptionEntry_Header(),
                Deatils = new List<Details>()
            };

            try
            {

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    #region Fetch Header Data
                    using (SqlCommand cmd = new SqlCommand("sp_MiscConsumptionEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@ShowActionOption", "Header");
                        cmd.Parameters.AddWithValue("@V_NO", rowId);
                        cmd.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);

                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.Read())
                            {
                                wrapper.Header = new MiscConsumptionEntry_Header
                                {
                                    V_TYPE = rdr["V_TYPE"]?.ToString(),
                                    V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                                    V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                                    V_TIME = rdr["V_TIME"]?.ToString(),
                                
                                    PARTY_CODE = rdr["party_code"] != DBNull.Value ? Convert.ToInt32(rdr["party_code"]) : 0,
                                    Add1 = rdr["ADD1"]?.ToString(),
                                    Add2 = rdr["ADD2"]?.ToString(),
                                    Add3 = rdr["ADD3"]?.ToString(),
                                     TRUCK_NO = rdr["TRUCK_NO"]?.ToString(),
                             
                                    REMARKS = rdr["REMARKS"]?.ToString(),
                                    DOC_ID = rdr["doc_id"]?.ToString()
                                };
                            }
                        }
                    }
                    #endregion

                    #region Fetch Dispatch Data
                    using (SqlCommand cmd4 = new SqlCommand("sp_MiscConsumptionEntry", con))
                    {
                        cmd4.CommandType = CommandType.StoredProcedure;
                        cmd4.Parameters.AddWithValue("@Action", "ShowData");
                        cmd4.Parameters.AddWithValue("@ShowActionOption", "Details");
                        cmd4.Parameters.AddWithValue("@V_NO", rowId);
                        cmd4.Parameters.AddWithValue("@V_TYPE", vtype);
                        cmd4.Parameters.AddWithValue("@COMP_CODE", GetGlobalCode.PubCompCode);
                        cmd4.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd4.Parameters.AddWithValue("@YEAR_CODE", GetGlobalCode.PubFYearCode);

                        using (SqlDataReader rdr = cmd4.ExecuteReader())
                        {
                            while (rdr.Read())
                            {
                                wrapper.Deatils.Add(new Details
                                {
                                    ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                                    DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                                    UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                                    NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                                    QTY = rdr["QTY"] != DBNull.Value ? Convert.ToInt32(rdr["QTY"]) : 0,
                                    REMARKS = rdr["REMARKS"]?.ToString()
                               
                                });
                            }
                        }
                    }
                    #endregion
                }


                var resultWrapper = new
                {
                    Header = wrapper.Header,
                    Details = wrapper.Deatils

                };

                return Json(new { success = true, data = resultWrapper });
            }
            catch (Exception ex)
            {

                return Json(new { success = false, message = "Error fetching purchase requisition data", error = ex.Message });
            }
        }




        [HttpPost]
        public JsonResult Delete(int code  , string vtype)
        {
            var getGlobalCode = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_MiscConsumptionEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getGlobalCode.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", getGlobalCode.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                        cmd.Parameters.AddWithValue("@V_TYPE", vtype);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Misc Consumption Entry deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Misc Consumption Entry .", error = ex.Message });
            }
        }


        public JsonResult GetPendingDocumnents(int PartyId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                    string query = @" SELECT  top 10  gate2.V_type ,  gate2.V_NO , FORMAT(gate1.v_date, 'yyyy-MM-dd') as v_date,  gate2.ITEM_CODE , gate2.item_name ,
                    gate2.remarks ,  gate2.QTY, (gate2.qty - ISNULL(gate2.ADJ_QTY, 0)) AS P_Qty, gate2.UOM_CODE, gate2.UOM_NAME,
                    gate2.NOS, gate2.srno FROM   GATE2
                    LEFT JOIN  GATE1   ON gate2.V_TYPE = gate1.V_TYPE  AND gate2.V_NO = gate1.V_NO AND gate2.COMP_CODE = gate1.COMP_CODE 
                    AND gate2.BRANCH_CODE = gate1.BRANCH_CODE  AND gate2.YEAR_CODE = gate1.YEAR_CODE
                    LEFT JOIN   DOCTYPE_MAST   ON DOCTYPE_MAST.CODE = gate2.V_TYPE ;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@PartyId", PartyId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                V_type = reader["V_type"].ToString(),
                                V_NO = reader["V_NO"].ToString(),
                                v_date = reader["v_date"]?.ToString(),
                                ITEM_CODE = reader["ITEM_CODE"].ToString(),
                                item_name = reader["item_name"].ToString(),
                                remarks = reader["remarks"].ToString(),
                                QTY = reader["QTY"].ToString(),
                                P_Qty = reader["P_Qty"].ToString(),
                                UOM_CODE = reader["UOM_CODE"].ToString(),
                                unitname = reader["UOM_NAME"].ToString(),
                                NOS = reader["NOS"].ToString(),
                                srno = reader["srno"].ToString(),
                            });
                        }
                    }
                }
            }

            return Json(dataList);
        }
            }
}
