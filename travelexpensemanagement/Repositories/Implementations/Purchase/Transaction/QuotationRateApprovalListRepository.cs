using Microsoft.Data.SqlClient;
using System.Data;
using System.Dynamic;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class QuotationRateApprovalListRepository : IQuotationRateApprovalListRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public QuotationRateApprovalListRepository(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
        }

        public async Task<IReadOnlyList<ExpandoObject>> GetQuotationRateListAsync()
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                { "@COMP_CODE", userSession.PubCompCode },
                { "@YEAR_CODE", userSession.PubFYearCode },
                { "@BRANCH_CODE", userSession.PubBranchCode },
                { "@Action", "List" }
            };

            return await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_QuotationRateApproval]",
                parameter);
        }

        public async Task<int> DeleteQuotationRateApprovalAsync(string docId)
        {
            using (var con = _dbcontext.GetErpConnection())
            {
                var userSession = _globalValue.GetGlobalVariables();

                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QuotationRateApproval]", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "Delete");
                    cmd.Parameters.AddWithValue("@COMP_CODE", userSession.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", userSession.PubBranchCode);
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);

                    var returnParam = new SqlParameter("@ResultVal", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    cmd.Parameters.Add(returnParam);

                    var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 5000)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);

                    await con.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    return (int)returnParam.Value;
                }
            }
        }

        public async Task<IReadOnlyList<ExpandoObject>> GetPurchaseReceiptHistoryAsync(string itemCode)
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                { "@COMP_CODE", userSession.PubCompCode },
                { "@BRANCH_CODE", userSession.PubBranchCode },
                { "@ItemCode", itemCode },
                { "@Action", "PurchaseReceiptHistory" }
            };

            return await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_QuotationRateApproval]",
                parameter);
        }

        public async Task<IReadOnlyList<ExpandoObject>> GetQuotationApprovalHistoryAsync(string itemCode)
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                { "@COMP_CODE", userSession.PubCompCode },
                { "@YEAR_CODE", userSession.PubFYearCode },
                { "@BRANCH_CODE", userSession.PubBranchCode },
                { "@ItemCode", itemCode },
                { "@Action", "QuotationApprovalHistory" }
            };

            return await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameter);
        }

        public async Task<IReadOnlyList<ExpandoObject>> GetPurchaseOrderHistoryAsync(string itemCode)
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                { "@COMP_CODE", userSession.PubCompCode },
                { "@BRANCH_CODE", userSession.PubBranchCode },
                { "@ItemCode", itemCode },
                { "@Action", "PurchaseOrderHistory" }
            };

            return await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_QuotationRateApproval]",
                parameter);
        }

        public async Task<IReadOnlyList<ExpandoObject>> GetPurchaseOrderApprovalEntryDetailsAsync(string docId)
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                { "@COMP_CODE", userSession.PubCompCode },
                { "@YEAR_CODE", userSession.PubFYearCode },
                { "@BRANCH_CODE", userSession.PubBranchCode },
                { "@V_TYPE", docId.Substring(0, 4) },
                { "@V_NO", docId.Substring(4) },
                { "@Action", "EntryDetail" }
            };

            return await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_QuotationRateApproval]",
                parameter);
        }

        public async Task<IReadOnlyList<ExpandoObject>> ExportAllDocumentsAsync()
        {
            var userSession = _globalValue.GetGlobalVariables();

            var parameter = new Dictionary<string, object>
            {
                { "@COMP_CODE", userSession.PubCompCode },
                { "@YEAR_CODE", userSession.PubFYearCode },
                { "@BRANCH_CODE", userSession.PubBranchCode },
                { "@Action", "Excel" }
            };

            return await _dbHelper.GetJsonFromProcedureAsync(
                "[dbo].[sp_QuotationRateApproval]",
                parameter);
        }

    }
}
