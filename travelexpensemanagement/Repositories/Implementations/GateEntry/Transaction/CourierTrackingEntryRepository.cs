using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry.Transaction;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
using static travelexpensemanagement.Controllers.GateEntry.Transaction.CourierTrackingEntryController;
using travelexpensemanagement.Models.GateEntry.Transaction;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class CourierTrackingEntryRepository : ICourierTrackingEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public CourierTrackingEntryRepository(DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public int GetNextDocNo(string docType)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = @"SELECT ISNULL(MAX(V_no), 0) + 1 FROM COURIER_TRACKING  WHERE V_TYPE = @V_TYPE 
            AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BranchCode", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YearCode", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", docType);

                    con.Open();
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        public string SaveCourierData(CourierTrackingModel model)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    var docID = model.DocType + model.DocNo;
                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", model.DocType ?? "");
                    cmd.Parameters.AddWithValue("@V_NO", model.DocNo ?? "");
                    cmd.Parameters.AddWithValue("@V_DATE", model.DocDate);
                    cmd.Parameters.AddWithValue("@DOC_ID", docID);
                    cmd.Parameters.AddWithValue("@PARTY_CODE", model.PartyCode ?? "");
                    cmd.Parameters.AddWithValue("@PARTY_NAME", model.PartyName ?? "");
                    cmd.Parameters.AddWithValue("@CITY_CODE", model.CITY_CODE ?? "");
                    cmd.Parameters.AddWithValue("@CITY_NAME", model.CITY_NAME ?? "");
                    cmd.Parameters.AddWithValue("@COURIER_NAME", model.CourierName ?? "");
                    cmd.Parameters.AddWithValue("@DOCKET_NO", model.DocketNo ?? "");
                    cmd.Parameters.AddWithValue("@RECD_BY", model.ReceivedBy ?? "");
                    cmd.Parameters.AddWithValue("@PURPOSE", model.Purpose ?? "");
                    cmd.Parameters.AddWithValue("@WEIGHT", string.IsNullOrWhiteSpace(model.Weight) ? DBNull.Value : Convert.ToDouble(model.Weight));
                    cmd.Parameters.AddWithValue("@REMARKS", model.Remarks ?? "");
                    cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                    cmd.Parameters.AddWithValue("@AED", "A");
                    cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@Action", model.ACTION);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return model.ACTION == "INSERT" ? "Record inserted successfully." : "Record updated successfully.";
        }

        public GetCourierTrackingModel GetCourierData(string docType, string docNo)
        {
            var global = _globalVariableService.GetGlobalVariables();
            var docid = docType + docNo;
            GetCourierTrackingModel model = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_InsertCourierTracking", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "GetID");
                    cmd.Parameters.AddWithValue("@DOC_ID", docid);
                    cmd.Parameters.AddWithValue("@COMP_CODE", global.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", global.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", global.PubFYearCode);
                    con.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            model = new GetCourierTrackingModel
                            {
                                VType = rdr["V_TYPE"]?.ToString(),
                                DocDate = rdr["V_DATE"] != DBNull.Value ? Convert.ToDateTime(rdr["V_DATE"]).ToString("dd/MM/yyyy") : null,
                                DocNo = rdr["DOC_ID"]?.ToString(),
                                PartyName = rdr["PARTY_NAME"]?.ToString(),
                                City = rdr["CITY_CODE"]?.ToString(),
                                CourierName = rdr["COURIER_NAME"]?.ToString(),
                                DocketNo = rdr["DOCKET_NO"]?.ToString(),
                                ReceivedBy = rdr["RECD_BY"]?.ToString(),
                                Purpose = rdr["PURPOSE"]?.ToString(),
                                Weight = rdr["WEIGHT"] != DBNull.Value ? Convert.ToDecimal(rdr["WEIGHT"]) : 0,
                                Remarks = rdr["REMARKS"]?.ToString()
                            };
                        }
                    }
                }
            }
            return model;
        }

        public RepositoryResponseList<GetCourierTrackingModel> GetCourierTrackingEntryList(string searchTerm, int pageNumber, int pageSize)
        {
            throw new NotImplementedException();
        }

        public Task<RepositoryResponse> DeleteCourierTrackingEntry(string vNo, string docType)
        {
            throw new NotImplementedException();
        }
        public CourierTrackingReportModel PrintCourierReport(PrintCourierReportModel model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string selForMul = "";

            selForMul = "{COURIER_TRACKING.COMP_CODE} = " + gv.PubCompCode;
            selForMul += " and {COURIER_TRACKING.BRANCH_CODE} = " + gv.PubBranchCode;
            selForMul += " and {COURIER_TRACKING.YEAR_CODE} = " + gv.PubFYearCode;
            selForMul += " and {COURIER_TRACKING.V_DATE} IN DATE(" +
                         model.FromDate.Value.ToString("yyyy,MM,dd") +
                         ") TO DATE(" +
                         model.ToDate.Value.ToString("yyyy,MM,dd") + ")";

            if (!string.IsNullOrWhiteSpace(model.VType))
            {
                selForMul += " and {COURIER_TRACKING.V_TYPE} = '" + model.VType + "'";
            }

            if (!string.IsNullOrWhiteSpace(model.PartyName))
            {
                selForMul += " and {COURIER_TRACKING.party_Name} = '" + model.PartyName + "'";
            }

            return new CourierTrackingReportModel
            {
                Reportname = "rptCourierTracking",
                Database = "ERPDB",
                SelectionFormula = selForMul,

                FormulaFields = new List<FormulaFieldModel>
            {
            new FormulaFieldModel
            {
                FormulaName="comp_name",
                FormulaValue=gv.CompanyName
            },
            new FormulaFieldModel
            {
                FormulaName="comp_add1",
                FormulaValue=gv.Address1
            },
            new FormulaFieldModel
            {
                FormulaName="comp_add2",
                FormulaValue=gv.Address2
            },
            new FormulaFieldModel
            {
                FormulaName="F1",
                FormulaValue=$"From Date {model.FromDate:dd/MM/yyyy} to {model.ToDate:dd/MM/yyyy}"
            },
            new FormulaFieldModel
            {
                FormulaName="RPTNAME",
                FormulaValue=$"COURIER TRACKING LIST ({model.VType})"
            }
        }
            };
        }

    }
}