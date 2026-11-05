using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;
using System.Text;
using System.Diagnostics;
namespace travelexpensemanagement.Repositories.Implementations.Sales.Transaction
{
    public class TransportRateRequestForQuotationRepository : ITransportRateRequestForQuotationRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public TransportRateRequestForQuotationRepository(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
        }

        public async Task<RepositoryResponseData<List<TransportRateRequestImportModel>>> ImportDispatch(int refNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var result = new List<TransportRateRequestImportModel>();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand("sp_TransportRateRequestForQuotation", con);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "ImportDispatch");
                cmd.Parameters.AddWithValue("@V_NO", refNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                await con.OpenAsync();

                using var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                {
                    result.Add(new TransportRateRequestImportModel
                    {
                        PartyCode = rdr["PartyCode"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["PartyCode"]),
                        CityCode = rdr["CityCode"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["CityCode"]),
                        DeliveryLocation = rdr["DeliveryLocation"]?.ToString(),
                        SaleGroup = rdr["SaleGroup"]?.ToString(),
                        PinCode = rdr["PinCode"]?.ToString(),
                        PartyName = rdr["PartyName"]?.ToString(),
                        TotalQty = rdr["TotalQty"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["TotalQty"]),
                        RefNo = refNo
                    });
                }

                return new RepositoryResponseData<List<TransportRateRequestImportModel>>
                {
                    status = true,
                    data = result,
                    message = result.Count == 0 ? "No dispatch planning records found." : "Data imported successfully."
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<List<TransportRateRequestImportModel>>
                {
                    status = false,
                    message = "Unable to import dispatch planning data. " + ex.Message
                };
            }
        }

