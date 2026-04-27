using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Master.TenacityGroupMaster;

namespace travelexpensemanagement.Controllers.Production.Master
{
    public class TenacityGroupMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public TenacityGroupMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Production/Master/TenacityGroupMaster/Index.cshtml");
        }

        [HttpPost]
        public IActionResult SaveOrUpdateTenacityMaster([FromBody] TenacityGroupMasterModel model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    if (model.Code > 0)
                    {
                        string updateQuery = @"UPDATE TENACITY_GRPMAST
                                       SET NAME=@NAME,
                                           DESCRIPTION=@DESCRIPTION,
                                           UUSER=@UUSER,
                                           UDATE=GETDATE(),
                                           AED='E',
                                           WSID=@WSID,
                                           LIP=@LIP,
                                           LID=@LID
                                       WHERE CODE=@CODE
                                       AND COMP_CODE=@COMP_CODE";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@CODE", model.Code);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@NAME", model.Name ?? "");
                            cmd.Parameters.AddWithValue("@DESCRIPTION", model.Description ?? "");
                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", 0);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@Name",model.Name);
                            cmd.Parameters.AddWithValue("@TENACITY_TYPE", model);

                            cmd.ExecuteNonQuery();
                        }

                        return Json(new { success = true, message = "Data Updated Successfully" });
                    }
                    else
                    {
                        
                        int code = 0;

                        string getCodeQuery = "SELECT ISNULL(MAX(CODE),0)+1 FROM TENACITY_GRPMAST WHERE COMP_CODE=@COMP_CODE";

                        using (SqlCommand getCodeCmd = new SqlCommand(getCodeQuery, con))
                        {
                            getCodeCmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            code = Convert.ToInt32(getCodeCmd.ExecuteScalar());
                        }

                        string insertQuery = @"INSERT INTO TENACITY_GRPMAST
                                       (CODE, COMP_CODE, NAME, DESCRIPTION, UUSER, UDATE, AED, WSID, LIP, LID)
                                       VALUES
                                       (@CODE, @COMP_CODE, @NAME, @DESCRIPTION, @UUSER,
                                        GETDATE(), 'A', @WSID, @LIP, @LID)";

                        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@CODE", code);
                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@NAME", model.Name ?? "");
                            cmd.Parameters.AddWithValue("@DESCRIPTION", model.Description ?? "");
                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LIP", 0);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                            cmd.ExecuteNonQuery();
                        }

                        return Json(new { success = true, message = "Data Inserted Successfully" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetById(int code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string query = @"SELECT CODE, NAME, DESCRIPTION 
                         FROM TENACITY_GRPMAST 
                         WHERE CODE=@CODE AND COMP_CODE=@COMP_CODE";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            return Json(new
                            {
                                success = true,
                                code = dr["CODE"],
                                name = dr["NAME"],
                                description = dr["DESCRIPTION"]
                            });
                        }
                    }
                }
            }

            return Json(new { success = false, message = "Record not found" });
        }
    }
}
