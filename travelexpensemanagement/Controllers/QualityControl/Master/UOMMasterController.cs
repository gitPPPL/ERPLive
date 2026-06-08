using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class UOMMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly LogService.LogService _logService;

        private int? userLevel;
        public UOMMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _logService = logService;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/UOMMaster/Index.cshtml");
        }
        [HttpPost]
        public IActionResult SaveUOM([FromBody] QCPUNIT_MAST model)
        {
            if (string.IsNullOrWhiteSpace(model.NAME))
            {
                return Json(new { success = false, message = "QC Unit name cannot be blank." });
            }

            //if (string.IsNullOrWhiteSpace(model.SHORTNAME))
            //{
            //    return Json(new { success = false, message = "Short name cannot be blank." });
            //}

            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            //if (action == "INSERT" && IsDuplicateUOM(model.NAME))
            if (IsDuplicateUOM(model.NAME, model.CODE))
            {
                return Json(new { success = false, message = "QC Unit name already exists." });
            }

            var result = SaveOrUpdateUOM(model, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }
        private string SaveOrUpdateUOM(QCPUNIT_MAST model, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCPUNIT_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", model.CODE);
                        cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
                        cmd.Parameters.AddWithValue("@SHORTNAME", model.SHORTNAME ?? "");
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
                        _logService.InsertLog("QCPUNIT_MAST", "QC UOM Master", "Master", mode, "", code.ToString(), null);
                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
        private bool IsDuplicateUOM(string name, int code)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QCPUNIT_MAST", con))
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

        [HttpGet]
        public JsonResult IsQcUOMDeletable(int docId)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            bool isExists = false;
            string msg = "";
            try
            {
                //===========Check Qc Group existence in QC Master===========
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCPUNIT_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Del_CheckInQcMast1");
                        cmd.Parameters.AddWithValue("@CODE", docId);
                        cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);

                        con.Open();
                        object result = cmd.ExecuteScalar();

                        string qcUOMName = result?.ToString();
                        isExists = string.IsNullOrEmpty(qcUOMName) ? false : true;

                        msg = $"QC UOM <b>{qcUOMName}</b> exists in QC Master and cannot be deleted.";
                    }
                    return Json(new { success = true, message = msg, isExists = isExists });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult DeleteUOMByCode(int docId)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCPUNIT_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", docId);

                        con.Open();
                        cmd.ExecuteNonQuery();
                        //===========log insert
                        _logService.InsertLog("QCPUNIT_MAST", "QC UOM Master", "Master", "Delete", "", docId.ToString(), null);
                    }
                }

                return Json(new { success = true, message = "UOM deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
 