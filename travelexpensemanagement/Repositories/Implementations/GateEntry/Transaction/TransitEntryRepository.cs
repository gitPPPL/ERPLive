using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Models;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class TransitEntryRepository : ITransitEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly LogService.LogService _logService;

        public TransitEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
            DropdownService dropdownService, GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }
        public RepositoryResponseList<object> GetDDl(string type, string VTypeId = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new RepositoryResponseList<object>();
            string query = "";
            switch (type)
            {
                case "DocType":
                    query = "SELECT CODE as Value, NAME as Text FROM DOCTYPE_MAST  WHERE DOCTYPE='Transit'";
                    break;
                case "DocStatus":
                    query = "Select Code as Value, Name as Text from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                    break;
                case "PartyName":
                    if (VTypeId == "TRIN")
                    {
                        query = "Select code as Value, name as Text from SUBGROUP_MAST  where Nature='Supplier' and COMP_CODE=" + gv.PubCompCode + "  order by name";
                    }

                    else if (VTypeId == "TROT")
                    {
                        query = "Select code as Value, name as Text from SUBGROUP_MAST  where Nature='Customer' and COMP_CODE=" + gv.PubCompCode + "   order by name ";
                    }
                    else
                    {
                        query = "Select code as Value, name as Text from SUBGROUP_MAST  where Nature in ('Supplier','Customer') and COMP_CODE=" + gv.PubCompCode + " order by name ";
                    }
                    break;
            }
            var data = _dropdownService.GetDropdownList(query);
            response.data = data;
            return response;
            //return Json(data);
        }
        public async Task<RepositoryResponseData<bool>> IsExist(int vNo, string form_No)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new RepositoryResponseData<bool>();
            bool flg = false;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"select 1 from waybill1 where V_NO <> @V_NO and comp_code=@comp_code and Branch_Code=@Branch_Code and Year_Code=@Year_Code and FORM_NO= @FORM_NO";
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@V_NO", vNo);
                        cmd.Parameters.AddWithValue("@comp_code", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@Year_Code", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@Branch_Code", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@FORM_NO", form_No);
                        var result = await cmd.ExecuteScalarAsync();
                        flg = result != null && Convert.ToInt32(result) == 1;
                        response.status = true;
                        response.data = flg;
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = ex.Message;
                return response;
            }
        }
        public async Task<RepositoryResponseList<object>> PartyGstinNo(int Partycode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var dataList = new List<object>();
            var response = new RepositoryResponseList<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                string query = @"
                   select gstin from subgroup_mast where code=@Partycode and comp_code=@CompCode";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    cmd.Parameters.AddWithValue("@Partycode", Partycode);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            dataList.Add(new
                            {
                                gstin = reader["gstin"].ToString()
                            });
                        }
                    }
                }
            }
            response.data = dataList;
            return response;
        }
        public async Task<RepositoryResponse> SaveData(TransitEntryModel data)
        {
            var response = new RepositoryResponse();
            if (data == null)
            {
                response.status = false;
                response.message = "Input model is null";
                return response;
            }

            string action = data.action == "INSERT" ? "INSERT" : "UPDATE";
            var result = await Submitbtn(data, action);

            if (result == "Success")
            {
                response.status = true;
                response.message = action == "UPDATE" ? "Updated Successfully!" : "Inserted Successfully!";
                return response;
            }
            else
            {
                response.status = false;
                response.message = result;
                return response;
            }
        }
        private async Task<string> Submitbtn(TransitEntryModel data, string action)
        {
            try
            {
                var globalVar = _globalVariableService.GetGlobalVariables();
                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("sp_TransitEntry", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);

                        cmd.Parameters.AddWithValue("@V_TYPE", data.V_TYPE);
                        cmd.Parameters.AddWithValue("@V_NO", data.V_NO);
                        cmd.Parameters.AddWithValue("@DOC_ID", data.V_TYPE + data.V_NO);
                        cmd.Parameters.AddWithValue("@FORM_NO", data.FORM_NO);
                        cmd.Parameters.AddWithValue("@FORM_DATE", data.FORM_DATE);
                        cmd.Parameters.AddWithValue("@EXPIRY_DATE", data.EXPIRY_DATE);
                        cmd.Parameters.AddWithValue("@PARTY_CODE", data.PARTY_CODE);
                        cmd.Parameters.AddWithValue("@PARTY_GSTIN", data.PARTY_GSTIN);
                        cmd.Parameters.AddWithValue("@OTHER_GSTIN", data.OTHER_GSTIN);
                        cmd.Parameters.AddWithValue("@NOS", data.NOS);
                        cmd.Parameters.AddWithValue("@BILL_NO", data.BILL_NO);
                        cmd.Parameters.AddWithValue("@BILL_DATE", data.BILL_DATE);
                        cmd.Parameters.AddWithValue("@GR_NO", data.GR_NO);
                        cmd.Parameters.AddWithValue("@GR_DATE", data.GR_DATE);
                        cmd.Parameters.AddWithValue("@TRUCK_NO", data.TRUCK_NO);
                        cmd.Parameters.AddWithValue("@TRANSPORT", data.TRANSPORT);
                        cmd.Parameters.AddWithValue("@ORD_TYPE", data.ORD_TYPE);
                        cmd.Parameters.AddWithValue("@ORD_NO", data.ORD_NO);
                        cmd.Parameters.AddWithValue("@HSN_CODE", data.HSN_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_DESC", data.ITEM_DESC);
                        cmd.Parameters.AddWithValue("@BILL_AMT", data.BILL_AMT);
                        cmd.Parameters.AddWithValue("@SGST_AMT", data.SGST_AMT);
                        cmd.Parameters.AddWithValue("@CGST_AMT", data.CGST_AMT);
                        cmd.Parameters.AddWithValue("@IGST_AMT", data.IGST_AMT);
                        cmd.Parameters.AddWithValue("@CESS_AMT", data.CESS_AMT);
                        cmd.Parameters.AddWithValue("@CESS_NONADVOLAMT", data.CESS_NONADVOLAMT);
                        cmd.Parameters.AddWithValue("@OTHER_AMT", data.OTHER_AMT);
                        cmd.Parameters.AddWithValue("@TOTAL_AMT", data.TOTAL_AMT);
                        cmd.Parameters.AddWithValue("@STATUS", data.STATUS);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        int rowsInserted = await cmd.ExecuteNonQueryAsync();
                        //=================Log Insert
                        string mode = "";
                        if (action == "INSERT")
                        {
                            mode = "Insert";
                        }
                        else
                        {
                            mode = "Update";
                            //_globalValidationdate.LogInsertUpdateDelete(destinationTable: "WAYBILL1", sourceTable: "WAYBILL1", transactionType: "Transaction",
                            //        codeVNo: data.V_NO.ToString(), vtype: data.V_TYPE);
                        }
                        _logService.InsertLog("WAYBILL1", "Transit Entry", "Transaction", mode, data.V_TYPE, data.V_NO.ToString(), null);
                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}
