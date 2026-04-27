using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Master.TenacityMaster;

namespace travelexpensemanagement.Controllers.Production.Master
{
    public class TenacityMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TenacityMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
     ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Production/Master/TenacityMaster/Index.cshtml");
        }
        [HttpPost]
        public IActionResult SaveOrUpdateTenacityMaster([FromBody] TenacityMaster model)
        {
           
            var globalVariable = _globalVariableService.GetGlobalVariables();

            
            if (model == null)
                return Json(new { success = false, message = "Model binding failed. Data not received." });

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_Tenacity_Master", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", model.Code);  
                        cmd.Parameters.AddWithValue("@NAME", model.Name ?? "");
                        cmd.Parameters.AddWithValue("@TENACITY_TYPE", model.TENACITY_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@MIN_STD", model.MIN_STD ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MAX_STD", model.MAX_STD ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TENACITY_CAT", model.TENACITY_CAT ?? "");
                        //cmd.Parameters.AddWithValue("@TENACITY_CATCODE",
                        //    string.IsNullOrEmpty(model.TENACITY_CAT) ? (object)DBNull.Value : Convert.ToInt32(model.TENACITY_CAT));
                        cmd.Parameters.AddWithValue("@TENACITY_CATCODE",
                        string.IsNullOrEmpty(model.TENACITY_CAT) ? (object)DBNull.Value : model.TENACITY_CAT);
                        cmd.Parameters.AddWithValue("@ACTIVE", model.Active ?? 0);
                        cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", 0);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@Action", model.Action);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Saved/Updated Successfully" });
            }
            catch (Exception ex)
            {
                
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetTenacityByCode(int code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            TenacityMaster master = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                string query = @"SELECT CODE, NAME, TENACITY_TYPE, MIN_STD, MAX_STD, TENACITY_CAT, ACTIVE
                         FROM TENACITY_MAST
                         WHERE COMP_CODE=@COMP_CODE AND CODE=@CODE AND ISNULL(AED,'A') <> 'D'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", code);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            master = new TenacityMaster
                            {
                                Code = Convert.ToInt32(dr["CODE"]),
                                Name = dr["NAME"].ToString(),
                                TENACITY_TYPE = dr["TENACITY_TYPE"].ToString(),
                                TENACITY_CAT = dr["TENACITY_CAT"].ToString(),
                                MIN_STD = dr["MIN_STD"] != DBNull.Value ? Convert.ToDecimal(dr["MIN_STD"]) : 0,
                                MAX_STD = dr["MAX_STD"] != DBNull.Value ? Convert.ToDecimal(dr["MAX_STD"]) : 0,
                                Active = dr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(dr["ACTIVE"]) : 0
                            };
                        }
                    }
                }
            }

            return Json(new { success = master != null, data = master });
        }
        
    }
}
