using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Models.FincialAccounting.Master;

namespace travelexpensemanagement.Controllers.FinancialAccounting.Master
{
    public class AccountMainGroupMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public AccountMainGroupMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public IActionResult Index()
        {
            return View("~/Views/FinancialAccounting/Master/AccountMainGroupMaster/Index.cshtml");
        }
        [HttpPost]
        public async Task<IActionResult> SaveGroupMaster([FromBody] AccountMainGroup model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    string checkQuery = @"SELECT COUNT(*) FROM GR_MAST WHERE COMP_CODE = @COMP_CODE AND NAME = @NAME";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        checkCmd.Parameters.AddWithValue("@NAME", model.group_name);
                        int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (count > 0)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Group name already exists!"
                            });
                        }
                    }
                    // -------------------------
                    // 2️⃣ INSERT IF NOT EXISTS
                    // -------------------------
                    using (SqlCommand cmd = new SqlCommand("sp_InsertGRMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@NAME", model.group_name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHORTNAME", model.short_name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TYPE", model.type ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SRNO", model.code);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EDATE", (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ACTIVE", model.active);
                        cmd.Parameters.AddWithValue("@Action", "Insert");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = "Inserted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }


        //public async Task<IActionResult> SaveGroupMaster([FromBody] AccountMainGroup model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);
        //    try
        //    {
        //        var globalVar = _globalVariableService.GetGlobalVariables();

        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_InsertGRMast", con)) 
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;

        //                cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
        //                cmd.Parameters.AddWithValue("@NAME", model.group_name ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SHORTNAME", model.short_name ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@TYPE", model.type ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@SRNO", model.code);
        //                cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
        //                cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
        //                cmd.Parameters.AddWithValue("@EUSER", (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@EDATE", (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@AED","A");

        //                cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
        //                cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
        //                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
        //                cmd.Parameters.AddWithValue("@ACTIVE", model.active);
        //                cmd.Parameters.AddWithValue("@Action", "Insert");
        //                await con.OpenAsync();
        //                await cmd.ExecuteNonQueryAsync();
        //            }
        //        }

        //        return Json(new { success = true, message = $"successful." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Server error: {ex.Message}");
        //    }
        //}
        [HttpPost]
        public async Task<IActionResult> UpdateGroupMaster([FromBody] AccountMainGroup model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertGRMast", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@CODE", model.code); 
                       cmd.Parameters.AddWithValue("@NAME", model.group_name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHORTNAME", model.short_name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@TYPE", model.type ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@SRNO", "");
                        cmd.Parameters.AddWithValue("@UUSER", (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@UDATE", (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE",DateTime.Now);
                        cmd.Parameters.AddWithValue("@AED", "A");

                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        cmd.Parameters.AddWithValue("@ACTIVE", model.active);
                        cmd.Parameters.AddWithValue("@Action", "Update");

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { success = true, message = $"Update successful." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Server error: {ex.Message}");
            }
        }

        public IActionResult GetAccountMainByCode([FromBody] CodeRequest request)
        {
            var item = new AccountMainGroup();
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT [CODE], [NAME], [SHORTNAME], [TYPE], [ACTIVE] FROM [GR_MAST] WHERE [CODE] = @CODE";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CODE", request.code);
                    con.Open();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            item = new AccountMainGroup
                            {
                                code = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                                group_name = rdr["NAME"]?.ToString(),
                                short_name = rdr["SHORTNAME"]?.ToString(),
                                type = rdr["TYPE"]?.ToString(),
                                active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0
                            };
                        }
                    }
                }
            }
            if (item == null || item.code == 0)
            {
                return NotFound(new { message = "No record found." });
            }
            return Json(item);
        }


        public class CodeRequest
        {
            public int code { get; set; }
        }
    }
}
