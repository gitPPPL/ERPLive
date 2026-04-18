using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class HODMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public HODMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/HODMaster/Index.cshtml");
            //~/Views/Payroll/Master/HODMaster
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeList()
        {
            try
            {
                var departmentList = await _dbHelper.GetJsonDataAsync($@" SELECT distinct CODE, NAME from  ERPDB.dbo.EMP_MAST  where comp_code = {_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");
                return Json(new { status = true, data = departmentList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DepartmentList()
        {
            try
            {
                var departmentList = await _dbHelper.GetJsonDataAsync($@" select CODE, NAME from DEPT_MAST where COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");

                return Json(new { status = true, data = departmentList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public class HODModel
        {
            public int? DepartmentCd { get; set; }
            public string? Allow { get; set; }
        }
        public class HODDetailModel
        {
            public string? EmployeeName { get; set; }
            public int? EmployeeCd { get; set; }
            public List<HODModel>? HODModels { get; set; }
        }

        [HttpGet]
        public JsonResult getExistOrNot(string HOD)
        {
            try
            {
                bool isExist = false;

                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = @"
                         SELECT CASE 
                        WHEN EXISTS (
                        SELECT 1 
                        FROM PAYGATE_HOD 
                        WHERE UPPER(ISNULL(EMP_CODE, '')) = UPPER(@HOD) 
                        AND COMP_CODE = @CompCode
                        ) 
                        THEN 1 ELSE 0 
                        END";

                        cmd.Parameters.AddWithValue("@HOD", HOD);
                        cmd.Parameters.AddWithValue("@CompCode", _globalValue.GetGlobalVariables().PubCompCode);
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


        [HttpPost]
        public async Task<IActionResult> SaveHODMast([FromBody] HODDetailModel model)
        {
            if (model == null)
            {
                return Json(new { status = false, message = "Data Save Failed" });
            }

            int totalInserted = 0;

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (var transaction = con.BeginTransaction())
                    {
                        var usersessionDt = _globalValue.GetGlobalVariables();
                        int SrNo = 1;

                        try
                        {
                            foreach (var HodDetail in model.HODModels)
                            {
                                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_HODMast_AED]", con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                                    cmd.Parameters.AddWithValue("@EmpCode", _dbHelper.Xnull(model.EmployeeCd));
                                    cmd.Parameters.AddWithValue("@EmpName", _dbHelper.Xnull(model.EmployeeName));
                                    cmd.Parameters.AddWithValue("@DepartmentCd", _dbHelper.Xnull(HodDetail.DepartmentCd));
                                    cmd.Parameters.AddWithValue("@allow", _dbHelper.Xnull(HodDetail.Allow));
                                    cmd.Parameters.AddWithValue("@active", 1);
                                    cmd.Parameters.AddWithValue("@SNo", SrNo);
                                    cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                                    cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);

                                    var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                    cmd.Parameters.Add(returnParam);

                                    await cmd.ExecuteNonQueryAsync();

                                    int result = (int)cmd.Parameters["@ReturnVal"].Value;
                                    if (result > 0)
                                    {
                                        totalInserted++;
                                    }
                                    else
                                    {
                                        throw new Exception("Stored procedure failed for a row.");
                                    }
                                }

                                SrNo++;
                            }

                            transaction.Commit();
                            return Json(new { status = true, message = "Data saved successfully", totalInserted });
                        }
                        catch (Exception ex)
                        {
                            // Log the exception details
                            //_logger.LogError(ex, "An error occurred while saving HOD data.");
                            transaction.Rollback();
                            return Json(new { status = false, message = "Transaction failed. No records were saved." });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                //_logger.LogError(ex, "An error occurred while connecting to the database.");
                return Json(new { status = false, message = "Data Save Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetHODDetailsById(string id)
        {
            try
            {
                string strqry = $@"
                SELECT HOD.EMP_CODE,HOD.EMP_NAME,HOD.DEPT_CODE,HOD.ALLOW,HOD.ACTIVE
                FROM PAYGATE_HOD HOD LEFT JOIN DEPT_MAST dm on hod.DEPT_CODE=dm.CODE and hod.COMP_CODE=dm.COMP_CODE 
                where hod.COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} and hod.EMP_CODE={id} ";
                var data = await _dbHelper.GetJsonDataAsync(strqry);
                if (data.Count > 0)
                    return Json(new { status = true, data = data });

                return Json(new { status = false, message = "Not found" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHODMast([FromBody] HODDetailModel model)
        {
            if (model == null)
            {
                return Json(new { status = false, message = "Data update failed: Model is null." });
            }

            try
            {
                int totalInserted = 0;
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        var usersessionDt = _globalValue.GetGlobalVariables();
                        int SrNo = 1;
                        using (var sqlcmd = new SqlCommand("DELETE FROM PAYGATE_HOD WHERE COMP_CODE = @companyCd AND EMP_CODE = @empCode", con, transaction))
                        {
                            sqlcmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                            sqlcmd.Parameters.AddWithValue("@empCode", model.EmployeeCd);
                            await sqlcmd.ExecuteNonQueryAsync();
                        }

                        try
                        {
                            foreach (var HodDetail in model.HODModels)
                            {
                                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_HODMast_AED]", con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                                    cmd.Parameters.AddWithValue("@EmpCode", _dbHelper.Xnull(model.EmployeeCd));
                                    cmd.Parameters.AddWithValue("@EmpName", _dbHelper.Xnull(model.EmployeeName));
                                    cmd.Parameters.AddWithValue("@DepartmentCd", _dbHelper.Xnull(HodDetail.DepartmentCd));
                                    cmd.Parameters.AddWithValue("@allow", _dbHelper.Xnull(HodDetail.Allow));
                                    cmd.Parameters.AddWithValue("@active", 1);
                                    cmd.Parameters.AddWithValue("@SNo", SrNo);
                                    cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                                    cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);

                                    var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                    cmd.Parameters.Add(returnParam);

                                    await cmd.ExecuteNonQueryAsync();

                                    int result = (int)cmd.Parameters["@ReturnVal"].Value;
                                    if (result > 0)
                                    {
                                        totalInserted++;
                                    }
                                    else
                                    {
                                        throw new Exception("Stored procedure failed for a row.");
                                    }
                                }

                                SrNo++;
                            }

                            transaction.Commit();
                            return Json(new { status = true, message = "Data update successfully", totalInserted });
                        }
                        catch (Exception ex)
                        {                            
                            transaction.Rollback();
                            return Json(new { status = false, message = "Transaction failed. No records were update." });
                        }


                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = $"Data update failed: {ex.Message}" });
            }
        }


    }
}
