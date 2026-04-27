using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sale;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class ScrapSaleSaudaListController : Controller
    {




        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ScrapSaleSaudaListController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbcontext;
            _globalVariableService = globalValue;
            _moduleService = moduleService;
        }





        public IActionResult Index()
        {
            return View("~/Views/Sales/Transaction/ScrapSaleSaudaList/Index.cshtml");
        }


        public IActionResult GetDataList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var ScrapSaleSauda_Model = new List<ScrapSaleSauda_Model>();
            int totalCount = 0;

            try
            {
                using (var conn = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_Sale_Sauda_Entry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                    cmd.Parameters.AddWithValue("@DOC_ID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@V_TYPE", "Saud");

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ScrapSaleSauda_Model.Add(new ScrapSaleSauda_Model
                            {

                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : (DateTime?)null,
                                CustomerName = reader["CustomerName"] != DBNull.Value ? reader["CustomerName"].ToString() : null,
                                doc_id = reader["doc_id"] != DBNull.Value ? reader["doc_id"].ToString() : null,
                                CityName = reader["City"] != DBNull.Value ? reader["City"].ToString() : null,
                                PHONE = reader["PHONE"] != DBNull.Value ? reader["PHONE"].ToString() : null,
                                DeliveryTo = reader["DeliveryTo"] != DBNull.Value ? reader["DeliveryTo"].ToString() : null,
                                ItemName = reader["ItemName"] != DBNull.Value ? reader["ItemName"].ToString() : null,
                                Type = reader["Type"] != DBNull.Value ? reader["Type"].ToString() : null,
                                PINO = reader["PINO"] != DBNull.Value ? reader["PINO"].ToString() : null,
                                REMARK = reader["REMARK"] != DBNull.Value ? reader["REMARK"].ToString() : null,


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

            return Json(new { success = true, lists = ScrapSaleSauda_Model, totalCount });
        }



    }
}
