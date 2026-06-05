using DocumentFormat.OpenXml.Office.Word;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Net.Http.Headers;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.GateEntry;
using travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction;
using static travelexpensemanagement.Controllers.GateEntry.Transaction.VehicleInwardEntryController;
using static travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction.VehicleInwardRepository;

namespace travelexpensemanagement.Repositories.Implementations.GateEntry.Transaction
{
    public class VehicleInwardRepository : IVehicleInwardRepository
    {
        private readonly GlobalVariableService _globalValue;
        private readonly DataBaseConnection _dbcontext;
        private readonly DbHelper _dbHelper;
        private readonly IWebHostEnvironment _env;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly LogService.LogService _logService;
        public VehicleInwardRepository(GlobalVariableService globalValue, DataBaseConnection dbcontext, DbHelper dbHelper, IWebHostEnvironment env,
            GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _globalValue = globalValue;
            _dbcontext = dbcontext;
            _dbHelper = dbHelper;
            _env = env;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }

        public async Task<RepositoryResponseData<DriverDetail>> DriverDetails(string mobileNo)
        {
            var response = new RepositoryResponseData<DriverDetail>();
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_GetVehicleInward_DriverAndVehicle_Details", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Driver");
                        cmd.Parameters.AddWithValue("@MobileNo", mobileNo);
                        cmd.Parameters.AddWithValue("@Comp_code", compCode);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var driverDetail = new DriverDetail
                                {
                                    dLNo = reader["DL_NO"]?.ToString(),
                                    pANNo = reader["PAN_NO"]?.ToString(),
                                    driverName = reader["DRIVER_NAME"]?.ToString(),
                                    driverNo = reader["DRIVER_NO"]?.ToString()
                                };
                                response.status = true;
                                response.message = "Driver details found successfully!";
                                response.data = driverDetail;
                                return response;
                            }
                            else
                            {
                                response.status = false;
                                response.message = "Driver not found";
                                return response;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "Error in loading driver details: " + ex.Message;
                return response;
            }
        }
        public async Task<RepositoryResponse> SaveOrUpdate(TransportInwardModel POmodel)
        {
            string path = @"Uploads\VehicleInward";
            string? attachmentFilePath = null;
            string? fileName = null;
            string? oldFile = null;

            string? tempFilePath = null;

            var response = new RepositoryResponse();
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;

                        if (POmodel.SaveOrUpdate == "Update")
                        {
                            using (SqlCommand cmdOld = new SqlCommand("SELECT IMAGEPATH FROM GATE1 WHERE DOC_ID = @DOC_ID AND COMP_CODE = @COMP_CODE AND YEAR_CODE = @YEAR_CODE AND BRANCH_CODE = @BRANCH_CODE", con, transaction))
                            {
                                cmdOld.Parameters.AddWithValue("@DOC_ID", POmodel.DOC_ID);
                                cmdOld.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmdOld.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmdOld.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                oldFile = (await cmdOld.ExecuteScalarAsync())?.ToString();
                            }
                        }

                        if (POmodel.SaveOrUpdate == "Update")
                        {
                            using (SqlCommand cmdOld = new SqlCommand(@"
                            SELECT IMAGEPATH
                            FROM GATE1
                            WHERE DOC_ID = @DOC_ID
                            AND COMP_CODE = @COMP_CODE
                            AND YEAR_CODE = @YEAR_CODE
                            AND BRANCH_CODE = @BRANCH_CODE",
                                con, transaction))
                            {
                                cmdOld.Parameters.AddWithValue("@DOC_ID", POmodel.DOC_ID);
                                cmdOld.Parameters.AddWithValue("@YEAR_CODE",
                                    usersessionDt.PubFYearCode ?? (object)DBNull.Value);

                                cmdOld.Parameters.AddWithValue("@COMP_CODE",
                                    usersessionDt.PubCompCode ?? (object)DBNull.Value);

                                cmdOld.Parameters.AddWithValue("@BRANCH_CODE",
                                    usersessionDt.PubBranchCode);

                                oldFile = (await cmdOld.ExecuteScalarAsync())?.ToString();
                            }
                        }

                        if (POmodel.Attachment != null &&
                            POmodel.Attachment.Length > 0)
                        {
                            if (!POmodel.Attachment.ContentType.StartsWith("image/"))
                            {
                                response.status = false;
                                response.message = "Only image allowed!";
                                return response;
                            }

                            string folder = Path.Combine(_env.WebRootPath, path);

                            if (!Directory.Exists(folder))
                                Directory.CreateDirectory(folder);

                            fileName = POmodel.Attachment.FileName;

                            //fileName = $"{Guid.NewGuid()}{ext}";

                            attachmentFilePath = Path.Combine(folder, fileName);

                            await using (var stream = new FileStream(
                                attachmentFilePath,
                                FileMode.Create,
                                FileAccess.Write,
                                FileShare.None,
                                8192,
                                true))
                            {
                                await POmodel.Attachment.CopyToAsync(stream);
                                await stream.FlushAsync();
                            }

                            if (!System.IO.File.Exists(attachmentFilePath))
                            {
                                throw new Exception("File upload failed.");
                            }
                        }

                        string? imagePath = null;

                        // CASE 1 : REMOVE IMAGE
                        if (POmodel.RemoveAttachment)
                        {
                            imagePath = null;
                        }

                        // CASE 2 : NEW IMAGE UPLOADED
                        else if (!string.IsNullOrWhiteSpace(fileName))
                        {
                            imagePath = fileName;
                        }

                        // CASE 3 : KEEP OLD IMAGE
                        else
                        {
                            imagePath = oldFile;
                        }

                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_TransportInwardEntry]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Transaction = transaction;
                                cmd.CommandType = CommandType.StoredProcedure;

                                if (POmodel.SaveOrUpdate == "Save")
                                    cmd.Parameters.AddWithValue("@Action", "Add");
                                else
                                    cmd.Parameters.AddWithValue("@Action", "Edit");

                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                cmd.Parameters.AddWithValue("@V_NO", _dbHelper.Xnull(POmodel.V_NO));
                                cmd.Parameters.AddWithValue("@V_TYPE", _dbHelper.Xnull(POmodel.V_TYPE));
                                cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(POmodel.DOC_ID));
                                cmd.Parameters.AddWithValue("@TRF_TYPE", _dbHelper.Xnull(POmodel.TRF_TYPE));
                                cmd.Parameters.AddWithValue("@TRF_NO", _dbHelper.Xnull(POmodel.TRF_NO));
                                cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(POmodel.V_DATE));
                                cmd.Parameters.AddWithValue("@V_TIME", _dbHelper.Xnull(POmodel.V_TIME));
                                cmd.Parameters.AddWithValue("@ITEM_TYPE", _dbHelper.Xnull(POmodel.ITEM_TYPE));
                                cmd.Parameters.AddWithValue("@PARTY_CODE", _dbHelper.Xnull(POmodel.PARTY_CODE));
                                cmd.Parameters.AddWithValue("@ADD1", _dbHelper.Xnull(POmodel.ADD1));
                                cmd.Parameters.AddWithValue("@ADD2", _dbHelper.Xnull(POmodel.ADD2));
                                cmd.Parameters.AddWithValue("@ADD3", _dbHelper.Xnull(POmodel.ADD3));
                                cmd.Parameters.AddWithValue("@PARTY_CITY", _dbHelper.Xnull(POmodel.PARTY_CITY));
                                cmd.Parameters.AddWithValue("@PARTY_GST", _dbHelper.Xnull(POmodel.PARTY_GST));
                                cmd.Parameters.AddWithValue("@PARTY_PINCODE", _dbHelper.Xnull(POmodel.PARTY_PINCODE));
                                cmd.Parameters.AddWithValue("@PARTY_ADDRESSID", _dbHelper.Xnull(POmodel.PARTY_ADDRESSID));
                                cmd.Parameters.AddWithValue("@BILL_NO", _dbHelper.Xnull(POmodel.BILL_NO));
                                cmd.Parameters.AddWithValue("@BILL_DATE", _dbHelper.Xnull(POmodel.BILL_DATE));
                                cmd.Parameters.AddWithValue("@CHALL_NO", _dbHelper.Xnull(POmodel.CHALL_NO));
                                cmd.Parameters.AddWithValue("@CHALL_DATE", _dbHelper.Xnull(POmodel.CHALL_DATE));
                                cmd.Parameters.AddWithValue("@TRUCK_NO", _dbHelper.Xnull(POmodel.TRUCK_NO));
                                cmd.Parameters.AddWithValue("@TRANSPORT_CODE", _dbHelper.Xnull(POmodel.TRANSPORT_CODE));
                                cmd.Parameters.AddWithValue("@DRIVER_NAME", _dbHelper.Xnull(POmodel.DRIVER_NAME));
                                cmd.Parameters.AddWithValue("@DRIVER_NO", _dbHelper.Xnull(POmodel.DRIVER_NO));
                                cmd.Parameters.AddWithValue("@TRANSIT_NO", _dbHelper.Xnull(POmodel.TRANSIT_NO));
                                cmd.Parameters.AddWithValue("@WAYBILL_NO", _dbHelper.Xnull(POmodel.WAYBILL_NO));
                                cmd.Parameters.AddWithValue("@BILL_AMT", _dbHelper.Xnull(POmodel.BILL_AMT));
                                cmd.Parameters.AddWithValue("@REMARKS", _dbHelper.Xnull(POmodel.REMARKS));
                                cmd.Parameters.AddWithValue("@DISP_PLAN_NO", _dbHelper.Xnull(POmodel.DISP_PLAN_NO));
                                cmd.Parameters.AddWithValue("@DISP_PLAN_TYPE", _dbHelper.Xnull(POmodel.DISP_PLAN_TYPE));
                                cmd.Parameters.AddWithValue("@WB_TYPE", _dbHelper.Xnull(POmodel.WB_TYPE));
                                cmd.Parameters.AddWithValue("@WB_NO", _dbHelper.Xnull(POmodel.WB_NO));
                                cmd.Parameters.AddWithValue("@MRN_TYPE", _dbHelper.Xnull(POmodel.MRN_TYPE));
                                cmd.Parameters.AddWithValue("@MRN_NO", _dbHelper.Xnull(POmodel.MRN_NO));
                                cmd.Parameters.AddWithValue("@REF_TYPE", _dbHelper.Xnull(POmodel.REF_TYPE));
                                cmd.Parameters.AddWithValue("@REF_NO", _dbHelper.Xnull(POmodel.REF_NO));
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", _dbHelper.Xnull(POmodel.FAPROV_STATUS));
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", _dbHelper.Xnull(POmodel.FAPROV_REMARKS));
                                cmd.Parameters.AddWithValue("@STATUS", _dbHelper.Xnull(POmodel.STATUS));
                                cmd.Parameters.AddWithValue("@ACTIVE", _dbHelper.Xnull(POmodel.ACTIVE));
                                cmd.Parameters.AddWithValue("@Remarks2", _dbHelper.Xnull(POmodel.Remarks2));
                                cmd.Parameters.AddWithValue("@PARTY_NAME", _dbHelper.Xnull(POmodel.PARTY_NAME));
                                cmd.Parameters.AddWithValue("@RC_NO", _dbHelper.Xnull(POmodel.RC_NO));
                                cmd.Parameters.AddWithValue("@DL_NO", _dbHelper.Xnull(POmodel.DL_NO));
                                cmd.Parameters.AddWithValue("@INSU_NO", _dbHelper.Xnull(POmodel.INSU_NO));
                                cmd.Parameters.AddWithValue("@PAN_NO", _dbHelper.Xnull(POmodel.PAN_NO));
                                cmd.Parameters.AddWithValue("@PURPOSE", _dbHelper.Xnull(POmodel.PURPOSE));
                                //cmd.Parameters.AddWithValue("@IMAGEPATH", _dbHelper.Xnull(imagePath));
                                cmd.Parameters.Add("@IMAGEPATH",
                                    SqlDbType.NVarChar,
                                    255).Value =
                                    string.IsNullOrWhiteSpace(imagePath)
                                        ? DBNull.Value
                                        : imagePath;
                                cmd.Parameters.AddWithValue("@R_TIME", _dbHelper.Xnull(POmodel.R_TIME));
                                cmd.Parameters.AddWithValue("@OUT_TIME", _dbHelper.Xnull(POmodel.OUT_TIME));
                                cmd.Parameters.AddWithValue("@R_DATE", _dbHelper.Xnull(POmodel.R_DATE));
                                cmd.Parameters.AddWithValue("@OUT_DATE", _dbHelper.Xnull(POmodel.OUT_DATE));
                                cmd.Parameters.AddWithValue("@RETURN_TYPE", _dbHelper.Xnull(POmodel.RETURN_TYPE));
                                cmd.Parameters.AddWithValue("@QRCODE_NO", _dbHelper.Xnull(POmodel.QRCODE_NO));
                                cmd.Parameters.AddWithValue("@INOUT_ACTIVE", _dbHelper.Xnull(POmodel.INOUT_ACTIVE));
                                cmd.Parameters.AddWithValue("@OUT_ALLOWED", _dbHelper.Xnull(POmodel.OUT_ALLOWED));
                                cmd.Parameters.AddWithValue("@OUT_ALLOWEDBY", _dbHelper.Xnull(POmodel.OUT_ALLOWEDBY));
                                cmd.Parameters.AddWithValue("@RETURN_DATE", _dbHelper.Xnull(POmodel.RETURN_DATE));
                                cmd.Parameters.AddWithValue("@RESPONSIBLE_PERSON", _dbHelper.Xnull(POmodel.RESPONSIBLE_PERSON));
                                cmd.Parameters.AddWithValue("@INSU_EXPDT", _dbHelper.Xnull(POmodel.INSU_EXPDT));
                                cmd.Parameters.AddWithValue("@DL_EXPDT", _dbHelper.Xnull(POmodel.DL_EXPDT));
                                cmd.Parameters.AddWithValue("@CONTAINER_NO", _dbHelper.Xnull(POmodel.CONTAINER_NO));
                                cmd.Parameters.AddWithValue("@CONTAINER_SIZE", _dbHelper.Xnull(POmodel.CONTAINER_SIZE));
                                cmd.Parameters.AddWithValue("@SHIP_PARTY", _dbHelper.Xnull(POmodel.SHIP_PARTY));
                                cmd.Parameters.AddWithValue("@SHIP_BILLNO", _dbHelper.Xnull(POmodel.SHIP_BILLNO));
                                cmd.Parameters.AddWithValue("@SHIP_BILLDATE", _dbHelper.Xnull(POmodel.SHIP_BILLDATE));
                                cmd.Parameters.AddWithValue("@EWB_DATE", _dbHelper.Xnull(POmodel.EWB_DATE));
                                cmd.Parameters.AddWithValue("@EWB_EXPDATE", _dbHelper.Xnull(POmodel.EWB_EXPDATE));
                                cmd.Parameters.AddWithValue("@PARTY_WBTIME", _dbHelper.Xnull(POmodel.PARTY_WBTIME));
                                cmd.Parameters.AddWithValue("@EWB_INVNO", _dbHelper.Xnull(POmodel.EWB_INVNO));
                                cmd.Parameters.AddWithValue("@EWB_INVAMT", _dbHelper.Xnull(POmodel.EWB_INVAMT));
                                cmd.Parameters.AddWithValue("@PARTY_WBSLIPNO", _dbHelper.Xnull(POmodel.PARTY_WBSLIPNO));
                                cmd.Parameters.AddWithValue("@PARTY_WBGRWT", _dbHelper.Xnull(POmodel.PARTY_WBGRWT));
                                cmd.Parameters.AddWithValue("@PARTY_WBTRWT", _dbHelper.Xnull(POmodel.PARTY_WBTRWT));
                                cmd.Parameters.AddWithValue("@PARTY_EWBCITY", _dbHelper.Xnull(POmodel.PARTY_EWBCITY));
                                cmd.Parameters.AddWithValue("@GR_NO", _dbHelper.Xnull(POmodel.GR_NO));
                                cmd.Parameters.AddWithValue("@GR_DATE", _dbHelper.Xnull(POmodel.GR_DATE));
                                //cmd.Parameters.AddWithValue("@status", 1);
                                cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID ?? (object)DBNull.Value);

                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                cmd.Parameters.Add(returnParam);
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);
                                await cmd.ExecuteNonQueryAsync();
                                string errorMessage = errorParam.Value?.ToString();
                                if ((int)returnParam.Value <= 0)
                                    success = false;
                            }

                            if (success)
                            {
                                //if (POmodel.Attachment != null && POmodel.Attachment.Length > 0)
                                //{
                                //    bool isExist = false;
                                //    using (SqlCommand cmd = new SqlCommand("sp_TransportInwardEntry_Img", con, transaction))
                                //    {
                                //        cmd.CommandType = CommandType.StoredProcedure;
                                //        cmd.Parameters.AddWithValue("@Action", "IsExist");
                                //        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                //        cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                //        cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                //        cmd.Parameters.AddWithValue("@V_TYPE", POmodel.V_TYPE);
                                //        cmd.Parameters.AddWithValue("@V_NO", POmodel.V_NO);
                                //        var result = await cmd.ExecuteScalarAsync();
                                //        isExist = Convert.ToInt32(result) == 1;
                                //    }
                                //    using (SqlCommand cmd = new SqlCommand("sp_TransportInwardEntry_Img", con, transaction))
                                //    {
                                //        cmd.CommandType = CommandType.StoredProcedure;

                                //        if (POmodel.SaveOrUpdate == "Save")
                                //        {
                                //            cmd.Parameters.AddWithValue("@Action", "Save");
                                //            cmd.Parameters.AddWithValue("@AED", "A");
                                //        }
                                //        else
                                //        {
                                //            if (isExist)
                                //            {
                                //                cmd.Parameters.AddWithValue("@Action", "Update");
                                //                cmd.Parameters.AddWithValue("@AED", "E");
                                //            }
                                //            else
                                //            {
                                //                cmd.Parameters.AddWithValue("@Action", "Save");
                                //                cmd.Parameters.AddWithValue("@AED", "A");
                                //            }
                                //        }
                                //        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                //        cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                //        cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                //        cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(POmodel.DOC_ID));
                                //        cmd.Parameters.AddWithValue("@V_NO", _dbHelper.Xnull(POmodel.V_NO));
                                //        cmd.Parameters.AddWithValue("@V_TYPE", _dbHelper.Xnull(POmodel.V_TYPE));
                                //        cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(POmodel.V_DATE));
                                //        cmd.Parameters.AddWithValue("@ROWID", 1);

                                //        cmd.Parameters.Add("@IMG_FILE", SqlDbType.VarBinary).Value = DBNull.Value;

                                //        cmd.Parameters.AddWithValue("@FILE_NAME", fileName);
                                //        cmd.Parameters.AddWithValue("@SRNO", 1);
                                //        cmd.Parameters.AddWithValue("@UUSER", usersessionDt.PubUserId);
                                //        cmd.Parameters.AddWithValue("@EUSER", usersessionDt.PubUserId);

                                //        cmd.Parameters.AddWithValue("@WSID", usersessionDt.PubWorkStationID);
                                //        cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);
                                //        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                                //        cmd.Parameters.AddWithValue("@FILE_TYPE", "Vehicle Inward");
                                //        cmd.Parameters.AddWithValue("@FILE_DESC", DBNull.Value);
                                //        cmd.Parameters.AddWithValue("@FILE_PATH", Path.Combine(path, fileName));

                                //        await cmd.ExecuteNonQueryAsync();
                                //    }
                                //}
                                //if ((!string.IsNullOrEmpty(oldFile) && POmodel.Attachment == null))
                                //{
                                //    using (SqlCommand cmd = new SqlCommand("sp_TransportInwardEntry_Img", con, transaction))
                                //    {
                                //        cmd.CommandType = CommandType.StoredProcedure;
                                //        cmd.Parameters.AddWithValue("@Action", "Delete");
                                //        cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                //        cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                //        cmd.Parameters.AddWithValue("@BRANCH_CODE", usersessionDt.PubBranchCode);
                                //        cmd.Parameters.AddWithValue("@V_TYPE", POmodel.V_TYPE);
                                //        cmd.Parameters.AddWithValue("@V_NO", POmodel.V_NO);
                                //        await cmd.ExecuteNonQueryAsync();
                                //    }
                                //}
                                // REMOVE IMAGE
                                if (POmodel.RemoveAttachment)
                                    {
                                        using (SqlCommand cmd =
                                            new SqlCommand("sp_TransportInwardEntry_Img",
                                            con,
                                            transaction))
                                        {
                                            cmd.CommandType = CommandType.StoredProcedure;

                                            cmd.Parameters.AddWithValue("@Action", "Delete");

                                            cmd.Parameters.AddWithValue("@COMP_CODE",
                                                usersessionDt.PubCompCode);

                                            cmd.Parameters.AddWithValue("@YEAR_CODE",
                                                usersessionDt.PubFYearCode);

                                            cmd.Parameters.AddWithValue("@BRANCH_CODE",
                                                usersessionDt.PubBranchCode);

                                            cmd.Parameters.AddWithValue("@V_TYPE",
                                                POmodel.V_TYPE);

                                            cmd.Parameters.AddWithValue("@V_NO",
                                                POmodel.V_NO);

                                            await cmd.ExecuteNonQueryAsync();
                                        }
                                    }

                                // SAVE / UPDATE NEW IMAGE
                                else if (!string.IsNullOrWhiteSpace(fileName))
                                    {
                                        bool isExist = false;

                                        using (SqlCommand cmd =
                                            new SqlCommand("sp_TransportInwardEntry_Img",
                                            con,
                                            transaction))
                                        {
                                            cmd.CommandType = CommandType.StoredProcedure;

                                            cmd.Parameters.AddWithValue("@Action", "IsExist");

                                            cmd.Parameters.AddWithValue("@COMP_CODE",
                                                usersessionDt.PubCompCode);

                                            cmd.Parameters.AddWithValue("@YEAR_CODE",
                                                usersessionDt.PubFYearCode);

                                            cmd.Parameters.AddWithValue("@BRANCH_CODE",
                                                usersessionDt.PubBranchCode);

                                            cmd.Parameters.AddWithValue("@V_TYPE",
                                                POmodel.V_TYPE);

                                            cmd.Parameters.AddWithValue("@V_NO",
                                                POmodel.V_NO);

                                            var result = await cmd.ExecuteScalarAsync();

                                            isExist = Convert.ToInt32(result) == 1;
                                        }

                                        using (SqlCommand cmd =
                                            new SqlCommand("sp_TransportInwardEntry_Img",
                                            con,
                                            transaction))
                                        {
                                            cmd.CommandType = CommandType.StoredProcedure;

                                            if (POmodel.SaveOrUpdate == "Save")
                                            {
                                                cmd.Parameters.AddWithValue("@Action", "Save");
                                                cmd.Parameters.AddWithValue("@AED", "A");
                                            }
                                            else
                                            {
                                                cmd.Parameters.AddWithValue("@Action",
                                                    isExist ? "Update" : "Save");

                                                cmd.Parameters.AddWithValue("@AED",
                                                    isExist ? "E" : "A");
                                            }

                                            cmd.Parameters.AddWithValue("@COMP_CODE",
                                                usersessionDt.PubCompCode);

                                            cmd.Parameters.AddWithValue("@BRANCH_CODE",
                                                usersessionDt.PubBranchCode);

                                            cmd.Parameters.AddWithValue("@YEAR_CODE",
                                                usersessionDt.PubFYearCode);

                                            cmd.Parameters.AddWithValue("@DOC_ID",
                                                _dbHelper.Xnull(POmodel.DOC_ID));

                                            cmd.Parameters.AddWithValue("@V_NO",
                                                _dbHelper.Xnull(POmodel.V_NO));

                                            cmd.Parameters.AddWithValue("@V_TYPE",
                                                _dbHelper.Xnull(POmodel.V_TYPE));

                                            cmd.Parameters.AddWithValue("@FILE_NAME",
                                                fileName);

                                            cmd.Parameters.AddWithValue("@FILE_PATH",
                                                Path.Combine(path, fileName));

                                            await cmd.ExecuteNonQueryAsync();
                                        }
                                    }
                                transaction.Commit();
                                //if ((POmodel.Attachment != null && !string.IsNullOrEmpty(oldFile)) || (!string.IsNullOrEmpty(oldFile) && POmodel.Attachment == null))
                                //{
                                //    string oldPath = Path.Combine(_env.WebRootPath, @"Uploads\VehicleInward", oldFile);

                                //    if (System.IO.File.Exists(oldPath))
                                //        System.IO.File.Delete(oldPath);
                                //}
                                if (!string.IsNullOrWhiteSpace(oldFile) &&
                                    (POmodel.RemoveAttachment ||
                                     !string.IsNullOrWhiteSpace(fileName)))
                                {
                                    string oldPath = Path.Combine(
                                        _env.WebRootPath,
                                        @"Uploads\VehicleInward",
                                        oldFile);

                                    if (System.IO.File.Exists(oldPath))
                                    {
                                        System.IO.File.Delete(oldPath);
                                    }
                                }
                                //=================Log Insert
                                string mode = "";
                                if (POmodel.SaveOrUpdate == "Save")
                                {
                                    mode = "Insert";
                                }
                                else
                                {
                                    mode = "Update";
                                    //_globalValidationdate.LogInsertUpdateDelete(destinationTable: "gate1", sourceTable: "gate1", transactionType: "Transaction",
                                    //        codeVNo: POmodel.V_NO.ToString(), vtype: POmodel.V_TYPE);
                                }
                                //_logService.InsertLog("GATE1", "Vehicle Inward", "Transaction", mode, POmodel.V_TYPE, POmodel.V_NO.ToString(), POmodel.V_DATE.Value);
                            }
                            else
                            {
                                transaction.Rollback();
                                if (!string.IsNullOrWhiteSpace(attachmentFilePath) &&
                                    System.IO.File.Exists(attachmentFilePath))
                                {
                                    System.IO.File.Delete(attachmentFilePath);
                                }
                            }

                            response.status = success;
                            response.message = success ? "Data save/update successfully." : "Failed to save or update details.";
                            return response;
                        }
                        catch (Exception ex)
                        {
                            transaction?.Rollback();
                            if (!string.IsNullOrWhiteSpace(attachmentFilePath) &&
                                    System.IO.File.Exists(attachmentFilePath))
                            {
                                System.IO.File.Delete(attachmentFilePath);
                            }
                            response.status = false;
                            response.message = "Transaction failed: " + ex.Message;
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "Error: " + ex.Message;
                return response;
            }
        }
        public async Task<RepositoryResponseData<RcRequest>> VehicleInfoApi(string rc_number)
        {
            var res = new RepositoryResponseData<RcRequest>();
            try
            {
                using var client = new HttpClient();

                string url = "https://kyc-api.surepass.io/api/v1/rc/rc-full";
                string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJmcmVzaCI6ZmFsc2UsImlhdCI6MTc1MTg3ODU4MiwianRpIjoiYzczZmFkMTAtZjk0MC00NzdkLThlNDgtMjU3ZTViMzVkYjY4IiwidHlwZSI6ImFjY2VzcyIsImlkZW50aXR5IjoiZGV2LnBhc2h1cGF0aWdycF9jb25zb2xlQHN1cmVwYXNzLmlvIiwibmJmIjoxNzUxODc4NTgyLCJleHAiOjIzODI1OTg1ODIsImVtYWlsIjoicGFzaHVwYXRpZ3JwX2NvbnNvbGVAc3VyZXBhc3MuaW8iLCJ0ZW5hbnRfaWQiOiJtYWluIiwidXNlcl9jbGFpbXMiOnsic2NvcGVzIjpbInVzZXIiXX19.vVom9nrkmom4XGJUEXAkntNzof1lHNwlHsRBdErWXQQ"; // Replace with your actual token

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var payload = new JObject
                {
                    ["id_number"] = rc_number
                };

                var content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(url, content);
                string responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    res.status = false;
                    res.message = (int)response.StatusCode + "\nAPI request failed\nDetails:" + responseData;
                    //res.data = responseData;
                    return res;
                }

                var jsonResponse = JObject.Parse(responseData);

                var vehicleData = jsonResponse["data"];
                if (vehicleData == null)
                {
                    res.status = false;
                    res.message = "No vehicle data found";
                    return res;
                }
                var vehicleInfo = new RcRequest
                {
                    RcNumber = vehicleData["rc_number"]?.ToString(),
                    ClientId = vehicleData["client_id"]?.ToString(),
                    RegistrationDate = vehicleData["registration_date"]?.ToObject<DateTime?>(),
                    OwnerName = vehicleData["owner_name"]?.ToString(),
                    FatherName = vehicleData["father_name"]?.ToString(),
                    PresentAddress = vehicleData["present_address"]?.ToString(),
                    PermanentAddress = vehicleData["permanent_address"]?.ToString(),
                    MobileNumber = vehicleData["mobile_number"]?.ToString(),
                    vehicleCategory = vehicleData["vehicle_category"]?.ToString(),
                    vehicleChasiNumber = vehicleData["vehicle_chasi_number"]?.ToString(),
                    VehicleEngineNumber = vehicleData["vehicle_engine_number"]?.ToString(),
                    MakerDescription = vehicleData["maker_description"]?.ToString(),
                    MakerModel = vehicleData["maker_model"]?.ToString(),
                    bodyType = vehicleData["body_type"]?.ToString(),
                    FuelType = vehicleData["fuel_type"]?.ToString(),
                    Color = vehicleData["color"]?.ToString(),
                    NormsType = vehicleData["norms_type"]?.ToString(),
                    fitUpTo = vehicleData["fit_up_to"]?.ToObject<DateTime?>(),
                    Financer = vehicleData["financer"]?.ToString(),
                    Financed = vehicleData["financed"]?.ToObject<bool?>(),
                    InsuranceCompany = vehicleData["insurance_company"]?.ToString(),
                    insurancePolicyNumber = vehicleData["insurance_policy_number"]?.ToString(),
                    insuranceUpto = vehicleData["insurance_upto"]?.ToObject<DateTime?>(),
                    ManufacturingDate = vehicleData["manufacturing_date"]?.ToObject<DateTime?>(),
                    ManufacturingDateFormatted = vehicleData["manufacturing_date_formatted"]?.ToString(),
                    RegisteredAt = vehicleData["registered_at"]?.ToString(),
                    LatestBy = vehicleData["latest_by"]?.ToString(),
                    LessInfo = vehicleData["less_info"]?.ToObject<bool?>(),
                    taxUpto = vehicleData["tax_upto"]?.ToObject<DateTime?>(),
                    //TaxPaidUpto = vehicleData["tax_paid_upto"]?.ToObject<DateTime?>(),
                    TaxPaidUpto = vehicleData["tax_paid_upto"]?.ToString(),
                    CubicCapacity = vehicleData["cubic_capacity"]?.ToString(),
                    //VehicleGrossWeight = vehicleData["vehicle_gross_weight"]?.ToString(),
                    vehicleGrossWeight = vehicleData["vehicle_gross_weight"]?.ToObject<decimal>(),
                    NoCylinders = vehicleData["no_cylinders"]?.ToString(),
                    SeatCapacity = vehicleData["seat_capacity"]?.ToString(),
                    SleeperCapacity = vehicleData["sleeper_capacity"]?.ToString(),
                    StandingCapacity = vehicleData["standing_capacity"]?.ToString(),
                    Wheelbase = vehicleData["wheelbase"]?.ToString(),
                    unladenWeight = vehicleData["unladen_weight"]?.ToObject<decimal>(),
                    VehicleCategoryDescription = vehicleData["vehicle_category_description"]?.ToString(),
                    PuccNumber = vehicleData["pucc_number"]?.ToString(),
                    puccUpto = vehicleData["pucc_upto"]?.ToObject<DateTime?>(),
                    PermitNumber = vehicleData["permit_number"]?.ToString(),
                    PermitIssueDate = vehicleData["permit_issue_date"]?.ToObject<DateTime?>(),
                    PermitValidFrom = vehicleData["permit_valid_from"]?.ToObject<DateTime?>(),
                    permitValidUpto = vehicleData["permit_valid_upto"]?.ToObject<DateTime?>(),
                    PermitType = vehicleData["permit_type"]?.ToString(),
                    NationalPermitNumber = vehicleData["national_permit_number"]?.ToString(),
                    NationalPermitUpto = vehicleData["national_permit_upto"]?.ToObject<DateTime?>(),
                    NationalPermitIssuedBy = vehicleData["national_permit_issued_by"]?.ToString(),
                    NonUseStatus = vehicleData["non_use_status"]?.ToString(),
                    NonUseFrom = vehicleData["non_use_from"]?.ToObject<DateTime?>(),
                    NonUseTo = vehicleData["non_use_to"]?.ToObject<DateTime?>(),
                    blacklistStatus = vehicleData["blacklist_status"]?.ToString(),
                    NocDetails = vehicleData["noc_details"]?.ToString(),
                    OwnerNumber = vehicleData["owner_number"]?.ToString(),
                    rcStatus = vehicleData["rc_status"]?.ToString().ToUpper(),
                    MaskedName = vehicleData["masked_name"]?.ToObject<bool?>(),
                    ChallanDetails = vehicleData["challan_details"]?.ToString()
                };
                res.status = true;
                res.data = vehicleInfo;
                return res;
            }
            catch (Exception ex)
            {
                res.status = false;
                res.message = ex.Message;
                return res;
            }
        }
        public async Task<RepositoryResponseData<vehicleInfoDb>> VehicleInfoFromDB(string vehicleNo)
        {
            var res = new RepositoryResponseData<vehicleInfoDb>();
            var compCode = _globalValue.GetGlobalVariables().PubCompCode;
            var vehicleInfo = new vehicleInfoDb();
            try
            {
                using (SqlConnection con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_GetVehicleInward_DriverAndVehicle_Details", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "Vehicle");
                        cmd.Parameters.AddWithValue("@Comp_code", compCode);
                        cmd.Parameters.AddWithValue("@TRUCK_NO", vehicleNo);
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                vehicleInfo.rcNumber = reader["RC_NO"]?.ToString();
                                vehicleInfo.insuranceNumber = reader["INSU_NO"]?.ToString();
                                vehicleInfo.purpose = reader["PURPOSE"]?.ToString();
                                vehicleInfo.grossWt = reader["PARTY_WBGRWT"]?.ToString();
                                vehicleInfo.bodyType = reader["PARTY_WBSLIPNO"]?.ToString();
                                vehicleInfo.vehicleRemarks = reader["Remarks2"]?.ToString();
                                vehicleInfo.fitmentupto = reader["EWB_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["EWB_DATE"]) : null;
                                vehicleInfo.taxupto = reader["CHALL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["CHALL_DATE"]) : null;
                                vehicleInfo.insuExp = reader["INSU_EXPDT"] != DBNull.Value ? Convert.ToDateTime(reader["INSU_EXPDT"]) : null;
                                vehicleInfo.transportCode = reader["CODE"] != DBNull.Value ? Convert.ToInt32(reader["CODE"]) : null;
                                vehicleInfo.transportName = reader["TRANSPORT_NAME"]?.ToString();
                            }
                        }
                    }
                }
                res.status = true;
                res.data = vehicleInfo;
                return res;
            }
            catch (Exception ex)
            {
                res.status = true;
                res.message = ex.Message;
                return res;
            }
        }
        public async Task<RepositoryResponseData<List<TransportInwardModel>>> TransportInwardRecordsById(string id)
        {
            var res = new RepositoryResponseData<List<TransportInwardModel>>();
            var TransportInward = new List<TransportInwardModel>();
            try
            {
                var usersession = _globalValue.GetGlobalVariables();
                var parameter = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", usersession.PubBranchCode},
                    {"@DOC_ID", id},
                    {"@Action", "TransportInwardDataByID"}
                };
                var transportlist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetTransportInwardEntry]", parameter);
                var json = JsonConvert.SerializeObject(transportlist);
                var transportInwardList = JsonConvert.DeserializeObject<List<TransportInwardModel>>(json);
                res.status = true;
                res.data = transportInwardList;
                return res;
            }
            catch (Exception ex)
            {
                res.status = false;
                res.message = "Data load failed" + ex.Message;
                return res;
            }
        }
        public async Task<RepositoryResponseData<DocInfo>> MaxVNo(string V_type)
        {
            var response = new RepositoryResponseData<DocInfo>();
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = userSession.PubBranchCode;
                var vType = V_type;
                var tableName = "GATE1";

                var yearParams = new Dictionary<string, object> { { "@YearCd", yearCode } };
                var vnoParams = new Dictionary<string, object>
            {
            { "@COMP_CODE", companyCode },
            { "@BRANCH_CODE", branchCode },
            { "@YEAR_CODE", yearCode },
            { "@V_TYPE", vType },
            { "@TableName", tableName }
            };

                string nextVNo = await _dbHelper.GetExecuteScalarAsync<string>("sp_GetMaxVNo", vnoParams, isStoredProc: true);
                string year = await _dbHelper.GetExecuteScalarAsync<string>("SELECT dbo.fn_GetCurrentYear(@YearCd)", yearParams);
                var docId = (vType) + (year) + (nextVNo);
                var newVno = year + nextVNo;
                var docIdNoList = new DocInfo
                {
                    DocId = docId,
                    VNo = newVno
                };
                response.status = true;
                response.data = docIdNoList;
                return response;
            }
            catch (Exception ex)
            {
                response.status = false;
                response.message = "Data load failed" + ex.Message;
                return response;
            }
        }
        public async Task<RepositoryResponseList<ExpandoObject>> DocType()
        {
            var response = new RepositoryResponseList<ExpandoObject>();
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='TruckInward' ");
                response.status = true;
                response.data = Doctype.ToList();
                return response;
            }
            catch (Exception ex)
            {
                response.status = true;
                response.message = "Data load failed: " + ex.Message;
                return response;
            }
        }
        public async Task<RepositoryResponseList<ExpandoObject>> PartyList()
        {
            var response = new RepositoryResponseList<ExpandoObject>();
            try
            {
                var UserLoginData = _globalValue.GetGlobalVariables();
                var PartyList = await _dbHelper.GetJsonDataAsync($@"select distinct sg.CODE, sg.NAME, sg.ADD1,sg.ADD2,sg.ADD3,sg.PINCODE, isnull(cm.NAME, '') as CityName, isnull(s.name, '') state, sg.STATE_CODE,sg.CITY_CODE,sg.GSTIN from SUBGROUP_MAST sg left join CITY_MAST cm on sg.CITY_CODE=cm.CODE left join STATE_MAST s on s.code=sg.STATE_CODE  where sg.COMP_CODE={UserLoginData.PubCompCode} order by NAME ");
                response.status = true;
                response.data = PartyList.ToList();
                return response;
            }
            catch (Exception ex)
            {
                response.status = true;
                response.message = "Data load failed: " + ex.Message;
                return response;
            }
        }
        public async Task<RepositoryResponseList<ExpandoObject>> TransportationList()
        {
            var response = new RepositoryResponseList<ExpandoObject>();
            try
            {
                var transactionList = await _dbHelper.GetJsonDataAsync($@"select CODE,NAME,PARTY_CODE from TRANSPORT_MAST where  COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by LTRIM(RTRIM(NAME)) ");
                response.status = true;
                response.data = transactionList.ToList();
                return response;
            }
            catch (Exception ex)
            {
                response.status = true;
                response.message = "Data load failed: " + ex.Message;
                return response;
            }
        }
        public async Task<RepositoryResponseList<ExpandoObject>> DONo()
        {
            var response = new RepositoryResponseList<ExpandoObject>();
            try
            {
                var gv = _globalValue.GetGlobalVariables();
                var transactionList = await _dbHelper.GetJsonDataAsync($@"Select a.V_NO as Code, a.V_TYPE as Name, a.VEHICLE_NO as TruckNo, a.SHIP_NAME as PartyName,
                                        a.SHIP_ADD1 as Add1, a.SHIP_ADD2 as Add2, a.SHIP_ADD3 as Add3, c.Code  as CityCode, c.NAME  as CityName, a.bill_code as BillCode,
                                        a.TRANSPORT_CODE as TransportCode, a.TRANSPORT_NAME as TransportName from DO1 a
                                        Left join CITY_MAST c on a.SHIP_CITY=c.CODE 
                                        where a.REF_NO is null and a.Status=1 and a.COMP_CODE={gv.PubCompCode} and a.YEAR_CODE = {gv.PubFYearCode} and a.BRANCH_CODE={gv.PubBranchCode}
                                        order by a.V_NO desc");
                response.status = true;
                response.data = transactionList.ToList();
                return response;
            }
            catch (Exception ex)
            {
                response.status = true;
                response.message = "Data load failed: " + ex.Message;
                return response;
            }
        }
    }
}
