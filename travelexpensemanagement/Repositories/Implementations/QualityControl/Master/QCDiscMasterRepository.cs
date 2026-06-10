using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class QCDiscMasterRepository : IQCDiscMasterRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public QCDiscMasterRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
        }

        public bool SaveAndUpdateData(QCDISC_MAST model)
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "QDIS");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE);
                    cmd.Parameters.AddWithValue("@ITEM_NAME", model.ITEM_NAME ?? string.Empty);
                    cmd.Parameters.AddWithValue("@QCP_CODE", model.QCP_CODE);
                    cmd.Parameters.AddWithValue("@QCP_DIFF", model.QCP_DIFF);
                    cmd.Parameters.AddWithValue("@UUSER", globalVariables.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", globalVariables.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVariables.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@EUSER", globalVariables.PubUserId);
                    cmd.Parameters.AddWithValue("@Action", model.ACTION ?? "INSERT");

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return true;
        }

        public (List<QCDISC_MAST> Data, int TotalCount) GetQcDiscOnChange(int itemCode)
        {
            List<QCDISC_MAST> discList = new();
            int totalCount = 0;
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "ONLOAD");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            discList.Add(new QCDISC_MAST
                            {
                                COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                V_TYPE = reader["V_TYPE"]?.ToString(),
                                ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                                ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                QCP_CODE = reader["QCP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["QCP_CODE"]) : 0,
                                QCP_DIFF = reader["QCP_DIFF"] != DBNull.Value ? Convert.ToDecimal(reader["QCP_DIFF"]) : 0,
                                UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                AED = reader["AED"]?.ToString(),
                                WSID = reader["WSID"]?.ToString(),
                                LIP = reader["LIP"]?.ToString(),
                                LID = reader["LID"]?.ToString(),
                                SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0,
                                ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value
                                ? Convert.ToInt32(reader["TotalCount"])
                                : 0;
                        }
                    }
                }
            }

            return (discList, totalCount);
        }

        public bool DeleteQcDiscByCode(int itemCode)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "DELETE");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return true;
        }
    }
}
