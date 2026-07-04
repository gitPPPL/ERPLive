using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models; // adjust namespace
using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;


namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class CourierTrackingEntryListRepository : ICourierTrackingEntryListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public CourierTrackingEntryListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public RepositoryResponseList<GetCourierTrackingModel> GetCourierTrackingEntryList(string searchTerm, int pageNumber, int pageSize)
        {
            var response = new RepositoryResponseList<GetCourierTrackingModel>
            {
                status = false,
                message = "No data found",
                totalCount = 0,
                data = new List<GetCourierTrackingModel>()
            };

            try
            {
                var global = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SearchTerm", searchTerm);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                        cmd.Parameters.AddWithValue("@Action", "SELECT");

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            var dataList = new List<GetCourierTrackingModel>();
                            while (reader.Read())
                            {
                                var model = new GetCourierTrackingModel
                                {
                                    VNo = reader["V_NO"]?.ToString() ?? "",
                                    DocType = reader["V_TYPE"]?.ToString() ?? "",
                                    DocNo = reader["DOC_ID"]?.ToString() ?? "",
                                    DocDate = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd") : "",
                                    PartyName = reader["PARTY_NAME"]?.ToString() ?? "",
                                    City = reader["CITY_NAME"]?.ToString() ?? "",
                                    CourierName = reader["COURIER_NAME"]?.ToString() ?? "",
                                    DocketNo = reader["DOCKET_NO"]?.ToString() ?? "",
                                    Purpose = reader["PURPOSE"]?.ToString() ?? "",
                                    Weight = reader["WEIGHT"] != DBNull.Value ? Convert.ToDecimal(reader["WEIGHT"]) : 0,
                                    Remarks = reader["REMARKS"]?.ToString() ?? ""
                                };
                                dataList.Add(model);
                            }

                            if (dataList.Any())
                            {
                                response.status = true;
                                response.message = "Data retrieved successfully";
                                response.totalCount = dataList.Count;
                                response.data = dataList;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.message = $"Error: {ex.Message}";
            }

            return response;
        }

        public async Task<RepositoryResponse> DeleteCourierTrackingEntry(string vNo, string docType)
        {
            var response = new RepositoryResponse
            {
                status = false,
                message = "Failed to delete courier tracking entry"
            };

            try
            {
                var global = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", docType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                        cmd.Parameters.AddWithValue("@Action", "DELETE");

                        var rowsParam = new SqlParameter("@RowsAffected", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };

                        cmd.Parameters.Add(rowsParam);

                        con.Open();   // Open only once

                        await cmd.ExecuteNonQueryAsync();

                        int rowsAffected = rowsParam.Value == DBNull.Value
                            ? 0
                            : Convert.ToInt32(rowsParam.Value);

                        if (rowsAffected > 0)
                        {
                            response.status = true;
                            response.message = "Courier tracking entry deleted successfully";
                        }
                        else
                        {
                            response.status = false;
                            response.message = "Record not found";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.message = $"Error: {ex.Message}";
            }

            return response;
        }

        // Fix for CS0535: Implementing GetNextDocNo
        public int GetNextDocNo(string docType)
        {
            int nextDocNo = 0;
            try
            {
                var global = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetNextDocNo", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DocType", docType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);

                        con.Open();
                        nextDocNo = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow)
                throw new Exception("Error fetching next document number", ex);
            }
            return nextDocNo;
        }

        // Fix for CS0535: Implementing SaveCourierData
        public string SaveCourierData(CourierTrackingModel model)
        {
            string result = "Failed";
            try
            {
                var global = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_SaveCourierData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_NO", model.V_No);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.DocType);
                        cmd.Parameters.AddWithValue("@PARTY_NAME", model.PartyName);
                        cmd.Parameters.AddWithValue("@CITY_NAME", model.City);
                        cmd.Parameters.AddWithValue("@COURIER_NAME", model.CourierName);
                        cmd.Parameters.AddWithValue("@DOCKET_NO", model.DocketNo);
                        cmd.Parameters.AddWithValue("@PURPOSE", model.Purpose);
                        cmd.Parameters.AddWithValue("@WEIGHT", model.Weight);
                        cmd.Parameters.AddWithValue("@REMARKS", model.Remarks);

                        con.Open();
                        result = cmd.ExecuteScalar()?.ToString() ?? "Failed";
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow)
                throw new Exception("Error saving courier data", ex);
            }
            return result;
        }

        // Fix for CS0535: Implementing GetCourierData
        public GetCourierTrackingModel GetCourierData(string docType, string docNo)
        {
            var result = new GetCourierTrackingModel();
            try
            {
                var global = _globalVariableService.GetGlobalVariables();
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetCourierData", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@DocType", docType);
                        cmd.Parameters.AddWithValue("@DocNo", docNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result.VNo = reader["V_NO"]?.ToString() ?? "";
                                result.DocType = reader["V_TYPE"]?.ToString() ?? "";
                                result.DocNo = reader["DOC_ID"]?.ToString() ?? "";
                                result.DocDate = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd") : "";
                                result.PartyName = reader["PARTY_NAME"]?.ToString() ?? "";
                                result.City = reader["CITY_NAME"]?.ToString() ?? "";
                                result.CourierName = reader["COURIER_NAME"]?.ToString() ?? "";
                                result.DocketNo = reader["DOCKET_NO"]?.ToString() ?? "";
                                result.Purpose = reader["PURPOSE"]?.ToString() ?? "";
                                result.Weight = reader["WEIGHT"] != DBNull.Value ? Convert.ToDecimal(reader["WEIGHT"]) : 0;
                                result.Remarks = reader["REMARKS"]?.ToString() ?? "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception (log or rethrow)
                throw new Exception("Error fetching courier data", ex);
            }
            return result;
        }
    }
}