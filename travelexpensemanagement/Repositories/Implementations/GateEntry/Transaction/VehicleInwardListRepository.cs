using DocumentFormat.OpenXml.Drawing;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class VehicleInwardListRepository : IVehicleInwardListRepository
    {
        private readonly GlobalVariableService _globalValue;
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly IWebHostEnvironment _env;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        public VehicleInwardListRepository(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, IWebHostEnvironment env,
            travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _env = env;
            _logService = logService;
        }
        public async Task<RepositoryResponse> DeleteTransportInward(string docid)
        {
            var response = new RepositoryResponse();
            try
            {
                if (string.IsNullOrEmpty(docid))
                {
                    response.status = false;
                    response.message = "Invalid ID";
                    return response;
                }

                var userSession = _globalValue.GetGlobalVariables();
                string VType = docid.Substring(0, 4);
                string VNo = docid.Substring(4);
                string? oldFile = null;
                
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand cmdOld = new SqlCommand("SELECT IMAGEPATH FROM GATE1 WHERE DOC_ID = @DOC_ID AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE", con, transaction))
                            {
                                cmdOld.Parameters.AddWithValue("@DOC_ID", docid);
                                cmdOld.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode ?? (object)DBNull.Value);
                                cmdOld.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode ?? (object)DBNull.Value);
                                cmdOld.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                oldFile = (await cmdOld.ExecuteScalarAsync())?.ToString();
                            }

                            string[] deleteQueries = {
                            "DELETE FROM gate1 WHERE COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE AND V_TYPE = @V_TYPE AND V_NO = @V_NO",
                        };

                            foreach (var query in deleteQueries)
                            {
                                using (var cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                    cmd.Parameters.AddWithValue("@V_NO", VNo);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            //=============Delete from img table
                            bool isExist = false;
                            using (SqlCommand cmd = new SqlCommand("sp_TransportInwardEntry_Img", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Action", "IsExist");
                                cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                cmd.Parameters.AddWithValue("@V_NO", VNo);
                                var result = await cmd.ExecuteScalarAsync();
                                isExist = Convert.ToInt32(result) == 1;
                            }
                            if (isExist)
                            {
                                using (SqlCommand cmd = new SqlCommand("sp_TransportInwardEntry_Img", con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@Action", "Delete");
                                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                                    cmd.Parameters.AddWithValue("@YEAR_CODE", userSession.PubFYearCode);
                                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                                    cmd.Parameters.AddWithValue("@V_TYPE", VType);
                                    cmd.Parameters.AddWithValue("@V_NO", VNo);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            transaction.Commit();
                            //==========Delete img file
                            if (!string.IsNullOrEmpty(oldFile))
                            {
                                string folder = System.IO.Path.Combine(_env.WebRootPath, "Attachments\\TransportInward");
                                string fullPath = System.IO.Path.Combine(folder, oldFile);

                                if (System.IO.File.Exists(fullPath))
                                {
                                    System.IO.File.Delete(fullPath);
                                }
                            }
                            //===========log insert
                            _logService.InsertLog("GATE1", "Vehicle Inward", "Transaction", "Delete", VType, VNo.ToString(), null);
                            response.status = true;
                            response.message = "Data deleted successfully";
                            return response;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            response.status = true;
                            response.message = $"Delete failed: {ex.Message}";
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = true;
                response.message = ex.Message;
                return response;
                //return Json(new { status = false, message = ex.Message });
            }
        }
        public async Task<RepositoryResponseList<ExpandoObject>> ExportVehicleInwardAsExcel()
        {
            var res = new RepositoryResponseList<ExpandoObject>();
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@Action", "Excel" }
                };
                var dataList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetTransportInwardEntry]", parameter);
                res.status = true;
                res.data = dataList.ToList();
                return res;
            }
            catch (Exception ex)
            {
                res.status = false;
                res.message = ex.Message;
                return res;
            }
        }
        public async Task<RepositoryResponseList<TransportInwardListModel>> GetTransportInwardList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var response = new RepositoryResponseList<TransportInwardListModel>();
            var TransportInwardList = new List<TransportInwardListModel>();
            var gv = _globalValue.GetGlobalVariables();
            int totalCount = 0;
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_GetTransportInwardEntry", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "TransportInwardEntryList");
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                TransportInwardList.Add(new TransportInwardListModel
                                {
                                    docid = reader["DOC_ID"]?.ToString(),
                                    vno = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : null,
                                    dono = reader["DoNo"] != DBNull.Value ? Convert.ToInt32(reader["DoNo"]) : null,
                                    vdate = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                    vtime = reader["V_TIME"]?.ToString(),
                                    partyname = reader["partyname"]?.ToString(),
                                    transport = reader["transport"]?.ToString(),
                                    truckno = reader["TRUCK_NO"]?.ToString()
                                });
                            }
                            if (await reader.NextResultAsync())
                            {
                                await reader.ReadAsync();
                                totalCount = (int)reader["TotalCount"];
                            }
                        }
                    }
                    response.status = true;
                    response.totalCount = totalCount;
                    response.data = TransportInwardList;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.status = true;
                response.message = ex.Message;
                return response;
            }
        }
        public async Task<RepositoryResponseList<ExpandoObject>> VehicleInwardEntryDetails(string docid)
        {
            var res = new RepositoryResponseList<ExpandoObject>();
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                if (string.IsNullOrEmpty(docid))
                {
                    res.status = false;
                    res.message = "Invalid ID";
                    return res;
                }
                var parameter = new Dictionary<string, object>
                {
                    {"@COMP_CODE", usersession.PubCompCode },
                    {"@YEAR_CODE", usersession.PubFYearCode },
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@V_TYPE", docid.Substring(0, 4) },
                    {"@V_NO", docid.Substring(4) },
                    {"@Action", "EntryDetail" }
                };
                var entryDetailList = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetTransportInwardEntry]", parameter);
                res.status = true;
                res.data = entryDetailList.ToList();
                return res;
            }
            catch (Exception ex)
            {
                res.status = false;
                res.message = ex.Message;
                return res;
            }
        }
    }
}
