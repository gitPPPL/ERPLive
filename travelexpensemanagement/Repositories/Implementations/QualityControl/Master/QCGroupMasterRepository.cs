using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Repositories.Implementations.QualityControl.Master
{
    public class QCGroupMasterRepository : IQCGroupMasterRepository
    {
        private readonly LogService.LogService _logService;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        public QCGroupMasterRepository(LogService.LogService logService, DataBaseConnection dbConnection, GlobalVariableService globalVariableService)
        {
            _logService = logService;
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }
        public RepositoryResponse DeleteQCGroupByCode(int docId)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCG_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@CODE", docId);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                //===========log insert
                _logService.InsertLog("QCG_MAST", "QC Group Master", "Master", "Delete", "", docId.ToString(), null);

                return new RepositoryResponse { status = true, message = "QC Group deleted successfully." };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse  { status = false, message = ex.Message };
            }
        }

        private string SaveOrUpdateQCGroup(QCG_MAST model, string action)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCG_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@CODE", model.CODE);
                        cmd.Parameters.AddWithValue("@NAME", model.NAME ?? "");
                        cmd.Parameters.AddWithValue("@QC_TYPE", model.QC_TYPE ?? "");
                        cmd.Parameters.AddWithValue("@ACTIVE", model.ACTIVE);
                        cmd.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@EUSER", globalVar.PubUserId);
                        cmd.Parameters.AddWithValue("@EDATE", DateTime.Now);
                        cmd.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID ?? "WEB");
                        cmd.Parameters.AddWithValue("@LIP", globalVar.PubLocalId ?? "127.0.0.1");
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName ?? "WEB");

                        con.Open();
                        string mode = action == "INSERT" ? "INSERT" : "UPDATE";
                        int code = 0;
                        if (action == "INSERT")
                        {
                            code = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        else
                        {
                            cmd.ExecuteNonQuery();
                            code = model.CODE;
                        }


                        //===========log insert
                        _logService.InsertLog("QCG_MAST", "QC Group Master", "Master", mode, "", code.ToString(), null);

                        return "Success";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }
        private bool IsDuplicateQCGroup(string name, int code)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand("sp_QCG_MAST", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Action", "Exist");
                    cmd.Parameters.AddWithValue("@Name", name.Trim());
                    cmd.Parameters.AddWithValue("@CODE", code);

                    con.Open();
                    object result = cmd.ExecuteScalar();
                    return result != null; // true if duplicate exist
                }
            }
        }

        public RepositoryResponseData<bool> IsQcGroupDeletable(int docId)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            bool isExists = false;
            string msg = "";
            try
            {
                //===========Check Qc Group existence in QC Master===========
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCG_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Del_CheckInQcMast");
                        cmd.Parameters.AddWithValue("@CODE", docId);
                        cmd.Parameters.AddWithValue("@Comp_Code", gv.PubCompCode);

                        con.Open();
                        object result = cmd.ExecuteScalar();

                        string qcGroupName = result?.ToString();
                        isExists = string.IsNullOrEmpty(qcGroupName) ? false : true;

                        msg = $"QC Group <b>{qcGroupName}</b> exists in QC Master and cannot be deleted.";
                    }
                    return new RepositoryResponseData<bool> { status = true, message = msg, data = isExists };
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<bool> { status = false, message = ex.Message };
            }
        }

        public RepositoryResponse SaveQCGroup(QCG_MAST model)
        {
            if (string.IsNullOrWhiteSpace(model.NAME))
            {
                return new RepositoryResponse { status = false, message = "QC Group Name cannot be blank." };
            }

            if (string.IsNullOrWhiteSpace(model.QC_TYPE))
            {
                return new RepositoryResponse { status = false, message = "QC Type cannot be blank." };
            }

            string action = model.ACTION == "INSERT" ? "INSERT" : "UPDATE";

            if (IsDuplicateQCGroup(model.NAME, model.CODE))
            {
                return new RepositoryResponse { status = false, message = "QC Group name already exists." };
            }

            var result = SaveOrUpdateQCGroup(model, action);

            if (result == "Success")
            {
                return new RepositoryResponse { status = true };
            }
            else
            {
                return new RepositoryResponse { status = false, message = result };
            }
        }
    }
}
