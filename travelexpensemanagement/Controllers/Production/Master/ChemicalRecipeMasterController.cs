using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Reflection.Metadata;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Master.ChemicalRecipe;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace travelexpensemanagement.Controllers.Production.Master
{
    public class ChemicalRecipeMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public ChemicalRecipeMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/Production/Master/ChemicalRecipeMaster/Index.cshtml");
        }

        [HttpGet]
        public IActionResult DocTypeDropdown()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from DOCTYPE_MAST where DOCTYPE='ChemicalReceipe'";
            var docType = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = docType });
        }

        [HttpGet]
        public IActionResult PlaceDropDown()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from ITEMDEPT_MAST where COMP_CODE=1 and TRAN_TYPE='Production' order by name";
            var placeList = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = placeList });
        }

        [HttpGet]
        public IActionResult ChemicalNameDropDown()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select a.CODE,a.NAME from ITEM_MAST a left join ITEM_MGROUP b on a.MGROUP_CODE=b.CODE and b.COMP_CODE=1 where a.Active=1 and a.COMP_CODE=1 and b.MGROUP_TYPE in ('Store') order by a.NAME";
            var chemicalList = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = chemicalList });
        }

        public JsonResult GenerateVNo(string vType)
        {
            string newV_NO = "00001";

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

                   
                    string lastV_NO_Query = @"SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1 FROM PROD_RECIPE1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE";
                  
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);

                    lastVnoCmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", vType);

                    int nextNo = Convert.ToInt32(lastVnoCmd.ExecuteScalar());
                    string runningPart = nextNo.ToString("D5");
                    newV_NO = prefixYR + runningPart;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GenerateVNo: {ex.Message}");
                return Json(new { success = false, message = "Error generating V_NO" });
            }

            return Json(new { success = true, v_NO = newV_NO });
        }

        [HttpPost]
        public IActionResult SaveAndUpdate([FromBody] ChemicalRecipeMaster model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            if (model == null)
            {
                return Json(new { success = false, message = "Model is null (binding failed)" });
            }
            try
            {
                bool isUpdate = !string.IsNullOrEmpty(model.DOC_ID);
                string docId = model.DOC_ID;

                if (!isUpdate)
                {
                    docId = model.V_TYPE + model.V_NO;
                }

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            SqlCommand cmd = new SqlCommand("sp_CHEMICAL_RECIPE_MASTER", con, tran);
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                            cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                            cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                            cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                            cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                            cmd.Parameters.AddWithValue("@DOC_ID", docId);
                            cmd.Parameters.AddWithValue("@DEPT_CODE", model.DEPT_CODE ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DEPT_NAME", model.DEPT_NAME ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            cmd.Parameters.AddWithValue("@Action", isUpdate ? "Update" : "InsertHeader");

                            cmd.ExecuteNonQuery();

                            // =========================
                            // 2️⃣ FOOTER SAVE
                            // =========================
                            if (model.Details != null && model.Details.Count > 0)
                            {
                                foreach (var item in model.Details)
                                {
                                    SqlCommand cmdDetail = new SqlCommand("sp_CHEMICAL_RECIPE_MASTER", con, tran);
                                    cmdDetail.CommandType = CommandType.StoredProcedure;

                                    cmdDetail.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                                    cmdDetail.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                                    cmdDetail.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                                    cmdDetail.Parameters.AddWithValue("@V_NO", model.V_NO);
                                    cmdDetail.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                    cmdDetail.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? DateTime.Now);
                                    cmdDetail.Parameters.AddWithValue("@DOC_ID", docId);

                                    cmdDetail.Parameters.AddWithValue("@ITEM_CODE", item.ITEM_CODE ?? (object)DBNull.Value);
                                    cmdDetail.Parameters.AddWithValue("@ITEM_NAME", item.ITEM_NAME ?? (object)DBNull.Value);
                                    cmdDetail.Parameters.AddWithValue("@PER", item.PER ?? (object)DBNull.Value);

                                    cmdDetail.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                                    cmdDetail.Parameters.AddWithValue("@LIP", globalVariable.PubLocalId);
                                    cmdDetail.Parameters.AddWithValue("@LID", Environment.MachineName);

                                    cmdDetail.Parameters.AddWithValue("@Action", "InsertFooter");

                                    cmdDetail.ExecuteNonQuery();
                                }
                            }
                            tran.Commit();

                            string message = isUpdate ? "Record Updated Successfully" : "Record Inserted Successfully";
                            return Json(new { success = true, message = message, isUpdate = isUpdate });
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return Json(new { success = false, error = ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult loadDataOnEdit(string docId)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new ChemicalRecipeMaster();
            var items = new List<ChemicalRecipeDetail>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_CHEMICAL_RECIPE_MASTER", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariable.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariable.PubFYearCode);
                    cmd.Parameters.AddWithValue("@Action", "Edit");

                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();

                    // ================= HEADER =================
                    if (reader.Read())
                    {
                        model = new ChemicalRecipeMaster
                        {
                            DOC_ID = reader["DOC_ID"]?.ToString(),
                            V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                            V_TYPE = reader["V_TYPE"]?.ToString(),
                            V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                            DEPT_CODE = reader["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["DEPT_CODE"]) : 0,
                            DEPT_NAME = reader["DEPT_NAME"]?.ToString()
                        };
                    }
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            items.Add(new ChemicalRecipeDetail
                            {
                                ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                                ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                PER = reader["PER"] != DBNull.Value ? Convert.ToDecimal(reader["PER"]) : 0
                            });
                        }
                    }
                }

                return Json(new { success = true, header = model, items = items });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
