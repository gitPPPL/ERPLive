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
    public class TransportMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public TransportMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/FinancialAccounting/Master/TransportMaster/Index.cshtml");
        }
        public IActionResult GetPartyList()
        {
            string query = "SELECT CODE,NAME FROM SUBGROUP_MAST ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        [HttpPost]
        public IActionResult SaveTransport([FromBody] TRANSPORT_MAST model)
        {
            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
            var result = SaveOrUpdateTransport(model, action);

            if (result == "Success")
            {
                string message = action == "INSERT"
                    ? "Transport inserted successfully!"
                    : "Transport updated successfully!";

                return Json(new { success = true, message = message });
            }
            else
            {
                return Json(new { success = false, message = result });
            }
        }

        public string SaveOrUpdateTransport(TRANSPORT_MAST transport, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_TRANSPORT_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", transport.CODE);
                        cmd.Parameters.AddWithValue("@NAME", transport.NAME ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_CODE", transport.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@OWNER_NAME", transport.OWNER_NAME ?? "");
                        cmd.Parameters.AddWithValue("@ADDRESS", transport.ADDRESS ?? "");
                        cmd.Parameters.AddWithValue("@GSTIN", transport.GSTIN ?? "");
                        cmd.Parameters.AddWithValue("@PAN", transport.PAN ?? "");
                        cmd.Parameters.AddWithValue("@TDS_PER", transport.TDS_PER);
                        cmd.Parameters.AddWithValue("@DECL_NO", transport.DECL_NO ?? "");
                        cmd.Parameters.AddWithValue("@DECL_DATE", string.IsNullOrEmpty(transport.DECL_DATE.ToString()) ? (object)DBNull.Value : transport.DECL_DATE);
                        cmd.Parameters.AddWithValue("@EXPIRY_DATE", string.IsNullOrEmpty(transport.EXPIRY_DATE.ToString()) ? (object)DBNull.Value : transport.EXPIRY_DATE);
                        cmd.Parameters.AddWithValue("@SALE_GROUP", transport.SALE_GROUP ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", transport.ACTIVE);

                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", transport.AED ?? "A");
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


        //[HttpPost]
        //public IActionResult SaveTransport([FromBody] TRANSPORT_MAST model)
        //{
        //    string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";
        //    var result = SaveOrUpdateTransport(model, action);

        //    if (result == "Success")
        //    {
        //        return Json(new { success = true });
        //    }
        //    else
        //    {
        //        return Json(new { success = false, message = result });
        //    }
        //}
        //public string SaveOrUpdateTransport(TRANSPORT_MAST transport, string action)
        //{
        //    var globalVar = _globalVariableService.GetGlobalVariables();
        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_TRANSPORT_MAST", con)) 
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@Action", action);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                cmd.Parameters.AddWithValue("@CODE", transport.CODE);
        //                cmd.Parameters.AddWithValue("@NAME", transport.NAME ?? "");
        //                cmd.Parameters.AddWithValue("@PARTY_CODE", transport.PARTY_CODE);
        //                cmd.Parameters.AddWithValue("@OWNER_NAME", transport.OWNER_NAME ?? "");
        //                cmd.Parameters.AddWithValue("@ADDRESS", transport.ADDRESS ?? "");
        //                cmd.Parameters.AddWithValue("@GSTIN", transport.GSTIN ?? "");
        //                cmd.Parameters.AddWithValue("@PAN", transport.PAN ?? "");
        //                cmd.Parameters.AddWithValue("@TDS_PER", transport.TDS_PER);
        //                cmd.Parameters.AddWithValue("@DECL_NO", transport.DECL_NO ?? "");
        //                cmd.Parameters.AddWithValue("@DECL_DATE", transport.DECL_DATE == DateTime.MinValue ? (object)DBNull.Value : transport.DECL_DATE);
        //                cmd.Parameters.AddWithValue("@EXPIRY_DATE", transport.EXPIRY_DATE == DateTime.MinValue ? (object)DBNull.Value : transport.EXPIRY_DATE);
        //                cmd.Parameters.AddWithValue("@SALE_GROUP", transport.SALE_GROUP ?? "");
        //                cmd.Parameters.AddWithValue("@ACTIVE", transport.ACTIVE);

        //                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@AED", transport.AED ?? "A");
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
        public JsonResult DeleteTransportByCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_TRANSPORT_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@CODE", code);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { success = true, message = "Transport record deleted successfully." });
        }


    }
}
