using System.Data;
using System.Runtime.Intrinsics.X86;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using Microsoft.Data.SqlClient;

namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    public class DocumentNumberingController : Controller
    {
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        string StrSystemName = "", StrSystemIP = "";
        int rowCnt;
        public DocumentNumberingController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/SystemInitilization/DocumentNumbering/Index.cshtml");
        }

 
        [HttpGet]
        public async Task<JsonResult> GetYearDt()
        {
            try
            {

                var con = _dbcontext.GetErpConnection();
                var yeatdtList = await _dbHelper.GetJsonDataAsync("select distinct CODE, PREFIXYR as YearCd from YEAR_MAST order by PREFIXYR desc");
                return Json(new { status = true, data = yeatdtList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetVoucherPrefixDt()
        {
            try
            {

                var con = _dbcontext.GetErpConnection();
                var yeatdtList = await _dbHelper.GetJsonDataAsync("select distinct CODE as Prefix, NAME as V_type from DOCTYPE_MAST order by V_type");
                return Json(new { status = true, data = yeatdtList });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        [HttpGet]
        public JsonResult getvoucherTypeExitOrNot(int yearcd, string Vprefix)
        {
            string strCode = "";

            try
            {
                bool isExist = false;
                using (var con = _dbcontext.GetErpConnection())
                {
                    var sessionData = _globalValue.GetGlobalVariables();
                    var compCode = sessionData.PubCompCode;
                    strCode = _dbHelper.Xnull(compCode).ToString() + _dbHelper.Xnull(yearcd).ToString() + _dbHelper.Xnull(Vprefix);

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = " select case when exists (select 1 from DOC_NUMBER where (cast(COMP_CODE as varchar)+cast(YEAR_CODE as varchar)+ cast(V_TYPE as varchar))=@code) then 1 else 0 end as Vtype";
                        cmd.Parameters.AddWithValue("@code", strCode);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }

                return Json(new { status = true, exists = isExist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data check failed: " + ex.Message });
            }
        }


        public class DocumentNumber
        {
            public string? code { get; set; }
            public int? YearCode { get; set; }
            public string? VoucherType { get; set; }
            public string? docType { get; set; }
            public string? Prefix { get; set; }
            public int? FromNo { get; set; }
            public int? ToNo { get; set; }
        }



        [HttpPost]
        public JsonResult SaveDocumentSerial([FromBody] DocumentNumber docNumber)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    var sessionData = _globalValue.GetGlobalVariables();
                    var StrUUser = (sessionData.PubUserId);
                    var companyCd = sessionData.PubCompCode;
                    StrSystemName = Environment.MachineName;
                    //StrSystemIP = _dbHelper.GetLocalIPAddress();
                    StrSystemIP = sessionData.PubLocalId;
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("sp_DocumentNumber", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@companyCd", companyCd);
                        cmd.Parameters.AddWithValue("@yearCd", docNumber.YearCode);
                        cmd.Parameters.AddWithValue("@V_Type", docNumber.Prefix);
                        cmd.Parameters.AddWithValue("@V_Prefix", docNumber.Prefix);
                        cmd.Parameters.AddWithValue("@FromNo", _dbHelper.Xnull(docNumber.FromNo));
                        cmd.Parameters.AddWithValue("@ToNo", _dbHelper.Xnull(docNumber.ToNo));
                        cmd.Parameters.AddWithValue("@Uuser", StrUUser);
                        cmd.Parameters.AddWithValue("@Lip", StrSystemIP);
                        cmd.Parameters.AddWithValue("@Lid", StrSystemName);
                        cmd.Parameters.AddWithValue("@docType", docNumber.docType);
                        rowCnt = cmd.ExecuteNonQuery();
                    }
                }
                if (rowCnt > 0)
                    return Json(new { status = true, message = "Data save successfully" });
                else
                    return Json(new { status = false, message = "Data save failed" });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data save failed" });
            }

        }

        [HttpPost]
        public JsonResult updateDocNumber([FromBody] DocumentNumber docNumber)
        {
            try
            {

                using (var con = _dbcontext.GetErpConnection())
                {
                    var sessionData = _globalValue.GetGlobalVariables();
                    var StrEUser = (sessionData.PubUserId);
                    StrSystemName = Environment.MachineName;
                    //StrSystemIP = _dbHelper.GetLocalIPAddress();
                    StrSystemIP = sessionData.PubLocalId;
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateDocumentNumber", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@code", docNumber.code);
                        cmd.Parameters.AddWithValue("@V_Prefix", docNumber.Prefix);
                        cmd.Parameters.AddWithValue("@FromNo", docNumber.FromNo);
                        cmd.Parameters.AddWithValue("@ToNo", docNumber.ToNo);
                        cmd.Parameters.AddWithValue("@Euser", StrEUser);
                        cmd.Parameters.AddWithValue("@Lip", StrSystemIP);
                        cmd.Parameters.AddWithValue("@Lid", StrSystemName);
                        cmd.Parameters.AddWithValue("@docType", docNumber.docType);
                        rowCnt = cmd.ExecuteNonQuery();
                    }
                }

                if (rowCnt > 0)
                    return Json(new { status = true, message = "Data update successfully" });
                else
                    return Json(new { status = false, message = "Data update failed" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data update failed" + ex.Message });
            }

        }

    }
}
