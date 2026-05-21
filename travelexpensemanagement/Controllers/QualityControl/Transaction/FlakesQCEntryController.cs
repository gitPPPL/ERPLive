
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.QualityControl.Transaction
{
    public class FlakesQCEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public FlakesQCEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
          ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
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
            return View("~/Views/QualityControl/Transaction/FlakesQCEntry/Index.cshtml");
        }
        public JsonResult GetVNo()
        {
            string newV_NO = "00000";
            try
            {
                newV_NO = _globalValidationdate.GetVNo("SFQC", "PROD1_QC");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }
            return Json(new { V_NO = newV_NO });
        }
        public JsonResult DDLInspBy()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from EMP_MAST where Resign_Date is NULL and COMP_CODE= " + getdata.PubCompCode + "   ORDER BY name asc";
                var DDLInspBylist = _dropdownService.GetDropdownList(query);
                return Json(DDLInspBylist);
            }
        }
        public JsonResult DDLItem()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select a.Code,a.name from item_mast a " +
                    " left join ITEM_GROUP b on a.GROUP_CODE=b.CODE and b.COMP_CODE=" + getdata.PubCompCode + " and b.SALE_GROUP in ('Flakes')" +
                    " where a.Active=1 and a.comp_code= " + getdata.PubCompCode + "  and a.shortname <> '' group by a.NAME,a.CODE order by a.NAME asc";

                var DDLInspBylist = _dropdownService.GetDropdownList(query);

                return Json(DDLInspBylist);
            }

        }
        public JsonResult DDLPordPlace()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select Code,name from ITEMDEPT_MAST where Tran_type='Production' and Place_type='Washline' and COMP_CODE=" + getdata.PubCompCode + "  ";

                var DDLPordPlaceList = _dropdownService.GetDropdownList(query);

                return Json(DDLPordPlaceList);
            }

        }
        public JsonResult DDLChemist()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select code,Name from EMP_MAST WHERE Comp_code=" + getdata.PubCompCode + "  and Resign_date is null and Type in ('Staff','Semi Staff') Order by Name ";

                var DDLChemistList = _dropdownService.GetDropdownList(query);

                return Json(DDLChemistList);
            }

        }
        public JsonResult DDLQCIncharge()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select code,Name from EMP_MAST WHERE Comp_code=" + getdata.PubCompCode + " and Resign_date is null and Type in ('Staff') Order by Name ";

                var DDLQCInchargeList = _dropdownService.GetDropdownList(query);

                return Json(DDLQCInchargeList);
            }

        }
        public JsonResult DDLGridItem()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select a.Code,a.name from item_mast a left join ITEM_GROUP b on a.GROUP_CODE=b.CODE and b.COMP_CODE= " + getdata.PubCompCode + "     where a.Active=1 and b.SALE_GROUP in ('Flakes') " +
                    "and a.comp_code=" + getdata.PubCompCode + "  group by a.name,a.CODE order by a.name";

                var DDLGridItemList = _dropdownService.GetDropdownList(query);

                return Json(DDLGridItemList);
            }

        }

        [HttpPost]
        public IActionResult SavedData([FromBody] FlakesQCEntryLIst_Model request)
        {
            if (request?.Header == null)
                return Json(new { success = false, message = "Input model is null" });

            var action = request.Header.action == "INSERT" ? "INSERT" : "Update";
            var result = SubmitRequest(request.Header, request.Deatils, action);

            return result == "Success"
                ? Json(new { success = true })
                : Json(new { success = false, message = result });
        }
        private string SubmitRequest(FlakesQCEntryLIst_Header FlakesQCEntryLIst_Header, List<FlakesQCEntryList_Details> FlakesQCEntryList_Details, string action)
        {
            {
                try
                {
                    var g = _globalVariableService.GetGlobalVariables();
                    using var conn = _dbConnection.GetErpConnection();
                    conn.Open();

                    string deletePRequest2Sql = @"  DELETE FROM PROD2_QC  WHERE COMP_CODE = @CompCode  AND V_NO = @VNo   AND BRANCH_CODE = @BranchCode   AND YEAR_CODE = @YearCode ;";
                    using (var deletePRequest2Cmd = conn.CreateCommand())
                    {
                        deletePRequest2Cmd.CommandText = deletePRequest2Sql;
                        deletePRequest2Cmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        deletePRequest2Cmd.Parameters.AddWithValue("@VNo", FlakesQCEntryLIst_Header.V_NO);
                        deletePRequest2Cmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                        deletePRequest2Cmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                        deletePRequest2Cmd.ExecuteNonQuery();
                    }

                    conn.Close();
                    conn.Open();
                    using (var cmd = new SqlCommand("sp_FlakesQCEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "Header");
                        cmd.Parameters.AddWithValue("@v_NO", FlakesQCEntryLIst_Header.V_NO);
                        cmd.Parameters.AddWithValue("@DOC_ID", ("SFQC") + FlakesQCEntryLIst_Header.V_NO);
                        cmd.Parameters.AddWithValue("@V_DATE", FlakesQCEntryLIst_Header.V_DATE);
                        cmd.Parameters.AddWithValue("@V_TYPE", "SFQC");
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@QCTIME", FlakesQCEntryLIst_Header.QCTIME);
                        cmd.Parameters.AddWithValue("@EMP_CODE", FlakesQCEntryLIst_Header.EMP_CODE);
                        cmd.Parameters.AddWithValue("@SHIFT", FlakesQCEntryLIst_Header.SHIFT);
                        cmd.Parameters.AddWithValue("@PLACE_CODE", FlakesQCEntryLIst_Header.PLACE_CODE);
                        cmd.Parameters.AddWithValue("@QC_INCHARGE", FlakesQCEntryLIst_Header.QC_INCHARGE);
                        cmd.Parameters.AddWithValue("@QC_INCHARGENAME", FlakesQCEntryLIst_Header.QC_INCHARGENAME);
                        cmd.Parameters.AddWithValue("@CHEMIST", FlakesQCEntryLIst_Header.CHEMIST);
                        cmd.Parameters.AddWithValue("@CHEMISTNAME", FlakesQCEntryLIst_Header.CHEMISTNAME);
                        cmd.Parameters.AddWithValue("@REMARKS", FlakesQCEntryLIst_Header.REMARKS ?? "");
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

                    foreach (var d in FlakesQCEntryList_Details)
                    {
                        if (d.ITEM_CODE == 0)
                            continue;
                        using var cmd2 = new SqlCommand("sp_FlakesQCEntry", conn) { CommandType = CommandType.StoredProcedure };
                            cmd2.Parameters.AddWithValue("@Action", action);
                            cmd2.Parameters.AddWithValue("@SaveAction", "Details");
                            cmd2.Parameters.AddWithValue("@DOC_ID", ("SFQC") + FlakesQCEntryLIst_Header.V_NO);
                            cmd2.Parameters.AddWithValue("@V_NO", FlakesQCEntryLIst_Header.V_NO);
                            cmd2.Parameters.AddWithValue("@V_TYPE", "SFQC");
                            cmd2.Parameters.AddWithValue("@V_DATE", FlakesQCEntryLIst_Header.V_DATE);
                            cmd2.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                            cmd2.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                            cmd2.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                            cmd2.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                            cmd2.Parameters.AddWithValue("@COLOR_NAME", d.COLOR_NAME);
                            cmd2.Parameters.AddWithValue("@PTYPE_NAME", d.PTYPE_NAME);
                            cmd2.Parameters.AddWithValue("@WIDTH", d.WIDTH);
                            cmd2.Parameters.AddWithValue("@GRAM", d.WBWt);
                            cmd2.Parameters.AddWithValue("@RESULT1", d.RESULT1);
                            cmd2.Parameters.AddWithValue("@RESULT2", d.RESULT2);
                            cmd2.Parameters.AddWithValue("@PRKG", d.PRKG);
                            cmd2.Parameters.AddWithValue("@WASTE", d.WASTE);
                            cmd2.Parameters.AddWithValue("@DNR", d.DNR);
                            cmd2.Parameters.AddWithValue("@CPRDN", d.CPRDN);
                            cmd2.Parameters.AddWithValue("@TIME1_WIDTH", d.TIME1_WIDTH);
                            cmd2.Parameters.AddWithValue("@TIME2_WIDTH", d.TIME2_WIDTH);
                            cmd2.Parameters.AddWithValue("@TIME3_WIDTH", d.TIME3_WIDTH);
                            cmd2.Parameters.AddWithValue("@TIME4_WIDTH", d.TIME4_WIDTH);
                            cmd2.Parameters.AddWithValue("@TIME5_WIDTH", d.TIME5_WIDTH);
                            cmd2.Parameters.AddWithValue("@Remarks", d.REMARKS);
                            cmd2.Parameters.AddWithValue("@COLOR_CODE", d.COLOR_CODE);
                            cmd2.Parameters.AddWithValue("@SHIFT", FlakesQCEntryLIst_Header.SHIFT);
                            cmd2.Parameters.AddWithValue("@PLACE_CODE", FlakesQCEntryLIst_Header.PLACE_CODE);
                            cmd2.Parameters.AddWithValue("@EMP_CODE", FlakesQCEntryLIst_Header.EMP_CODE);
                            cmd2.Parameters.AddWithValue("@PC_LOWMELT", d.PC_LOWMELT);
                            cmd2.Parameters.AddWithValue("@GLUE_CONTENT", d.GLUE_CONTENT);
                            cmd2.Parameters.AddWithValue("@OTHERS", d.OTHERS);
                            cmd2.Parameters.AddWithValue("@GRADE", d.GRADE);
                            cmd2.Parameters.AddWithValue("@YELLOWP", d.YELLOWP);
                            cmd2.Parameters.AddWithValue("@BLUEP", d.BLUEP);
                            cmd2.Parameters.AddWithValue("@OTHERP", d.OTHERP);
                            cmd2.Parameters.AddWithValue("@YELLOW160C", d.YELLOW160C);
                            cmd2.Parameters.AddWithValue("@MOISTURE", d.MOISTURE);
                            cmd2.Parameters.AddWithValue("@BULKDENSITY", d.BULKDENSITY);
                            cmd2.Parameters.AddWithValue("@PH_FLAKES", d.PH_FLAKES);
                            cmd2.Parameters.AddWithValue("@OVERSIZED", d.OVERSIZED);
                            cmd2.Parameters.AddWithValue("@Pord_No", d.Pord_No);
                            cmd2.Parameters.AddWithValue("@Pord_Type", d.Pord_Type);
                            cmd2.Parameters.AddWithValue("@UUSER", g.PubUserId);
                            cmd2.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd2.Parameters.AddWithValue("@EUSER", g.PubUserId);
                            cmd2.Parameters.AddWithValue("@EDATE", DateTime.Now);
                            cmd2.Parameters.AddWithValue("@AED", "A");
                            cmd2.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                            cmd2.Parameters.AddWithValue("@LIP", g.PubLocalId);
                            cmd2.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd2.ExecuteNonQuery();

                        string updated = @"
                        Update PROD_SFG2 
                        set REF_TYPE = 'SFQC', REF_NO = @REF_NO 
                        where V_TYPE = @REfType  
                        and V_NO = @V_NO 
                        and COMP_CODE = @CompCode 
                        and BRANCH_CODE = @BranchCode;";

                        using (var UpdateProdSpgCmd = conn.CreateCommand())
                        {
                            UpdateProdSpgCmd.CommandText = updated; 
                            UpdateProdSpgCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                            UpdateProdSpgCmd.Parameters.AddWithValue("@V_NO", d.Refcode); 
                            UpdateProdSpgCmd.Parameters.AddWithValue("@REF_NO", FlakesQCEntryLIst_Header.V_NO); 
                            UpdateProdSpgCmd.Parameters.AddWithValue("@REfType", d.REfType);
                            UpdateProdSpgCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                            UpdateProdSpgCmd.ExecuteNonQuery();
                        }

                    }
                    return "Success";
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }

        }

    }
}
