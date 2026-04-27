using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class SalaryStructureMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public SalaryStructureMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/SalaryStructureMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> DesignationList()
        {
            try
            {
                var designationList = await _dbHelper.GetJsonDataAsync($@" select CODE, NAME from DESG_MAST where COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} ");

                return Json(new { status = true, data = designationList });
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
                var departmentList = await _dbHelper.GetJsonDataAsync($@" select CODE, NAME from DEPT_MAST where COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} ");

                return Json(new { status = true, data = departmentList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        public class SalaryStructModel
        {
            public int? Code { get; set; }           
            public int? Designation { get; set; }
            public int? Department { get; set; }
            public int? MaxSalary { get; set; }
            public int? MinSalary { get; set; }
        }

        [HttpGet]
        public JsonResult getExistOrNot(string Department, int designation)
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
                        FROM PAY_SALARY_STRUCTURE 
                        WHERE  DEPT_CODE=@department and DESG_CODE=@designation
                        AND COMP_CODE = @CompCode
                        ) 
                        THEN 1 ELSE 0 
                        END";

                        cmd.Parameters.AddWithValue("@department", Department);
                        cmd.Parameters.AddWithValue("@designation", designation);
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
        public async Task<IActionResult> SaveSalaryStructMast([FromBody] SalaryStructModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { status = false, message = "Data Save Failed" });
                }

                using (var con = _dbcontext.GetErpConnection())
                {
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_SalaryStructMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@DepartmentCd", _dbHelper.Xnull(model.Department));
                        cmd.Parameters.AddWithValue("@DesignationCd", _dbHelper.Xnull(model.Designation));
                        cmd.Parameters.AddWithValue("@MaxSalary", _dbHelper.Xnull(model.MaxSalary));
                        cmd.Parameters.AddWithValue("@MinSalary", _dbHelper.Xnull(model.MinSalary));                     
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);

                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        x = (int)cmd.Parameters["@ReturnVal"].Value;

                    }
                }

                if (x > 0)
                    return Json(new { status = true, message = "Data save Successfully" });
                return Json(new { status = false, message = "Data save failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Save Failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetSalaryStructDetailsById(string id)
        {
            try
            {
                string strqry = $@"
             select distinct  pss.code,pss.DEPT_CODE,pss.DESG_CODE,isnull(dpm.NAME, '') department,isnull(dgm.NAME, '') designation ,pss.MAX_SALARY,pss.MIN_SALARY
			 from PAY_SALARY_STRUCTURE pss 
             left join DEPT_MAST dpm on pss.DEPT_CODE=dpm.CODE and pss.COMP_CODE=dpm.COMP_CODE
             left join DESG_MAST dgm on pss.DESG_CODE=dgm.CODE and pss.COMP_CODE=dgm.COMP_CODE	 
             WHERE pss.COMP_CODE = '{_globalValue.GetGlobalVariables().PubCompCode}' and pss.code={id} ";
                var data = await _dbHelper.GetJsonDataAsync(strqry);
                if (data.Count > 0)
                    return Json(new { status = true, data = data[0] });

                return Json(new { status = false, message = "Not found" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> UpdateSalaryStructMast([FromBody] SalaryStructModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { status = false, message = "Data update Failed" });
                }

                using (var con = _dbcontext.GetErpConnection())
                {
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_SalaryStructMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.Code));
                        cmd.Parameters.AddWithValue("@DepartmentCd", _dbHelper.Xnull(model.Department));
                        cmd.Parameters.AddWithValue("@DesignationCd", _dbHelper.Xnull(model.Designation));
                        cmd.Parameters.AddWithValue("@MaxSalary", _dbHelper.Xnull(model.MaxSalary));
                        cmd.Parameters.AddWithValue("@MinSalary", _dbHelper.Xnull(model.MinSalary));
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                        cmd.Parameters.Add(returnParam);
                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        x = (int)cmd.Parameters["@ReturnVal"].Value;
                    }
                }

                if (x > 0)
                    return Json(new { status = true, message = "Data update Successfully" });
                return Json(new { status = false, message = "Data update failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data update Failed" });
            }

        }
    }
}
