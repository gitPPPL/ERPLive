using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class QCDiscMasterListRepository : IQCDiscMasterListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        public QCDiscMasterListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _globalValidationdate = globalValidationdate;
        }

        public (List<object> Data, int TotalCount) GetAllListData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();

            List<object> list = new();
            int totalCount = 0;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "QDIS");
                    cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                    cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@Action", "SELECT");

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                                ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                QCP_CODE = reader["QCP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["QCP_CODE"]) : 0,
                                PARAMETER_NAME = reader["PARAMETER_NAME"]?.ToString(),
                                QCP_DIFF = reader["QCP_DIFF"] != DBNull.Value ? Convert.ToDecimal(reader["QCP_DIFF"]) : 0
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }
                    }
                }
            }

            return (list, totalCount);
        }

        public byte[] ExportAllDocs()
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var parameters = new Dictionary<string, object>
            {
                { "@COMP_CODE", gv.PubCompCode },
                { "@Action", "Excel" }
            };

            return _globalValidationdate.ExportToExcel(
                "sp_QCDISC_MAST",
                "QCDisk Master",
                parameters);
        }

    }

}
