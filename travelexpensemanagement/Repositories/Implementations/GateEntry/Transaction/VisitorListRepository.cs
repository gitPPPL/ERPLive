using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Gate_Entry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class VisitorListRepository : IVisitorListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public VisitorListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public (List<VISITOR> visitors, int totalCount) GetAllVisitors(string searchTerm, int pageNumber, int pageSize)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            var visitors = new List<VISITOR>();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    conn.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            visitors.Add(new VISITOR
                            {
                                V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                                V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null,
                                DOC_ID = reader["DOC_ID"].ToString(),
                                NAME = reader["NAME"]?.ToString(),
                                ORGANIZATION = reader["ORGANIZATION"]?.ToString(),
                                IN_TIME = reader["IN_TIME"]?.ToString(),
                                OUT_TIME = reader["OUT_TIME"]?.ToString(),
                                MEET_NAME = reader["MEET_NAME"]?.ToString(),
                                PURPOSE = reader["PURPOSE"]?.ToString(),
                                ADDRESS = reader["ADDRESS"]?.ToString(),
                                MOBILE_NO = reader["MOBILE_NO"]?.ToString(),
                                VEHICLE_NO = reader["VEHICLE_NO"]?.ToString(),
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }

            return (visitors, totalCount);
        }

        public VISITOR GetVisitorByVno(string docId, out string base64Image)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            VISITOR visitor = null;
            base64Image = null;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "GETBYID");
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    conn.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            visitor = new VISITOR
                            {
                                V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : (int?)null,
                                V_TYPE = rdr["V_TYPE"]?.ToString(),
                                V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : (DateTime?)null,
                                DOC_ID = rdr["DOC_ID"]?.ToString(),
                                NAME = rdr["NAME"]?.ToString(),
                                CARD_NO = rdr["CARD_NO"]?.ToString(),
                                ORGANIZATION = rdr["ORGANIZATION"]?.ToString(),
                                ADDRESS = rdr["ADDRESS"]?.ToString(),
                                MEET_CODE = rdr["MEET_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["MEET_CODE"]) : (int?)null,
                                MEET_NAME = rdr["MEET_NAME"]?.ToString(),
                                IN_TIME = rdr["IN_TIME"]?.ToString(),
                                OUT_DATE = rdr["OUT_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["OUT_DATE"]) : (DateTime?)null,
                                OUT_TIME = rdr["OUT_TIME"]?.ToString(),
                                MOBILE_NO = rdr["MOBILE_NO"]?.ToString(),
                                PURPOSE = rdr["PURPOSE"]?.ToString(),
                                VEHICLE_NO = rdr["VEHICLE_NO"]?.ToString(),
                                MATERIAL = rdr["MATERIAL"]?.ToString(),
                                REMARKS = rdr["REMARKS"]?.ToString(),
                                IMG_FILE = rdr["IMG_FILE"] != DBNull.Value ? (byte[])rdr["IMG_FILE"] : null,
                                FILE_NAME = rdr["FILE_NAME"]?.ToString()
                            };
                        }
                    }
                }
            }

            if (visitor != null && visitor.IMG_FILE != null)
            {
                base64Image = Convert.ToBase64String(visitor.IMG_FILE);
            }

            return visitor;
        }

        public async Task<DataTable> ExportVisitorToExcel(string searchTerm)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DataTable dt = new DataTable();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "EXPORT");
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageNumber", 1);
                    cmd.Parameters.AddWithValue("@PageSize", 100000);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        } 

        public async Task<DataTable> ExportVisitorToPdf(string searchTerm)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DataTable dt = new DataTable();

            using (var conn = _dbConnection.GetErpConnection())
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "SELECT_PDF");
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@SearchTerm", (object)searchTerm ?? DBNull.Value);

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            return dt;
        }

    }
}
