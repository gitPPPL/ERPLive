using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.QualityControl.Master;
using static travelexpensemanagement.Controllers.Payroll.Master.HODMasterController;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class QCGroupMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly LogService.LogService _logService;


        public QCGroupMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/QCGroupMaster/Index.cshtml");
        }
        [HttpPost]
        public IActionResult SaveQCGroup([FromBody] QCG_MAST model)
        {
            if (string.IsNullOrWhiteSpace(model.NAME))
            {
                return Json(new { success = false, message = "QC Group Name cannot be blank." });
            }

            if (string.IsNullOrWhiteSpace(model.QC_TYPE))
            {
                return Json(new { success = false, message = "QC Type cannot be blank." });
            }

            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            //if (action == "INSERT" && IsDuplicateQCGroup(model.NAME))
            if (IsDuplicateQCGroup(model.NAME, model.CODE))
            {
                return Json(new { success = false, message = "QC Group name already exists." });
            }

            var result = SaveOrUpdateQCGroup(model, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }
        private string SaveOrUpdateQCGroup(QCG_MAST model, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCG_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", model.CODE);
                        cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
                        cmd.Parameters.AddWithValue("@QC_TYPE", model.QC_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

                        con.Open();
                        string mode = action == "INSERT" ? "INSERT" : "UPDATE";
                        int code = 0;
                        if (action == "INSERT")
                        {
                            code = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        else
                        {
                            cmd.ExecuteNonQuery();
                            code = model.CODE;
                        }


                        //===========log insert
                        _logService.InsertLog("QCG_MAST", "QC Group Master", "Master", mode, "", code.ToString(), null);

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
        private bool IsDuplicateQCGroup(string name, int code)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QCG_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "Exist");
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.Parameters.AddWithValue("@CODE", code);

                    con.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null; // true if duplicate exist
                }
            }
        }
        [HttpPost]
        public JsonResult DeleteQCGroupByCode(int docId)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCG_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", docId);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                //===========log insert
                _logService.InsertLog("QCG_MAST", "QC Group Master", "Master", "Delete", "", docId.ToString(), null);

                return Json(new { success = true, message = "QC Group deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
 