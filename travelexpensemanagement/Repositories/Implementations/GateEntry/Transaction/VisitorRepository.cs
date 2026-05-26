using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Gate_Entry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class VisitorRepository : IVisitorRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public VisitorRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public string GenerateVNo()
        {
            string newV_NO = "00001";
            string vType = "VISI";

            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                string query = @"SELECT ISNULL(MAX(CAST(RIGHT(V_NO,5) AS INT)), 0) + 1  FROM VISITOR WHERE V_TYPE=@V_TYPE AND COMP_CODE=@COMP_CODE  AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE";
                SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@COMP_CODE", getdata.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                cmd.Parameters.AddWithValue("@YEAR_CODE", getdata.PubFYearCode);

                int nextNo = Convert.ToInt32(cmd.ExecuteScalar());

                newV_NO = prefixYR + nextNo.ToString("D5");
            }

            return newV_NO;
        }

        public bool IsDuplicate(string docId)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM VISITOR WHERE DOC_ID=@docId", con))
                {
                    cmd.Parameters.AddWithValue("@docId", docId);
                    con.Open();
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
        }

        public VISITOR GetVisitorImage(string docId)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT IMG_FILE, FILE_NAME FROM VISITOR WHERE DOC_ID=@docId", con))
                {
                    cmd.Parameters.AddWithValue("@docId", docId);
                    con.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new VISITOR
                            {
                                IMG_FILE = reader["IMG_FILE"] as byte[],
                                FILE_NAME = reader["FILE_NAME"]?.ToString()
                            };
                        }
                    }
                }
            }
            return null;
        }

        public bool SaveUpdateVisitor(VISITOR model, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID);

                    cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
                    cmd.Parameters.AddWithValue("@ORGANIZATION", model.ORGANIZATION ?? "");
                    cmd.Parameters.AddWithValue("@ADDRESS", model.ADDRESS ?? "");

                    cmd.Parameters.AddWithValue("@MEET_CODE", model.MEET_CODE ?? 0);
                    cmd.Parameters.AddWithValue("@MEET_NAME", model.MEET_NAME ?? "");

                    cmd.Parameters.AddWithValue("@IN_TIME", model.IN_TIME ?? "");
                    cmd.Parameters.AddWithValue("@OUT_DATE", model.OUT_DATE);
                    cmd.Parameters.AddWithValue("@OUT_TIME", model.OUT_TIME ?? "");

                    cmd.Parameters.AddWithValue("@PURPOSE", model.PURPOSE ?? "");
                    cmd.Parameters.AddWithValue("@MOBILE_NO", model.MOBILE_NO ?? "");
                    cmd.Parameters.AddWithValue("@VEHICLE_NO", model.VEHICLE_NO ?? "");
                    cmd.Parameters.AddWithValue("@MATERIAL", model.MATERIAL ?? "");

                    cmd.Parameters.AddWithValue("@CARD_NO", model.CARD_NO ?? "");
                    cmd.Parameters.AddWithValue("@CARD_CODE", model.CARD_CODE ?? 0);

                    cmd.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary, -1).Value = model.IMG_FILE ?? (object)DBNull.Value;

                    cmd.Parameters.AddWithValue("@FILE_NAME", model.FILE_NAME ?? "");
                    cmd.Parameters.AddWithValue("@REMARKS", model.REMARKS ?? "");

                    cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public bool DeleteVisitor(string docId)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);

                    con.Open();
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }

        public object GetVisitorByMobile(string mobileNo)
        {
            if (string.IsNullOrEmpty(mobileNo))
                return new { success = false };

            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("sp_VISITOR_MGMT", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "GETBYMOBILE");
                    cmd.Parameters.AddWithValue("@MOBILE_NO", mobileNo);

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                success = true,
                                data = new
                                {
                                    name = reader["NAME"]?.ToString(),
                                    address = reader["ADDRESS"]?.ToString(),
                                    organization = reader["ORGANIZATION"]?.ToString(),
                                    purpose = reader["PURPOSE"]?.ToString(),
                                    meet_CODE = reader["MEET_CODE"]?.ToString(),
                                    meet_NAME = reader["MEET_NAME"]?.ToString(),
                                    vehicle_NO = reader["VEHICLE_NO"]?.ToString(),
                                    material = reader["MATERIAL"]?.ToString(),
                                    remarks = reader["REMARKS"]?.ToString()
                                }
                            };
                        }
                    }
                }
            }

            return new { success = false };
        }

    }
}
