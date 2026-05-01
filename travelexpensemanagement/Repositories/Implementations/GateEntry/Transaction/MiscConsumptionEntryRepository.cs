using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class MiscConsumptionEntryRepository : IMiscConsumptionRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public MiscConsumptionEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public string GenerateVNo(string vType)
        {
            string newV_NO = "00000";

            var getdata = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // PREFIXYR
                string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                string prefixYR = "0000";

                using (SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con))
                {
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";
                }

                // LAST V_NO
                string lastV_NO_Query = @"SELECT MAX(CAST(V_NO AS INT)) FROM GATE1 WHERE COMP_CODE = @CompCode AND YEAR_CODE = @YearCode AND BRANCH_CODE = @BranchCode AND V_TYPE = @Vtype";

                using (SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con))
                {
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@BranchCode", 1);
                    lastVnoCmd.Parameters.AddWithValue("@Vtype", vType);

                    object result = lastVnoCmd.ExecuteScalar();

                    if (result != DBNull.Value && result != null)
                    {
                        int lastV_NO = Convert.ToInt32(result);
                        newV_NO = (lastV_NO + 1).ToString("D5");
                    }
                    else
                    {
                        newV_NO = prefixYR + "00001";
                    }
                }
            }

            return newV_NO;
        }

        public string SaveMiscConsumption(MiscConsumptionEntry_Header header, List<Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                // DELETE OLD DETAILS
                string deleteSql = @"DELETE FROM GATE2 WHERE COMP_CODE = @CompCode AND V_NO = @VNo AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode;";

                using (var deleteCmd = conn.CreateCommand())
                {
                    deleteCmd.CommandText = deleteSql;
                    deleteCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@VNo", header.V_NO);
                    deleteCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                    deleteCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                    deleteCmd.ExecuteNonQuery();
                }

                conn.Close();
                conn.Open();

                // HEADER SAVE
                using (var cmd = new SqlCommand("sp_MiscConsumptionEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@DOC_ID", header.V_TYPE + header.V_NO);

                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);

                    cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                    cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                    cmd.Parameters.AddWithValue("@V_TIME", header.V_TIME);

                    cmd.Parameters.AddWithValue("@RETURN_DATE", header.RETURN_DATE);
                    cmd.Parameters.AddWithValue("@RESPONSIBLE_PERSON", header.RESPONSIBLE_PERSONB);

                    cmd.Parameters.AddWithValue("@PARTY_CODE", header.PARTY_CODE);
                    cmd.Parameters.AddWithValue("@PARTY_NAME", header.PARTY_NAME);

                    cmd.Parameters.AddWithValue("@TRUCK_NO", header.TRUCK_NO);
                    cmd.Parameters.AddWithValue("@WAYBILL_NO", header.WAYBILL_NO);

                    cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS);

                    cmd.Parameters.AddWithValue("@ADD1", header.Add1);
                    cmd.Parameters.AddWithValue("@ADD2", header.Add2);
                    cmd.Parameters.AddWithValue("@ADD3", header.Add3);

                    cmd.Parameters.AddWithValue("@PARTY_CITY", header.PARTY_CITY);
                    cmd.Parameters.AddWithValue("@PARTY_GST", header.PARTY_GST);
                    cmd.Parameters.AddWithValue("@PARTY_PINCODE", header.PARTY_PINCODE);
                    cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", header.PARTY_ADDRESSID);

                    cmd.Parameters.AddWithValue("@ITEM_TYPE", header.ITEM_TYPE);

                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);

                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.ExecuteNonQuery();
                }

                // DETAILS SAVE
                foreach (var item in details)
                {
                    if (string.IsNullOrWhiteSpace(item.ITEM_NAME))
                        continue;

                    using var cmd = new SqlCommand("sp_MiscConsumptionEntry", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "INSERT");
                    cmd.Parameters.AddWithValue("@SaveAction", "Details");

                    cmd.Parameters.AddWithValue("@DOC_ID", header.V_TYPE + header.V_NO);
                    cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@V_TYPE", header.V_TYPE);
                    cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);

                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);

                    cmd.Parameters.AddWithValue("@ITEM_CODE", item.ITEM_CODE);
                    cmd.Parameters.AddWithValue("@ITEM_NAME", item.ITEM_NAME);
                    cmd.Parameters.AddWithValue("@DEPT_CODE", item.DEPT_CODE);

                    cmd.Parameters.AddWithValue("@UOM_CODE", item.UOM_CODE);
                    cmd.Parameters.AddWithValue("@UOM_NAME", item.UOM_NAME);

                    cmd.Parameters.AddWithValue("@NOS", item.NOS);
                    cmd.Parameters.AddWithValue("@QTY", item.QTY);

                    cmd.Parameters.AddWithValue("@REMARKS", item.REMARKS);

                    cmd.Parameters.AddWithValue("@REF_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@REF_TYPE", header.V_TYPE);

                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);

                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);

                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.ExecuteNonQuery();
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
