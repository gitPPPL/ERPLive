using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Transaction;
using travelexpensemanagement.Pages.Admin.SystemInitilization.DocumentTypeMasterList;
using travelexpensemanagement.Repositories.Interfaces.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Inventory.Transaction
{
    public class InventoryDepartmentIssueRepository : IInventoryDepartmentIssueRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;

        public InventoryDepartmentIssueRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService, DropdownService dropdownService)
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
                            VDate = reader["VDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["VDate"]),
                            ItemName = reader["ItemName"] == DBNull.Value ? null : reader["ItemName"].ToString(),
                            Nos = reader["Nos"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Nos"]),
                            Qty = reader["Qty"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Qty"]),
                            Unit = reader["Unit"] == DBNull.Value ? null : reader["Unit"].ToString(),
                            Make = reader["Make"] == DBNull.Value ? null : reader["Make"].ToString(),
                            Place = reader["Place"] == DBNull.Value ? null : reader["Place"].ToString(),
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

        public async Task<(string Status, string Message)> SubmitRequest(InventryDepartmentIssue_Header header, List<InventryDepartmentIssue_Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                using var conn = _dbConnection.GetErpConnection();

                await conn.OpenAsync();



                string docId = string.IsNullOrWhiteSpace(header.DOC_ID) ? $"{header.V_TYPE}{header.V_NO}" : header.DOC_ID;

                using (var cmd = new SqlCommand("sp_InventoryDepartmentIssue", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "HEADER");
                    cmd.Parameters.AddWithValue("@DOC_ID", docId);
                    cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@SHIFT", header.SHIFT);
                    cmd.Parameters.AddWithValue("@SLIP_NO", header.SLIP_NO);
                    cmd.Parameters.AddWithValue("@REMARKS", (object?)header.REMARKS ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PORD_TYPE", header.PORD_TYPE);
                    cmd.Parameters.AddWithValue("@PORD_NO", header.PORD_NO);
                    cmd.Parameters.AddWithValue("@PLAN_TYPE", header.PLAN_TYPE);
                    cmd.Parameters.AddWithValue("@PLAN_NO", header.PLAN_NO);
                    cmd.Parameters.AddWithValue("@STATUS", header.STATUS);                            
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (details != null && details.Count > 0)
                {
                    foreach (var detail in details)
                    {
                        if (detail == null || detail.ITEM_CODE <= 0)
                            continue;                   

                        using var cmd = new SqlCommand("sp_InventoryDepartmentIssue", conn)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@SaveAction", "DETAILS");
                        cmd.Parameters.AddWithValue("@DOC_ID", docId);
                        cmd.Parameters.AddWithValue("@V_NO", header.V_NO);
                        cmd.Parameters.AddWithValue("@V_TYPE", (object?)header.V_TYPE ?? DBNull.Value);
                        cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE;
                        cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", detail.ITEM_CODE);
                        cmd.Parameters.AddWithValue("@ITEM_NAME", (object?)detail.ITEM_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UOM_CODE", (object?)detail.UOM_CODE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UOM_NAME", (object?)detail.UOM_NAME ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LOT_NO", (object?)detail.LOT_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@NOS", (object?)detail.NOS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@QTY", (object?)detail.QTY ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REMARKS", (object?)detail.REMARKS ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@RATE", (object?)detail.RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@AMOUNT", (object?)detail.AMOUNT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LAND_RATE", (object?)detail.LAND_RATE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@LAND_AMT", (object?)detail.LAND_AMT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SHIFT", (object?)detail.SHIFT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORD_TYPE", (object?)detail.PORD_TYPE ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@PORD_NO", (object?)detail.PORD_NO ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@FROM_DEPT", (object?)detail.FROM_DEPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TO_DEPT", (object?)detail.TO_DEPT ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@EMPTY_YN", (object?)detail.EMPTY_YN ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SNO", detail.SNO);
                        cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return ("Success", "Data Save Successfully");

            }
            catch (Exception ex)
            {
                return ("Error", ex.Message);
            }
        }

        public object DDlPlaceFrom(string formName)
        {
            var getData = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "";
                if (formName == "AdjustmentIssue" || formName == "AdjustmentReceived")
                {
                    query = " select code,name from ITEMDEPT_MAST where Active=1 and COMP_CODE=" + getData.PubCompCode + " and TRAN_TYPE='Store' order by name";
                }
                else
                {
                    query = "select code,name   from ITEMDEPT_MAST where Active=1 and COMP_CODE=" + getData.PubCompCode + " and TRAN_TYPE='Production' order by name";
                }

                var data = _dropdownService.GetDropdownList(query);

                return data;
            }
        }

    }
}



