using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SaleSaudaEntryListRepository : ISaleSaudaEntryListRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public SaleSaudaEntryListRepository(DataBaseConnection dbConnection, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, travelexpensemanagement.LogService.LogService logService)
        {
            _dbHelper = dbHelper;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _logService = logService;
        }

        public async Task<object> LoadListDataAsync(string searchTerm = "", int pageNo = 1, int pageSize = 20)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                if (pageNo < 1)
                    pageNo = 1;

                if (pageSize < 1)
                    pageSize = 20;

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using SqlCommand cmd = new SqlCommand("sp_SaleSauda_Entry", con);

                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "SAUD");
                    cmd.Parameters.AddWithValue("@PageNo", pageNo);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@SearchTerm", searchTerm?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Action", "GetList");

                    using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                    int totalCount = 0;

                    var list = new List<object>();

                    if (await reader.ReadAsync())
                    {
                        if (reader["TotalRecords"] != DBNull.Value)
                        {
                            totalCount = Convert.ToInt32(reader["TotalRecords"]);
                        }
                    }

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new
                            {
                                vNo = reader["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(reader["V_NO"]),
                                vType = reader["V_TYPE"] == DBNull.Value ? "" : reader["V_TYPE"].ToString(),
                                vDate = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]).ToString("yyyy-MM-dd"),
                                docId = reader["docId"] == DBNull.Value ? "" : reader["docId"].ToString(),
                                partyCode = reader["PARTY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARTY_CODE"]),
                                customerName = reader["CustomerName"] == DBNull.Value ? "" : reader["CustomerName"].ToString(),
                                cityCode = reader["CITY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["CITY_CODE"]),
                                cityName = reader["CityName"] == DBNull.Value ? "" : reader["CityName"].ToString(),
                                phone = reader["PHONE"] == DBNull.Value ? "" : reader["PHONE"].ToString(),
                                itemCode = reader["ITEM_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ITEM_CODE"]),
                                itemName = reader["ItemName"] == DBNull.Value ? "" : reader["ItemName"].ToString(),
                                itemType = reader["ITEM_TYPE"] == DBNull.Value ? "" : reader["ITEM_TYPE"].ToString(),
                                remark = reader["REMARK"] == DBNull.Value ? "" : reader["REMARK"].ToString()
                            });
                        }
                    }
                    
                    return new { success = true, data = list, pageNo = pageNo, pageSize = pageSize, totalCount = totalCount, hasMore = (pageNo * pageSize) < totalCount };
                }
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        public async Task<object> ValidateEditAsync(int vNo, string vType)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using SqlConnection con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                // =========================================================
                // 1. APPROVAL_STATUS CHECK
                // =========================================================

                string approvalCheckQuery = @"
                SELECT COUNT(1)
                FROM APPROVAL_STATUS
                WHERE V_TYPE = @V_TYPE
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE
                  AND STATUS = 'OPEN'
                  AND USER_CODE <> @USER_CODE";

                using (SqlCommand approvalCheckCmd = new SqlCommand(approvalCheckQuery, con))
                {
                    approvalCheckCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    approvalCheckCmd.Parameters.AddWithValue("@V_NO", vNo);
                    approvalCheckCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    approvalCheckCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    approvalCheckCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    approvalCheckCmd.Parameters.AddWithValue("@USER_CODE", globalVariables.PubUserId);

                    int approvalExists = Convert.ToInt32(
                        await approvalCheckCmd.ExecuteScalarAsync()
                    );

                    if (approvalExists > 0)
                    {
                        string lastUserQuery = @"
                        SELECT TOP 1 USER_NAME
                        FROM APPROVAL_STATUS
                        WHERE V_TYPE = @V_TYPE
                          AND V_NO = @V_NO
                          AND COMP_CODE = @COMP_CODE
                          AND BRANCH_CODE = @BRANCH_CODE
                          AND YEAR_CODE = @YEAR_CODE
                          AND STATUS = 'OPEN'
                          AND USER_CODE <> @USER_CODE
                        ORDER BY SRNO DESC";

                        using SqlCommand lastUserCmd = new SqlCommand(lastUserQuery, con);

                        lastUserCmd.Parameters.AddWithValue("@V_TYPE", vType);
                        lastUserCmd.Parameters.AddWithValue("@V_NO", vNo);
                        lastUserCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                        lastUserCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                        lastUserCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                        lastUserCmd.Parameters.AddWithValue("@USER_CODE", globalVariables.PubUserId);

                        var lastUser = await lastUserCmd.ExecuteScalarAsync();

                        return new
                        {
                            success = false,
                            message = $"This Document Approval is in process at User:{lastUser}., Edit not allowed."
                        };
                    }
                }

                // =========================================================
                // 2. SALE2 CHECK
                //    WARNING ONLY - EDIT IS NOT BLOCKED
                // =========================================================

                string saleQuery = @"
                SELECT TOP 1
                    V_NO,
                    V_DATE
                FROM SALE2
                WHERE SAUDA_TYPE = @V_TYPE
                  AND SAUDA_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND V_TYPE IN ('SAGT', 'SABS', 'SASI')";

                int? saleVNo = null;
                string saleDate = "";

                using (SqlCommand saleCmd = new SqlCommand(saleQuery, con))
                {
                    saleCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    saleCmd.Parameters.AddWithValue("@V_NO", vNo);
                    saleCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    saleCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);

                    using SqlDataReader reader = await saleCmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        saleVNo = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]);

                        saleDate = reader["V_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["V_DATE"]).ToString("dd-MM-yyyy");
                    }
                }

                // ========================================================
                // 3. ORDER2 CHECK
                //    WARNING ONLY - EDIT IS NOT BLOCKED
                // =========================================================

                string orderQuery = @"
                SELECT TOP 1
                    V_NO,
                    V_DATE
                FROM ORDER2
                WHERE SAUDA_TYPE = @V_TYPE
                  AND SAUDA_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE";

                int? orderVNo = null;
                string orderDate = "";

                using (SqlCommand orderCmd = new SqlCommand(orderQuery, con))
                {
                    orderCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    orderCmd.Parameters.AddWithValue("@V_NO", vNo);
                    orderCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    orderCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    orderCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);

                    using SqlDataReader reader = await orderCmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        orderVNo = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]);

                        orderDate = reader["V_DATE"] == DBNull.Value ? "" : Convert.ToDateTime(reader["V_DATE"]).ToString("dd-MM-yyyy");
                    }
                }

                // =========================================================
                // 4. GET FAPROV_STATUS FROM SAUDA
                // =========================================================

                string approvalStatusQuery = @"
                SELECT FAPROV_STATUS
                FROM SAUDA
                WHERE V_TYPE = @V_TYPE
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE";

                string approvalStatus = "";

                using (SqlCommand statusCmd = new SqlCommand(approvalStatusQuery, con))
                {
                    statusCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    statusCmd.Parameters.AddWithValue("@V_NO", vNo);
                    statusCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    statusCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    statusCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);

                    var result = await statusCmd.ExecuteScalarAsync();

                    if (result != null && result != DBNull.Value)
                    {
                        approvalStatus = result.ToString();
                    }
                }

                // ========================================================
                // 5. APPROVED DOCUMENT EDIT CHECK
                // ========================================================

                if (approvalStatus == "Approved")
                {
                    var eaFlag = await GetEAFlagAsync(vType);

                    // isFinalApprovalBody ka actual old logic yahan lagana hai
                    bool isFinalApprovalBody = false;

                    if (!isFinalApprovalBody || eaFlag != "EA")
                    {
                        return new
                        {
                            success = false,
                            message = "Edit Locked, Document once Approved Not allowed to EDIT."
                        };
                    }
                }

                // ========================================================
                // 6. RETURN VALIDATION RESULT
                // =========================================================

                return new
                {
                    success = true,

                    approvalStatus = approvalStatus,

                    saleWarning = saleVNo.HasValue ? $"This document exists in SALE Serial No :{saleVNo} dated :{saleDate}" : "",

                    orderWarning = orderVNo.HasValue ? $"This document exists in ORDER Serial No :{orderVNo} dated :{orderDate}" : ""
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        private async Task<string> GetEAFlagAsync(string vType)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using SqlConnection con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                string query = @"
                SELECT ISNULL(FLAG_F, '')
                FROM DOC_APPROSTAGE
                WHERE COMP_CODE = @COMP_CODE
                  AND DOC_CODE = @DOC_CODE
                  AND USER_CODE = @USER_CODE";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                cmd.Parameters.AddWithValue("@DOC_CODE", vType);
                cmd.Parameters.AddWithValue("@USER_CODE", globalVariables.PubUserId);

                var result = await cmd.ExecuteScalarAsync();

                return result?.ToString() ?? "";
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<object> DeleteDataAsync(int vNo, string vType)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using SqlConnection con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                using SqlCommand cmd = new SqlCommand("sp_SaleSauda_Entry", con);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                cmd.Parameters.AddWithValue("@Action", "DeleteData");

                await cmd.ExecuteNonQueryAsync();

                return new
                {
                    success = true,
                    message = "Record deleted successfully."
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }

        public async Task<object> ValidateDeleteAsync(int vNo, string vType)
        {
            try
            {
                var globalVariables = _globalVariableService.GetGlobalVariables();

                using SqlConnection con = _dbConnection.GetErpConnection();
                await con.OpenAsync();

                // =========================================================
                // 1. DOCUMENT STATUS
                // =========================================================

                string statusQuery = @"
                SELECT STATUS
                FROM SAUDA
                WHERE V_TYPE = @V_TYPE
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE";

                object statusResult;

                using (SqlCommand statusCmd = new SqlCommand(statusQuery, con))
                {
                    statusCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    statusCmd.Parameters.AddWithValue("@V_NO", vNo);
                    statusCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    statusCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    statusCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);

                    statusResult = await statusCmd.ExecuteScalarAsync();
                }

                if (statusResult == null || statusResult == DBNull.Value)
                {
                    return new
                    {
                        success = false,
                        message = "Document Status CLOSE/CANCEL, Deletion not allowed."
                    };
                }

                int status = Convert.ToInt32(statusResult);

                // Old: cmbStatus.SelectedValue = 3
                if (status == 3)
                {
                    return new
                    {
                        success = false,
                        message = "Document Closed, Deletion not allowed."
                    };
                }

                // Old: cmbStatus.SelectedValue = 2
                if (status == 2)
                {
                    return new
                    {
                        success = false,
                        message = "Document Cancelled, Deletion not allowed."
                    };
                }

                // Old MaxSrNo(...) <> 1
                if (status != 1)
                {
                    return new
                    {
                        success = false,
                        message = "Document Status CLOSE/CANCEL, Deletion not allowed."
                    };
                }

                // =========================================================
                // 2. APPROVAL_STATUS CHECK
                // =========================================================

                string approvalQuery = @"
                SELECT TOP 1 STATUS
                FROM APPROVAL_STATUS
                WHERE V_TYPE = @V_TYPE
                  AND V_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE
                  AND STATUS = 'OPEN'
                  AND USER_CODE <> @USER_CODE";

                using (SqlCommand approvalCmd = new SqlCommand(approvalQuery, con))
                {
                    approvalCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    approvalCmd.Parameters.AddWithValue("@V_NO", vNo);
                    approvalCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    approvalCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    approvalCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);
                    approvalCmd.Parameters.AddWithValue("@USER_CODE", globalVariables.PubUserId);

                    var approvalResult = await approvalCmd.ExecuteScalarAsync();

                    if (approvalResult != null && approvalResult != DBNull.Value)
                    {
                        return new
                        {
                            success = false,
                            message = "This Document Approval is in process, Deletion not allowed."
                        };
                    }
                }

                // =========================================================
                // 3. ORDER2 CHECK
                // =========================================================

                string orderQuery = @"
                SELECT TOP 1
                    V_NO,
                    V_DATE
                FROM ORDER2
                WHERE SAUDA_TYPE = @V_TYPE
                  AND SAUDA_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE
                  AND YEAR_CODE = @YEAR_CODE";

                using (SqlCommand orderCmd = new SqlCommand(orderQuery, con))
                {
                    orderCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    orderCmd.Parameters.AddWithValue("@V_NO", vNo);
                    orderCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    orderCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);
                    orderCmd.Parameters.AddWithValue("@YEAR_CODE", globalVariables.PubFYearCode);

                    using SqlDataReader reader = await orderCmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        int orderVNo = reader["V_NO"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["V_NO"]);

                        string orderDate = reader["V_DATE"] == DBNull.Value
                            ? ""
                            : Convert.ToDateTime(reader["V_DATE"]).ToString("dd-MM-yyyy");

                        return new
                        {
                            success = false,
                            warning = $"This document exists in Sale Order No :{orderVNo} dated :{orderDate}"
                        };
                    }
                }

                // =========================================================
                // 4. SALE2 CHECK
                // =========================================================

                string saleQuery = @"
                SELECT TOP 1
                    V_NO,
                    V_DATE
                FROM SALE2
                WHERE SAUDA_TYPE = @V_TYPE
                  AND SAUDA_NO = @V_NO
                  AND COMP_CODE = @COMP_CODE
                  AND BRANCH_CODE = @BRANCH_CODE";

                using (SqlCommand saleCmd = new SqlCommand(saleQuery, con))
                {
                    saleCmd.Parameters.AddWithValue("@V_TYPE", vType);
                    saleCmd.Parameters.AddWithValue("@V_NO", vNo);
                    saleCmd.Parameters.AddWithValue("@COMP_CODE", globalVariables.PubCompCode);
                    saleCmd.Parameters.AddWithValue("@BRANCH_CODE", globalVariables.PubBranchCode);

                    using SqlDataReader reader = await saleCmd.ExecuteReaderAsync();

                    if (await reader.ReadAsync())
                    {
                        int saleVNo = reader["V_NO"] == DBNull.Value ? 0  : Convert.ToInt32(reader["V_NO"]);

                        string saleDate = reader["V_DATE"] == DBNull.Value ? ""  : Convert.ToDateTime(reader["V_DATE"]).ToString("dd-MM-yyyy");

                        return new
                        {
                            success = false,
                            warning = $"This document exists in Sale Invoice No :{saleVNo} dated :{saleDate}"
                        };
                    }
                }

                // =========================================================
                // 5. ALL VALIDATIONS PASSED
                // =========================================================

                return new
                {
                    success = true
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    success = false,
                    message = ex.Message
                };
            }
        }


    }
}   
