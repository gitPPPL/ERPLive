using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.PlantMaintenance.Master.VehicleMaster;

namespace travelexpensemanagement.Controllers.PlantMaintenance.Master
{
    public class VehicleMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public VehicleMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/PlantMaintenance/Master/VehicleMaster/Index.cshtml");
        }

        [HttpGet]
        public IActionResult ColorDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from COLOR_MAST where COMP_CODE=5";
            var color = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = color });

        }
        [HttpGet]
        public IActionResult CountryDDL()
        {
            var getData= _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from COUNTRY_MAST";
            var countryMast= _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data= countryMast });
        }
        [HttpGet]
        public IActionResult PlaceDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from PLACE_MAST where COMP_CODE=1";
            var place = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = place });
        }
        [HttpGet]
        public IActionResult VehicleNameDDl()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from MACHINE_MAST where TYPE='Vehicle' and COMP_CODE=1 ";
            var vehical = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = vehical });
        }
        [HttpGet]
        public IActionResult MakeDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "Select CODE,NAME from ITEMMAKE_MAST where COMP_CODE=1 Order by NAME";
            var make = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = make });
        }
        [HttpPost]
        public IActionResult SaveAndUpdateVehicleMaster([FromBody] VehicleMaster model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            if (model == null)
            {
                return Json(new { success = false, message = "Model is Null" });
            }
            try
            {
                string action = (model.CODE == null || model.CODE == 0) ? "Insert" : "Update";

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("SP_VEHICLE_MASTER", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE",model.CODE);
                    cmd.Parameters.AddWithValue("@VEHICLE_NAME", model.VEHICLE_NAME);
                    cmd.Parameters.AddWithValue("@SHORTNAME", model.SHORTNAME);
                    cmd.Parameters.AddWithValue("@VEHICLE_CATEGORY", model.VEHICLE_CATEGORY);
                    cmd.Parameters.AddWithValue("@VEHICLE_REGNO", model.VEHICLE_REGNO);
                    cmd.Parameters.AddWithValue("@COLOR_CODE", model.COLOR_CODE);
                    cmd.Parameters.AddWithValue("@MAKE_CODE", model.MAKE_CODE);
                    cmd.Parameters.AddWithValue("@MODEL", model.MODEL);
                    cmd.Parameters.AddWithValue("@CHASSIS_NO", model.CHASSIS_NO);
                    cmd.Parameters.AddWithValue("@ENGINE_NO", model.ENGINE_NO);
                    cmd.Parameters.AddWithValue("@FUEL_TYPE", model.FUEL_TYPE);
                    cmd.Parameters.AddWithValue("@PLACE_CODE", model.PLACE_CODE);
                    cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                    cmd.Parameters.AddWithValue("@COUNTRY_CODE", model.COUNTRY_CODE);
                    cmd.Parameters.Add("@ROADTAX_DATE", SqlDbType.SmallDateTime).Value = model.ROADTAX_DATE ?? (object)DBNull.Value;
                    cmd.Parameters.Add("@ROADTAX_DUEDATE", SqlDbType.SmallDateTime).Value = model.ROADTAX_DUEDATE ?? (object)DBNull.Value;
                    cmd.Parameters.AddWithValue("@ROADTAX_RECNO", model.ROADTAX_RECNO);
                    cmd.Parameters.Add("@NEXT_SERVICE_DATE", SqlDbType.SmallDateTime).Value = model.NEXT_SERVICE_DATE ?? (object)DBNull.Value;
                    cmd.Parameters.AddWithValue("@FC", model.FC);
                    cmd.Parameters.Add("@FC_DUEDATE", SqlDbType.SmallDateTime).Value = model.FC_DUEDATE ?? (object)DBNull.Value;
                    cmd.Parameters.Add("@FC_DATE", SqlDbType.SmallDateTime).Value = model.FC_DATE ?? (object)DBNull.Value;
                    cmd.Parameters.AddWithValue("@FC_RECNO", model.FC_RECNO);
                    cmd.Parameters.AddWithValue("@FC_REMARKS", model.FC_REMARKS);
                    cmd.Parameters.AddWithValue("@POLLUTION_NO", model.POLLUTION_NO);
                    cmd.Parameters.Add("@POLLUTION_DATE", SqlDbType.SmallDateTime).Value = model.POLLUTION_DATE ?? (object)DBNull.Value;
                    cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", globalVariable.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.Parameters.AddWithValue("@Action", action);
                    con.Open();
                    cmd.ExecuteNonQuery();

                }
                string message = action == "Insert" ? "Data Inserted Successfully!!" : "Data Updated Successfully!!";

                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult LoadDataOnEdit(int code)
        {
            var globalVariable= _globalVariableService.GetGlobalVariables();
            object data = null;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("SP_VEHICLE_MASTER", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@Action", "Select");

                    con.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                         data = new
                        {
                            code = reader["CODE"],
                            vehicleName = reader["VEHICLE_NAME"],
                            shortName = reader["SHORTNAME"],
                            vehicleCategory = reader["VEHICLE_CATEGORY"],
                            vehicleRegNo = reader["VEHICLE_REGNO"],
                            makeCode = reader["MAKE_CODE"],
                            model = reader["MODEL"],
                            chassisNo = reader["CHASSIS_NO"],
                            engineNo = reader["ENGINE_NO"],
                            fuelType = reader["FUEL_TYPE"],
                            placeCode = reader["PLACE_CODE"],
                            countryCode = reader["COUNTRY_CODE"],
                            colorCode = reader["COLOR_CODE"],

                            roadTaxDate = reader["ROADTAX_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["ROADTAX_DATE"]).ToString("yyyy-MM-dd"),
                            roadTaxDueDate = reader["ROADTAX_DUEDATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["ROADTAX_DUEDATE"]).ToString("yyyy-MM-dd"),
                            roadTaxRecNo = reader["ROADTAX_RECNO"],

                            nextServiceDate = reader["NEXT_SERVICE_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["NEXT_SERVICE_DATE"]).ToString("yyyy-MM-dd"),

                            fc = reader["FC"],
                            fcDate = reader["FC_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["FC_DATE"]).ToString("yyyy-MM-dd"),
                            fcDueDate = reader["FC_DUEDATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["FC_DUEDATE"]).ToString("yyyy-MM-dd"),
                            fcRecNo = reader["FC_RECNO"],
                            fcRemarks = reader["FC_REMARKS"],

                            pollutionNo = reader["POLLUTION_NO"],
                            pollutionDate = reader["POLLUTION_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["POLLUTION_DATE"]).ToString("yyyy-MM-dd"),

                            active = reader["ACTIVE"]
                        };
                    }
                    return Json(new { success = true, data =data});

                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
