using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Master.LotMaster;
namespace travelexpensemanagement.Controllers.Production.Master
{
    public class LotMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public LotMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Production/Master/LotMaster/Index.cshtml");
        }

        [HttpPost]
        public IActionResult SaveAndUpdateData([FromBody] LotMaster model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            try
            {
                string actionType = model.CODE == 0 ? "Insert" : "Update";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_Lot_Master", con);
                    con.Open();
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", model.CODE);
                    cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);

                    cmd.Parameters.AddWithValue("@NAME",model.Name);
                    cmd.Parameters.AddWithValue("@SHORTNAME",model.ShortName);
                    
                    cmd.Parameters.AddWithValue("@Action", actionType);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();

                }
                return Json(new { success = true, action = actionType, message = actionType == "Insert" ? "Data Inserted Successfully!" : "Data Updated Successfully!" });
               
            }catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult LoadOnEdit(int code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            LotMaster data = new LotMaster();

            using(SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT CODE, NAME, SHORTNAME FROM LOT_MAST WHERE COMP_CODE=@COMP_CODE AND CODE=@CODE";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                cmd.Parameters.AddWithValue("@CODE", code);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    data.CODE= Convert.ToInt32(dr["CODE"]);
                    data.Name = dr["NAME"].ToString();
                    data.ShortName = dr["SHORTNAME"].ToString();

                }

            }
            return Json(new { success = true, data = data });

        }
    }
}
