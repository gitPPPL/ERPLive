using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.QualityMaster;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class TempratureMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly ITemperatureMasterListRepository _temperatureMasterListRepository;
        public TempratureMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
         DropdownService dropdownService, DbHelper dbHelper, ITemperatureMasterListRepository temperatureMasterListRepository,
         ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _temperatureMasterListRepository = temperatureMasterListRepository;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/TempratureMasterList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetTemperatureList(string searchTerm = "", int pageNumber = 1,int pageSize = 10)
        {
            try
            {
                var result = _temperatureMasterListRepository.GetTemperatureList(searchTerm, pageNumber, pageSize);

                return Json(new
                {
                    success = true,
                    lists = result.Data,
                    totalCount = result.TotalCount
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching categories",
                    error = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetCategoryCode(int code)
        {
            try
            {
                var data = _temperatureMasterListRepository.GetCategoryCode(code);

                return Json(new
                {
                    success = true,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error fetching bank",
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public JsonResult Delete(int code)
        {
            try
            {
                _temperatureMasterListRepository.Delete(code);

                return Json(new
                {
                    success = true,
                    message = "Temprature Master deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error deleting category.",
                    error = ex.Message
                });
            }
        }
        
        [HttpGet]
        public IActionResult ExportAllDocs()
        {
            try
            {
                var fileBytes = _temperatureMasterListRepository.ExportAllDocs();

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"TemperatureMaster_{DateTime.Now:ddMMyyyy}.xlsx"
                );
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

        //public IActionResult GetTemperatureList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode; 
        //    var TempratureMasterModel = new List<TempratureMasterModel>();
        //    int totalCount = 0;

        //    try
        //    {
        //        using (SqlConnection conn = _dbConnection.GetErpConnection())
        //        using (SqlCommand cmd = new SqlCommand("sp_TempratureMaster", conn))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            cmd.Parameters.AddWithValue("@Action", "SELECT");
        //            cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
        //            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        //            cmd.Parameters.AddWithValue("@PageSize", pageSize);
        //            cmd.Parameters.AddWithValue("@COMP_CODE", compCode); 
        //            cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

        //            conn.Open();
        //            using (SqlDataReader reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    TempratureMasterModel.Add(new TempratureMasterModel
        //                    {
        //                        CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
        //                        Name = reader["NAME"] != DBNull.Value ? reader["NAME"].ToString() : string.Empty,
        //                        ShortName = reader["SHORTNAME"] != DBNull.Value ? reader["SHORTNAME"].ToString() : string.Empty,
        //                        SortNo = reader["SORT_NO"] != DBNull.Value ? Convert.ToInt32(reader["SORT_NO"]) : 0,
        //                        Active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
        //                    });
        //                }

        //                if (reader.NextResult() && reader.Read())
        //                {
        //                    totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error fetching categories", error = ex.Message });
        //    }

        //    return Json(new { success = true, lists = TempratureMasterModel, totalCount });
        //}

        //[HttpGet]
        //public IActionResult GetCategoryCode(int code)
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        //    TempratureMasterModel TempratureMasterModel = null;

        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_TempratureMaster", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@Action", "SELECT");
        //                cmd.Parameters.AddWithValue("@CODE", code);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

        //                con.Open();
        //                using (SqlDataReader rdr = cmd.ExecuteReader())
        //                {
        //                    if (rdr.Read())
        //                    {
        //                        TempratureMasterModel = new TempratureMasterModel
        //                        {
        //                            CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
        //                            Name = rdr["NAME"] != DBNull.Value ? rdr["NAME"].ToString() : null,
        //                            ShortName = rdr["SHORTNAME"] != DBNull.Value ? rdr["SHORTNAME"].ToString() : null,
        //                            SortNo = rdr["SORT_NO"] != DBNull.Value ? Convert.ToInt32(rdr["SORT_NO"]) : 0,
        //                            UUser = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
        //                            UDate = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
        //                            EUser = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
        //                            EDate = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
        //                            Aed = rdr["AED"] != DBNull.Value ? rdr["AED"].ToString() : null,
        //                            Wsid = rdr["WSID"] != DBNull.Value ? rdr["WSID"].ToString() : null,
        //                            Lip = rdr["LIP"] != DBNull.Value ? rdr["LIP"].ToString() : null,
        //                            Lid = rdr["LID"] != DBNull.Value ? rdr["LID"].ToString() : null,
        //                            Active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
        //                            VType = rdr["V_TYPE"] != DBNull.Value ? rdr["V_TYPE"].ToString() : null

        //                        };
        //                    }
        //                }
        //            }
        //        }

        //        return Json(new { success = true, data = TempratureMasterModel });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error fetching bank", error = ex.Message });
        //    }
        //}

        //[HttpPost]
        //public JsonResult Delete(int code)
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_TempratureMaster", con)) // Use your actual SP name
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@Action", "DELETE");
        //                cmd.Parameters.AddWithValue("@CODE", code);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

        //                con.Open();
        //                cmd.ExecuteNonQuery();
        //            }
        //        }

        //        return Json(new { success = true, message = " Temprature Master deleted successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error deleting  category.", error = ex.Message });
        //    }
        //}

        //[HttpGet]
        //public IActionResult ExportAllDocs()
        //{
        //    try
        //    {
        //        var gv = _globalVariableService.GetGlobalVariables();

        //        var parameters = new Dictionary<string, object>
        //        {
        //            { "@COMP_CODE", gv.PubCompCode },
        //            { "@Action", "Excel" }
        //        };

        //        var fileBytes = _globalValidationdate.ExportToExcel("sp_TempratureMaster", "Temprature Master", parameters);

        //        return File(
        //            fileBytes,
        //            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //            $"TemperatureMaster_{DateTime.Now:ddMMyyyy}.xlsx"
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

    }
}
