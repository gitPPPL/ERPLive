using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class SalesExportCostingRepository : ISalesExportCostingRepository
    {
        private readonly GlobalVariableService _globalVariableService;
        private readonly DataBaseConnection _dbConnection;
        private readonly LogService.LogService _logService;
        
        public SalesExportCostingRepository(GlobalValidationdate globalValidationdate, GlobalVariableService globalVariableService,
            DataBaseConnection dbConnection, LogService.LogService logService)
        {
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;
            _logService = logService;
        }
        const string doctype = "EXPC";

        public RepositoryResponse SaveSalesExportCosting(SalesExportCostingModel model)
        {
            if (model == null || model.items == null)
            {
                return new RepositoryResponse { status = false, message = "Invalid request" };
            }
            var gv = _globalVariableService.GetGlobalVariables();
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            //Header
                            string docId = $"{model.V_TYPE}{model.V_NO}";
                            string action = (model.ACTION) == "INSERT" ? "HEADERINSERT" : "HEADERUPDATE";
                            string mode = "";

                            using (SqlCommand cmd = new SqlCommand("sp_SalesExportCosting", con, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@Action", action);
                                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                cmd.Parameters.AddWithValue("@V_TYPE", doctype);
                                cmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOC_ID", docId);
                                cmd.Parameters.AddWithValue("@PARTY_CODE", (object?)model.PARTY_CODE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DEL_LOCATION", (object?)model.DEL_LOCATION ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@AGENT_CODE", (object?)model.AGENT_CODE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMM_RATE", (object?)model.COMM_RATE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CURRENCY", (object?)model.CURRENCY ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@EX_RATE", (object?)model.EX_RATE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@EX_FRTRATE", (object?)model.EX_FRTRATE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@LOADING_QTY", (object?)model.LOADING_QTY ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@STUFF_QTY", (object?)model.STUFF_QTY ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OCEAN_FRTUSD", (object?)model.OCEAN_FRTUSD ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OCEAN_FRTEXPS", (object?)model.OCEAN_FRTEXPS ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OCEAN_ANSCH", (object?)model.OCEAN_ANSCH ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OCEAN_ILHAUCOST", (object?)model.OCEAN_ILHAUCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OCEAN_PORTHANDCH", (object?)model.OCEAN_PORTHANDCH ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OCEAN_BLFEE", (object?)model.OCEAN_BLFEE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OCEAN_SEALCOST", (object?)model.OCEAN_SEALCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OCEAN_DOCFEEEXPORT", (object?)model.OCEAN_DOCFEEEXPORT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CONCER_EXPS", (object?)model.CONCER_EXPS ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SHIP_RAILFRT", (object?)model.SHIP_RAILFRT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@BUSY_SEASONCH", (object?)model.BUSY_SEASONCH ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@LOCAL_TPTCOST", (object?)model.LOCAL_TPTCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@INSU_COST", (object?)model.INSU_COST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@BANK_CHARGES", (object?)model.BANK_CHARGES ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@BL_CHARGES", (object?)model.BL_CHARGES ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CLEARING_COST", (object?)model.CLEARING_COST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOOR_DELUSD", (object?)model.DOOR_DELUSD ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOOR_DELCOST", (object?)model.DOOR_DELCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DOC_CHARGES", (object?)model.DOC_CHARGES ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHA_AGENCYCOST", (object?)model.CHA_AGENCYCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHA_NOMCOST", (object?)model.CHA_NOMCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHA_EXAMCOST", (object?)model.CHA_EXAMCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHA_CGMCOST", (object?)model.CHA_CGMCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHA_CMCCOST", (object?)model.CHA_CMCCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHA_VGMCOST", (object?)model.CHA_VGMCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CHA_LULCOST", (object?)model.CHA_LULCOST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SAMPLE_COSTING", (object?)model.SAMPLE_COSTING ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SAMPLE_COSTAMT", (object?)model.SAMPLE_COSTAMT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CUSTOM_COST", (object?)model.CUSTOM_COST ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@GRS_LESS1PER", (object?)model.GRS_LESS1PER ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DISC_AMT", (object?)model.DISC_AMT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@EPCG_AMT", (object?)model.EPCG_AMT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADV_LICAMT", (object?)model.ADV_LICAMT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ROAD_TAPEAMT", (object?)model.ROAD_TAPEAMT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@DDBAK_AMT", (object?)model.DDBAK_AMT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@COSTPERKG_EXPLANT", (object?)model.COSTPERKG_EXPLANT ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ADD_DBK", (object?)model.ADD_DBK ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@LESS_MEIS", (object?)model.LESS_MEIS ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@OUR_OFFERRATE", (object?)model.OUR_OFFERRATE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@COSTING_TYPE", (object?)model.COSTING_TYPE ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@REMARKS", (object?)model.REMARKS ?? DBNull.Value);
                                if (model.ACTION == "INSERT")
                                {
                                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                    mode = "INSERT";
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                                    mode = "UPDATE";
                                }
                                cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                                cmd.ExecuteNonQuery();
                            }

                            //Footer
                            string DelQry = @"DELETE FROM COSTING_EXPORT2 WHERE V_NO = @V_NO AND V_TYPE = @V_TYPE AND COMP_CODE = @COMP_CODE 
                                                AND BRANCH_CODE = @BRANCH_CODE AND YEAR_CODE = @YEAR_CODE";
                            using (SqlCommand fDelcmd = new SqlCommand(DelQry, con, tran))
                            {

                                fDelcmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                fDelcmd.Parameters.AddWithValue("@V_TYPE", doctype);
                                fDelcmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                fDelcmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                fDelcmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                                fDelcmd.ExecuteNonQuery();
                            }
                            int i = 1;
                            foreach (var item in model.items)
                            {
                                using (SqlCommand fcmd = new SqlCommand("sp_SalesExportCosting", con, tran))
                                {
                                    fcmd.CommandType = CommandType.StoredProcedure;
                                    fcmd.Parameters.AddWithValue("@Action", "FOOTERINSERT");
                                    fcmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                                    fcmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                                    fcmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                                    fcmd.Parameters.AddWithValue("@V_TYPE", doctype);
                                    fcmd.Parameters.AddWithValue("@V_NO", (object?)model.V_NO ?? DBNull.Value);
                                    fcmd.Parameters.AddWithValue("@V_DATE", (object?)model.V_DATE ?? DBNull.Value);
                                    fcmd.Parameters.AddWithValue("@DOC_ID", docId);
                                    fcmd.Parameters.AddWithValue("@ITEM_CODE", (object?)item.ITEM_CODE ?? DBNull.Value);
                                    fcmd.Parameters.AddWithValue("@RATE", (object?)item.RATE ?? DBNull.Value);
                                    fcmd.Parameters.AddWithValue("@QTY", (object?)item.QTY ?? DBNull.Value);
                                    fcmd.Parameters.AddWithValue("@HSN_CODE", (object?)item.HSN_CODE ?? DBNull.Value);
                                    fcmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                                    fcmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                                    fcmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                                    fcmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                    fcmd.Parameters.AddWithValue("@SNO", i++);

                                    fcmd.ExecuteNonQuery();

                                }
                            }
                            tran.Commit();

                            //Log Service
                            //_logService.InsertLog("COSTING_EXPORT1", "Sales Export Costing", "Transaction", mode, doctype, model.V_NO.ToString(), model.V_DATE);
                            //_logService.InsertLog("COSTING_EXPORT2", "Sales Export Costing", "Transaction", mode, doctype, model.V_NO.ToString(), model.V_DATE);
                            return new RepositoryResponse { status = true, message = "Saved Successfully!" };
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            return new RepositoryResponse { status = false, message = ex.Message };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }
        
        public RepositoryResponseData<SalesExportCostingModel> GetDataById(int docId)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var data = new SalesExportCostingModel();
            try
            {
                using (var con = _dbConnection.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_SalesExportCosting", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "GetById");
                        cmd.Parameters.AddWithValue("@V_NO", docId);
                        cmd.Parameters.AddWithValue("@V_TYPE", doctype);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                data.V_TYPE = reader["V_TYPE"] == DBNull.Value ? null : reader["V_TYPE"].ToString();
                                data.V_NO = reader["V_NO"] == DBNull.Value ? null : Convert.ToInt32(reader["V_NO"]);
                                data.V_DATE = reader["V_DATE"] == DBNull.Value ? null : Convert.ToDateTime(reader["V_DATE"]);
                                data.PARTY_CODE = reader["PARTY_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["PARTY_CODE"]);
                                data.DEL_LOCATION = reader["DEL_LOCATION"] == DBNull.Value ? null : Convert.ToInt32(reader["DEL_LOCATION"]);
                                data.AGENT_CODE = reader["AGENT_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["AGENT_CODE"]);
                                data.COMM_RATE = reader["COMM_RATE"] == DBNull.Value ? null : Convert.ToDecimal(reader["COMM_RATE"]);
                                data.CURRENCY = reader["CURRENCY"] == DBNull.Value ? null : reader["CURRENCY"].ToString();
                                data.EX_RATE = reader["EX_RATE"] == DBNull.Value ? null : Convert.ToDecimal(reader["EX_RATE"]);
                                data.EX_FRTRATE = reader["EX_FRTRATE"] == DBNull.Value ? null : Convert.ToDecimal(reader["EX_FRTRATE"]);
                                data.LOADING_QTY = reader["LOADING_QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["LOADING_QTY"]);
                                data.STUFF_QTY = reader["STUFF_QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["STUFF_QTY"]);
                                data.OCEAN_FRTUSD = reader["OCEAN_FRTUSD"] == DBNull.Value ? null : Convert.ToDecimal(reader["OCEAN_FRTUSD"]);
                                data.OCEAN_FRTEXPS = reader["OCEAN_FRTEXPS"] == DBNull.Value ? null : Convert.ToDecimal(reader["OCEAN_FRTEXPS"]);
                                data.OCEAN_ANSCH = reader["OCEAN_ANSCH"] == DBNull.Value ? null : Convert.ToDecimal(reader["OCEAN_ANSCH"]);
                                data.OCEAN_ILHAUCOST = reader["OCEAN_ILHAUCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["OCEAN_ILHAUCOST"]);
                                data.OCEAN_PORTHANDCH = reader["OCEAN_PORTHANDCH"] == DBNull.Value ? null : Convert.ToDecimal(reader["OCEAN_PORTHANDCH"]);
                                data.OCEAN_BLFEE = reader["OCEAN_BLFEE"] == DBNull.Value ? null : Convert.ToDecimal(reader["OCEAN_BLFEE"]);
                                data.OCEAN_SEALCOST = reader["OCEAN_SEALCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["OCEAN_SEALCOST"]);
                                data.OCEAN_DOCFEEEXPORT = reader["OCEAN_DOCFEEEXPORT"] == DBNull.Value ? null : Convert.ToDecimal(reader["OCEAN_DOCFEEEXPORT"]);
                                data.CONCER_EXPS = reader["CONCER_EXPS"] == DBNull.Value ? null : Convert.ToDecimal(reader["CONCER_EXPS"]);
                                data.SHIP_RAILFRT = reader["SHIP_RAILFRT"] == DBNull.Value ? null : Convert.ToDecimal(reader["SHIP_RAILFRT"]);
                                data.BUSY_SEASONCH = reader["BUSY_SEASONCH"] == DBNull.Value ? null : Convert.ToDecimal(reader["BUSY_SEASONCH"]);
                                data.LOCAL_TPTCOST = reader["LOCAL_TPTCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["LOCAL_TPTCOST"]);
                                data.INSU_COST = reader["INSU_COST"] == DBNull.Value ? null : Convert.ToDecimal(reader["INSU_COST"]);
                                data.BANK_CHARGES = reader["BANK_CHARGES"] == DBNull.Value ? null : Convert.ToDecimal(reader["BANK_CHARGES"]);
                                data.BL_CHARGES = reader["BL_CHARGES"] == DBNull.Value ? null : Convert.ToDecimal(reader["BL_CHARGES"]);
                                data.CLEARING_COST = reader["CLEARING_COST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CLEARING_COST"]);
                                data.DOOR_DELUSD = reader["DOOR_DELUSD"] == DBNull.Value ? null : Convert.ToDecimal(reader["DOOR_DELUSD"]);
                                data.DOOR_DELCOST = reader["DOOR_DELCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["DOOR_DELCOST"]);
                                data.DOC_CHARGES = reader["DOC_CHARGES"] == DBNull.Value ? null : Convert.ToDecimal(reader["DOC_CHARGES"]);
                                data.CHA_AGENCYCOST = reader["CHA_AGENCYCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CHA_AGENCYCOST"]);
                                data.CHA_NOMCOST = reader["CHA_NOMCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CHA_NOMCOST"]);
                                data.CHA_EXAMCOST = reader["CHA_EXAMCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CHA_EXAMCOST"]);
                                data.CHA_CGMCOST = reader["CHA_CGMCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CHA_CGMCOST"]);
                                data.CHA_CMCCOST = reader["CHA_CMCCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CHA_CMCCOST"]);
                                data.CHA_VGMCOST = reader["CHA_VGMCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CHA_VGMCOST"]);
                                data.CHA_LULCOST = reader["CHA_LULCOST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CHA_LULCOST"]);
                                data.SAMPLE_COSTING = reader["SAMPLE_COSTING"] == DBNull.Value ? null : reader["SAMPLE_COSTING"].ToString();
                                data.SAMPLE_COSTAMT = reader["SAMPLE_COSTAMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["SAMPLE_COSTAMT"]);
                                data.CUSTOM_COST = reader["CUSTOM_COST"] == DBNull.Value ? null : Convert.ToDecimal(reader["CUSTOM_COST"]);
                                data.GRS_LESS1PER = reader["GRS_LESS1PER"] == DBNull.Value ? null : Convert.ToDecimal(reader["GRS_LESS1PER"]);
                                data.DISC_AMT = reader["DISC_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["DISC_AMT"]);
                                data.EPCG_AMT = reader["EPCG_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["EPCG_AMT"]);
                                data.ADV_LICAMT = reader["ADV_LICAMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["ADV_LICAMT"]);
                                data.ROAD_TAPEAMT = reader["ROAD_TAPEAMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["ROAD_TAPEAMT"]);
                                data.DDBAK_AMT = reader["DDBAK_AMT"] == DBNull.Value ? null : Convert.ToDecimal(reader["DDBAK_AMT"]);
                                data.COSTPERKG_EXPLANT = reader["COSTPERKG_EXPLANT"] == DBNull.Value ? null : Convert.ToDecimal(reader["COSTPERKG_EXPLANT"]);
                                data.ADD_DBK = reader["ADD_DBK"] == DBNull.Value ? null : Convert.ToDecimal(reader["ADD_DBK"]);
                                data.LESS_MEIS = reader["LESS_MEIS"] == DBNull.Value ? null : Convert.ToDecimal(reader["LESS_MEIS"]);
                                data.OUR_OFFERRATE = reader["OUR_OFFERRATE"] == DBNull.Value ? null : Convert.ToDecimal(reader["OUR_OFFERRATE"]);
                                data.COSTING_TYPE = reader["COSTING_TYPE"] == DBNull.Value ? null : reader["COSTING_TYPE"].ToString();
                                data.REMARKS = reader["REMARKS"] == DBNull.Value ? null : reader["REMARKS"].ToString();
                            }

                            if (reader.NextResult())
                            {
                                while (reader.Read())
                                {
                                    var item = new SalesExportCostingItemsModel
                                    {
                                        ITEM_CODE = reader["ITEM_CODE"] == DBNull.Value ? null : Convert.ToInt32(reader["ITEM_CODE"]),
                                        ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                        RATE = reader["RATE"] == DBNull.Value ? null : Convert.ToDecimal(reader["RATE"]),
                                        QTY = reader["QTY"] == DBNull.Value ? null : Convert.ToDecimal(reader["QTY"]),
                                        HSN_CODE = reader["HSN_CODE"] == DBNull.Value ? null : reader["HSN_CODE"].ToString()
                                    };

                                    data.items.Add(item);
                                }
                            }
                        }
                    }
                }
                return new RepositoryResponseData<SalesExportCostingModel> { status = true, data = data };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<SalesExportCostingModel> { status = false, message = ex.Message };
            }
        }
    }
}
