using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class ShiftMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public ShiftMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/ShiftMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDesignationList()
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"
                SELECT CODE, NAME FROM DESG_MAST WHERE COMP_CODE = '{UsersessionDt.PubCompCode}' ORDER BY NAME";
                var data = await _dbHelper.GetJsonDataAsync(strqry);                
                return Json(new { status = true, data = data });
                
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public class ShiftModel
        {            
            public int? Code { get; set; }
            public string? Shift { get; set; }              
            public int? DesignationCd { get; set; }
            public string? Shift_StartTm { get; set; }      
            public string? Shift_EndTm { get; set; }       
            public string? From_start { get; set; }        
            public string? To_start { get; set; }          
            public int? Ot_grace { get; set; }
            public int? Late_grace { get; set; } 
        }

        [HttpGet]
        public JsonResult getExistOrNot(string  shift ,int designation)
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
                        FROM SHIFT_MAST 
                        WHERE UPPER(ISNULL(SHIFT, '')) = UPPER(@shift) and DESG_CODE=@designation
                        AND COMP_CODE = @CompCode
                        ) 
                        THEN 1 ELSE 0 
                        END";

                        cmd.Parameters.AddWithValue("@shift", shift);
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
        public async Task<IActionResult> SaveShiftMast([FromBody] ShiftModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ShiftMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);                        
                        cmd.Parameters.AddWithValue("@Shift", _dbHelper.Xnull(model.Shift));
                        cmd.Parameters.AddWithValue("@DesignationCd", _dbHelper.Xnull(model.DesignationCd));
                        cmd.Parameters.AddWithValue("@Shift_StartTm", _dbHelper.Xnull(model.Shift_StartTm));
                        cmd.Parameters.AddWithValue("@Shift_EndTm", _dbHelper.Xnull(model.Shift_EndTm));
                        cmd.Parameters.AddWithValue("@From_start", _dbHelper.Xnull(model.From_start));
                        cmd.Parameters.AddWithValue("@To_start", _dbHelper.Xnull(model.To_start));
                        cmd.Parameters.AddWithValue("@Ot_grace", _dbHelper.Xnull(model.Ot_grace));
                        cmd.Parameters.AddWithValue("@Late_grace", _dbHelper.Xnull(model.Late_grace));
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
        public async Task<IActionResult> GetShiftDetailsById(string id)
        {
            try
            {
                string strqry = $@"
                 select sm.CODE,SHIFT,dm.NAME as DESG_CODE,
                (LEFT(SHIFT_STARTTIME, 2) + ':' + RIGHT(SHIFT_STARTTIME, 2)) as SHIFT_STARTTIME,
                (LEFT(SHIFT_ENDTIME, 2) + ':' + RIGHT(SHIFT_ENDTIME, 2)) as SHIFT_ENDTIME,
                (LEFT(FROM_START, 2) + ':' + RIGHT(FROM_START, 2)) as FROM_START,
                (LEFT(TO_START, 2) + ':' + RIGHT(TO_START, 2)) as TO_START,
                 OT_GRACE,LATE_GRACE from SHIFT_MAST sm left join DESG_MAST dm on sm.DESG_CODE=dm.CODE
                WHERE sm.COMP_CODE = '{_globalValue.GetGlobalVariables().PubCompCode}' and sm.code={id} ";
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
        public async Task<IActionResult> UpdateShiftMast([FromBody] ShiftModel model)
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
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_ShiftMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.Code));
                        cmd.Parameters.AddWithValue("@Shift", _dbHelper.Xnull(model.Shift));
                        cmd.Parameters.AddWithValue("@DesignationCd", _dbHelper.Xnull(model.DesignationCd));
                        cmd.Parameters.AddWithValue("@Shift_StartTm", _dbHelper.Xnull(model.Shift_StartTm));
                        cmd.Parameters.AddWithValue("@Shift_EndTm", _dbHelper.Xnull(model.Shift_EndTm));
                        cmd.Parameters.AddWithValue("@From_start", _dbHelper.Xnull(model.From_start));
                        cmd.Parameters.AddWithValue("@To_start", _dbHelper.Xnull(model.To_start));
                        cmd.Parameters.AddWithValue("@Ot_grace", _dbHelper.Xnull(model.Ot_grace));
                        cmd.Parameters.AddWithValue("@Late_grace", _dbHelper.Xnull(model.Late_grace));
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
