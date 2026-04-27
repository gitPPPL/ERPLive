using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;  // Use Microsoft.Data.SqlClient instead of System.Data.SqlClient
using System;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Utilities;
using travelexpensemanagement.Models.PayRoll;
using travelexpensemanagement.ModuleService;
using UglyToad.PdfPig.Core;

namespace travelexpensemanagement.Controllers.Admin.Utilities
{
    public class LoomIncentiveParameterMasterController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public LoomIncentiveParameterMasterController(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            DropdownService dropdownService,
            DbHelper dbHelper,
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
            return View("~/Views/Admin/Utilities/LoomIncentiveParameterMaster/Index.cshtml");
        }
                
        public JsonResult DDLPartyMast()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select a.Code ,a.NAME from SUBGROUP_MAST a where a.Active=1 and a.NATURE='Supplier' and " +
                    "a.comp_code=" + getdata.PubCompCode + " order by a.name asc ";

                var PartyList = _dropdownService.GetDropdownList(query);

                return Json(PartyList);
            }

        }

        public JsonResult DDLInspBy()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection()) // Ensure this is Microsoft.Data.SqlClient.SqlConnection
            {
                string query = "select 'COLR' Code,'Color' Name Union all select 'GRAM','Gram' Union all select 'MESH','Mesh' Union all select 'MAKE','Make' Union all select 'SIZE','Size'";

                var DDLInspBylist = _dropdownService.GetDropdownList(query);

                return Json(DDLInspBylist);
            }
        }
        public JsonResult DDLLoomType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection()) 
            {
               
                string query = "SELECT MAKE_TYPE AS code, MAKE_TYPE AS NAME " +
                               "FROM MACHINE_MAST " +
                               "WHERE COMP_CODE = '" + getdata.PubCompCode + "' " +
                               "AND TYPE = 'Loom' " +
                               "AND ISNULL(MAKE_TYPE, '') <> '' " +
                               "GROUP BY MAKE_TYPE " +
                               "ORDER BY MAKE_TYPE";

              
                var DDLLoomType = _dropdownService.GetDropdownList(query);

                return Json(DDLLoomType);
            }
        }
        public JsonResult DDLConversionCode()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection()) 
            {
               
                string query = "select code,name from MESH_MAST where COMP_CODE=" + getdata.PubCompCode + " Group by code,name order by code";

              
                var DDLConversionCode = _dropdownService.GetDropdownList(query);

                return Json(DDLConversionCode);
            }
        }

        [HttpPost]
        public IActionResult SaveTempMaster([FromBody] LoomIncentiveParameterMaster_Model data)
        {
            if (data == null)
            {
                return Json(new { success = false, message = "Input model is null" });
            }

            string action = data.action == "INSERT" ? "Insert" : "Update";

            var result = Submitbtn(data, action);

            if (result == "Success")
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }

        [HttpPost]
        private string Submitbtn(LoomIncentiveParameterMaster_Model data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_PAY_LOOMINCENPARM_MAST", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", data.Code);
                        cmd.Parameters.AddWithValue("@NAME", data.Name);
                        cmd.Parameters.AddWithValue("@V_TYPE", data.V_Type);

                        cmd.Parameters.AddWithValue("@LOOM_TYPE", data.LoomType);
                        cmd.Parameters.AddWithValue("@CONV_CODE", data.ConvCode);
                        cmd.Parameters.AddWithValue("@CONV_NAME", data.ConvName);

                        cmd.Parameters.AddWithValue("@PER", data.Per);
                        cmd.Parameters.AddWithValue("@FIX_AMT", data.FixAmt);
                        cmd.Parameters.AddWithValue("@ACTIVE", data.Active);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                   
                        int rowsInserted = cmd.ExecuteNonQuery();

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {

                return $"Error: {ex.Message}";
            }
        }








    }
}
