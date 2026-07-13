using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Data.SqlClient;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Repositories;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;

public class MiscConsumptionListRepository : IMiscConsumptionListRepository
{
    private readonly DataBaseConnection _dbConnection;
    private readonly GlobalVariableService _globalVariableService;

    public MiscConsumptionListRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
    {
        _dbConnection = dbConnection;
        _globalVariableService = globalVariableService;
    }

    public (List<MiscConsumptionEntry_Header>, int) GetList(string searchTerm, int pageNumber, int pageSize)
    {
        var getvariabledata = _globalVariableService.GetGlobalVariables();

        int totalCount = 0;
        var headerList = new List<MiscConsumptionEntry_Header>();

        using (var conn = _dbConnection.GetErpConnection())
        using (var cmd = new SqlCommand("sp_MiscConsumptionEntry", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Action", "SELECT");
            cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);
            cmd.Parameters.AddWithValue("@COMP_CODE", getvariabledata.PubCompCode);
            cmd.Parameters.AddWithValue("@YEAR_CODE", getvariabledata.PubFYearCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", getvariabledata.PubBranchCode);

            conn.Open();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    headerList.Add(new MiscConsumptionEntry_Header
                    {
                        V_TYPE = reader["Vtype"]?.ToString(),
                        V_NO = reader["V_NO"] != DBNull.Value ? Convert.ToInt32(reader["V_NO"]) : 0,
                        V_DATE = reader["Voucherdate"] != DBNull.Value ? Convert.ToDateTime(reader["Voucherdate"]) : DateTime.MinValue,
                        PARTY_NAME = reader["PartyName"]?.ToString(),
                        DOC_ID = reader["DOC_ID"]?.ToString(),
                        VtypeCode = reader["vCode"]?.ToString()
                    });
                }
                if (reader.NextResult())
                {
                    if (reader.Read())
                    {
                        totalCount = reader["TotalCount"] != DBNull.Value
                            ? Convert.ToInt32(reader["TotalCount"])
                            : 0;
                    }
                }
            }
        }

        return (headerList, totalCount);
    }

    public MiscConsumptionEntryModel GetDataByCode(int rowId, string vtype)
    {
        var global = _globalVariableService.GetGlobalVariables();

        var wrapper = new MiscConsumptionEntryModel
        {
            Header = new MiscConsumptionEntry_Header(),
            Deatils = new List<Details>()
        };

        using (SqlConnection con = _dbConnection.GetErpConnection())
        {
            con.Open();

            // Header
            using (SqlCommand cmd = new SqlCommand("sp_MiscConsumptionEntry", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "ShowData");
                cmd.Parameters.AddWithValue("@ShowActionOption", "Header");
                cmd.Parameters.AddWithValue("@V_NO", rowId);
                cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                cmd.Parameters.AddWithValue("@V_TYPE", vtype);

                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        wrapper.Header = new MiscConsumptionEntry_Header
                        {
                            V_TYPE = rdr["V_TYPE"]?.ToString(),
                            V_NO = rdr["V_NO"] != DBNull.Value ? Convert.ToInt32(rdr["V_NO"]) : 0,
                            V_DATE = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]) : DateTime.MinValue,
                            V_TIME = rdr["V_TIME"]?.ToString(),
                            PARTY_CODE = rdr["party_code"] != DBNull.Value ? Convert.ToInt32(rdr["party_code"]) : 0,
                            Add1 = rdr["ADD1"]?.ToString(),
                            Add2 = rdr["ADD2"]?.ToString(),
                            Add3 = rdr["ADD3"]?.ToString(),
                            TRUCK_NO = rdr["TRUCK_NO"]?.ToString(),
                            ITEM_TYPE = rdr["item_type"]?.ToString(),
                            REMARKS = rdr["REMARKS"]?.ToString(),
                            DOC_ID = rdr["doc_id"]?.ToString()
                        };
                    }
                }
            }

            // Details
            using (SqlCommand cmd = new SqlCommand("sp_MiscConsumptionEntry", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "ShowData");
                cmd.Parameters.AddWithValue("@ShowActionOption", "Details");
                cmd.Parameters.AddWithValue("@V_NO", rowId);
                cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);

                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        wrapper.Deatils.Add(new Details
                        {
                            ITEM_CODE = rdr["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["ITEM_CODE"]) : 0,
                            DEPT_CODE = rdr["DEPT_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["DEPT_CODE"]) : 0,
                            UOM_CODE = rdr["UOM_CODE"] != DBNull.Value ? Convert.ToInt32(rdr["UOM_CODE"]) : 0,
                            NOS = rdr["NOS"] != DBNull.Value ? Convert.ToInt32(rdr["NOS"]) : 0,
                            QTY = rdr["QTY"] != DBNull.Value ? Convert.ToInt32(rdr["QTY"]) : 0,
                            REMARKS = rdr["REMARKS"]?.ToString(),
                            REF_TYPE= rdr["REF_TYPE"]?.ToString(),
                            REF_NO = rdr["REF_NO"] != DBNull.Value ? Convert.ToInt32(rdr["REF_NO"]) : 0,
                        });
                    }
                }
            }
        }

        return wrapper;
    }

    public async Task<RepositoryResponse> Delete(string vNo, string docType)
    {
        var response = new RepositoryResponse();

        try
        {
            var global = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_MiscConsumptionEntry", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Action", "DELETE");
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                cmd.Parameters.AddWithValue("@V_TYPE", docType);

                await con.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    int rows = 0;

                    if (reader.Read())
                    {
                        rows = reader["RowsAffected"] != DBNull.Value
                            ? Convert.ToInt32(reader["RowsAffected"])
                            : 0;
                    }

                    response.status = rows > 0;
                    response.message = rows > 0 ? "Deleted successfully" : "Delete failed";
                }
            }
        }
        catch (Exception ex)
        {
            response.status = false;
            response.message = ex.Message;
        }

        return response;
    }

    public List<object> GetPendingDocuments(int partyId)
    {
        var global = _globalVariableService.GetGlobalVariables();
        var dataList = new List<object>();

        using (SqlConnection con = _dbConnection.GetErpConnection())
        using (SqlCommand cmd = new SqlCommand("sp_MiscConsumptionEntry", con))
        {
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Action", "LoadPendingData");
            cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
            cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
            cmd.Parameters.AddWithValue("@PARTY_CODE", partyId);

            con.Open();

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    dataList.Add(new
                    {
                        V_type = reader["V_type"].ToString(),
                        V_NO = reader["V_NO"].ToString(),
                        v_date = reader["v_date"]?.ToString(),
                        ITEM_CODE = reader["ITEM_CODE"].ToString(),
                        item_name = reader["item_name"].ToString(),
                        remarks = reader["remarks"].ToString(),
                        QTY = reader["QTY"].ToString(),
                        P_Qty = reader["P_Qty"].ToString(),
                        UOM_CODE = reader["UOM_CODE"].ToString(),
                        unitname = reader["UOM_NAME"].ToString(),
                        NOS = reader["NOS"].ToString(),
                        DEPT_CODE= reader["DEPT_CODE"].ToString(),
                        NAME= reader["NAME"].ToString(),
                        srno = reader["srno"].ToString(),
                        refType = reader["REF_TYPE"]?.ToString(),
                        refNo = reader["REF_NO"]?.ToString()
                    });
                }
            }
        }

        return dataList;
    }
}