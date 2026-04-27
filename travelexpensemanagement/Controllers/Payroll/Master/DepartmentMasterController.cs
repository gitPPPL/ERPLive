using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class DepartmentMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public DepartmentMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/DepartmentMaster/Index.cshtml");
        }
                

        public class DepartmentModel
        {           
            public int? Code { get; set; }
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public int? Colony { get; set; }
            public int? ProrateAllow { get; set; }
            public int? D23 { get; set; }
            public int? D24 { get; set; }
            public int? D25 { get; set; }
            public int? D26 { get; set; }
            public int? D27 { get; set; }
            public int? D28 { get; set; }
            public int? D29 { get; set; }
            public int? D30 { get; set; }
            public int? active { get; set; }
        }


        [HttpGet]
        public JsonResult getExistOrNot(string inputData)
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
                        FROM DEPT_MAST 
                        WHERE UPPER(ISNULL(NAME, '')) = UPPER(@Inputdata) 
                        AND COMP_CODE = @CompCode
                        ) 
                        THEN 1 ELSE 0 
                        END";

                        cmd.Parameters.AddWithValue("@Inputdata", inputData);
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
        public async Task<IActionResult> SaveDepartmentMast([FromBody] DepartmentModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_DepartmentMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);                      
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@Colony", _dbHelper.Xnull(model.Colony));
                        cmd.Parameters.AddWithValue("@ProrateAllow", _dbHelper.Xnull(model.ProrateAllow));
                        cmd.Parameters.AddWithValue("@D23", _dbHelper.Xnull(model.D23));
                        cmd.Parameters.AddWithValue("@D24", _dbHelper.Xnull(model.D24));
                        cmd.Parameters.AddWithValue("@D25", _dbHelper.Xnull(model.D25));
                        cmd.Parameters.AddWithValue("@D26", _dbHelper.Xnull(model.D26));
                        cmd.Parameters.AddWithValue("@D27", _dbHelper.Xnull(model.D27));
                        cmd.Parameters.AddWithValue("@D28", _dbHelper.Xnull(model.D28));
                        cmd.Parameters.AddWithValue("@D29", _dbHelper.Xnull(model.D29));
                        cmd.Parameters.AddWithValue("@D30", _dbHelper.Xnull(model.D30));
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.active));
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
                    return Json(new { status = true, message = "Data Save Successfully" });
                return Json(new { status = false, message = "Data Save Failed" });
                
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Save Failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentDetailsById(string id)
        {
            try
            {
                string strqry = $@"
                select CODE, NAME, SHORTNAME, ISNULL(D23,0) AS D23, ISNULL(D24,0) AS D24, ISNULL(D25,0) AS D25, ISNULL(D26,0) AS D26, ISNULL(D27,0) AS D27, ISNULL(D28,0) AS D28, ISNULL(D29,0) AS D29, ISNULL(D30,0) AS D30, ISNULL(COLONY, 0) COLONY, ISNULL(PRORATA_ALLOW, 0) PRORATA_ALLOW, ACTIVE  from DEPT_MAST
                WHERE COMP_CODE = '{_globalValue.GetGlobalVariables().PubCompCode}' and code={id} ";
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
        public async Task<IActionResult> UpdateDepartmentMast([FromBody] DepartmentModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_DepartmentMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.Code));
                        cmd.Parameters.AddWithValue("@Name", _dbHelper.Xnull(model.Name));
                        cmd.Parameters.AddWithValue("@ShortName", _dbHelper.Xnull(model.ShortName));
                        cmd.Parameters.AddWithValue("@Colony", _dbHelper.Xnull(model.Colony));
                        cmd.Parameters.AddWithValue("@ProrateAllow", _dbHelper.Xnull(model.ProrateAllow));
                        cmd.Parameters.AddWithValue("@D23", _dbHelper.Xnull(model.D23));
                        cmd.Parameters.AddWithValue("@D24", _dbHelper.Xnull(model.D24));
                        cmd.Parameters.AddWithValue("@D25", _dbHelper.Xnull(model.D25));
                        cmd.Parameters.AddWithValue("@D26", _dbHelper.Xnull(model.D26));
                        cmd.Parameters.AddWithValue("@D27", _dbHelper.Xnull(model.D27));
                        cmd.Parameters.AddWithValue("@D28", _dbHelper.Xnull(model.D28));
                        cmd.Parameters.AddWithValue("@D29", _dbHelper.Xnull(model.D29));
                        cmd.Parameters.AddWithValue("@D30", _dbHelper.Xnull(model.D30));
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.active));
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
