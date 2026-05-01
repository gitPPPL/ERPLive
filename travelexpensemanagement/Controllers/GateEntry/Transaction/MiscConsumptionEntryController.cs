using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Controllers.GateEntry.Transaction
{
    [SessionAuthorize]
    public class MiscConsumptionEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IMiscConsumptionRepository _repository;
        public MiscConsumptionEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DropdownService dropdownService, DbHelper dbHelper, GlobalValidationdate globalValidationdate, IMiscConsumptionRepository repository,
            ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View("~/Views/GateEntry/Transaction/MiscConsumptionEntry/Index.cshtml");
        }

        public JsonResult DDLItemMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT   b.code,  b.name  FROM item_mast b WHERE b.active = 1  AND b.comp_code = " + getdata.PubCompCode + ";";

                var ItemList = _dropdownService.GetDropdownList(query);

                return Json(ItemList);
            }
        }

        public JsonResult DDLDeptMaster()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select  b.CODE ,  b.name  from ITEMDEPT_MAST b where B.ACTIVE=1 AND b.comp_code=" + getdata.PubCompCode + "";

                var DeptList = _dropdownService.GetDropdownList(query);

                return Json(DeptList);
            }
        }

        public JsonResult DDLUnit()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select  b.CODE ,   b.name  from ITEMUNIT_MAST b where B.ACTIVE=1 AND b.comp_code=" + getdata.PubCompCode + "";

                var UnitList = _dropdownService.GetDropdownList(query);

                return Json(UnitList);
            }
        }

        public JsonResult GetDropdown(string type)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            string query = "";

            switch (type)
            {
                case "DocType":
                    query = "Select Code,Name from DOCTYPE_MAST where DOCTYPE in ('MiscConsumption') order by Name";
                    break;

                case "Party":
                    query = $"Select CODE, name from SUBGROUP_MAST where ACTIVE=1 AND COMP_CODE={getdata.PubCompCode} order by name";
                    break;
            }

            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }

        public JsonResult GetAddressByPartyCode(int PartyId)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string query = @"  SELECT   b.ADD1,  b.ADD2, b.ADD3  FROM SUBGROUP_MAST AS a
                                LEFT JOIN SUBGROUP_ADDRESS AS b ON b.CODE = a.CODE AND b.COMP_CODE = a.COMP_CODE  WHERE  a.code = @PartyId    and  a.COMP_CODE = @CompCode     AND a.ACTIVE = 1;  ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@PartyId", PartyId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dataList.Add(new
                            {
                                Add1 = reader["Add1"].ToString(),
                                Add2 = reader["Add2"].ToString(),
                                Add3 = reader["Add3"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(dataList);
        }

        //public JsonResult GetVNo(string Vtype)
        //{
        //    string newV_NO = "00000";

        //    try
        //    {
        //        var getdata = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            con.Open();

        //            // Get PREFIXYR from YEAR_MAST table
        //            string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
        //            using (SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con))
        //            {
        //                prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
        //                string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

        //                // Fetch last V_NO from GATE1
        //                string lastV_NO_Query = @"
        //                        SELECT MAX(CAST(V_NO AS INT)) 
        //                        FROM GATE1 
        //                        WHERE COMP_CODE = @CompCode 
        //                        AND YEAR_CODE = @YearCode 
        //                        AND BRANCH_CODE = @BranchCode 
        //                        AND V_TYPE = @Vtype";

        //                using (SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con))
        //                {
        //                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
        //                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
        //                    lastVnoCmd.Parameters.AddWithValue("@BranchCode", 1);
        //                    lastVnoCmd.Parameters.AddWithValue("@Vtype", Vtype);

        //                    object result = lastVnoCmd.ExecuteScalar();

        //                    if (result != DBNull.Value && result != null)
        //                    {
        //                        int lastV_NO = Convert.ToInt32(result);
        //                        newV_NO = (lastV_NO + 1).ToString("D5");
        //                    }
        //                    else
        //                    {
        //                        newV_NO = prefixYR + "00001";
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
        //        return Json(new { error = "An error occurred while generating the V_NO." });
        //    }

        //    return Json(new { V_NO = newV_NO });
        //}

        [HttpGet]
        public JsonResult GetVNo(string Vtype)
        {
            try
            {
                var vno = _repository.GenerateVNo(Vtype);

                return Json(new
                {
                    success = true,
                    V_NO = vno
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public IActionResult SaveData([FromBody] MiscConsumptionEntryModel request)
        {
            if (request?.Header == null)
            {
                return Json(new
                {
                    success = false, message = "Invalid request data"
                });
            }

            try
            {
                var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";

                var result = _repository.SaveMiscConsumption(
                    request.Header,
                    request.Deatils,
                    action
                );

                if (result == "Success")
                {
                    return Json(new
                    {
                        success = true,
                        message = "Saved successfully"
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        //[HttpPost]
        //public IActionResult SavedData([FromBody] MiscConsumptionEntryModel request)
        //{
        //    if (request?.Header == null)
        //    {

        //        return Json(new { success = false, message = "Input model is null" });
        //    }

        //    var action = request.Header.action == "INSERT" ? "INSERT" : "UPDATE";
        //    var result = SubmitRequest(request.Header, request.Deatils, action);

        //    return result == "Success"
        //        ? Json(new { success = true })
        //        : Json(new { success = false, message = result });
        //}

        //private string SubmitRequest(MiscConsumptionEntry_Header header, List<Details> details, string action)
        //{
        //    try
        //    {
        //        var g = _globalVariableService.GetGlobalVariables();
        //        using var conn = _dbConnection.GetErpConnection();
        //        conn.Open();

        //        Delete from PREQUEST2
        //        string deletePRequest2Sql = @"
        //            DELETE FROM GATE2 
        //            WHERE COMP_CODE = @CompCode 
        //            AND V_NO = @VNo 
        //            AND BRANCH_CODE = @BranchCode 
        //            AND YEAR_CODE = @YearCode;";

        //        using (var deletePRequest2Cmd = conn.CreateCommand())
        //        {
        //            deletePRequest2Cmd.CommandText = deletePRequest2Sql;
        //            deletePRequest2Cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
        //            deletePRequest2Cmd.Parameters.AddWithValue("@VNo", header.V_NO);
        //            deletePRequest2Cmd.Parameters.AddWithValue("@BranchCode", 1);
        //            deletePRequest2Cmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
        //            deletePRequest2Cmd.ExecuteNonQuery();
        //        }

        //        conn.Close();

        //        conn.Open();

        //        using (var cmd = new SqlCommand("sp_MiscConsumptionEntry", conn))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@Action", action);
        //            cmd.Parameters.AddWithValue("@SaveAction", "Header");
        //            cmd.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE) + header.V_NO);
        //            cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
        //            cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
        //            cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
        //            cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
        //            cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
        //            cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
        //            cmd.Parameters.AddWithValue("@V_TIME", header.V_TIME);
        //            cmd.Parameters.AddWithValue("@RETURN_DATE", header.RETURN_DATE);
        //            cmd.Parameters.AddWithValue("@RESPONSIBLE_PERSON", header.RESPONSIBLE_PERSONB);
        //            cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
        //            cmd.Parameters.AddWithValue("@PARTY_NAME", header.PARTY_NAME);
        //            cmd.Parameters.AddWithValue("@TRUCK_NO", header.TRUCK_NO);
        //            cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO);
        //            cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS);
        //            cmd.Parameters.AddWithValue("@ADD1", header.Add1);
        //            cmd.Parameters.AddWithValue("@ADD2", header.Add2);
        //            cmd.Parameters.AddWithValue("@ADD3", header.Add3);
        //            cmd.Parameters.AddWithValue("@PARTY_CITY", header.PARTY_CITY);
        //            cmd.Parameters.AddWithValue("@PARTY_GST", header.PARTY_GST);
        //            cmd.Parameters.AddWithValue("@PARTY_PINCODE", header.PARTY_PINCODE);
        //            cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", header.PARTY_ADDRESSID);
        //            cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE);
        //            cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
        //            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //            cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
        //            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //            cmd.Parameters.AddWithValue("@AED", "A");
        //            cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
        //            cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
        //            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
        //            cmd.ExecuteNonQuery();
        //        }

        //        foreach (var Details in details)
        //        {
        //            if (string.IsNullOrWhiteSpace(Details.ITEM_NAME))
        //                continue;

        //            using var cmd3 = new SqlCommand("sp_MiscConsumptionEntry", conn) { CommandType = CommandType.StoredProcedure };
        //            cmd3.Parameters.AddWithValue("@Action", "INSERT");
        //            cmd3.Parameters.AddWithValue("@SaveAction", "Details");

        //            cmd3.Parameters.AddWithValue("@DOC_ID", (header.V_TYPE) + header.V_NO);
        //            cmd3.Parameters.AddWithValue("@V_NO", header.V_NO);
        //            cmd3.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
        //            cmd3.Parameters.AddWithValue("@V_DATE", header.V_DATE);
        //            cmd3.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
        //            cmd3.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
        //            cmd3.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
        //            cmd3.Parameters.AddWithValue("@ITEM_CODE", Details.ITEM_CODE);
        //            cmd3.Parameters.AddWithValue("@ITEM_NAME", Details.ITEM_NAME);
        //            cmd3.Parameters.AddWithValue("@DEPT_CODE", Details.DEPT_CODE);
        //            cmd3.Parameters.AddWithValue("@UOM_CODE", Details.UOM_CODE);
        //            cmd3.Parameters.AddWithValue("@UOM_NAME", Details.UOM_NAME);
        //            cmd3.Parameters.AddWithValue("@NOS", Details.NOS);
        //            cmd3.Parameters.AddWithValue("@QTY", Details.QTY);
        //            cmd3.Parameters.AddWithValue("@REMARKS", Details.REMARKS);
        //            cmd3.Parameters.AddWithValue("@REF_NO", header.V_NO);
        //            cmd3.Parameters.AddWithValue("@REF_TYPE", header.V_TYPE);
        //            cmd3.Parameters.AddWithValue("@UUSER", g.PubUserId);
        //            cmd3.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //            cmd3.Parameters.AddWithValue("@EUSER", g.PubUserId);
        //            cmd3.Parameters.AddWithValue("@EDATE", DBNull.Value);
        //            cmd3.Parameters.AddWithValue("@AED", "A");
        //            cmd3.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
        //            cmd3.Parameters.AddWithValue("@LIP", g.PubLocalId);
        //            cmd3.Parameters.AddWithValue("@LID", Environment.MachineName);
        //            cmd3.ExecuteNonQuery();
        //        }
        //        return "Success";
        //    }
        //    catch (Exception ex)
        //    {
        //        return $"Error: {ex.Message}";
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("VISITOR", vdate, vtype, vno);
            Console.WriteLine("vdate: " + vdate);
            Console.WriteLine("today: " + DateTime.Today);
            Console.WriteLine("LoginDate: " + global.PubLoginDate.Date);
            return Ok(result);
        }
    }
}
