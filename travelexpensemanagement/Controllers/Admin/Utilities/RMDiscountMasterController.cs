using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Utilities;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class RMDiscountMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public RMDiscountMasterController(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService,
            travelexpensemanagement.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/Admin/Utilities/RMDiscountMaster/Index.cshtml");
        }
        public JsonResult GetddSaudaItemName()
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = $@" SELECT a.CODE AS value, a.SHORTNAME AS text FROM ITEM_MAST a LEFT JOIN ITEM_MGROUP b ON b.CODE = a.MGROUP_CODE AND b.COMP_CODE = a.COMP_CODE
                WHERE b.MGROUP_TYPE IN ('Raw') AND a.COMP_CODE = {globalVar.PubCompCode} AND a.Active = 1 ORDER BY a.SHORTNAME ASC";
                var resultList = _dropdownService.GetDropdownList(query);
                return Json(resultList);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        public JsonResult GetddItemName()
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                string query = $@" SELECT a.CODE AS value, a.SHORTNAME AS text FROM ITEM_MAST a LEFT JOIN ITEM_MGROUP b ON b.CODE = a.MGROUP_CODE AND b.COMP_CODE = a.COMP_CODE
                WHERE b.MGROUP_TYPE IN ('Raw') AND a.COMP_CODE = {globalVar.PubCompCode} AND a.Active = 1 ORDER BY a.SHORTNAME ASC";
                var resultList = _dropdownService.GetDropdownList(query);
                return Json(resultList);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        [HttpPost]
        public IActionResult SaveRMDiscountMaster([FromBody] RMDiscountMaster model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data" });
            try
            {
                var connection = _dbConnection.GetErpConnection();
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (connection)
                using (var cmd = new SqlCommand("USP_RMDISC_MAST", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", model.ACTION);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@DTYPE", "Purchase");
                    cmd.Parameters.AddWithValue("@CODE", model.Code ?? 0);
                    cmd.Parameters.AddWithValue("@SAUDA_ITEM", model.SaudaItem);
                    cmd.Parameters.AddWithValue("@ITEM_CODE", model.ItemCode);
                    cmd.Parameters.AddWithValue("@EFF_DATE", model.EffectiveDate);
                    cmd.Parameters.AddWithValue("@RATE", model.Rate);
                    cmd.Parameters.AddWithValue("@ABOVE_PER", model.AbovePercentage ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ABOVE_AMT", model.AboveAmount ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@REMARKS", model.Remarks ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);

                    if (model.ACTION == "UPDATE")
                    {
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
                string msg = model.ACTION == "UPDATE" ? "Record updated successfully!" : "Record inserted successfully!";

                return Json(new { success = true, message = msg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving data: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult GetID(int Code)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                RMDiscountMaster model = null;
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(@" SELECT TOP 1 CODE,DTYPE,SAUDA_ITEM,ITEM_CODE,EFF_DATE,RATE,ABOVE_PER,ABOVE_AMT,REMARKS
                FROM RMDISC_MAST WHERE CODE = @CODE AND COMP_CODE = @COMP_CODE", conn))
                {
                    cmd.Parameters.AddWithValue("@CODE", Code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);

                    conn.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            model = new RMDiscountMaster
                            {
                                Code = Convert.ToInt32(dr["CODE"]),
                                DType = dr["DTYPE"].ToString(),
                                SaudaItem = dr["SAUDA_ITEM"] as int?,
                                ItemCode = Convert.ToInt32(dr["ITEM_CODE"]),
                                EffectiveDate = Convert.ToDateTime(dr["EFF_DATE"]),
                                Rate = Convert.ToDecimal(dr["RATE"]),
                                AbovePercentage = dr["ABOVE_PER"] as decimal?,
                                AboveAmount = dr["ABOVE_AMT"] as decimal?,
                                Remarks = dr["REMARKS"]?.ToString()
                            };
                        }
                    }
                }
                return Json(new { success = true, data = model });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
