using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tesseract;    
using travelexpensemanagement.Dbconnection;

public class TestpageController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly DataBaseConnection _dbConnection;
    //private readonly string _connectionString = "Data Source=118.139.164.161;Initial Catalog=Hrms_db;Persist Security Info=True;User ID=noida;Password=Kwalityy@214#;Trust Server Certificate=True";
    private readonly string _connectionString = "Data Source=192.168.20.51;Initial Catalog=ERPDB;Persist Security Info=True;User ID=sa;Password=Pass@123;Trust Server Certificate=True";
    private readonly string PubWhatsupTokenId = "0ba3f59a551b9a8881caba3572031b81183859298391c1cbdc8e915ec725430a";

    public TestpageController(IWebHostEnvironment env, DataBaseConnection db)
    {
        _env = env;
        //_connectionString = _connectionString;
    }
    public IActionResult Index()
    {
        return View();
    }
    [HttpPost]
    public IActionResult ScanBase64([FromBody] ImageModel model)
    {
        var base64 = model.ImageData.Split(',')[1];
        byte[] bytes = Convert.FromBase64String(base64);
        string imgPath = Path.Combine(_env.WebRootPath, "Images");
        Directory.CreateDirectory(imgPath);
        string file = Path.Combine(imgPath, Guid.NewGuid() + ".jpg");
        System.IO.File.WriteAllBytes(file, bytes);
        string text = ExtractText(file);

        return Json(new
        {
            name = ParseName(text),
            company = ParseCompany(text),
            email = ParseEmail(text),
            phone = ParsePhone(text),
            address = ParseAddress(text),
            ocrText = text
        });
    }
    

    [HttpPost]
    public async Task<IActionResult> SaveBusinessCard([FromBody] BusinessCard card)
    {
        try
        {
            // Save the image and get the image path
            string imagePath = SaveImage(card.ImageBase64);
            string visitorName = HttpContext.Session.GetString("VISITOR_NAME") ?? "Guest";
            // Construct the SQL query to insert data into the BusinessCard table
            using SqlConnection con = new SqlConnection(_connectionString);
            string q = @"
            INSERT INTO BusinessCard
            (Name, Gender, MobileNo, Email, Company, Address, TradeType,
             UserFeedback, CreatedDate, ImagePath, CardData, DeviceId, DeviceName, Website, Customer, Supplier, VISITOR_NAME)
            VALUES
            (@Name, @Gender, @MobileNo, @Email, @Company, @Address, @TradeType,
             @UserFeedback, @CreatedDate, @ImagePath, @CardData, @DeviceId, @DeviceName, @Website, @Customer, @Supplier, @VISITOR_NAME)";

            using SqlCommand cmd = new SqlCommand(q, con);

            // Add parameters to the query
            cmd.Parameters.AddWithValue("@Name", card.Name ?? "");
            cmd.Parameters.AddWithValue("@Gender", card.Gender ?? "Male");
            cmd.Parameters.AddWithValue("@MobileNo", card.MobileNo ?? "");
            cmd.Parameters.AddWithValue("@Email", card.Email ?? "");
            cmd.Parameters.AddWithValue("@Company", card.Company ?? "");
            cmd.Parameters.AddWithValue("@Address", card.Address ?? "");
            cmd.Parameters.AddWithValue("@TradeType", card.TradeType ?? "");
            cmd.Parameters.AddWithValue("@UserFeedback", card.UserFeedback ?? "");
            cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);  
            cmd.Parameters.AddWithValue("@ImagePath", imagePath);
            cmd.Parameters.AddWithValue("@CardData", card.OcrText ?? "");
            cmd.Parameters.AddWithValue("@DeviceId", card.DeviceId ?? "");
            cmd.Parameters.AddWithValue("@DeviceName", card.DeviceName ?? "");
            cmd.Parameters.AddWithValue("@Website", card.Website ?? "");
            cmd.Parameters.AddWithValue("@Customer", string.Join(",", card.CustomerProducts ?? new List<string>()));
            cmd.Parameters.AddWithValue("@Supplier", string.Join(",", card.SupplierProducts ?? new List<string>()));
            cmd.Parameters.AddWithValue("@VISITOR_NAME", visitorName);

            // Open connection and execute the query
            con.Open();
            await cmd.ExecuteNonQueryAsync();

            if (!string.IsNullOrEmpty(card.Email))
            {
                try { SendThankYouEmail(card); }
                catch { }
            }

            // 📲 WHATSAPP (SAFE)
            if (!string.IsNullOrEmpty(card.MobileNo))
            {
                try
                {
                    string mobile = Regex.Replace(card.MobileNo, @"\D", "");
                    await SendWhatsAppMessage("plastindia", mobile, card.Name);
                }
                catch { }
            }
            return Json(new { message = "Business Card Saved Successfully ✅" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
    private async Task<bool> SendWhatsAppMessage(
        string templateName,
        string phoneNumber,
        string f1 = "",
        string f2 = "",
        string f3 = "",
        string f4 = "",
        string f5 = "",
        string f6 = "",
        string f7 = "",
        string f8 = "",
        string f9 = "",
        string f10 = "")
    {
        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string apiUrl =
                "https://sparklebot.in/api/v1/pashupatigrpcom/messages/template";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", PubWhatsupTokenId);

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            using var form = new MultipartFormDataContent();

            form.Add(new StringContent(templateName), "template_name");
            form.Add(new StringContent("en"), "template_language");
            form.Add(new StringContent(phoneNumber), "phone_number");

            if (templateName == "plastindia" || templateName == "vno")
            {
                if (string.IsNullOrEmpty(f1))
                    f1 = "NA";

                form.Add(new StringContent(f1), "field_1");
            }

            var response = await client.PostAsync(apiUrl, form);
            var responseText = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode &&
                   responseText.ToLower().Contains("success");
        }
        catch
        {
            return false;
        }
    }

    private void SendThankYouEmail(BusinessCard card)
    {
        string salutation = "Sir";

        if (!string.IsNullOrEmpty(card.Gender))
        {
            salutation = card.Gender.Equals("Female", StringComparison.OrdinalIgnoreCase)
                ? "Madam"
                : "Sir";
        }

        string body = $@"
<html>
<body style='font-family: Arial, sans-serif; font-size:14px;'>

<p>Dear {salutation},</p>
<p><b>Kind attention to:</b> {card.Name}</p>

<p>
Thank you for visiting our stall at the <b>PLASTINDIA Exhibition</b>.<br/>
It was a pleasure to connect with you and share insights about our offerings.
</p>

<p>
We are pleased to inform you that <b>Pashupati Group</b> is actively engaged in providing reliable
and industry-focused solutions in the fields of
<b>Plastic Recycling</b> and <b>Packaging Solutions</b>.
</p>

<p>
<b>📍 Stall Details:</b><br/>
<b>Hall No.:</b> H4F<br/>
<b>Stall No.:</b> B15
</p>

<p>
<b>For any inquiries, please contact us at the address below :</b>
</p>

<p>
<b><u>For Packaging Products</u></b><br/>
PP/HDPE Woven Fabric & Bags | BOPP Bags | FIBC Bags<br/>
📧 <b>fabric@pashupatigrp.com</b>
</p>

<p>
<b><u>For Plastic Recycling Products</u></b><br/>
rPET Flakes, rPET Chips, rPET MasterBatch, rPSF, PP Fibre, rHDPE Granules,
rPP Granules, rLDPE Granules.<br/>
📧 <b>recycling@pashupatigrp.com</b>
</p>

<p>
<b><u>For Plastic Recycling Raw Material Supply</u></b><br/>
PET Bottles, PET & Polyester Waste, PP, HD, LD, LLDPE, PPCP Waste.<br/>
📧 <b>wastemgmt@pashupatigrp.com</b>
</p>

<p>
<b>Visit our website:</b><br/>
👉 <a href='https://www.pashupatigrp.com'><b>www.pashupatigrp.com</b></a>
</p>

<p style='color:gray; font-size:12px;'>
<b>Please note:</b> This email has been sent from noreply@pashupatigrp.com.
Replies to this email address are not monitored.
</p>

<p>
We look forward to the opportunity of working together and building a successful business relationship.
</p>

<p>
<b>Warm regards,</b><br/>
<b>Pashupati Group</b>
</p>

</body>
</html>";

        var mail = new MailMessage
        {
            From = new MailAddress("noreply@pashupatigrp.com", "Pashupati Group"),
            Subject = "Thank You for Visiting Us at PLASTINDIA Exhibition",
            Body = body,
            IsBodyHtml = true
        };

        mail.To.Add(card.Email);

        var smtp = new SmtpClient("smtp-mail.outlook.com", 587)
        {
            Credentials = new NetworkCredential(
                "noreply@pashupatigrp.com",
                "Apple@213"   // ⚠️ move to config in production
            ),
            EnableSsl = true
        };

        smtp.Send(mail);
    }

    [HttpGet]
    public IActionResult DownloadBusinessCards()
    {
        string visitorName = HttpContext.Session.GetString("VISITOR_NAME");
        //string userRole = visitorName == "Admin" ? "Admin" : "USER"; // or get from session
        string userRole = visitorName != null && visitorName.Equals("admin", StringComparison.OrdinalIgnoreCase)
    ? "Admin"
    : "USER";


        DataTable dt = new DataTable();
        using (SqlConnection con = new SqlConnection(_connectionString))
        {
            con.Open();

            string query;

            if (userRole == "Admin")
            {
                query = @"
                Select CardId as ID, VISITOR_NAME as UserName, Name as Visitor, Email, MobileNo,Company as CompanyName,Address, TradeType, UserFeedback, CreatedDate, DeviceName,Website,Customer,Supplier From BusinessCard
                ORDER BY CreatedDate DESC";
            }
            else
            {
                query = @"
                Select CardId as ID,VISITOR_NAME as UserName, Name as Visitor, Email, MobileNo,Company as CompanyName,Address, TradeType, UserFeedback, CreatedDate, DeviceName,Website,Customer,Supplier From BusinessCard
                WHERE VISITOR_NAME = @VISITOR_NAME
                ORDER BY CreatedDate DESC";
            }

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                if (userRole != "Admin")
                {
                    cmd.Parameters.AddWithValue("@VISITOR_NAME", visitorName);
                }

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
        }
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Business Cards");
        worksheet.Cell(1, 1).InsertTable(dt);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        string fileName = userRole == "Admin"
            ? $"All_BusinessCards_{DateTime.Now:ddMMyyyy}.xlsx"
            : $"{visitorName}_BusinessCards_{DateTime.Now:ddMMyyyy}.xlsx";

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName
        );
    }

    private string SaveImage(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return "";

        var data = base64.Split(',')[1];
        byte[] bytes = Convert.FromBase64String(data);

        string folder = Path.Combine(_env.WebRootPath, "BusinessCards");
        Directory.CreateDirectory(folder);

        string fileName = Guid.NewGuid() + ".jpg";
        string filePath = Path.Combine(folder, fileName);

        System.IO.File.WriteAllBytes(filePath, bytes);

        return "/BusinessCards/" + fileName; // relative path
    }
    private string ExtractText(string path)
    {
        string tessdata = Path.Combine(_env.WebRootPath, "tessdata");

        using var engine = new TesseractEngine(tessdata, "eng", EngineMode.LstmOnly);
        using var img = Pix.LoadFromFile(path);
        using var page = engine.Process(img, PageSegMode.Auto);

        return page.GetText();
    }

    private string ParseEmail(string text)
        => Regex.Match(text, @"[\w\.-]+@[\w\.-]+\.\w+").Value;

    private string ParsePhone(string text)
        => Regex.Match(text, @"\+?\d[\d\s-]{7,}\d").Value;

    private string ParseName(string text)
    {
        foreach (var l in text.Split('\n'))
        {
            var c = Regex.Replace(l, @"[^A-Za-z\s]", "").Trim();
            if (c.Split(' ').Length >= 2)
                return c;
        }
        return "";
    }
    private string ParseCompany(string text)
    {
        foreach (var l in text.Split('\n'))
            if (l.ToUpper() == l && l.Length > 3)
                return l.Trim();
        return "";
    }
    private string ParseAddress(string text)
    {
        var lines = text.Split('\n');
        if (lines.Length >= 3)
            return string.Join(", ", lines[^3..]);
        return "";
    }
}




