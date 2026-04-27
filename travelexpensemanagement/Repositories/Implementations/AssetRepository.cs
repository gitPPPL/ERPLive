using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Admin.Setup;
using travelexpensemanagement.Repositories.Interfaces;

namespace travelexpensemanagement.Repositories.Implementations
{
    public class AssetRepository : IAssetRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public AssetRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        //  Duplicate Check
        public bool IsDuplicate(int yearCode, int compCode, int acCode)
        {
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM ASSET_MAST 
                    WHERE YEAR_CODE=@YEAR_CODE AND COMP_CODE=@COMP_CODE AND AC_CODE=@AC_CODE", con))
                {
                    cmd.Parameters.AddWithValue("@YEAR_CODE", yearCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                    cmd.Parameters.AddWithValue("@AC_CODE", acCode);

                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }
        //  Insert
        public bool InsertAsset(AssetModel model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                using (SqlCommand cmd = new SqlCommand("sp_InsertAssetMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@YEAR_CODE", Convert.ToInt32(globalVar.PubFYearCode));
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@COMP_CODE", Convert.ToInt32(globalVar.PubCompCode));
                    cmd.Parameters.AddWithValue("@AC_CODE", model.AC_CODE);
                    cmd.Parameters.AddWithValue("@AC_NAME", model.AC_NAME);
                    cmd.Parameters.AddWithValue("@OP_AMT", model.OP_AMT);
                    cmd.Parameters.AddWithValue("@DEP_AMT", model.DEP_AMT);
                    cmd.Parameters.AddWithValue("@DEP_RATE", model.DEP_RATE);
                    cmd.Parameters.AddWithValue("@SHIFT_CALC", model.SHIFT_CALC);
                    cmd.Parameters.AddWithValue("@LIFE", model.LIFE);
                    cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", "");
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@Action", "Insert");

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
        //  Get By SRNO
        public AssetModel GetAssetBySrno(int srno)
        {
            AssetModel asset = null;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT AC_CODE, AC_NAME, OP_AMT, DEP_AMT, DEP_RATE, SHIFT_CALC, LIFE, SRNO 
                    FROM ASSET_MAST 
                    WHERE SRNO=@SRNO", con))
                {
                    cmd.Parameters.AddWithValue("@SRNO", srno);
                    con.Open();

                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            asset = new AssetModel
                            {
                                AC_CODE = Convert.ToInt32(rdr["AC_CODE"]),
                                AC_NAME = rdr["AC_NAME"].ToString(),
                                OP_AMT = Convert.ToDecimal(rdr["OP_AMT"]),
                                DEP_AMT = Convert.ToDecimal(rdr["DEP_AMT"]),
                                DEP_RATE = Convert.ToDecimal(rdr["DEP_RATE"]),
                                SHIFT_CALC = Convert.ToInt32(rdr["SHIFT_CALC"]),
                                LIFE = Convert.ToInt32(rdr["LIFE"]),
                                SRNO = Convert.ToInt32(rdr["SRNO"])
                            };
                        }
                    }
                }
            }

            return asset;
        }
        //  Update
        public bool UpdateAsset(AssetModel model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertAssetMaster", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@SRNO", model.SRNO);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", Convert.ToInt32(globalVar.PubFYearCode));
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                    cmd.Parameters.AddWithValue("@COMP_CODE", Convert.ToInt32(globalVar.PubCompCode));
                    cmd.Parameters.AddWithValue("@AC_CODE", model.AC_CODE);
                    cmd.Parameters.AddWithValue("@AC_NAME", model.AC_NAME);
                    cmd.Parameters.AddWithValue("@OP_AMT", model.OP_AMT);
                    cmd.Parameters.AddWithValue("@DEP_AMT", model.DEP_AMT);
                    cmd.Parameters.AddWithValue("@DEP_RATE", model.DEP_RATE);
                    cmd.Parameters.AddWithValue("@SHIFT_CALC", model.SHIFT_CALC);
                    cmd.Parameters.AddWithValue("@LIFE", model.LIFE);
                    cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@Action", "Update");

                    con.Open();
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}