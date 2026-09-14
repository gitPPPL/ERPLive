using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SalesCreditLimitEntryRepository : ISalesCreditLimitEntryRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        public SalesCreditLimitEntryRepository(GlobalVariableService globalVariableService,
            DbHelper dbHelper, DataBaseConnection dbConnection)
        {
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
        }

        public async Task<RepositoryResponseData<object>> GetDrCrAmtByPartyCodeAsync(int code)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new RepositoryResponseData<object>();
            try
            {
                string DrQry = $@"SELECT SUM(ISNULL(AMT, 0)) FROM LEDGER2 where DR_CODE=@DR_CODE AND COMP_CODE=@COMP_CODE AND DR_CODE>0 and Branch_code= @Branch_code;";
                var parameters = new Dictionary<string, object>
                {
                    { "@DR_CODE", code},
                    { "@COMP_CODE", gv.PubCompCode},
                    { "@Branch_code", gv.PubBranchCode}
                };
                var drAmt = await _dbHelper.GetExecuteScalarAsync<decimal>(DrQry, parameters);

                string CrQry = $@"SELECT SUM(ISNULL(AMT, 0)) FROM LEDGER2 where CR_CODE=@CR_CODE AND COMP_CODE=@COMP_CODE AND CR_CODE>0 and Branch_code= @Branch_code;";
                var parameters1 = new Dictionary<string, object>
                {
                    { "@CR_CODE", code},
                    { "@COMP_CODE", gv.PubCompCode},
                    { "@Branch_code", gv.PubBranchCode}
                };
                var crAmt = await _dbHelper.GetExecuteScalarAsync<decimal>(CrQry, parameters1);
                response.status = true;
                response.message = "Success";
                response.data = new
                {
                    DrAmt = drAmt,
                    CrAmt = crAmt
                };
                //return Json(new { success = true, DrAmt, crAmt });
            }
            catch (Exception ex)
            {
                //return Json(new { success = false, message = ex.Message });
                response.status = false;
                response.message = ex.Message;
            }
            return response;
        }

        public async Task<RepositoryResponse> SaveSalesCreditLimitAsync(SalesCreditLimitModel model, string doctype)
        {
            if (model == null) return new RepositoryResponse { status = false, message = "Invalid Request" };

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    //Approval
                    var appQry = $@"SELECT APPROV_USER FROM DOC_APPROSTAGE WHERE USER_CODE = {gv.PubUserId} AND DOC_CODE = '{doctype}' AND COMP_CODE = {gv.PubCompCode}";

                    var isFinalApprover = await _dbHelper.GetExecuteScalarAsync<string>(appQry) == "FINAL";

                    string appRemarks = "";
                    string appStatus = "";

                    if (isFinalApprover)
                    {
                        appRemarks = "Document Approved.";
                        appStatus = "Approved";
                    }

                    using (SqlCommand cmd = new SqlCommand("sp_SalesCreditLimit", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", model.ACTION);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", doctype);
                        cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DOC_ID", $"{doctype}{model.V_NO}");
                        cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", (object?)model.PARTY_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GR_CODE", (object?)model.GR_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CR_LIMIT", (object?)model.CR_LIMIT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CR_DAYS", (object?)model.CR_DAYS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EFF_FROM", (object?)model.EFF_FROM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", (object?)model.REMARKS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FAPROV_STATUS", appStatus);
                        cmd.Parameters.AddWithValue("@FAPROV_REMARKS", appRemarks);
                        if (model.ACTION == "INSERT")
                        {
                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                        }
                        cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@OURCR_DAYS", (object?)model.OURCR_DAYS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@OURAPPROVAL_TYPE", (object?)model.OURAPPROVAL_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@APPROVAL_TYPE", (object?)model.APPROVAL_TYPE ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }
                return new RepositoryResponse { status = true, message = model.ACTION == "INSERT" ? "Saved Successfully!" : "Updated Successfully!" };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<SalesCreditLimitModel>> GetDataById(int vNo, string doctype)
        {
            if (vNo <= 0)
            {
                return new RepositoryResponseData<SalesCreditLimitModel> { status = false, message = "Invalid Request!" };
            }

            var gv = _globalVariableService.GetGlobalVariables();
            var data = new SalesCreditLimitModel();

            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_SalesCreditLimit", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetById");
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@V_TYPE", doctype);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                data.V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : null;
                                data.V_DATE = reader["V_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["V_DATE"]) : null;
                                data.PARTY_CODE = reader["PARTY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["PARTY_CODE"]) : null;
                                data.GR_CODE = reader["GR_CODE"] != DBNull.Value ? Convert.ToInt32(reader["GR_CODE"]) : null;
                                data.GR_NAME = reader["GROUP_NAME"]?.ToString();
                                data.CR_LIMIT = reader["CR_LIMIT"] != DBNull.Value ? Convert.ToDecimal(reader["CR_LIMIT"]) : null;
                                data.CR_DAYS = reader["CR_DAYS"] != DBNull.Value ? Convert.ToInt32(reader["CR_DAYS"]) : null;
                                data.OURCR_DAYS = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["OURCR_DAYS"]) : null;
                                data.APPROVAL_TYPE = reader["APPROVAL_TYPE"]?.ToString();
                                data.OURAPPROVAL_TYPE = reader["OURAPPROVAL_TYPE"]?.ToString();
                                data.REMARKS = reader["REMARKS"]?.ToString();
                            }
                        }
                    }

                    string appQry = $@"select APPROV_USER from DOC_APPROSTAGE where USER_CODE={gv.PubUserId} and DOC_CODE='{doctype}' and comp_code={gv.PubCompCode}";
                    var result = await _dbHelper.GetExecuteScalarAsync<string>(appQry);
                    bool isFinal = result == "FINAL";
                    data.IsFinalUser = isFinal;
                }
                return new RepositoryResponseData<SalesCreditLimitModel> { status = true, data = data };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<SalesCreditLimitModel> { status = false, message = ex.Message };
            }
        }
    }
}
