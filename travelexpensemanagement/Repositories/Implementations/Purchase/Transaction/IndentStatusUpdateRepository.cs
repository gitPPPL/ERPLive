using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Models.Purchase.Transaction.IndentStatusUpdateModel;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class IndentStatusUpdateRepository : IIndentStatusUpdateRepository
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;
        private readonly DropdownService _dropdownService;
        public IndentStatusUpdateRepository(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalVariableService, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, travelexpensemanagement.LogService.LogService logService, travelexpensemanagement.Common.DropdownService.DropdownService dropdownService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalVariableService = globalVariableService;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _logService = logService;
        }

        public async Task<List<StorePurchaseOrderStatusModel>> GetStorePurchaseOrderStatusAsync(DateTime fromDate, DateTime toDate,int? supplierCode)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var list = new List<StorePurchaseOrderStatusModel>();

            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                await con.OpenAsync();

                string query = @"
                    SELECT
                        a.V_TYPE,
                        a.V_NO,
                        a.V_DATE,
                        d.NAME AS PartyName,
                        a.ITEM_CODE,
                        e.NAME AS ItemName,
                        a.QTY,
                        ISNULL(SUM(c.RECD_QTY),0) AS RecdQty,
                        (ISNULL(a.QTY,0)-ISNULL(SUM(c.RECD_QTY),0)) AS BalQty,
                        a.DISP_THROUGH,
                        a.DISP_REF,
                        a.DISP_REMARKS,
                        b.PARTY_CODE,
                        a.SNO
                    FROM ORDER2 a
                    LEFT JOIN ORDER1 b
                        ON a.V_NO=b.V_NO
                        AND a.V_TYPE=b.V_TYPE
                        AND a.V_DATE=b.V_DATE
                        AND a.COMP_CODE=b.COMP_CODE
                        AND a.BRANCH_CODE=b.BRANCH_CODE
                        AND a.YEAR_CODE=b.YEAR_CODE
                    LEFT JOIN PURCHASE2 c
                        ON a.ITEM_CODE=c.ITEM_CODE
                        AND a.V_TYPE=c.PO_TYPE
                        AND a.V_NO=c.PO_NO
                        AND a.COMP_CODE=c.COMP_CODE
                    LEFT JOIN SUBGROUP_MAST d
                        ON b.PARTY_CODE=d.CODE
                        AND a.COMP_CODE=d.COMP_CODE
                    LEFT JOIN ITEM_MAST e
                        ON a.ITEM_CODE=e.CODE
                        AND a.COMP_CODE=e.COMP_CODE
                    LEFT JOIN DOCTYPE_MAST f
                        ON c.V_TYPE=f.CODE
                    WHERE
                        f.DOCTYPE='Materialreceipt'
                        AND a.V_TYPE='PORD'
                        AND a.STATUS=1
                        AND b.STATUS=1
                        AND a.V_DATE BETWEEN @FromDate AND @ToDate
                        AND a.COMP_CODE=@CompCode
                        AND a.BRANCH_CODE=@BranchCode
                        AND a.YEAR_CODE=@YearCode";

                if (supplierCode.HasValue && supplierCode.Value > 0)
                {
                    query += " AND b.PARTY_CODE=@SupplierCode";
                }

                query += @"
                GROUP BY
                    a.V_TYPE,a.V_NO,a.V_DATE,
                    d.NAME,
                    a.ITEM_CODE,
                    e.NAME,
                    a.DISP_THROUGH,
                    a.DISP_REF,
                    a.DISP_REMARKS,
                    b.PARTY_CODE,
                    a.QTY,
                    a.SNO
                     HAVING
                        (ISNULL(a.QTY,0)-ISNULL(SUM(c.RECD_QTY),0))>0";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@FromDate", fromDate);
                    cmd.Parameters.AddWithValue("@ToDate", toDate);
                    cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                    if (supplierCode.HasValue && supplierCode.Value > 0)
                    {
                        cmd.Parameters.AddWithValue("@SupplierCode", supplierCode.Value);
                    }

                    using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await rdr.ReadAsync())
                        {
                            list.Add(new StorePurchaseOrderStatusModel
                            {
                                VType = Convert.ToString(rdr["V_TYPE"]),
                                VNo = Convert.ToInt32(rdr["V_NO"]),
                                VDate = Convert.ToDateTime(rdr["V_DATE"]),

                                PartyCode = Convert.ToInt32(rdr["PARTY_CODE"]),
                                PartyName = Convert.ToString(rdr["PartyName"]),

                                ItemCode = Convert.ToInt32(rdr["ITEM_CODE"]),
                                ItemName = Convert.ToString(rdr["ItemName"]),

                                Qty = Convert.ToDecimal(rdr["QTY"]),
                                RecdQty = Convert.ToDecimal(rdr["RecdQty"]),
                                BalQty = Convert.ToDecimal(rdr["BalQty"]),

                                DispThrough = Convert.ToString(rdr["DISP_THROUGH"]),
                                DispRef = Convert.ToString(rdr["DISP_REF"]),
                                DispRemarks = Convert.ToString(rdr["DISP_REMARKS"]),
                                SNO = Convert.ToString(rdr["SNO"])
                            });
                        }
                    }
                }
            }

            return list;
        }

        public async Task<(bool Success, string Message)> SaveIndentStatusAsync(List<IndentStatusUpdateSaveModel> model)
        {
            if (model == null || model.Count == 0)
            {
                return (false, "No Data to update.");
            }

            var gv = _globalVariableService.GetGlobalVariables();
            int ctr = 0;

            using (SqlConnection con = _dbcontext.GetErpConnection())
            {
                await con.OpenAsync();

                foreach (var item in model)
                {
                    if (item.VNo <= 0)
                        continue;

                    using (SqlCommand cmd = new SqlCommand("sp_UpdateIndentStatus", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@DispThrough", item.DispThrough ?? "");
                        cmd.Parameters.AddWithValue("@DispRef", item.DispRef ?? "");
                        cmd.Parameters.AddWithValue("@DispRemarks", item.DispRemarks ?? "");

                        cmd.Parameters.AddWithValue("@VNo", item.VNo);
                        cmd.Parameters.AddWithValue("@VDate", item.VDate);
                        cmd.Parameters.AddWithValue("@ItemCode", item.ItemCode);
                        cmd.Parameters.AddWithValue("@Sno", item.Sno);

                        cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

                        ctr += await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            return ctr > 0 ? (true, "Status Updated Successfully.") : (false, "No Data Updated.");
        }

    }
}
