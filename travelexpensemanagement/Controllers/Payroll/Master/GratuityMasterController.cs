using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.Models.Payroll.Master;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class GratuityMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        string yearPrefix, VNO;
        int x;
        string  VType = "GRAT";
        public GratuityMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/GratuityMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDocid()
        {
            try
            {
                var CompanyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var YearCd = _globalValue.GetGlobalVariables().PubFYearCode;               
                string VType = "GRAT";


                string query = @"
            SELECT ISNULL(
                RIGHT('00000' + 
                    CAST(CAST(MAX(SUBSTRING(CONVERT(VARCHAR, V_NO), 5, LEN(CONVERT(VARCHAR, V_NO)))) AS INT) + 1 AS VARCHAR), 5),
            '00001')
            FROM PAY_GRATUITY
            WHERE COMP_CODE = @COMP_CODE              
              AND V_TYPE = @V_TYPE;
        ";

                var parameters = new Dictionary<string, object>
        {
            { "@COMP_CODE",  CompanyCd },            
            { "@V_TYPE", VType }
        };

                string newVNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);
                yearPrefix = await _dbHelper.GetExecuteScalarAsync<string>(
                  $"SELECT PREFIXYR as PrefixYr FROM YEAR_MAST WHERE CODE = '{YearCd}'");

                var DocId = _dbHelper.Xnull(VType).ToString() + _dbHelper.Xnull(yearPrefix) + _dbHelper.Xnull(newVNo);

                return Json(new { status = true, DocId = DocId });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async  Task<IActionResult> GetYearList()
        {
            try
            {
                var YearList = await _dbHelper.GetJsonDataAsync("  select distinct code, PREFIXYR from YEAR_MAST order by PREFIXYR desc ");
                return Json(new { status = true, data = YearList });
            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeList()
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
                select distinct e.CODE as EmpCd, e.NAME as EmpName, e.FATHER_NAME,isnull(d.NAME, '') as DEPT_CODE
                from EMP_MAST e left join DEPT_MAST d on e.DEPT_CODE=d.CODE 
                and e.COMP_CODE=d.COMP_CODE
                where e.COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} ";               
                var data = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = data });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
 

        [HttpGet]
        public JsonResult getExistOrNot(string Docid)
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
                        FROM PAY_GRATUITY 
                        WHERE  DOC_ID=@DOC_ID
                        AND COMP_CODE = @CompCode
                        ) 
                        THEN 1 ELSE 0 
                        END";
                                              
                        cmd.Parameters.AddWithValue("@DOC_ID", Docid);
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
        public async Task<IActionResult> SaveGratuityMast([FromBody] GratuityModel model)
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
                    string DocId = _dbHelper.Xnull(model.Docid).ToString();
                    if (DocId == null || DocId.Length > 5)
                    {
                        VNO = (DocId).Substring(4);
                    }
                    DateTime Fromdate = Convert.ToDateTime(model.VDate);

                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_GratuityMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@VType", _dbHelper.Xnull(VType));
                        cmd.Parameters.AddWithValue("@VNo", _dbHelper.Xnull(VNO));
                        cmd.Parameters.AddWithValue("@VDate", _dbHelper.Xnull(Fromdate));
                        cmd.Parameters.AddWithValue("@Docid", _dbHelper.Xnull(model.Docid));
                        cmd.Parameters.AddWithValue("@EmpCd", _dbHelper.Xnull(model.EmpCd));
                        cmd.Parameters.AddWithValue("@EmpName", _dbHelper.Xnull(model.EmpName));
                        cmd.Parameters.AddWithValue("@Gyear", _dbHelper.Xnull(model.Gyear));
                        cmd.Parameters.AddWithValue("@GDays", _dbHelper.Xnull(model.GDays));
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
        public async Task<IActionResult> GetGratuityDetailsById(string id)
        {
            try
            {
                string strqry = $@" 
                select COMP_CODE,V_TYPE,V_NO, V_DATE,DOC_ID,EMP_CODE,EMP_NAME,GYEAR,GDAYS 
                from PAY_GRATUITY
                WHERE COMP_CODE = '{_globalValue.GetGlobalVariables().PubCompCode}' and DOC_ID='{id}' ";
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
        public async Task<IActionResult> UpdateGratuityMast([FromBody] GratuityModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_GratuityMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@VType", _dbHelper.Xnull(VType));
                        cmd.Parameters.AddWithValue("@VNo", _dbHelper.Xnull(model.Vno));
                        cmd.Parameters.AddWithValue("@VDate", _dbHelper.Xnull(model.VDate));
                        cmd.Parameters.AddWithValue("@Docid", _dbHelper.Xnull(model.Docid));
                        cmd.Parameters.AddWithValue("@EmpCd", _dbHelper.Xnull(model.EmpCd));
                        cmd.Parameters.AddWithValue("@EmpName", _dbHelper.Xnull(model.EmpName));
                        cmd.Parameters.AddWithValue("@Gyear", _dbHelper.Xnull(model.Gyear));
                        cmd.Parameters.AddWithValue("@GDays", _dbHelper.Xnull(model.GDays));
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
