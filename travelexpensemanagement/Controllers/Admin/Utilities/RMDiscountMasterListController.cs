using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class RMDiscountMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public RMDiscountMasterListController(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            DropdownService dropdownService,
            DbHelper dbHelper,
            travelexpensemanagement.ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/Utilities/RMDiscountMasterList/Index.cshtml");
        }
        [HttpGet]
        public IActionResult GetRMDiscountMasterList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            List<dynamic> list = new List<dynamic>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("USP_RMDISC_MAST", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                cmd.Parameters.AddWithValue("@SearchTerm",
                        string.IsNullOrWhiteSpace(searchTerm) ? DBNull.Value : searchTerm);
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@DTYPE", DBNull.Value);
                cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                conn.Open();
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new
                        {
                            CODE = Convert.ToInt32(dr["CODE"]),
                            SItem = dr["SItem"],
                            ICode = dr["ICode"],
                            SaudaItemName = dr["Sauda Item"],   
                            ItemName = dr["Item Name"],        
                            EffectFrom = Convert.ToDateTime(dr["Effect From"]).ToString("yyyy-MM-dd"),
                            Rate = dr["Rate"],
                            Remarks = dr["Remarks"],
                            AbovePer = dr["Above%"],
                            AboveAmt = dr["AboveAmt"],
                            UUSER = dr["UUSER"],                 
                            UDATE = dr["UDATE"]
                        });
                    }
                    if (dr.NextResult() && dr.Read())
                    {
                        totalCount = Convert.ToInt32(dr["TotalRecords"]);
                    }
                }
            }
            return Json(new { items = list, totalCount });
        }

        [HttpPost]
        public IActionResult Delete(int code)
        {
            try
            {
                var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("USP_RMDISC_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@CODE", code);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Record Deleted Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}
