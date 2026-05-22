using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Transaction;
using travelexpensemanagement.Repositories.Interfaces.QualityControl;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl
{
    public class FlakesQCEntryRepository : IFlakesQCEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;

        public FlakesQCEntryRepository(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService , GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;

        }

        public string SubmitRequest(  FlakesQCEntryLIst_Header header, List<FlakesQCEntryList_Details> details, string action)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                using var conn = _dbConnection.GetErpConnection();
                conn.Open();

                // DELETE OLD DATA
                string deleteSql = @"
                    DELETE FROM PROD2_QC
                    WHERE COMP_CODE = @CompCode
                    AND V_NO = @VNo
                    AND BRANCH_CODE = @BranchCode
                    AND YEAR_CODE = @YearCode";

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
                using (var cmd = new SqlCommand("sp_FlakesQCEntry", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@SaveAction", "Header");
                    cmd.Parameters.AddWithValue("@v_NO", header.V_NO);
                    cmd.Parameters.AddWithValue("@DOC_ID", "SFQC" + header.V_NO);
                    cmd.Parameters.AddWithValue("@V_DATE", header.V_DATE);
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
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd.ExecuteNonQuery();
                }

                // SAVE DETAILS
                foreach (var d in details)
                {
                    if (d.ITEM_CODE == 0)
                        continue;

                    using var cmd2 = new SqlCommand("sp_FlakesQCEntry", conn);

                    cmd2.CommandType = CommandType.StoredProcedure;

                    cmd2.Parameters.AddWithValue("@Action", action);
                    cmd2.Parameters.AddWithValue("@SaveAction", "Details");
                    cmd2.Parameters.AddWithValue("@DOC_ID", "SFQC" + header.V_NO);
                    cmd2.Parameters.AddWithValue("@V_NO", header.V_NO);
                    cmd2.Parameters.AddWithValue("@V_TYPE", "SFQC");
                    cmd2.Parameters.AddWithValue("@V_DATE", header.V_DATE);
                    cmd2.Parameters.AddWithValue("@COMP_CODE", g.PubCompCode);
                    cmd2.Parameters.AddWithValue("@BRANCH_CODE", g.PubBranchCode);
                    cmd2.Parameters.AddWithValue("@YEAR_CODE", g.PubFYearCode);
                    cmd2.Parameters.AddWithValue("@ITEM_CODE", d.ITEM_CODE);
                    cmd2.Parameters.AddWithValue("@COLOR_NAME", d.COLOR_NAME);
                    cmd2.Parameters.AddWithValue("@PTYPE_NAME", d.PTYPE_NAME);
                    cmd2.Parameters.AddWithValue("@WIDTH", d.WIDTH);
                    cmd2.Parameters.AddWithValue("@GRAM", d.WBWt);
                    cmd2.Parameters.AddWithValue("@RESULT1", d.RESULT1);
                    cmd2.Parameters.AddWithValue("@RESULT2", d.RESULT2);
                    cmd2.Parameters.AddWithValue("@PRKG", d.PRKG);
                    cmd2.Parameters.AddWithValue("@WASTE", d.WASTE);
                    cmd2.Parameters.AddWithValue("@DNR", d.DNR);
                    cmd2.Parameters.AddWithValue("@CPRDN", d.CPRDN);
                    cmd2.Parameters.AddWithValue("@TIME1_WIDTH", d.TIME1_WIDTH);
                    cmd2.Parameters.AddWithValue("@TIME2_WIDTH", d.TIME2_WIDTH);
                    cmd2.Parameters.AddWithValue("@TIME3_WIDTH", d.TIME3_WIDTH);
                    cmd2.Parameters.AddWithValue("@TIME4_WIDTH", d.TIME4_WIDTH);
                    cmd2.Parameters.AddWithValue("@TIME5_WIDTH", d.TIME5_WIDTH);
                    cmd2.Parameters.AddWithValue("@Remarks", d.REMARKS ?? "");
                    cmd2.Parameters.AddWithValue("@COLOR_CODE", d.COLOR_CODE);
                    cmd2.Parameters.AddWithValue("@SHIFT", header.SHIFT);
                    cmd2.Parameters.AddWithValue("@PLACE_CODE", header.PLACE_CODE);
                    cmd2.Parameters.AddWithValue("@EMP_CODE", header.EMP_CODE);
                    cmd2.Parameters.AddWithValue("@PC_LOWMELT", d.PC_LOWMELT);
                    cmd2.Parameters.AddWithValue("@GLUE_CONTENT", d.GLUE_CONTENT);
                    cmd2.Parameters.AddWithValue("@OTHERS", d.OTHERS);
                    cmd2.Parameters.AddWithValue("@GRADE", d.GRADE);
                    cmd2.Parameters.AddWithValue("@YELLOWP", d.YELLOWP);
                    cmd2.Parameters.AddWithValue("@BLUEP", d.BLUEP);
                    cmd2.Parameters.AddWithValue("@OTHERP", d.OTHERP);
                    cmd2.Parameters.AddWithValue("@YELLOW160C", d.YELLOW160C);
                    cmd2.Parameters.AddWithValue("@MOISTURE", d.MOISTURE);
                    cmd2.Parameters.AddWithValue("@BULKDENSITY", d.BULKDENSITY);
                    cmd2.Parameters.AddWithValue("@PH_FLAKES", d.PH_FLAKES);
                    cmd2.Parameters.AddWithValue("@OVERSIZED", d.OVERSIZED);
                    cmd2.Parameters.AddWithValue("@Pord_No", d.Pord_No);
                    cmd2.Parameters.AddWithValue("@Pord_Type", d.Pord_Type);
                    cmd2.Parameters.AddWithValue("@UUSER", g.PubUserId);
                    cmd2.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd2.Parameters.AddWithValue("@EUSER", g.PubUserId);
                    cmd2.Parameters.AddWithValue("@EDATE", DateTime.Now);
                    cmd2.Parameters.AddWithValue("@AED", "A");
                    cmd2.Parameters.AddWithValue("@WSID", g.PubWorkStationID);
                    cmd2.Parameters.AddWithValue("@LIP", g.PubLocalId);
                    cmd2.Parameters.AddWithValue("@LID", Environment.MachineName);

                    cmd2.ExecuteNonQuery();

                    if (action == "UPDATE")
                    {
                        _globalValidationdate.LogInsertUpdateDelete(destinationTable: "PROD1_QC", sourceTable: "PROD1_QC", transactionType: "Transaction",
                        codeVNo: header.V_NO.ToString(), vtype: header.V_TYPE);
                    }

                    // UPDATE PROD_SFG2
                    string updateSql = @"
                        UPDATE PROD_SFG2
                        SET REF_TYPE = 'SFQC',
                            REF_NO = @REF_NO
                        WHERE V_TYPE = @REfType
                        AND V_NO = @V_NO
                        AND COMP_CODE = @CompCode
                        AND BRANCH_CODE = @BranchCode";

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