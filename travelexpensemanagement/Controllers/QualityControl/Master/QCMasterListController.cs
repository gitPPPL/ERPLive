using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class QCMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;

        public QCMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     DropdownService dropdownService, DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/QCMasterList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetQCMasterLList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var QCMasterLList = new List<QCMasterList>();
            int totalCount = 0;
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Select");
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@NAME", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);

                    // Other unused parameters
                    cmd.Parameters.AddWithValue("@CODE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@SHORTNAME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@QCGROUP_CODE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@UDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                    cmd.Parameters.AddWithValue("@ACTIVE", DBNull.Value);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // First result: paginated data
                        while (reader.Read())
                        {
                            QCMasterLList.Add(new QCMasterList
                            {
                                Code = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                                Name = reader["NAME"]?.ToString(),
                                ShortName = reader["SHORTNAME"]?.ToString(),
                                QCGroup = reader["QCGROUP_CODE"]?.ToString(),
                                MaxPPM = reader["PPM"] != DBNull.Value ? Convert.ToString(reader["PPM"]) : null,
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                            });
                        }
                        // Second result: total count
                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
            return Json(new { groups = QCMasterLList, totalCount });
        }
    }
}
