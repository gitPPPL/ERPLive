using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection.Emit;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;
using travelexpensemanagement.Models.QualityControl.Master;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class QCMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;

        public QCMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     DropdownService dropdownService, DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/QCMaster/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetDropdown(string type)
        {
            string query = "";
  
            switch (type)
            {
                case "QCGroup":
                    query = $@"Select Code, Name From QCG_MAST order by Name asc";
                    break;
            }
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetddlParameter()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = $@"Select a.CODE as Code, a.name as Name, b.NAME as Unit, b.CODE as Ucode from QCP_MAST a left join QCPUNIT_MAST b on a.QUNIT_CODE =b.code where a.comp_code={compCode}";
            var dataList = await _dbHelper.GetJsonDataAsync(query);
            return Json(new { success = true, data = dataList });
        }

        [HttpGet]
        public JsonResult getExistOrNot(string inputData)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            try
            {
                bool isExist = false;

                using (var con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST"))
                    {
                        cmd.Connection = con;
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "Exist");
                        cmd.Parameters.AddWithValue("@Name", inputData);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }

                return Json(new { status = true, exists = isExist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data check failed: " + ex.Message });
            }
        }

        private int GetLastCode()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            int maxCode = 0;
            const string sql = @"SELECT ISNULL(MAX(CAST(Code AS INT)), 0) + 1 FROM QC_MAST WHERE COMP_CODE = @COMP_CODE";

            using (var conn = _dbConnection.GetErpConnection())
            {
                conn.Open();
                using (var cmd = new SqlCommand(sql,conn))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                    var result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out int val))
                    {
                        maxCode = val;
                    }
                }
            }
            return maxCode;
        }

        [HttpPost]
        public async Task<IActionResult> InsertDataQcMaster([FromBody] QCMaster model)
        {
            int code = 0;

            if (model == null)
            {
                return Json(new { success = false, message = "Invalid Data!" });
            }

            code = GetLastCode();
            if(code <= 0)
            {
                return Json(new { success = false, message = "Invalid Code!" });
            }
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        var globalVar = _globalVariableService.GetGlobalVariables();

                        // Insert into QC_MAST (Header)
                        using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", code);
                            cmd.Parameters.AddWithValue("@NAME", model.Name ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHORTNAME", model.ShortName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@QCGROUP_CODE", model.QCGroup);
                            cmd.Parameters.AddWithValue("@ACTIVE", model.active);
                            cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PPM", model.MaxPPM);
                            cmd.Parameters.AddWithValue("@Action", "Insert");

                            await cmd.ExecuteNonQueryAsync();
                        }
                        int srno = 1;
                        // Insert into QC_MAST1 (Details)
                        foreach (var detail in model.Details)
                        {
                            using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST1", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", code);
                                cmd.Parameters.AddWithValue("@QCP_CODE", detail.Parameter);
                                cmd.Parameters.AddWithValue("@QCP_UNIT", detail.Unit);
                                cmd.Parameters.AddWithValue("@QCP_STD", detail.StdResult);
                                cmd.Parameters.AddWithValue("@DEDUCT_QTY", detail.DeductQty ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDUCT_TYPE", detail.DeductType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", detail.Remarks ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SRNO", srno);
                                cmd.Parameters.AddWithValue("@MOBILE_APP", "NO");
                                cmd.Parameters.AddWithValue("@PPM_YN", detail.Ppm == "YES" ? "YES" : "NO");
                                cmd.Parameters.AddWithValue("@BASE_PRICE", detail.BasePrice);
                                cmd.Parameters.AddWithValue("@Action", "Insert");

                                await cmd.ExecuteNonQueryAsync();
                                srno++;
                            }
                        }
                        // Commit transaction if all succeed
                        transaction.Commit();
                        return Json(new { success = true, message = "Insert successful." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback everything if anything fails
                        return StatusCode(500, $"Transaction failed: {ex.Message}");
                    }
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDataQcMaster([FromBody] QCMaster model)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        var globalVar = _globalVariableService.GetGlobalVariables();

                        // Insert into QC_MAST (Header)
                        using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST", con, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", model.code);
                            cmd.Parameters.AddWithValue("@NAME", model.Name ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@SHORTNAME", model.ShortName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@QCGROUP_CODE", model.QCGroup);
                            cmd.Parameters.AddWithValue("@ACTIVE", model.active);
                            cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                            cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                            cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                            cmd.Parameters.AddWithValue("@AED", "A");
                            cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PPM", model.MaxPPM);
                            cmd.Parameters.AddWithValue("@Action", "Update");

                            await cmd.ExecuteNonQueryAsync();
                        }
                        // Insert into QC_MAST1 (Details)
                        using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM QC_MAST1 WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE", con, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            deleteCmd.Parameters.AddWithValue("@CODE", model.code);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        int srno = 1;
                        foreach (var detail in model.Details)
                        {
                            using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST1", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", model.code);
                                cmd.Parameters.AddWithValue("@QCP_CODE", detail.Parameter);
                                cmd.Parameters.AddWithValue("@QCP_UNIT", detail.Unit);
                                cmd.Parameters.AddWithValue("@QCP_STD", detail.StdResult);
                                cmd.Parameters.AddWithValue("@DEDUCT_QTY", detail.DeductQty ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDUCT_TYPE", detail.DeductType ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", detail.Remarks ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SRNO", srno);
                                cmd.Parameters.AddWithValue("@MOBILE_APP", "NO");
                                cmd.Parameters.AddWithValue("@PPM_YN", detail.Ppm == "YES" ? "YES" : "NO");
                                cmd.Parameters.AddWithValue("@BASE_PRICE", detail.BasePrice);
                                cmd.Parameters.AddWithValue("@Action", "Insert");

                                await cmd.ExecuteNonQueryAsync();
                                srno++;
                            }
                        }
                        // Commit transaction if all succeed
                        transaction.Commit();
                        return Json(new { success = true, message = "Insert successful." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollback everything if anything fails
                        return StatusCode(500, $"Transaction failed: {ex.Message}");
                    }
                }
            }
        }
        
        [HttpPost]
        public async Task<JsonResult> SaveDeductRates([FromBody] List<DeductRateModel> rates)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        var globalVar = _globalVariableService.GetGlobalVariables();
                        if (rates == null || !rates.Any())
                            return Json(new { success = false, message = "No rate data provided." });
                        int code = Convert.ToInt32(rates.First().Code);
                        int qcpCode = Convert.ToInt32(rates.First().nextQcpCode);
                        using (SqlCommand deleteCmd = new SqlCommand(@"
                    DELETE FROM QC_MAST2 
                    WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE AND QCP_CODE = @QCP_CODE", con, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            deleteCmd.Parameters.AddWithValue("@CODE", code);
                            deleteCmd.Parameters.AddWithValue("@QCP_CODE", qcpCode);
                            await deleteCmd.ExecuteNonQueryAsync();
                        }
                        foreach (var rate in rates)
                        {
                            int srno = new Random().Next(1000, 9999); // Replace with proper logic if needed

                            using (SqlCommand cmd = new SqlCommand("Insert_QC_MAST2", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", Convert.ToInt32(rate.Code));
                                cmd.Parameters.AddWithValue("@QCP_CODE", Convert.ToInt32(rate.nextQcpCode));
                                cmd.Parameters.AddWithValue("@FROM_RESULT", rate.From);
                                cmd.Parameters.AddWithValue("@TO_RESULT", rate.To);
                                cmd.Parameters.AddWithValue("@DEDUCT_TYPE", rate.Type ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEDUCT_RATE", rate.Rate);
                                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                cmd.Parameters.AddWithValue("@AED", "A");
                                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@SRNO", srno);
                                cmd.Parameters.AddWithValue("@DED_TYPE", rate.Type ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Action", "Insert");
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();
                        return Json(new { success = true, message = "Rates saved successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = $"Error: {ex.Message}" });
                    }
                }
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> CheckDeductRates([FromBody] CheckDeductRateRequest request)
        {
            if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.ParameterId))
            {
                return BadRequest("Invalid input parameters.");
            }
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var deductRates = new List<DeductRateModelList>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                string query = @"
            SELECT FROM_RESULT, TO_RESULT, DEDUCT_TYPE, DEDUCT_RATE 
            FROM QC_MAST2 
            WHERE CODE = @CODE AND COMP_CODE = @COMP_CODE AND QCP_CODE = @QCP_CODE";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CODE", int.Parse(request.Code));
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@QCP_CODE", int.Parse(request.ParameterId));
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            deductRates.Add(new DeductRateModelList
                            {
                                FromResult = reader["FROM_RESULT"] != DBNull.Value ? Convert.ToDecimal(reader["FROM_RESULT"]) : (decimal?)null,
                                ToResult = reader["TO_RESULT"] != DBNull.Value ? Convert.ToDecimal(reader["TO_RESULT"]) : (decimal?)null,
                                DeductType = reader["DEDUCT_TYPE"]?.ToString(),
                                DeductRate = reader["DEDUCT_RATE"] != DBNull.Value ? Convert.ToDecimal(reader["DEDUCT_RATE"]) : (decimal?)null,
                            });
                        }
                    }
                }
            }
            return Json(deductRates);
        }
        
        [HttpPost]
        public JsonResult GetQCMasterListByCode([FromBody] CodeRequest request)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var qcMaster = new QCMaster();
            qcMaster.Details = new List<DetailModel>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string masterQuery = @"
            SELECT CODE, NAME, SHORTNAME, QCGROUP_CODE, ACTIVE,PPM
            FROM QC_MAST
            WHERE CODE = @CODE AND COMP_CODE = @COMP_CODE";

                using (SqlCommand cmd = new SqlCommand(masterQuery, con))
                {
                    cmd.Parameters.AddWithValue("@CODE", request.code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            qcMaster = new QCMaster
                            {
                                Name = rdr["NAME"]?.ToString(),
                                ShortName = rdr["SHORTNAME"]?.ToString(),
                                //QCGroup = rdr["QCGROUP_CODE"]?.ToString(),
                                QCGroup = rdr["QCGROUP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["QCGROUP_CODE"]) : 0,
                                MaxPPM = rdr["PPM"] != DBNull.Value ? Convert.ToDecimal(rdr["PPM"]) : 0,
                                active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
                                Details = new List<DetailModel>() // init here just in case
                            };
                        }
                    }
                }
                string detailQuery = @"
            SELECT distinct qm1.CODE, QCP_CODE, qcm.name as QCP_Name, QCP_UNIT, QCP_STD, DEDUCT_QTY, DEDUCT_TYPE, REMARKS, PPM_YN, BASE_PRICE
            FROM QC_MAST1 qm1
           left Join QCP_MAST qcm on qm1.QCP_CODE=qcm.CODE
            WHERE qm1.CODE = @CODE AND qm1.COMP_CODE = @COMP_CODE";

                using (SqlCommand cmd = new SqlCommand(detailQuery, con))
                {
                    cmd.Parameters.AddWithValue("@CODE", request.code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            qcMaster.Details.Add(new DetailModel
                            {
                                Code = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                //Parameter = rdr["QCP_Name"]?.ToString(),
                                ParameterValue = rdr["QCP_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["QCP_CODE"]) : 0,
                                Unit = rdr["QCP_UNIT"] != DBNull.Value ? Convert.ToInt32(rdr["QCP_UNIT"]) : 0,
                                StdResult = rdr["QCP_STD"] != DBNull.Value ? Convert.ToDecimal(rdr["QCP_STD"]) : 0,
                                DeductQty = rdr["DEDUCT_QTY"]?.ToString(),
                                DeductType = rdr["DEDUCT_TYPE"]?.ToString(),
                                Remarks = rdr["REMARKS"]?.ToString(),
                                Ppm = rdr["PPM_YN"]?.ToString(),
                                BasePrice = rdr["BASE_PRICE"] != DBNull.Value ? Convert.ToDecimal(rdr["BASE_PRICE"]) : 0
                            });
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(qcMaster?.Name))
            {
                return Json(new { success = false, message = "No record found." });
            }
            return Json(new { success = true, data = qcMaster });
        }
        
        public class CodeRequest
        {
            public int code { get; set; }
        }
    }
}
