using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityMaster;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class TemperatureMasterListRepository : ITemperatureMasterListRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        public TemperatureMasterListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _globalValidationdate = globalValidationdate;
        }

        public (List<TempratureMasterModel> Data, int TotalCount)GetTemperatureList(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            List<TempratureMasterModel> temperatureList = new();
            int totalCount = 0;

            using (SqlConnection conn = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_TempratureMaster", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@SearchTerm",string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
                cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                cmd.Parameters.AddWithValue("@CODE", DBNull.Value);

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        temperatureList.Add(new TempratureMasterModel
                        {
                            CODE = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : 0,
                            Name = reader["NAME"]?.ToString() ?? string.Empty,
                            ShortName = reader["SHORTNAME"]?.ToString() ?? string.Empty,
                            SortNo = reader["SORT_NO"] != DBNull.Value ? Convert.ToInt32(reader["SORT_NO"]) : 0,
                            Active = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0
                        });
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                    }
                }
            }

            return (temperatureList, totalCount);
        }
        
        public TempratureMasterModel GetCategoryCode(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        
            TempratureMasterModel model = null;
                                                             
            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_TempratureMaster", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                        
                cmd.Parameters.AddWithValue("@Action", "SELECT");
                cmd.Parameters.AddWithValue("@CODE", code);
                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
            
                con.Open();
            
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        model = new TempratureMasterModel
                        {
                            CODE = rdr["CODE"] != DBNull.Value ? Convert.ToInt32(rdr["CODE"]) : 0,
                            Name = rdr["NAME"]?.ToString(),
                            ShortName = rdr["SHORTNAME"]?.ToString(),
                            SortNo = rdr["SORT_NO"] != DBNull.Value ? Convert.ToInt32(rdr["SORT_NO"]) : 0,
                            UUser = rdr["UUSER"] != DBNull.Value ? Convert.ToInt32(rdr["UUSER"]) : 0,
                            UDate = rdr["UDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["UDATE"]) : DateTime.MinValue,
                            EUser = rdr["EUSER"] != DBNull.Value ? Convert.ToInt32(rdr["EUSER"]) : 0,
                            EDate = rdr["EDATE"] != DBNull.Value ? Convert.ToDateTime(rdr["EDATE"]) : DateTime.MinValue,
                            Aed = rdr["AED"]?.ToString(),
                            Wsid = rdr["WSID"]?.ToString(),
                            Lip = rdr["LIP"]?.ToString(),
                            Lid = rdr["LID"]?.ToString(),
                            Active = rdr["ACTIVE"] != DBNull.Value ? Convert.ToInt32(rdr["ACTIVE"]) : 0,
                            VType = rdr["V_TYPE"]?.ToString()
                                                                                                                                                                                
                        };
                    }
                }
            }

            return model;
        }

        public bool Delete(int code)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_TempratureMaster", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "DELETE");
                cmd.Parameters.AddWithValue("@CODE", code);
                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return true;
        }

        public byte[] ExportAllDocs()
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var parameters = new Dictionary<string, object>
            {
                { "@COMP_CODE", gv.PubCompCode },
                { "@Action", "Excel" }
            };

            return _globalValidationdate.ExportToExcel("sp_TempratureMaster", "Temprature Master",parameters);
        }

    }

}
