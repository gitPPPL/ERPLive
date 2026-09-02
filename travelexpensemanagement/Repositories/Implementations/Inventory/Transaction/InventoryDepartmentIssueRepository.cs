using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Pages.Admin.SystemInitilization.DocumentTypeMasterList;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class InventoryDepartmentIssueRepository : IInventoryDepartmentIssueRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;

        public InventoryDepartmentIssueRepository(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            DropdownService dropdownService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
        }

        public object DDlVType(string formName)
        {
            var getData = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string vType = formName switch
                {
                    "RecdAfterProdEntry" => "'PRDR'",
                    "SFGIssueToDispatch" => "'RAID'",
                    "SFGAdjustmentIssue" => "'RMAI'",
                    "SFGAdjustmentReceived" => "'RMAR'",
                    "ChemicalIssue" => "'CMIS'",
                    "ChemicalReceived" => "'CMRC'",
                    "AdjustmentIssue" => "'STAI','STPR'",
                    "AdjustmentReceived" => "'STAR','SRCO'",
                    "RMGTOWaste" => "'RAIV','RAIT'",
                    _ => throw new ArgumentException(
                        $"Invalid Formname: {formName}",
                        nameof(formName))
                };

                string query = $@" SELECT  CODE,NAME FROM DOCTYPE_MAST WHERE CODE IN ({vType})  ORDER BY NAME";

                var data = _dropdownService.GetDropdownList(query);

                return data;
            }
        }

        public object DDlItemName(string formName, string V_TYPE)
        {
            var getData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_InventoryDepartmentIssue", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@ACTION", SqlDbType.VarChar, 100).Value = "ItemName";
                cmd.Parameters.Add("@FormName", SqlDbType.VarChar, 100).Value = formName;
                cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar, 50).Value = V_TYPE;
                cmd.Parameters.Add("@COMP_CODE", SqlDbType.VarChar, 50).Value = getData.PubCompCode;
                cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.VarChar, 50).Value = getData.PubBranchCode;

                con.Open();

                var data = new List<object>();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new
                        {
                            ItemCode = reader["icode"]?.ToString(),
                            ItemName = reader["itemname"]?.ToString(),
                            unit = reader["unit"]?.ToString(),
                            ucode = reader["ucode"]?.ToString(),
                            ShortName = reader["ShortName"]?.ToString(),
                        });
                    }
                }

                return data;
            }
        }

        public object CopyData(string V_TYPE)
        {
            var getData = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_InventoryDepartmentIssue", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@ACTION", SqlDbType.VarChar, 100).Value = "CopyData";
                cmd.Parameters.Add("@V_TYPE", SqlDbType.VarChar, 50).Value = V_TYPE;
                cmd.Parameters.Add("@COMP_CODE", SqlDbType.VarChar, 50).Value = getData.PubCompCode;
                cmd.Parameters.Add("@BRANCH_CODE", SqlDbType.VarChar, 50).Value = getData.PubBranchCode;
                cmd.Parameters.Add("@Year_Code", SqlDbType.VarChar, 50).Value = getData.PubFYearCode;

                con.Open();

                var data = new List<object>();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        data.Add(new
                        {

                            VNo = reader["VNo"] == DBNull.Value ? (long?)null : Convert.ToInt64(reader["VNo"]),
                            VDate = reader["VDate"] == DBNull.Value  ? (DateTime?)null  : Convert.ToDateTime(reader["VDate"]),
                            ItemName = reader["ItemName"] == DBNull.Value  ? null  : reader["ItemName"].ToString(),
                            Nos = reader["Nos"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Nos"]),
                            Qty = reader["Qty"] == DBNull.Value   ? (decimal?)null  : Convert.ToDecimal(reader["Qty"]),
                            Unit = reader["Unit"] == DBNull.Value ? null  : reader["Unit"].ToString(),
                            Make = reader["Make"] == DBNull.Value  ? null : reader["Make"].ToString(),
                            Place = reader["Place"] == DBNull.Value  ? null  : reader["Place"].ToString(),
                            Remarks = reader["Remarks"] == DBNull.Value ? null : reader["Remarks"].ToString(),
                            ItemCode = reader["ItemCode"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["ItemCode"]),
                            UOM_CODE = reader["UOM_CODE"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["UOM_CODE"]),
                            MAKE_CODE = reader["MAKE_CODE"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["MAKE_CODE"]),
                            PlaceCode = reader["PlaceCode"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["PlaceCode"])
                        });
                    }
                }

                return data;
            }
        }

    }
}