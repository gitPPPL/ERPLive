using DocumentFormat.OpenXml.Office.Word;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl
{
    public class FlakesQCEntryExcluRepository : IFlakesQCEntryExcluRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;

        public FlakesQCEntryExcluRepository( DataBaseConnection dbConnection,  GlobalVariableService globalVariableService , GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
        }

        public string SubmitRequest(FlexQCEntryExcru_Header header, List<FlexQCEntryExcru_Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                // DELETE OLD DATA
                string deleteSql = @"  DELETE FROM PROD2_QC WHERE COMP_CODE = @CompCode
                    AND V_NO = @VNo AND BRANCH_CODE = @BranchCode  AND YEAR_CODE = @YearCode";

                using (var deleteCmd = conn.CreateCommand())
                {
                    deleteCmd.CommandText = deleteSql;
                    deleteCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                    deleteCmd.Parameters.AddWithValue("@VNo", header.V_NO);
                    deleteCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                    deleteCmd.Parameters.AddWithValue("@YearCode", g.PubFYearCode);
                    deleteCmd.ExecuteNonQuery();
                }

                // SAVE HEADER
                using (var cmd = new SqlCommand("sp_FlexQCEntryExcru", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@DOC_ID", "SFQC" + header.V_NO);
                    cmd.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE == null ? DBNull.Value : Convert.ToDateTime(header.V_DATE);
                    cmd.Parameters.AddWithValue("@V_TYPE", "SFQC");
                    cmd.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd.Parameters.AddWithValue("@QCTIME", header.QCTIME);
                    cmd.Parameters.AddWithValue("@EMP_CODE", header.EMP_CODE);
                    cmd.Parameters.AddWithValue("@SHIFT", header.SHIFT);
                    cmd.Parameters.AddWithValue("@PLACE_CODE", header.PLACE_CODE);
                    cmd.Parameters.AddWithValue("@QC_INCHARGE", header.QC_INCHARGE);
                    cmd.Parameters.AddWithValue("@QC_INCHARGENAME", header.QC_INCHARGENAME);
                    cmd.Parameters.AddWithValue("@CHEMIST", header.CHEMIST);
                    cmd.Parameters.AddWithValue("@CHEMISTNAME", header.CHEMISTNAME);
                    cmd.Parameters.AddWithValue("@REMARKS", header.REMARKS ?? "");
                    cmd.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.ExecuteNonQuery();
                }

                foreach (var d in details)
                {
                    if (d.ITEM_CODE == 0)
                        continue;

                    using var cmd2 = new SqlCommand("sp_FlexQCEntryExcru", conn);

                    cmd2.CommandType = CommandType.StoredProcedure;

                    cmd2.Parameters.AddWithValue("@Action", action);
                    cmd2.Parameters.AddWithValue("@SaveAction", "Details");
                    cmd2.Parameters.AddWithValue("@DOC_ID", "SFQC" + header.V_NO);
                    cmd2.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd2.Parameters.AddWithValue("@V_TYPE", "SFQC");
                    cmd2.Parameters.Add("@V_DATE", SqlDbType.SmallDateTime).Value = header.V_DATE == null ? DBNull.Value : Convert.ToDateTime(header.V_DATE);
                    cmd2.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd2.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd2.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd2.Parameters.AddWithValue("@SHIFT", header.SHIFT);
                    cmd2.Parameters.AddWithValue("@PLACE_CODE", header.PLACE_CODE);
                    cmd2.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                    cmd2.Parameters.AddWithValue("@DEPT_CODE", d.DEPT_CODE);
                    cmd2.Parameters.AddWithValue("@DEPT_NAME", d.DEPT_NAME);
                    cmd2.Parameters.AddWithValue("@BATCH_NO", d.BatchNo);
                    cmd2.Parameters.AddWithValue("@BAG_NO", d.BagNo);
                    cmd2.Parameters.AddWithValue("@JUMBO_NO", d.JUMBO_NO);
                    cmd2.Parameters.AddWithValue("@WB_WT", d.WBWt);
                    cmd2.Parameters.AddWithValue("@GROSS_WT", d.GrWt);
                    cmd2.Parameters.AddWithValue("@TARE_WT", d.TrWt);
                    cmd2.Parameters.AddWithValue("@NET_WT", d.NET_WT);
                    cmd2.Parameters.AddWithValue("@MFI", d.MFI);
                    cmd2.Parameters.AddWithValue("@ASH_CONTENT", d.ASH_CONTENT);
                    cmd2.Parameters.AddWithValue("@PP", d.PP);
                    cmd2.Parameters.AddWithValue("@HD", d.HD);
                    cmd2.Parameters.AddWithValue("@LD", d.LD);
                    cmd2.Parameters.AddWithValue("@COLOR_MIX", d.COLOR_MIX);
                    cmd2.Parameters.AddWithValue("@WRAPPER", d.WRAPPER);
                    cmd2.Parameters.AddWithValue("@FOAM", d.FOAM);
                    cmd2.Parameters.AddWithValue("@RUBBER", d.RUBBER);
                    cmd2.Parameters.AddWithValue("@MOIS_CONTENT", d.MOIS_CONTENT);
                    cmd2.Parameters.AddWithValue("@BOTTOM", d.BOTTOM);
                    cmd2.Parameters.AddWithValue("@STATUSS", d.STATUSS);
                    cmd2.Parameters.AddWithValue("@Remarks", d.REMARKS);
                    cmd2.Parameters.AddWithValue("@Ref_Type", d.Ref_Type);
                    cmd2.Parameters.AddWithValue("@Ref_No", d.Ref_No); 
                    cmd2.Parameters.AddWithValue("@STATUS_CODE", d.STATUS_CODE);
                    cmd2.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd2.Parameters.Add("@UDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                    cmd2.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd2.Parameters.Add("@EDATE", SqlDbType.SmallDateTime).Value = DateTime.Now;
                    cmd2.Parameters.AddWithValue("@AED", "A");
                    cmd2.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd2.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd2.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd2.ExecuteNonQuery();

                    //if (action == "UPDATE")
                    //{
                    //    _globalValidationdate.LogInsertUpdateDelete(destinationTable: "PROD1_QC", sourceTable: "PROD1_QC", transactionType: "Transaction",
                    //    codeVNo: header.V_NO.ToString(), vtype: header.V_TYPE);
                    //}

                    //UPDATE PROD_SFG2
                    string updateSql = @" UPDATE PROD_SFG2 SET REF_TYPE = 'SFQC', REF_NO = @REF_NO
                    WHERE V_TYPE = @REfType AND V_NO = @V_NO AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode";
                    using (var updateCmd = conn.CreateCommand())
                    {
                        updateCmd.CommandText = updateSql;
                        updateCmd.Parameters.AddWithValue("@CompCode", g.PubCompCode);
                        updateCmd.Parameters.AddWithValue("@V_NO", d.Refcode);
                        updateCmd.Parameters.AddWithValue("@REF_NO", header.V_NO);
                        updateCmd.Parameters.AddWithValue("@REfType", d.REfType);
                        updateCmd.Parameters.AddWithValue("@BranchCode", g.PubBranchCode);
                        updateCmd.ExecuteNonQuery();
                    }
                }

                return "Success";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

    }
}