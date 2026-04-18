using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class NewEmployeeRequirementController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public NewEmployeeRequirementController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/NewEmployeeRequirement/Index.cshtml");
        }

        public class NewEmpRequireModel
        { 
           
                public int? CODE { get; set; }
                public int? DEPT_CODE { get; set; }
                public int? DESG_CODE { get; set; }
                public int? PLACE_CODE { get; set; }
                public int? NOS { get; set; }
                public string? REMARKS { get; set; }   
                public int? ACTIVE { get; set; }
            
        }
        [HttpGet]
        public async Task<IActionResult> DesignationList()
        {
            try
            {
                var designationList = await _dbHelper.GetJsonDataAsync($@" select CODE, NAME from DESG_MAST where COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}  order by NAME ");

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
                var departmentList = await _dbHelper.GetJsonDataAsync($@" select CODE, NAME from DEPT_MAST where COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode}  order by NAME ");

                return Json(new { status = true, data = departmentList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> WorkPlaceList()
        {
            try
            {
                var placeList = await _dbHelper.GetJsonDataAsync($@" select CODE, NAME from PLACE_MAST where COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} order by NAME ");

                return Json(new { status = true, data = placeList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveNewEmpRequireMast([FromBody] NewEmpRequireModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_Pay_NewEmpRequireMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                         cmd.Parameters.AddWithValue("@MType", "NREQ");                      
                        cmd.Parameters.AddWithValue("@DepartmentCd", _dbHelper.Xnull(model.DEPT_CODE));
                        cmd.Parameters.AddWithValue("@DesignationCd", _dbHelper.Xnull(model.DESG_CODE));
                        cmd.Parameters.AddWithValue("@PlaceCd", _dbHelper.Xnull(model.PLACE_CODE));
                        cmd.Parameters.AddWithValue("@NOS", _dbHelper.Xnull(model.NOS));
                        cmd.Parameters.AddWithValue("@Remark", _dbHelper.Xnull(model.REMARKS));
                        cmd.Parameters.AddWithValue("@FaprovRemark", "");
                        cmd.Parameters.AddWithValue("@FAprovStatus", "");
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.ACTIVE));
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
        public async Task<IActionResult> GetNewEmpDetailById(string id)
        {
            try
            {
                string strqry = $@"
                select distinct  NEmp.CODE, dp.NAME DEPT_CODE, dg.NAME DESG_CODE,pl.NAME PLACE_CODE, NEmp.NOS,NEmp.REMARKS,NEmp.FAPROV_STATUS,NEmp.FAPROV_REMARKS,NEmp.ACTIVE FROM PAY_NEWEMPREQ NEmp
                left join DEPT_MAST dp on NEmp.DEPT_CODE=dp.CODE and NEmp.COMP_CODE=dp.COMP_CODE
                left join DESG_MAST dg on NEmp.DESG_CODE=dg.CODE and NEmp.COMP_CODE=dg.COMP_CODE
                left join PLACE_MAST pl on NEmp.PLACE_CODE=pl.CODE and NEmp.COMP_CODE=pl.COMP_CODE            
                WHERE NEmp.COMP_CODE = '{_globalValue.GetGlobalVariables().PubCompCode}' and NEmp.code={id} ";
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
        public async Task<IActionResult> UpdateNewEmpReqMast([FromBody] NewEmpRequireModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_Pay_NewEmpRequireMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@MType", "NREQ");
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.CODE));
                        cmd.Parameters.AddWithValue("@DepartmentCd", _dbHelper.Xnull(model.DEPT_CODE));
                        cmd.Parameters.AddWithValue("@DesignationCd", _dbHelper.Xnull(model.DESG_CODE));
                        cmd.Parameters.AddWithValue("@PlaceCd", _dbHelper.Xnull(model.PLACE_CODE));
                        cmd.Parameters.AddWithValue("@NOS", _dbHelper.Xnull(model.NOS));
                        cmd.Parameters.AddWithValue("@Remark", _dbHelper.Xnull(model.REMARKS));
                        cmd.Parameters.AddWithValue("@FaprovRemark", "");
                        cmd.Parameters.AddWithValue("@FAprovStatus", "");
                        cmd.Parameters.AddWithValue("@active", _dbHelper.Xnull(model.ACTIVE));
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
