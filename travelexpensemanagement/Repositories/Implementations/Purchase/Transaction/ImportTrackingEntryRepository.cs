using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class ImportTrackingEntryRepository : IImportTrackingEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public ImportTrackingEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public async Task<RepositoryResponseData<ImportTrackingSaudaDetailsModel>> GetSaudaDetails(string saudaNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var model = new ImportTrackingSaudaDetailsModel();

            try
            {
                using (var con = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_ImportTrackingEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "SaudaDetails");
                    cmd.Parameters.AddWithValue("@DOC_ID", saudaNo);
                    cmd.Parameters.AddWithValue("@Comp_Code", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@Branch_Code", gv.PubBranchCode);

                    await con.OpenAsync();

                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        if (!await rdr.ReadAsync())
                            return new RepositoryResponseData<ImportTrackingSaudaDetailsModel> { status = false, message = "Sauda not found." };

                        model.VDate = rdr["V_Date"] == DBNull.Value ? null : Convert.ToDateTime(rdr["V_Date"]);
                        model.PartyCode = Convert.ToInt32(rdr["PARTY_CODE"] == DBNull.Value ? 0 : rdr["PARTY_CODE"]);
                        model.ItemCode = Convert.ToInt32(rdr["ITEM_CODE"] == DBNull.Value ? 0 : rdr["ITEM_CODE"]);
                        model.PayTermCode = Convert.ToInt32(rdr["PAYTERM_CODE"] == DBNull.Value ? 0 : rdr["PAYTERM_CODE"]);
                        model.Currency = rdr["Currency"]?.ToString();

                        return new RepositoryResponseData<ImportTrackingSaudaDetailsModel> { status = true, data = model };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<ImportTrackingSaudaDetailsModel> { status = false, message = "Unable to load Sauda details." + ex.Message };
            }
        }

        public async Task<RepositoryResponseData<object>> GetEntryBySauda(string saudaNo, string vType)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (var con = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_ImportTrackingEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", "VnoExistsForSaudaNo");
                    cmd.Parameters.AddWithValue("@V_type", vType);
                    cmd.Parameters.AddWithValue("@DOC_ID", saudaNo);
                    cmd.Parameters.AddWithValue("@Comp_Code", gv.PubCompCode);

                    await con.OpenAsync();
                    object result = await cmd.ExecuteScalarAsync();

                    if (result == null || result == DBNull.Value)
                        return new RepositoryResponseData<object> { status = true, data = new { exists = false, vNo = "" } };

                    string vNo = Convert.ToString(result);
                    return new RepositoryResponseData<object> { status = true, data = new { exists = true, vNo = vNo } };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<object> { status = false, message = "Unable to check existing entry. " + ex.Message };
            }
        }

        public async Task<RepositoryResponseData<object>> GetEntry(string vType, int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using (var con = _dbConnection.GetErpConnection())
                using (var cmd = new SqlCommand("sp_ImportTrackingEntry", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetData");
                    cmd.Parameters.AddWithValue("@V_TYPE", vType);
                    cmd.Parameters.AddWithValue("@V_NO", vNo);
                    cmd.Parameters.AddWithValue("@DOC_ID", vType + vNo);
                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);

                    await con.OpenAsync();

                    using (var rdr = await cmd.ExecuteReaderAsync())
                    {
                        var header = ReadRows(rdr);
                        if (header.Count == 0)
                            return new RepositoryResponseData<object> { status = false, message = "Entry not found." };

                        await rdr.NextResultAsync();
                        var containers = ReadRows(rdr);

                        await rdr.NextResultAsync();
                        var expenses = ReadRows(rdr);

                        await rdr.NextResultAsync();
                        var files = ReadRows(rdr);

                        return new RepositoryResponseData<object> { status = true, data = new { header = header[0], containers, expenses, files } };
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<object> { status = false, message = "Unable to load entry. " + ex.Message };
            }
        }
        private static List<Dictionary<string, object>> ReadRows(SqlDataReader rdr)
        {
            var rows = new List<Dictionary<string, object>>();
            while (rdr.Read())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < rdr.FieldCount; i++)
                    row[rdr.GetName(i)] = rdr.IsDBNull(i) ? null : rdr.GetValue(i);
                rows.Add(row);
            }
            return rows;
        }

        public async Task<RepositoryResponse> Save(ImportTrackingSaveRequest req)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            if (!req.Containers.Any(c => !string.IsNullOrWhiteSpace(c.CONTAINER_NO)))
                return new RepositoryResponse { status = false, message = "No record for Container Detail in grid." };

            var okExt = new[] { "pdf", "jpg", "jpeg", "png", "bmp", "xls", "xlsx", "doc", "docx" };
            foreach (var f in req.NewFiles)
            {
                string ext = Path.GetExtension(f.FILE_NAME).TrimStart('.').ToLower();
                if (Convert.FromBase64String(f.FILE_DATA).Length > 307200 || !okExt.Contains(ext))
                    return new RepositoryResponse { status = false, message = $"Invalid file for {f.FILE_TYPE} (max 300 KB; PDF, image or Office file only)." };
            }

            using (var con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();
                using (var tran = con.BeginTransaction())
                {
                    try
                    {
                        string docId = req.V_TYPE + req.V_NO;

                        int ycode = 0;
                        var cy = new SqlCommand("SELECT YEAR_CODE FROM EXIM1 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE", con, tran);
                        cy.Parameters.AddWithValue("@V_TYPE", req.V_TYPE);
                        cy.Parameters.AddWithValue("@V_NO", req.V_NO);
                        cy.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cy.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        object y = await cy.ExecuteScalarAsync();
                        if (y != null && y != DBNull.Value && Convert.ToInt32(y) > 0)
                        {
                            ycode = Convert.ToInt32(y);
                        }
                        else
                        {
                            ycode = Convert.ToInt32(gv.PubFYearCode);
                        }

                        await SaveHeader(con, tran, req, docId);
                        await SaveContainers(con, tran, req, docId, ycode);
                        await SaveExpenses(con, tran, req, docId);
                        await SaveFiles(con, tran, req, docId);

                        tran.Commit();
                        return new RepositoryResponse { status = true, message = "Saved successfully!" };
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        return new RepositoryResponse { status = false, message = "Unable to save. " + ex.Message };
                    }
                }
            }
        }

        // ---------- EXIM1 ----------
        private async Task SaveHeader(SqlConnection con, SqlTransaction tran, ImportTrackingSaveRequest req, string docId)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var h = req.Header;

            var cmd = new SqlCommand("sp_ImportTrackingEntry", con, tran);

            cmd.CommandType = CommandType.StoredProcedure;

            if (req.ACTION == "INSERT")
            {
                cmd.Parameters.AddWithValue("@Action", "InsertIntoHeader");
                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
            }
            else
            {
                cmd.Parameters.AddWithValue("@Action", "UpdateHeader");
                cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
            }

            AddKeys(cmd, req.V_TYPE, req.V_NO, req.V_DATE, docId);

            cmd.Parameters.AddWithValue("@SAUDA_TYPE", h.SAUDA_TYPE ?? "");
            cmd.Parameters.AddWithValue("@SAUDA_NO", h.SAUDA_NO);
            cmd.Parameters.AddWithValue("@SAUDA_DATE", Dt(h.SAUDA_DATE));
            cmd.Parameters.AddWithValue("@ITEM_CODE", h.ITEM_CODE);
            cmd.Parameters.AddWithValue("@ITEM_NAME", h.ITEM_NAME ?? "");
            cmd.Parameters.AddWithValue("@SIZE", h.SIZE ?? "");
            cmd.Parameters.AddWithValue("@DEL_LOCATION", h.DEL_LOCATION ?? "");
            cmd.Parameters.AddWithValue("@SUPPLIER", h.SUPPLIER);
            cmd.Parameters.AddWithValue("@AGENT", h.AGENT);
            cmd.Parameters.AddWithValue("@EXPECTED_LOADING", h.EXPECTED_LOADING);
            cmd.Parameters.AddWithValue("@PAYMENT_TERMS", h.PAYMENT_TERMS);
            cmd.Parameters.AddWithValue("@PAYMENT_BANK", h.PAYMENT_BANK);
            cmd.Parameters.AddWithValue("@ETD", Dt(h.ETD));
            cmd.Parameters.AddWithValue("@ETA", Dt(h.ETA));
            cmd.Parameters.AddWithValue("@SUPPLIER_INVNO", h.SUPPLIER_INVNO ?? "");
            cmd.Parameters.AddWithValue("@SUPPLIER_INVDATE", Dt(h.SUPPLIER_INVDATE));
            cmd.Parameters.AddWithValue("@SUPPLIER_INVAMT", h.SUPPLIER_INVAMT);
            cmd.Parameters.AddWithValue("@EXCH_RATE", h.EXCH_RATE);
            cmd.Parameters.AddWithValue("@INV_AMTINR", h.INV_AMTINR);
            cmd.Parameters.AddWithValue("@CURRENCY", h.CURRENCY ?? "");
            cmd.Parameters.AddWithValue("@SCANDOC_VERIFYDATE", Dt(h.SCANDOC_VERIFYDATE));
            cmd.Parameters.AddWithValue("@CHAHANDOVER_DATE", Dt(h.CHAHANDOVER_DATE));
            cmd.Parameters.AddWithValue("@CHA_NAME", h.CHA_NAME);
            cmd.Parameters.AddWithValue("@BL_NO", h.BL_NO ?? "");
            cmd.Parameters.AddWithValue("@BL_DATE", Dt(h.BL_DATE));
            cmd.Parameters.AddWithValue("@BL_RECDDATE", Dt(h.BL_RECDDATE));
            cmd.Parameters.AddWithValue("@BL_HANDOVERDATE", Dt(h.BL_HANDOVERDATE));
            cmd.Parameters.AddWithValue("@CHA_CHARGEDETAIL", h.CHA_CHARGEDETAIL ?? "");
            cmd.Parameters.AddWithValue("@CHKLIST_APPROVDATE", Dt(h.CHKLIST_APPROVDATE));
            cmd.Parameters.AddWithValue("@BE_NO", h.BE_NO ?? "");
            cmd.Parameters.AddWithValue("@BE_DATE", Dt(h.BE_DATE));
            cmd.Parameters.AddWithValue("@ETA_BEDATE", Dt(h.ETA_BEDATE));
            cmd.Parameters.AddWithValue("@IGM_DATE", Dt(h.IGM_DATE));
            cmd.Parameters.AddWithValue("@SAMPLE_COLLECTED", h.SAMPLE_COLLECTED ?? "");
            cmd.Parameters.AddWithValue("@SAMPLE_COLDATE", Dt(h.SAMPLE_COLDATE));
            cmd.Parameters.AddWithValue("@SAMPLE_TESTREPORTDATE", Dt(h.SAMPLE_TESTREPORTDATE));
            cmd.Parameters.AddWithValue("@ASSESMENT_TYPE", h.ASSESMENT_TYPE ?? "");
            cmd.Parameters.AddWithValue("@ASSESMENT_DATE", Dt(h.ASSESMENT_DATE));
            cmd.Parameters.AddWithValue("@DUTY_AMT", h.DUTY_AMT);
            cmd.Parameters.AddWithValue("@DUTY_PAYDATE", Dt(h.DUTY_PAYDATE));
            cmd.Parameters.AddWithValue("@DUTY_PAYMODE", h.DUTY_PAYMODE ?? "");
            cmd.Parameters.AddWithValue("@ADVANCE_LICNO", h.ADVANCE_LICNO ?? "");
            cmd.Parameters.AddWithValue("@DOC_HANDOVERDTBANK", Dt(h.DOC_HANDOVERDTBANK));
            cmd.Parameters.AddWithValue("@BANK_NAME", h.BANK_NAME);
            cmd.Parameters.AddWithValue("@SHIPLINE_PAYDATE", Dt(h.SHIPLINE_PAYDATE));
            cmd.Parameters.AddWithValue("@SHIPLINE_NAME", h.SHIPLINE_NAME);
            cmd.Parameters.AddWithValue("@HOLLAGE_CHARGES", h.HOLLAGE_CHARGES);
            cmd.Parameters.AddWithValue("@DO_DATE", Dt(h.DO_DATE));
            cmd.Parameters.AddWithValue("@SECURITY_AMT", h.SECURITY_AMT);
            cmd.Parameters.AddWithValue("@REFUND_AMT", h.REFUND_AMT);
            cmd.Parameters.AddWithValue("@RECEIPT_DATE", Dt(h.RECEIPT_DATE));
            cmd.Parameters.AddWithValue("@FREE_DAYS", h.FREE_DAYS);
            cmd.Parameters.AddWithValue("@TENTATIVE_HODATE", Dt(h.TENTATIVE_HODATE));
            cmd.Parameters.AddWithValue("@SBLC_NO", h.SBLC_NO ?? "");
            cmd.Parameters.AddWithValue("@SBLC_APPLDATE", Dt(h.SBLC_APPLDATE));
            cmd.Parameters.AddWithValue("@SBLC_PAYDATE", Dt(h.SBLC_PAYDATE));
            cmd.Parameters.AddWithValue("@SBLC_DUEDATE", Dt(h.SBLC_DUEDATE));
            cmd.Parameters.AddWithValue("@SBLC_AVLAMT", h.SBLC_AVLAMT);
            cmd.Parameters.AddWithValue("@SBLC_DISBAMT", h.SBLC_DISBAMT);
            cmd.Parameters.AddWithValue("@SBLC_DISBDATE", Dt(h.SBLC_DISBDATE));
            cmd.Parameters.AddWithValue("@SBLC_INTRATESOFR", h.SBLC_INTRATESOFR);
            cmd.Parameters.AddWithValue("@OTH_CHARGES", h.OTH_CHARGES);
            cmd.Parameters.AddWithValue("@DIFF_DAYS", h.DIFF_DAYS);
            cmd.Parameters.AddWithValue("@INT_RATE", h.INT_RATE);
            cmd.Parameters.AddWithValue("@RAIL_DATE", Dt(h.RAIL_DATE));
            cmd.Parameters.AddWithValue("@ICD_DATE", Dt(h.ICD_DATE));
            cmd.Parameters.AddWithValue("@VSL_NAME", h.VSL_NAME ?? "");
            cmd.Parameters.AddWithValue("@POD", h.POD ?? "");
            cmd.Parameters.AddWithValue("@POL", h.POL ?? "");
            cmd.Parameters.AddWithValue("@DO_VALIDDATE", Dt(h.DO_VALIDDATE));
            cmd.Parameters.AddWithValue("@SEC_DATE", Dt(h.SEC_DATE));
            cmd.Parameters.AddWithValue("@REFUND_RECDATE", Dt(h.REFUND_RECDATE));
            cmd.Parameters.AddWithValue("@DETENTION_AMT", h.DETENTION_AMT);
            cmd.Parameters.AddWithValue("@DETENTION_DATE", Dt(h.DETENTION_DATE));
            cmd.Parameters.AddWithValue("@DETENTION_REASON", h.DETENTION_REASON ?? "");
            cmd.Parameters.AddWithValue("@DETREASON_APPROVBY", h.DETREASON_APPROVBY ?? "");
            cmd.Parameters.AddWithValue("@CUSTOM_OOCDATE", Dt(h.CUSTOM_OOCDATE));
            cmd.Parameters.AddWithValue("@FACTORY_DATE", Dt(h.FACTORY_DATE));
            cmd.Parameters.AddWithValue("@LC_NO", h.LC_NO ?? "");
            cmd.Parameters.AddWithValue("@LC_DATE", Dt(h.LC_DATE));
            cmd.Parameters.AddWithValue("@LC_BENIFICIARY", h.LC_BENIFICIARY);
            cmd.Parameters.AddWithValue("@LC_ISSUEBANK", h.LC_ISSUEBANK);
            cmd.Parameters.AddWithValue("@LC_EXPIRYDATE", Dt(h.LC_EXPIRYDATE));
            cmd.Parameters.AddWithValue("@LC_AMT", h.LC_AMT);
            cmd.Parameters.AddWithValue("@TENTATIVE_HORETDATE", Dt(h.TENTATIVE_HORETDATE));
            cmd.Parameters.AddWithValue("@MOVE_DATE", Dt(h.MOVE_DATE));
            cmd.Parameters.AddWithValue("@TENCONT_HORETDATE", Dt(h.TENCONT_HORETDATE));
            cmd.Parameters.AddWithValue("@LATEST_SHIPDATE", Dt(h.LATEST_SHIPDATE));
            cmd.Parameters.AddWithValue("@QLTY_CLAIMAMT", h.QLTY_CLAIMAMT);
            cmd.Parameters.AddWithValue("@BOE_SUBMITDATE", Dt(h.BOE_SUBMITDATE));
            cmd.Parameters.AddWithValue("@QC_AMT", h.QC_AMT);
            cmd.Parameters.AddWithValue("@ENHANCE_RATE", h.ENHANCE_RATE ?? "");
            cmd.Parameters.AddWithValue("@PASSING_RATE", h.PASSING_RATE);
            cmd.Parameters.AddWithValue("@UNDER_PROTEST", h.UNDER_PROTEST ?? "");
            cmd.Parameters.AddWithValue("@CHA_CHARGETYPE", h.CHA_CHARGETYPE ?? "");
            cmd.Parameters.AddWithValue("@CONCOR_EXPS", h.CONCOR_EXPS);
            cmd.Parameters.AddWithValue("@CHA_AMT", h.CHA_AMT);
            cmd.Parameters.AddWithValue("@CFS_CHARGES", h.CFS_CHARGES ?? "");
            cmd.Parameters.AddWithValue("@CFS_DSTUFFDATE", Dt(h.CFS_DSTUFFDATE));
            cmd.Parameters.AddWithValue("@CFS_AMT", h.CFS_AMT);
            cmd.Parameters.AddWithValue("@CFS_PAYDATE", Dt(h.CFS_PAYDATE));
            cmd.Parameters.AddWithValue("@QC_AMTRECD", h.QC_AMTRECD ?? "");
            cmd.Parameters.AddWithValue("@ACTUAL_CONTHODATE", Dt(h.ACTUAL_CONTHODATE));
            cmd.Parameters.AddWithValue("@PI_NO", h.PI_NO ?? "");
            cmd.Parameters.AddWithValue("@PI_DATE", Dt(h.PI_DATE));
            cmd.Parameters.AddWithValue("@OFFER_NO", h.OFFER_NO ?? "");
            cmd.Parameters.AddWithValue("@ORIGIN_COUNTRY", h.ORIGIN_COUNTRY);
            cmd.Parameters.AddWithValue("@ACCESSIBLE_VALUE", h.ACCESSIBLE_VALUE);
            cmd.Parameters.AddWithValue("@BCD", h.BCD);
            cmd.Parameters.AddWithValue("@SWS", h.SWS);
            cmd.Parameters.AddWithValue("@GST", h.GST);
            cmd.Parameters.AddWithValue("@LC_RCPTDATEBANK", Dt(h.LC_RCPTDATEBANK));
            cmd.Parameters.AddWithValue("@LC_ACCPTDATEFACTORY", Dt(h.LC_ACCPTDATEFACTORY));
            cmd.Parameters.AddWithValue("@PAY_TYPE", h.PAY_TYPE ?? "");
            cmd.Parameters.AddWithValue("@PAY_REFNO", h.PAY_REFNO ?? "");
            cmd.Parameters.AddWithValue("@PAY_AMT", h.PAY_AMT);
            cmd.Parameters.AddWithValue("@PAY_DATE", Dt(h.PAY_DATE));

            cmd.Parameters.AddWithValue("@TEST_MEMONO", h.TEST_MEMONO);
            cmd.Parameters.AddWithValue("@TEST_MEMODATE", Dt(h.TEST_MEMODATE));

            AddAudit(cmd);

            await cmd.ExecuteNonQueryAsync();
        }

        // ---------- EXIM2 ----------
        private async Task SaveContainers(SqlConnection con, SqlTransaction tran, ImportTrackingSaveRequest req, string docId, int ycode)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var del = new SqlCommand("DELETE FROM EXIM2 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE", con, tran);
            AddKeys(del, req.V_TYPE, req.V_NO, req.V_DATE, docId);
            await del.ExecuteNonQueryAsync();

            int srno = 0;
            foreach (var c in req.Containers.Where(c => !string.IsNullOrWhiteSpace(c.CONTAINER_NO)))
            {
                var cmd = new SqlCommand("sp_ImportTrackingEntry", con, tran);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "InsertIntoFooter");

                AddKeys(cmd, req.V_TYPE, req.V_NO, req.V_DATE, docId, ycode);

                cmd.Parameters.AddWithValue("@CONTAINER_NO", c.CONTAINER_NO.Trim());
                cmd.Parameters.AddWithValue("@GRS_WEIGHT", c.GRS_WEIGHT);
                cmd.Parameters.AddWithValue("@PKGS", c.PKGS);
                cmd.Parameters.AddWithValue("@CONT_SIZE", (c.CONT_SIZE ?? "").Trim());
                cmd.Parameters.AddWithValue("@TRANSPORT_CODE", c.TRANSPORT_CODE);
                cmd.Parameters.AddWithValue("@FREIGHT", c.FREIGHT);
                cmd.Parameters.AddWithValue("@SRNO", ++srno);
                cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);

                AddAudit(cmd);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ---------- EXIM3 ----------
        private async Task SaveExpenses(SqlConnection con, SqlTransaction tran, ImportTrackingSaveRequest req, string docId)
        {
            var del = new SqlCommand("DELETE FROM EXIM3 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE", con, tran);
            AddKeys(del, req.V_TYPE, req.V_NO, req.V_DATE, docId);
            await del.ExecuteNonQueryAsync();

            int srno = 0;
            foreach (var x in req.Expenses.Where(x => !string.IsNullOrWhiteSpace(x.EXPS_TYPE)))
            {
                var cmd = new SqlCommand("sp_ImportTrackingEntry", con, tran);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "InsertExpenses");

                AddKeys(cmd, req.V_TYPE, req.V_NO, req.V_DATE, docId);

                cmd.Parameters.AddWithValue("@SAUDA_TYPE", req.Header.SAUDA_TYPE ?? "");
                cmd.Parameters.AddWithValue("@SAUDA_NO", req.Header.SAUDA_NO);
                cmd.Parameters.AddWithValue("@EXPS_TYPE", x.EXPS_TYPE);
                cmd.Parameters.AddWithValue("@PARTY_CODE", x.PARTY_CODE);
                cmd.Parameters.AddWithValue("@INV_NO", x.INV_NO ?? "");
                cmd.Parameters.AddWithValue("@INV_DATE", Dt(x.INV_DATE));
                cmd.Parameters.AddWithValue("@INV_AMT", x.INV_AMT);
                cmd.Parameters.AddWithValue("@REMARKS", (x.REMARKS ?? "").Trim());
                cmd.Parameters.AddWithValue("@REASON", (x.REASON ?? "").Trim());
                cmd.Parameters.AddWithValue("@APPROVED_BY", (x.APPROVED_BY ?? "").Trim());
                cmd.Parameters.AddWithValue("@PAY_DATE", Dt(x.PAY_DATE));
                cmd.Parameters.AddWithValue("@PAY_REFNO", (x.PAY_REFNO ?? "").Trim());
                cmd.Parameters.AddWithValue("@SRNO", ++srno);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ---------- Attachments ----------
        private async Task SaveFiles(SqlConnection con, SqlTransaction tran, ImportTrackingSaveRequest req, string docId)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string[] fileOrder = { "PI", "BL", "BE", "LC", "INV", "DP", "SBLC", "Pic1", "Pic2" };

            var del = new SqlCommand($@"DELETE FROM IMG_TABLE WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE", con, tran);
            AddKeys(del, req.V_TYPE, req.V_NO, req.V_DATE, docId);
            await del.ExecuteNonQueryAsync();

            foreach (var f in req.NewFiles)
            {
                var cmd = new SqlCommand(@"INSERT INTO IMG_TABLE (V_NO,DOC_ID,V_TYPE,V_DATE,COMP_CODE,BRANCH_CODE,YEAR_CODE,ROWID,IMG_FILE,FILE_TYPE,FILE_NAME,
                                        UUSER,UDATE,AED,WSID,LIP,LID) VALUES (@V_NO,@DOC_ID,@V_TYPE,@V_DATE,@COMP_CODE,@BRANCH_CODE,@YEAR_CODE,@ROWID,@IMG_FILE,
                                        @FILE_TYPE,@FILE_NAME,@USERID,format(getdate(),'yyyy-MM-dd HH:mm'),@AED,@WSID,@LIP,@LID)", con, tran);

                AddKeys(cmd, req.V_TYPE, req.V_NO, req.V_DATE, docId);
                cmd.Parameters.AddWithValue("@ROWID", Array.IndexOf(fileOrder, f.FILE_TYPE));
                cmd.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary, -1).Value = Convert.FromBase64String(f.FILE_DATA);
                cmd.Parameters.AddWithValue("@FILE_TYPE", f.FILE_TYPE);
                cmd.Parameters.AddWithValue("@FILE_NAME", f.FILE_NAME);
                cmd.Parameters.AddWithValue("@USERID", gv.PubUserId);
                cmd.Parameters.AddWithValue("@AED", "A");
                AddAudit(cmd);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // ---------- Helpers ----------
        private static object Dt(DateTime? d) => d.HasValue ? d.Value.ToString("yyyyMMdd") : DBNull.Value;

        private void AddKeys(SqlCommand c, string vType, int vNo, DateTime vDate, string docId, int ycode = 0)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            c.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
            c.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
            if (ycode > 0)
                c.Parameters.AddWithValue("@YEAR_CODE", ycode);
            else
                c.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
            c.Parameters.AddWithValue("@V_TYPE", vType);
            c.Parameters.AddWithValue("@V_NO", vNo);
            c.Parameters.AddWithValue("@V_DATE", vDate.ToString("yyyyMMdd"));
            c.Parameters.AddWithValue("@DOC_ID", docId);
        }

        private void AddAudit(SqlCommand c)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            c.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
            c.Parameters.AddWithValue("@LIP", gv.PubLocalId);
            c.Parameters.AddWithValue("@LID", Environment.MachineName);
        }
    }
}
