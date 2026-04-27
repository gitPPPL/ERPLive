using Microsoft.AspNetCore.Mvc;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Repositories.Interfaces;

namespace travelexpensemanagement.Controllers.Admin.Setup
{
    public class AssetsMasterController : Controller
    {
        private readonly IAssetRepository _assetRepository;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly DropdownService _dropdownService;

        public AssetsMasterController(IAssetRepository assetRepository, GlobalVariableService globalVariableService, DropdownService dropdownService, DataBaseConnection dbConnection)
        {
            _assetRepository = assetRepository;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbConnection = dbConnection;
        }

        public IActionResult Index()
        {
            return View("~/Views/Admin/Setup/AssetsMaster/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetddlACName()
        {
            var parameters = new Dictionary<string, object> {{ "@Type", "ACName" } };
            var data = _dropdownService.GetMultipleDropdownList("sp_GetDropdownData",CommandType.StoredProcedure,parameters);
            return Json(data);
        }
        [HttpPost]
        public IActionResult InsertAsset([FromForm] AssetModel model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //FIX (string → int conversion)
            int yearCode = Convert.ToInt32(globalVar.PubFYearCode);
            int compCode = Convert.ToInt32(globalVar.PubCompCode);

            if (_assetRepository.IsDuplicate(yearCode, compCode, model.AC_CODE))
            {
                return Json(new { success = false, message = "Duplicate record exists" });
            }
            bool result = _assetRepository.InsertAsset(model);
            return Json(new
            {
                success = result,
                message = result ? "Inserted successfully" : "Insert failed"
            });
        }

        [HttpPost]
        public IActionResult GetAssetBySrno([FromBody] int srno)
        {
            var data = _assetRepository.GetAssetBySrno(srno);
            if (data == null)
                return NotFound();
            return Json(data);
        }

        [HttpPost]
        public IActionResult UpdateAsset([FromForm] AssetModel model)
        {
            bool result = _assetRepository.UpdateAsset(model);
            return Json(new
            {
                success = result,
                message = result ? "Updated successfully" : "Update failed"
            });
        }
    }
}


//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using Microsoft.Extensions.Configuration;
//using System.Data;
//using travelexpensemanagement.Authorize;
//using travelexpensemanagement.Controllers.DropdownService;
//using travelexpensemanagement.Controllers.Globalvariable;
//using travelexpensemanagement.Dbconnection;
//using travelexpensemanagement.Models.Admin.Setup;
//using static travelexpensemanagement.Controllers.Admin.Setup.ItemDepartmentMasterController;

//namespace travelexpensemanagement.Controllers.Admin.Setup
//{
//    [SessionAuthorize]
//    public class AssetsMasterController : Controller
//    {
//        private readonly DataBaseConnection _dbConnection;
//        private readonly GlobalVariableService _globalVariableService;
//        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
//        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;

//        public AssetsMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
//     travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper)
//        {
//            _dbConnection = dbConnection;
//            _globalVariableService = globalVariableService;
//            _dropdownService = dropdownService;
//            _dbHelper = dbHelper;
//        }
//        public IActionResult Index()
//        {
//            return View("~/Views/Admin/Setup/AssetsMaster/Index.cshtml");
//        }
//        [HttpGet]
//        public JsonResult GetddlACName()
//        {
//            string query = "Select Code, name From SUBGROUP_MAST where NATURE ='Others' order by NAME";
//            var moduelList = _dropdownService.GetDropdownList(query);
//            return Json(moduelList);
//        }
//        [HttpPost]
//        public IActionResult InsertAsset([FromForm] AssetModel model)
//        {
//            var globalVar = _globalVariableService.GetGlobalVariables();
//            try
//            {
//                using (SqlConnection con = _dbConnection.GetErpConnection())
//                {
//                    con.Open();
//                    using (SqlCommand checkCmd = new SqlCommand(@"
//                SELECT COUNT(*) FROM ASSET_MAST 
//                WHERE YEAR_CODE = @YEAR_CODE AND AC_CODE = @AC_CODE AND COMP_CODE = @COMP_CODE", con))
//                    {
//                        checkCmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
//                        checkCmd.Parameters.AddWithValue("@AC_CODE", model.AC_CODE);
//                        checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

//                        int count = (int)checkCmd.ExecuteScalar();
//                        if (count > 0)
//                        {
//                            return Json(new { success = false, message = "Duplicate asset record already exists for this year, branch, and company." });
//                        }
//                    }
//                    using (SqlCommand cmd = new SqlCommand("sp_InsertAssetMaster", con))
//                    {
//                        cmd.CommandType = CommandType.StoredProcedure;

//                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
//                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1); // Satprakash Sir
//                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
//                        cmd.Parameters.AddWithValue("@AC_CODE", model.AC_CODE);
//                        cmd.Parameters.AddWithValue("@AC_NAME", model.AC_NAME);
//                        cmd.Parameters.AddWithValue("@OP_AMT", model.OP_AMT);
//                        cmd.Parameters.AddWithValue("@DEP_AMT", model.DEP_AMT);
//                        cmd.Parameters.AddWithValue("@DEP_RATE", model.DEP_RATE);
//                        cmd.Parameters.AddWithValue("@SHIFT_CALC", model.SHIFT_CALC);
//                        cmd.Parameters.AddWithValue("@LIFE", model.LIFE);
//                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
//                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
//                        cmd.Parameters.AddWithValue("@EUSER", "");
//                        cmd.Parameters.AddWithValue("@EDATE", "");
//                        cmd.Parameters.AddWithValue("@AED", "A");
//                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
//                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
//                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
//                        cmd.Parameters.AddWithValue("@Action", "Insert");

//                        cmd.ExecuteNonQuery();
//                    }
//                }

//                return Json(new { success = true, message = "Asset inserted successfully." });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, "Internal server error: " + ex.Message);
//            }
//        }

//        [HttpPost]
//        public IActionResult GetAssetBySrno([FromBody] CodeRequest request)
//        {
//            AssetModel asset = null;

//            using (SqlConnection con = _dbConnection.GetErpConnection())
//            {
//                using (SqlCommand cmd = new SqlCommand(@"
//            SELECT AC_CODE, AC_NAME, OP_AMT, DEP_AMT, DEP_RATE, SHIFT_CALC, LIFE, SRNO 
//            FROM ASSET_MAST 
//            WHERE SRNO = @SRNO", con))
//                {
//                    cmd.Parameters.AddWithValue("@SRNO", request.code);
//                    con.Open();

//                    using (SqlDataReader rdr = cmd.ExecuteReader())
//                    {
//                        if (rdr.Read())
//                        {
//                            asset = new AssetModel
//                            {
//                                AC_CODE = rdr["AC_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["AC_CODE"]) : 0,
//                                AC_NAME = rdr["AC_NAME"]?.ToString(),
//                                OP_AMT = rdr["OP_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["OP_AMT"]) : 0,
//                                DEP_AMT = rdr["DEP_AMT"] != DBNull.Value ? Convert.ToDecimal(rdr["DEP_AMT"]) : 0,
//                                DEP_RATE = rdr["DEP_RATE"] != DBNull.Value ? Convert.ToDecimal(rdr["DEP_RATE"]) : 0,
//                                SHIFT_CALC = rdr["SHIFT_CALC"] != DBNull.Value ? Convert.ToInt32(rdr["SHIFT_CALC"]) : 0,
//                                LIFE = rdr["LIFE"] != DBNull.Value ? Convert.ToInt32(rdr["LIFE"]) : 0,
//                                SRNO = rdr["SRNO"] != DBNull.Value ? Convert.ToInt32(rdr["SRNO"]) : 0
//                            };
//                        }
//                    }
//                }
//            }
//            if (asset == null)
//            {
//                return NotFound(new { message = "No asset found for the given SRNO." });
//            }
//            return Json(asset);
//        }
//        [HttpPost]
//        public IActionResult UpdateAsset([FromForm] AssetModel model)
//        {
//            var globalVar = _globalVariableService.GetGlobalVariables();
//            try
//            {
//                using (SqlConnection con = _dbConnection.GetErpConnection())
//                {
//                    using (SqlCommand cmd = new SqlCommand("sp_InsertAssetMaster", con))
//                    {
//                        cmd.CommandType = CommandType.StoredProcedure;

//                        cmd.Parameters.AddWithValue("@SRNO", model.SRNO);
//                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
//                        cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
//                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
//                        cmd.Parameters.AddWithValue("@AC_CODE", model.AC_CODE);
//                        cmd.Parameters.AddWithValue("@AC_NAME", model.AC_NAME);
//                        cmd.Parameters.AddWithValue("@OP_AMT", model.OP_AMT);
//                        cmd.Parameters.AddWithValue("@DEP_AMT", model.DEP_AMT);
//                        cmd.Parameters.AddWithValue("@DEP_RATE", model.DEP_RATE);
//                        cmd.Parameters.AddWithValue("@SHIFT_CALC", model.SHIFT_CALC);
//                        cmd.Parameters.AddWithValue("@LIFE", model.LIFE);
//                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
//                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
//                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
//                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
//                        cmd.Parameters.AddWithValue("@AED", "A");
//                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
//                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
//                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
//                        cmd.Parameters.AddWithValue("@Action", "Update");

//                        con.Open();
//                        int rowsAffected = cmd.ExecuteNonQuery();

//                        if (rowsAffected > 0)
//                        {
//                            return Json(new { success = true, message = "Asset updated successfully." });
//                        }
//                        else
//                        {
//                            return Json(new { success = false, message = "No record updated. Check SRNO." });
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, "Internal server error: " + ex.Message);
//            }
//        }
//        public class CodeRequest
//        {
//            public int code { get; set; }
//        }
//    }
//}
