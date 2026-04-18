using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.Helpers;
using travelexpensemanagement.Models.Admin.Setup;
using static iTextSharp.text.pdf.AcroFields;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    [SessionAuthorize]
    public class EmailSettingController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly DbHelper.DbHelper _dbHelper;
        private readonly EncryptionHelper _encryptionHelper;

        public EmailSettingController(DataBaseConnection dbConnection, DbHelper.DbHelper dbHelper, EncryptionHelper encryptionHelper)
        {
            _dbConnection = dbConnection;
            _dbHelper = dbHelper;
            _encryptionHelper = encryptionHelper;
        }
        public IActionResult Index()
        {
            string compCode = HttpContext.Session.GetString("COMP_CODE");
            //return View();
            return View("~/Views/Admin/SystemInitilization/EmailSetting/Index.cshtml");
        }
        [HttpGet]
        public async Task<IActionResult> GetEmailUserList()
        {
            string query = "SELECT DISTINCT Code, USER_ID FROM EMAIL_SETTING1";
            var dataTable = await _dbHelper.ExecuteQueryAsync(query);

            var result = dataTable.AsEnumerable()
                .Select(row => new
                {
                    id = row["USER_ID"].ToString(),
                    //text = $"{row["Code"]} - {row["USER_ID"]}",
                    text = $"{row["USER_ID"]}",
                    code = row["Code"].ToString()
                })
                .ToList();

            return Json(result);
        }
        [HttpGet]
        public JsonResult GetDocument()
        {
            List<object> departments = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code,name from DOCTYPE_MAST order by DOCTYPE";
                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    departments.Add(new
                    {
                        Value = reader["code"].ToString(),
                        Text = reader["name"].ToString()
                    });
                }
            }
            return Json(departments);
        }
        public IActionResult SubmitEmailSettings([FromBody] EmailSettingModel1 model)
        {
            string compCodeStr = HttpContext.Session.GetString("COMP_CODE");
            if (string.IsNullOrEmpty(compCodeStr) || !int.TryParse(compCodeStr, out int compCode))
            {
                return Json(new { success = false, message = "COMP_CODE not found in session." });
            }
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return Json(new { success = false, message = "No data submitted." });
            }
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                // Begin a transaction
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        // ========= INSERT FOREACH ============ 
                        if (model.Items.Any(i => i.Insnerttype == "Insert"))
                        {
                            int nextCode = 0;
                         
                            using (SqlCommand cmdMax = new SqlCommand("SELECT ISNULL(MAX(CODE), 0) + 1 FROM EMAIL_SETTING1 WHERE COMP_CODE = @COMP_CODE", con, transaction))
                            {
                                cmdMax.Parameters.AddWithValue("@COMP_CODE", compCode);
                                nextCode = Convert.ToInt32(cmdMax.ExecuteScalar());
                            }

                            foreach (var item in model.Items.Where(i => i.Insnerttype == "Insert"))
                            {
                                //string encryptedPassword = EncryptionHelper.Encrypt(item.Password);
                                string encryptedPassword = _encryptionHelper.Encrypt(item.Password);
                                string query = @"
                                INSERT INTO EMAIL_SETTING1 
                                (COMP_CODE, CODE, USER_ID, V_DATE, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL, V_TYPE, DESCRIPTION, SIGNATURE, AUTO_MAIL, UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID, ACTIVE, SNO)
                                VALUES 
                                (@COMP_CODE, @CODE, @USER_ID, @V_DATE, @WEBPASSWORD, @SMTP_SERVER, @SMTP_PORT, @SMTP_USSL, @V_TYPE, @DESCRIPTION, @SIGNATURE, @AUTO_MAIL, @UUSER, @UDATE, @EUSER, @EDATE, @AED, @WSID, @LIP, @LID, @ACTIVE, @SNO)";

                                using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                                    cmd.Parameters.AddWithValue("@CODE", nextCode);
                                    cmd.Parameters.AddWithValue("@USER_ID", (object?)item.UserId ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_DATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@WEBPASSWORD", encryptedPassword ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_SERVER", (object?)item.SmtpServer ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_PORT", (object?)item.SmtpPort ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_USSL", (object?)item.SmtpUssl ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)item.DocumentCode ?? DBNull.Value);
                                    // Optional fields
                                    cmd.Parameters.AddWithValue("@DESCRIPTION", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SIGNATURE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AUTO_MAIL", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ACTIVE", 1);
                                    cmd.Parameters.AddWithValue("@SNO", DBNull.Value);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                        // ========= UPDATE FOREACH ============ 
                        if (model.Items.Any(i => i.Insnerttype == "Update"))
                        {
                            // Delete existing records before updating (if compCode and code exist)
                            var updateItems = model.Items.Where(i => i.Insnerttype == "Update" && !string.IsNullOrEmpty(i.compCode) && !string.IsNullOrEmpty(i.code)).ToList();

                            if (updateItems.Any())
                            {
                                foreach (var item in updateItems)
                                {
                                    using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM EMAIL_SETTING1 WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE", con, transaction))
                                    {
                                        deleteCmd.Parameters.AddWithValue("@COMP_CODE", item.compCode);
                                        deleteCmd.Parameters.AddWithValue("@CODE", item.code);
                                        deleteCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            // Now Insert updated records
                            foreach (var item in updateItems)
                            {
                                //string encryptedPassword = EncryptionHelper.Encrypt(item.Password);
                                string encryptedPassword = _encryptionHelper.Encrypt(item.Password);

                                string query = @"
                                INSERT INTO EMAIL_SETTING1 
                                (COMP_CODE, CODE, USER_ID, V_DATE, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL, V_TYPE, DESCRIPTION, SIGNATURE, AUTO_MAIL, UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID, ACTIVE, SNO)
                                VALUES 
                                (@COMP_CODE, @CODE, @USER_ID, @V_DATE, @WEBPASSWORD, @SMTP_SERVER, @SMTP_PORT, @SMTP_USSL, @V_TYPE, @DESCRIPTION, @SIGNATURE, @AUTO_MAIL, @UUSER, @UDATE, @EUSER, @EDATE, @AED, @WSID, @LIP, @LID, @ACTIVE, @SNO)";

                                using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", item.compCode);
                                    cmd.Parameters.AddWithValue("@CODE", item.code);
                                    cmd.Parameters.AddWithValue("@USER_ID", (object?)item.UserId ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_DATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@WEBPASSWORD", encryptedPassword ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_SERVER", (object?)item.SmtpServer ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_PORT", (object?)item.SmtpPort ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_USSL", (object?)item.SmtpUssl ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)item.DocumentCode ?? DBNull.Value);

                                    // Optional fields
                                    cmd.Parameters.AddWithValue("@DESCRIPTION", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SIGNATURE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AUTO_MAIL", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ACTIVE", 1);
                                    cmd.Parameters.AddWithValue("@SNO", DBNull.Value);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        // Commit the transaction if all operations are successful
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction in case of an error
                        transaction.Rollback();
                        return Json(new { success = false, message = $"Error occurred: {ex.Message}" });
                    }
                }
            }
            return Json(new { success = true, message = "All settings processed successfully!" });
        }
        [HttpGet]
        public JsonResult GetDocumentList()
        {
            List<object> docTypes = new List<object>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "SELECT CODE, NAME FROM DOCTYPE_MAST ORDER BY DOCTYPE";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        docTypes.Add(new
                        {
                            value = reader["CODE"].ToString(),
                            text = reader["NAME"].ToString()
                        });
                    }
                }
            }
            return Json(docTypes);
        }
        ///show data List in Insert form start Block
        [HttpGet]
        public async Task<IActionResult> GetAlldataList(string compCode, string code)
        {
            List<EmailSettingModel> list = new List<EmailSettingModel>();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"
                SELECT 
                    USER_ID, 
                    V_DATE, 
                    WEBPASSWORD, 
                    SMTP_SERVER, 
                    SMTP_PORT, 
                    SMTP_USSL, 
                    do.Name as V_TYPE 
                FROM EMAIL_SETTING1 em
                LEFT JOIN DOCTYPE_MAST do ON em.V_TYPE = do.CODE
                WHERE em.COMP_CODE = @CompCode AND em.CODE = @Code";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", compCode);
                        cmd.Parameters.AddWithValue("@Code", code);

                        con.Open();
                        using (SqlDataReader rdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await rdr.ReadAsync())
                            {
                                list.Add(new EmailSettingModel
                                {
                                    UserId = rdr["USER_ID"]?.ToString(),
                                    Document = rdr["V_TYPE"]?.ToString(), // 'do.Name as V_TYPE'
                                    Password = rdr["WEBPASSWORD"]?.ToString(),
                                    SmtpServer = rdr["SMTP_SERVER"]?.ToString(),
                                    SmtpPort = rdr["SMTP_PORT"]?.ToString(),
                                    SmtpUssl = rdr["SMTP_USSL"]?.ToString(),
                                    Date = Convert.ToDateTime(rdr["V_DATE"]).ToString("yyyy-MM-dd")
                                });
                            }
                        }
                    }
                }

                return Json(new EmailSettingModel1 { Items = list });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving data", error = ex.Message });
            }
        }
        ///show data List in Insert form start Block
        public IActionResult UpdateEmailSettings([FromBody] EmailSettingModel1 model)
        {
            string compCodeStr = HttpContext.Session.GetString("COMP_CODE");
            if (string.IsNullOrEmpty(compCodeStr) || !int.TryParse(compCodeStr, out int compCode))
            {
                return Json(new { success = false, message = "COMP_CODE not found in session." });
            }
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return Json(new { success = false, message = "No data submitted." });
            }
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();
                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        if (model.Items.Any(i => i.Insnerttype == "Update"))
                        {
                            var updateItems = model.Items.Where(i => i.Insnerttype == "Update" && !string.IsNullOrEmpty(i.compCode) && !string.IsNullOrEmpty(i.code)).ToList();

                            foreach (var item in updateItems)
                            {
                                using (SqlCommand deleteCmd = new SqlCommand("DELETE FROM EMAIL_SETTING1 WHERE COMP_CODE = @COMP_CODE AND CODE = @CODE", con, transaction))
                                {
                                    deleteCmd.Parameters.AddWithValue("@COMP_CODE", item.compCode);
                                    deleteCmd.Parameters.AddWithValue("@CODE", item.code);
                                    deleteCmd.ExecuteNonQuery();
                                }
                            }
                            // Now Insert updated records
                            foreach (var item in updateItems)
                            {
                                //string encryptedPassword = EncryptionHelper.Encrypt(item.Password);
                                string encryptedPassword = _encryptionHelper.Encrypt(item.Password);

                                string query = @"
                                INSERT INTO EMAIL_SETTING1 
                                (COMP_CODE, CODE, USER_ID, V_DATE, WEBPASSWORD, SMTP_SERVER, SMTP_PORT, SMTP_USSL, V_TYPE, DESCRIPTION, SIGNATURE, AUTO_MAIL, UUSER, UDATE, EUSER, EDATE, AED, WSID, LIP, LID, ACTIVE, SNO)
                                VALUES 
                                (@COMP_CODE, @CODE, @USER_ID, @V_DATE, @WEBPASSWORD, @SMTP_SERVER, @SMTP_PORT, @SMTP_USSL, @V_TYPE, @DESCRIPTION, @SIGNATURE, @AUTO_MAIL, @UUSER, @UDATE, @EUSER, @EDATE, @AED, @WSID, @LIP, @LID, @ACTIVE, @SNO)";

                                using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@COMP_CODE", item.compCode);
                                    cmd.Parameters.AddWithValue("@CODE", item.code);
                                    cmd.Parameters.AddWithValue("@USER_ID", (object?)item.UserId ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_DATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@WEBPASSWORD", encryptedPassword ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_SERVER", (object?)item.SmtpServer ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_PORT", (object?)item.SmtpPort ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SMTP_USSL", (object?)item.SmtpUssl ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@V_TYPE", (object?)item.DocumentCode ?? DBNull.Value);

                                    // Optional fields
                                    cmd.Parameters.AddWithValue("@DESCRIPTION", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@SIGNATURE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AUTO_MAIL", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UUSER", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@UDATE", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@EUSER", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@EDATE", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@AED", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@WSID", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LIP", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@LID", DBNull.Value);
                                    cmd.Parameters.AddWithValue("@ACTIVE", 1);
                                    cmd.Parameters.AddWithValue("@SNO", DBNull.Value);

                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction in case of an error
                        transaction.Rollback();
                        return Json(new { success = false, message = $"Error occurred: {ex.Message}" });
                    }
                }
            }
            return Json(new { success = true, message = "All settings processed successfully!" });
        }
    }
}
