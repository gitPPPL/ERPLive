using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using TravelExpenseManagement.Models.Admin.Utilities;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class GramSizeConversionMasterListController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private int? userLevel;
        public GramSizeConversionMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/GramSizeConversionMasterList/Index.cshtml");
        }


        public IActionResult GetList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var SaveTempMasterRequest = new List<SaveTempMasterRequest>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_PAY_GRAMSIZECONV", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);

                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SaveTempMasterRequest.Add(new SaveTempMasterRequest
                            {
                                CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                ItemType = reader["ITEM_TYPE"] != DBNull.Value ? reader["ITEM_TYPE"].ToString() : string.Empty,
                             
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching categories", error = ex.Message });
            }

            return Json(new { success = true, lists = SaveTempMasterRequest, totalCount });
        }


        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            SaveTempMasterRequest saveTempMasterRequest = null;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_GRAMSIZECONV", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ShowData");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);

                        con.Open();
                        using (SqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                saveTempMasterRequest = new SaveTempMasterRequest();
                                saveTempMasterRequest.tableData = new List<GramSizeConversionMaster_Model>();

                                while (rdr.Read())
                                {
                                    if (saveTempMasterRequest.CODE == null)
                                        saveTempMasterRequest.CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : (int?)null;

                                    if (saveTempMasterRequest.CAT_CODE == null)
                                        saveTempMasterRequest.CAT_CODE = rdr["CAT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CAT_CODE"]) : (int?)null;

                                    if (saveTempMasterRequest.ItemType == null)
                                        saveTempMasterRequest.ItemType = rdr["ITEM_TYPE"] != DBNull.Value ? rdr["ITEM_TYPE"].ToString() : null;

                                    // Add row to tableData
                                    var row = new GramSizeConversionMaster_Model
                                    {
                                        FromSize = rdr["FROM_SIZE"] != DBNull.Value ? Convert.ToDecimal(rdr["FROM_SIZE"]) : (decimal?)null,
                                        ToSize = rdr["TO_SIZE"] != DBNull.Value ? Convert.ToDecimal(rdr["TO_SIZE"]) : (decimal?)null,
                                        Per = rdr["PER"] != DBNull.Value ? Convert.ToDecimal(rdr["PER"]) : (decimal?)null
                                    };

                                    saveTempMasterRequest.tableData.Add(row);
                                }
                            }
                        }
                    }
                }

                return Json(new { success = true, data = saveTempMasterRequest });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching data", error = ex.Message });
            }
        }


        [HttpPost]
        public JsonResult Delete(int code)
        {
            var globalvariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_PAY_GRAMSIZECONV", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalvariable.PubCompCode);


                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Gram Size Conversion Master deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting Gram Size Conversion Master.", error = ex.Message });
            }
        }



    }
}