        public async Task<RepositoryResponse> Save(TransportRateRequestForQuotationModel model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                await con.OpenAsync();
                using var tran = con.BeginTransaction();

                try
                {
                    // Document exist check
                    bool exists;
                    using (var chk = new SqlCommand(@"SELECT 1 FROM RFQ1 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE AND BRANCH_CODE=@BRANCH_CODE AND 
                                                        YEAR_CODE=@YEAR_CODE", con, tran))
                    {
                        chk.Parameters.AddWithValue("@V_TYPE", model.VType);
                        chk.Parameters.AddWithValue("@V_NO", model.VNo);
                        chk.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        chk.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        chk.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        exists = await chk.ExecuteScalarAsync() != null;
                    }

                    if (!model.IsEdit && exists)
                        throw new Exception("This document number already exists. Please reselect Doc Type to get a new number.");
                    if (model.IsEdit && !exists)
                        throw new Exception("Document not found for update.");

                    // 1) Remove old footer rows
                    using (var cmd = new SqlCommand(@"DELETE FROM RFQ2 WHERE V_TYPE=@V_TYPE AND V_NO=@V_NO AND COMP_CODE=@COMP_CODE
                                              AND BRANCH_CODE=@BRANCH_CODE AND YEAR_CODE=@YEAR_CODE", con, tran))
                    {
                        cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                        cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 2) Header (RFQ1): Insert or Update
                    using (var cmd = new SqlCommand("sp_TransportRateRequestForQuotation", con, tran))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", model.IsEdit ? "Update" : "HeaderInsert");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                        cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                        cmd.Parameters.AddWithValue("@V_DATE", model.VDate);
                        cmd.Parameters.AddWithValue("@TRANSPORT_NAME", model.TransportName);
                        cmd.Parameters.AddWithValue("@REF_TYPE", (object?)model.RefType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REF_NO", (object?)model.RefNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                        if (model.IsEdit)
                            cmd.Parameters.AddWithValue("@EUSER", gv.PubUserId);
                        else
                            cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 3) Footer rows (RFQ2)
                    int sno = 1;
                    foreach (var r in model.Rows)
                    {
                        using var cmd = new SqlCommand("sp_TransportRateRequestForQuotation", con, tran);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "FooterInsert");
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                        cmd.Parameters.AddWithValue("@V_NO", model.VNo);
                        cmd.Parameters.AddWithValue("@V_DATE", model.VDate);
                        cmd.Parameters.AddWithValue("@WEIGHT", r.Weight);
                        cmd.Parameters.AddWithValue("@ITEM_TYPE", r.ItemType ?? "");
                        cmd.Parameters.AddWithValue("@DEL_CITY_CODE", r.DelCityCode);
                        cmd.Parameters.AddWithValue("@VEHICLE_TYPE", r.VehicleType ?? "");
                        cmd.Parameters.AddWithValue("@DELIVERY_PINCODE", r.DeliveryPincode ?? "");
                        cmd.Parameters.AddWithValue("@REMARKS", r.Remarks ?? "");
                        cmd.Parameters.AddWithValue("@PARTY_CODE", (object?)r.PartyCode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REF_TYPE", (object?)r.RefType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@REF_NO", (object?)r.RefNo ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                        cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                        cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                        cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@SNO", sno++);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    tran.Commit();
                    return new RepositoryResponse
                    {
                        status = true,
                        message = model.IsEdit ? "Updated successfully." : "Saved successfully."
                    };
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = ex.Message };
            }
        }

        public async Task<RepositoryResponseData<TransportRateRequestForQuotationModel>> GetById(string vType, int vNo)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using var con = _dbConnection.GetErpConnection();
                using var cmd = new SqlCommand("sp_TransportRateRequestForQuotation", con);

                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Action", "GetById");
                cmd.Parameters.AddWithValue("@V_TYPE", vType);
                cmd.Parameters.AddWithValue("@V_NO", vNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                await con.OpenAsync();

                using var rdr = await cmd.ExecuteReaderAsync();

                // HEADER - RFQ1
                if (!await rdr.ReadAsync())
                {
                    return new RepositoryResponseData<TransportRateRequestForQuotationModel>
                    {
                        status = false,
                        message = "RFQ document not found."
                    };
                }

                var model = new TransportRateRequestForQuotationModel
                {
                    VType = rdr["V_TYPE"]?.ToString() ?? "",
                    VNo = Convert.ToInt32(rdr["V_NO"]),
                    VDate = Convert.ToDateTime(rdr["V_DATE"]),
                    TransportName = rdr["TRANSPORT_NAME"] == DBNull.Value ? "[]" : rdr["TRANSPORT_NAME"].ToString() ?? "[]",
                    RefType = rdr["REF_TYPE"] == DBNull.Value ? null : rdr["REF_TYPE"].ToString(),
                    RefNo = rdr["REF_NO"] == DBNull.Value ? null : Convert.ToInt32(rdr["REF_NO"]),
                };

                // FOOTER - RFQ2
                if (await rdr.NextResultAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        model.Rows.Add(new TransportRateRequestRowModel
                        {
                            Weight = rdr["WEIGHT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["WEIGHT"]),
                            ItemType = rdr["ITEM_TYPE"]?.ToString(),
                            DelCityCode = rdr["DEL_CITY_CODE"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["DEL_CITY_CODE"]),
                            VehicleType = rdr["VEHICLE_TYPE"]?.ToString(),
                            DeliveryPincode = rdr["DELIVERY_PINCODE"]?.ToString(),
                            Remarks = rdr["REMARKS"]?.ToString(),
                            PartyCode = rdr["PARTY_CODE"] == DBNull.Value ? null : Convert.ToInt32(rdr["PARTY_CODE"]),
                            RefType = rdr["REF_TYPE"] == DBNull.Value ? null : rdr["REF_TYPE"].ToString(),
                            RefNo = rdr["REF_NO"] == DBNull.Value ? null : Convert.ToInt32(rdr["REF_NO"])
                        });
                    }
                }

                return new RepositoryResponseData<TransportRateRequestForQuotationModel>
                {
                    status = true,
                    data = model,
                    message = "RFQ document loaded successfully."
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponseData<TransportRateRequestForQuotationModel>
                {
                    status = false,
                    message = ex.Message
                };
            }
        }

        public async Task<RepositoryResponse> SendTransportRequestEmail(TransportEmailRequestModel model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                var transportDetails = new List<string>();
                var recipients = new List<(string Name, string Email)>();

                string smtpHost = "";
                string smtpUser = "";
                string smtpPassword = "";
                int smtpPort = 0;
                bool enableSsl = true;

                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // 1. Get transport requirement details
                    using (var cmd = new SqlCommand(@"SELECT A.WEIGHT, A.VEHICLE_TYPE, A.ITEM_TYPE, A.REMARKS, A.DELIVERY_PINCODE, B.NAME AS CITY_NAME
                                                        FROM RFQ2 A LEFT JOIN CITY_MAST B ON A.DEL_CITY_CODE = B.CODE
                                                        WHERE A.COMP_CODE = @COMP_CODE AND A.BRANCH_CODE = @BRANCH_CODE AND A.YEAR_CODE = @YEAR_CODE
                                                        AND A.V_TYPE = @V_TYPE AND A.V_NO = @V_NO", con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.VType);
                        cmd.Parameters.AddWithValue("@V_NO", model.VNo);

                        using var rdr = await cmd.ExecuteReaderAsync();

                        while (await rdr.ReadAsync())
                        {
                            decimal weight = rdr["WEIGHT"] == DBNull.Value ? 0 : Convert.ToDecimal(rdr["WEIGHT"]) / 1000;
                            string city = rdr["CITY_NAME"]?.ToString() ?? "";
                            string pincode = rdr["DELIVERY_PINCODE"]?.ToString() ?? "";
                            string remarks = rdr["REMARKS"]?.ToString() ?? "";
                            string delivery = city;

                            if (!string.IsNullOrWhiteSpace(pincode))
                                delivery += $" (Pincode - {pincode})";

                            string item = $@"
                            <br><b>Vehicle type:</b> {System.Net.WebUtility.HtmlEncode(rdr["VEHICLE_TYPE"]?.ToString() ?? "")}
                            <br><b>Weight:</b> {weight:F2} ton
                            <br><b>Item type:</b> {System.Net.WebUtility.HtmlEncode(rdr["ITEM_TYPE"]?.ToString() ?? "")}
                            <br><b>Delivery:</b> {System.Net.WebUtility.HtmlEncode(delivery)}";

                            if (!string.IsNullOrWhiteSpace(remarks))
                                item += $"<br><b>Remarks:</b> {System.Net.WebUtility.HtmlEncode(remarks)}";

                            transportDetails.Add(item);
                        }
                    }

                    if (transportDetails.Count == 0)
                        return new RepositoryResponse
                        {
                            status = false,
                            message = "Transport requirement details not found."
                        };

                    // 2. Get transporter email addresses
                    if (model.SendToTransporter)
                    {
                        foreach (int code in model.TransporterCodes.Distinct())
                        {
                            using var cmd = new SqlCommand(@"SELECT A.NAME AS USER_NAME, B.EMAIL AS EMAIL_ID FROM TRANSPORT_MAST A
                                                            LEFT JOIN SUBGROUP_MAST B ON A.PARTY_CODE = B.CODE AND A.COMP_CODE = B.COMP_CODE
                                                            WHERE A.SALE_GROUP = 'SALE' AND A.COMP_CODE = @COMP_CODE AND A.CODE = @CODE AND ISNULL(B.EMAIL, '')
                                                            <> ''", con);

                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", code);

                            using var rdr = await cmd.ExecuteReaderAsync();

                            if (await rdr.ReadAsync())
                            {
                                recipients.Add((
                                    rdr["USER_NAME"]?.ToString() ?? "",
                                    rdr["EMAIL_ID"]?.ToString() ?? ""
                                ));
                            }
                        }

                        if (recipients.Count == 0)
                            return new RepositoryResponse
                            {
                                status = false,
                                message = "No valid email addresses found for selected transporters."
                            };
                    }
                    else
                    {
                        // 3. Use manually entered email
                        recipients.Add(("", model.EmailId.Trim()));
                    }

                    // 4. Get SMTP settings
                    using (var cmd = new SqlCommand(@"SELECT TOP 1 SMTP_SERVER, USER_ID, PASSWORD, SMTP_PORT, SMTP_USSL FROM EMAIL_SETTING1 
                                                    WHERE COMP_CODE = @COMP_CODE AND V_TYPE = @V_TYPE", con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", "SAGT");

                        using var rdr = await cmd.ExecuteReaderAsync();

                        if (!await rdr.ReadAsync())
                            return new RepositoryResponse
                            {
                                status = false,
                                message = "Please check email settings. Mail not sent."
                            };

                        smtpHost = rdr["SMTP_SERVER"]?.ToString() ?? "";
                        smtpUser = rdr["USER_ID"]?.ToString() ?? "";
                        smtpPort = Convert.ToInt32(rdr["SMTP_PORT"]);

                        // Existing VB.NET password decryption
                        string encryptedPassword = rdr["PASSWORD"]?.ToString() ?? "";
                        var ansi = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;
                        byte[] bytes = ansi.GetBytes(encryptedPassword);
                        smtpPassword = ansi.GetString(bytes.Select(b => (byte)(255 - b)).ToArray());

                        string sslValue = rdr["SMTP_USSL"]?.ToString() ?? "";

                        enableSsl = sslValue.Equals("true", StringComparison.OrdinalIgnoreCase) || sslValue.Equals("yes", StringComparison.OrdinalIgnoreCase)
                                    || sslValue == "1" || sslValue.Equals("Y", StringComparison.OrdinalIgnoreCase);
                    }
                }

                // 5. Generate and send emails
                string subject = "Requirement of transport vehicle";

                foreach (var recipient in recipients)
                {
                    string mailBody = $@"
                    <html>
                    <body>
                        Dear {System.Net.WebUtility.HtmlEncode(recipient.Name)}, 
                        <br><br>
                        We need Transport Vehicle as below:
                        {string.Join("<br>", transportDetails)}
                        <br><br>
                        Please confirm availability.
                        <br><br>
                        {System.Net.WebUtility.HtmlEncode(gv.CompanyName)}
                    </body>
                    </html>";

                    using var mail = new System.Net.Mail.MailMessage();
                    mail.From = new System.Net.Mail.MailAddress(smtpUser);
                    mail.To.Add(recipient.Email);
                    mail.Subject = subject;
                    mail.Body = mailBody;
                    mail.IsBodyHtml = true;

                    using var smtp = new System.Net.Mail.SmtpClient(smtpHost, smtpPort);
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword);
                    //smtp.EnableSsl = enableSsl;
                    smtp.EnableSsl = true;
                    smtp.Timeout = 10000;

                    await smtp.SendMailAsync(mail);
                }

                return new RepositoryResponse
                {
                    status = true,
                    message = model.SendToTransporter ? $"Email sent successfully to {recipients.Count} transporter(s)." : "Email sent successfully."
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Unable to send email. " + ex.Message };
            }
        }

        public async Task<RepositoryResponse> SendTransportRequestWhatsApp(TransportWhatsAppRequestModel model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                var details = new List<string>();
                var recipients = new List<string>();

                using (var con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    // 1. Get transport requirement details
                    using (var cmd = new SqlCommand(@"SELECT A.WEIGHT, A.VEHICLE_TYPE, A.ITEM_TYPE, A.REMARKS, A.DELIVERY_PINCODE, B.NAME AS CITY_NAME
                                              FROM RFQ2 A LEFT JOIN CITY_MAST B ON A.DEL_CITY_CODE = B.CODE
                                              WHERE A.COMP_CODE = @COMP_CODE AND A.BRANCH_CODE = @BRANCH_CODE AND A.YEAR_CODE = @YEAR_CODE
                                              AND A.V_TYPE = @V_TYPE AND A.V_NO = @V_NO", con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                        cmd.Parameters.AddWithValue("@V_TYPE", model.VType ?? "");
                        cmd.Parameters.AddWithValue("@V_NO", model.VNo);

                        using var rdr = await cmd.ExecuteReaderAsync();

                        while (await rdr.ReadAsync())
                        {
                            decimal weight = rdr["WEIGHT"] == DBNull.Value ? 0 : Math.Round(Convert.ToDecimal(rdr["WEIGHT"]) / 1000, 2);
                            string pincode = rdr["DELIVERY_PINCODE"]?.ToString() ?? "";
                            string remarks = rdr["REMARKS"]?.ToString() ?? "";

                            var sb = new StringBuilder();
                            sb.Append($"Vehicle type: {rdr["VEHICLE_TYPE"]},\n");
                            sb.Append($"Weight: {weight:0.##} Ton,\n");
                            sb.Append($"Item Type: {rdr["ITEM_TYPE"]},\n");
                            sb.Append($"Delivery At: {rdr["CITY_NAME"]}");

                            if (pincode != "")
                                sb.Append($", Pincode: {pincode}");
                            if (remarks != "")
                                sb.Append($",\nRemarks: {remarks}");

                            details.Add(sb.ToString());
                        }
                    }

                    if (details.Count == 0)
                        return new RepositoryResponse { status = false, message = "Transport requirement details not found." };

                    // 2. Get recipients
                    if (model.SendToTransporter)
                    {
                        foreach (int code in model.TransporterCodes.Distinct())
                        {
                            using var cmd = new SqlCommand(@"SELECT B.MOBILE AS MOBILE_NO FROM TRANSPORT_MAST A
                                                     LEFT JOIN SUBGROUP_MAST B ON A.PARTY_CODE = B.CODE AND A.COMP_CODE = B.COMP_CODE
                                                     WHERE A.SALE_GROUP = 'SALE' AND A.COMP_CODE = @COMP_CODE AND A.CODE = @CODE
                                                     AND ISNULL(B.MOBILE, '') <> ''", con);

                            cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", code);

                            var mobile = (await cmd.ExecuteScalarAsync())?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(mobile))
                                recipients.Add(mobile);
                        }

                        if (recipients.Count == 0)
                            return new RepositoryResponse { status = false, message = "No valid mobile numbers found for selected transporters." };
                    }
                    else
                    {
                        recipients.Add((model.MobileNo ?? "").Trim());
                    }
                }

                // 3. Send WhatsApp
                string matter = string.Join("\n\n", details);

                int sentCount = 0;

                foreach (var mobile in recipients.Distinct())
                {
                    /*===============================================================================
                        WhatsAppService is not available now, so the actual sending is commented out.
                        Uncomment and use the actual WhatsApp sending service when available.
                      ===============================================================================

                    bool sent = await WhatsAppService.SendWhatsAppMessage("rfq", mobile, "We need Transport Vehicle as below:", matter, gv.CompanyName);
                    if (sent) sentCount++;*/                                                                                                             
                }

                if (sentCount == 0)
                    return new RepositoryResponse { status = false, message = "Unable to send WhatsApp message." };

                return new RepositoryResponse
                {
                    status = true,
                    message = model.SendToTransporter
                        ? $"WhatsApp sent to {sentCount} of {recipients.Distinct().Count()} transporter(s)."
                        : "WhatsApp message sent successfully."
                };
            }
            catch (Exception ex)
            {
                return new RepositoryResponse { status = false, message = "Unable to send WhatsApp message. " + ex.Message };
            }
        }
    }
}
