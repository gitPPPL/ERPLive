using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class CostCategoryMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public CostCategoryMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    DropdownService dropdownService, DbHelper dbHelper,
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
            return View("~/Views/FinancialAccounting/Master/CostCategoryMaster/Index.cshtml");
        }
        [HttpPost]
        public IActionResult SaveCostCatMaster([FromBody] COSTCAT_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            var result = SaveOrUpdateCostCategory(model, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }
        public string SaveOrUpdateCostCategory(COSTCAT_MAST model, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();   
                    if (action == "INSERT")
                    {
                        using (SqlCommand checkCmd = new SqlCommand(@" SELECT COUNT(*) FROM COSTCAT_MAST WHERE COMP_CODE = @COMP_CODE AND NAME = @NAME   AND COSTTYPE = @COSTTYPE", con))
                        {
                            checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                            checkCmd.Parameters.AddWithValue("@NAME", model.NAME.Trim());
                            checkCmd.Parameters.AddWithValue("@COSTTYPE", model.COSTTYPE);

                            int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (exists > 0)
                            {
                                return "Cost Category already exists with same Name & Cost Type!";
                            }
                        }
                    }
                    using (SqlCommand cmd = new SqlCommand("sp_COSTCAT_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", model.CODE);
                        cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
                        cmd.Parameters.AddWithValue("@COSTCODE", model.COSTCODE ?? "");
                        cmd.Parameters.AddWithValue("@COSTTYPE", model.COSTTYPE ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", model.AED ?? "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

                        cmd.ExecuteNonQuery();
                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        //[HttpPost]
        //public IActionResult SaveCostCatMaster([FromBody] COSTCAT_MAST model)
        //{
        //    string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
        //    var result = SaveOrUpdateCostCategory(model, action);

        //    if (result == "Success")
        //    {
        //        return Json(new { success = true });
        //    }
        //    else
        //    {
        //        return Json(new { success = false, message = result });
        //    }
        //}
        //public string SaveOrUpdateCostCategory(COSTCAT_MAST model, string action)
        //{
        //    var globalVar = _globalVariableService.GetGlobalVariables(); // Assumed

        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection()) // Assumed service
        //        {
        //            if (action == "INSERT")
        //            {
        //                using (SqlCommand checkCmd = new SqlCommand(@"
        //    SELECT COUNT(*) 
        //    FROM COSTCAT_MAST 
        //    WHERE COMP_CODE = @COMP_CODE 
        //      AND NAME = @NAME
        //      AND COSTTYPE = @COSTTYPE
        //", con))
        //                {
        //                    checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                    checkCmd.Parameters.AddWithValue("@NAME", model.NAME.Trim());
        //                    checkCmd.Parameters.AddWithValue("@COSTTYPE", model.COSTTYPE);

        //                    int exists = Convert.ToInt32(checkCmd.ExecuteScalar());
        //                    if (exists > 0)
        //                    {
        //                        return "Cost Category already exists with same Name & Cost Type!";
        //                    }
        //                }
        //            }



        //            using (SqlCommand cmd = new SqlCommand("sp_COSTCAT_MAST", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@Action", action);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                cmd.Parameters.AddWithValue("@CODE", model.CODE);
        //                cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
        //                cmd.Parameters.AddWithValue("@COSTCODE", model.COSTCODE ?? "");
        //                cmd.Parameters.AddWithValue("@COSTTYPE", model.COSTTYPE ?? "");
        //                cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
        //                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@AED", model.AED ?? "A");
        //                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
        //                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
        //                cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

        //                con.Open();
        //                cmd.ExecuteNonQuery();
        //                return "Success";
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return "Error: " + ex.Message;
        //    }
        //}

        [HttpPost]
        public JsonResult DeleteCostCategoryByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_COSTCAT_MAST", con)) // Use your actual SP name
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { success = true, message = "Cost category deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting cost category.", error = ex.Message });
            }
        }


    }
}
