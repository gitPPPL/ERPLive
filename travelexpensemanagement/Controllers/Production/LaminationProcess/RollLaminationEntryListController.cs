using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.LaminationProcess;

namespace travelexpensemanagement.Controllers.Production.LaminationProcess
{
    [SessionAuthorize]
    public class RollLaminationEntryListController : Controller
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        public RollLaminationEntryListController(GlobalVariableService globalVariableService, DataBaseConnection dbConnection)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
        }
        public IActionResult Index()
        {
            return View("~/Views/Production/LaminationProcess/RollLaminationEntryList/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetAllRollLaminationList(int pageNumber = 1, int pageSize = 10, string searchTerm = "")
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var RollLaminationList = new List<RollLaminationListModel>();
            int totalCount = 0;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_RollLamination", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "SELECT");
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                RollLaminationList.Add(new RollLaminationListModel
                                {
                                    vNo = reader["VNo"] != DBNull.Value ? Convert.ToInt32(reader["VNo"]) : null,
                                    vType = reader["VType"]?.ToString(),
                                    vDate = reader["VDate"] != DBNull.Value ? Convert.ToDateTime(reader["VDate"]) : null,
                                    itemName = reader["ItemName"]?.ToString(),
                                    rollNo = reader["RollNo"]?.ToString(),
                                    meter = reader["Meter"] != DBNull.Value ? Convert.ToInt32(reader["Meter"]) : null,
                                    grossWeight = reader["GrossWt"] != DBNull.Value ? Convert.ToDecimal(reader["GrossWt"]) : null,
                                    netWeight = reader["NetWt"] != DBNull.Value ? Convert.ToDecimal(reader["NetWt"]) : null,
                                    averageWeight = reader["AvgWt"] != DBNull.Value ? Convert.ToDecimal(reader["AvgWt"]) : null,
                                    gram = reader["Gram"] != DBNull.Value ? Convert.ToDecimal(reader["Gram"]) : null,
                                    rollNoLam = reader["RollNoLam"]?.ToString(),
                                    meterLam = reader["MeterLam"] != DBNull.Value ? Convert.ToInt32(reader["MeterLam"]) : null,
                                    netWeightLam = reader["NetWtLam"] != DBNull.Value ? Convert.ToDecimal(reader["NetWtLam"]) : null,
                                    averageWeightLam = reader["AvgWtLam"] != DBNull.Value ? Convert.ToDecimal(reader["AvgWtLam"]) : null,
                                    gramLam = reader["GramLam"] != DBNull.Value ? Convert.ToDecimal(reader["GramLam"]) : null,
                                    size = reader["Size"] != DBNull.Value ? Convert.ToDecimal(reader["Size"]) : null,
                                    sizeLam = reader["SizeLam"] != DBNull.Value ? Convert.ToDecimal(reader["SizeLam"]) : null,
                                    tenacity = reader["Tenacity"].ToString(),
                                    place = reader["PlaceName"]?.ToString()
                                });
                            }
                            if (reader.NextResult())
                            {
                                reader.Read();
                                totalCount = (int)reader["TotalCount"];
                            }
                        }
                        return Json(new { success = true, rollLaminationList = RollLaminationList, totalCount });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult DeleteRollLamination(int vNo)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_RollLamination", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Delete");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.ExecuteNonQuery();
                    }
                    return Json(new { success = true, message = "Deleted Successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = true, message = ex.Message });
            }
        }
    }
}