public class ImageModel
{
    public string ImageData { get; set; }
}

//public class BusinessCard
//{
//    public string Name { get; set; }
//    public string Gender { get; set; }
//    public string MobileNo { get; set; }
//    public string Email { get; set; }
//    public string Company { get; set; }
//    public string Address { get; set; }
//    public string TradeType { get; set; }
//    public string UserFeedback { get; set; }
//    public string ImageBase64 { get; set; }   // 👈 ADD
//    public string ImagePath { get; set; }     // 👈 ADD
//    public string OcrText { get; set; }
//    public List<string> CustomerProducts { get; set; }
//    public List<string> SupplierProducts { get; set; }
//    public string DeviceId { get; set; }
//    public string DeviceName { get; set; }
//    public string Website { get; set; }
//}

public class VisitorCardDetails
{
    public int CardId { get; set; }
    public string Name { get; set; }
    public string Gender { get; set; }
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string Company { get; set; }
    public string Address { get; set; }
    public string TradeType { get; set; }
    public string UserFeedback { get; set; }
    public DateTime CreatedDate { get; set; }
    public string DeviceName { get; set; }
    public string Website { get; set; }
    public string Customer { get; set; }
    public string Supplier { get; set; }
    public string VISITOR_NAME { get; set; }
}

public class BusinessCard
{
    public string Name { get; set; }
    public string Gender { get; set; }
    public string MobileNo { get; set; }
    public string Email { get; set; }
    public string Company { get; set; }
    public string Address { get; set; }
    public string TradeType { get; set; }
    public string UserFeedback { get; set; }
    public string ImageBase64 { get; set; }   
    public string ImagePath { get; set; }     
    public string OcrText { get; set; }
    public List<string> CustomerProducts { get; set; }
    public List<string> SupplierProducts { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string Website { get; set; }   // Add this field
}


