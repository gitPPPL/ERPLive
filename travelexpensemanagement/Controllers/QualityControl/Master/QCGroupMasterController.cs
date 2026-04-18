using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class QCGroupMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        private int? userLevel;
        public QCGroupMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
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

            if (action == "INSERT" && IsDuplicateQCGroup(model.NAME))
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
        private bool IsDuplicateQCGroup(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM QCG_MAST WHERE NAME = @Name", con))
                {
                    cmd.Parameters.AddWithValue("@Name", name.Trim());

                    con.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }
        [HttpPost]
        public JsonResult DeleteQCGroupByCode(int code)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCG_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", code);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "QC Group deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
 