using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;

namespace travelexpensemanagement.Controllers.Purchase.Master
{
    public class POMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        string yearPrefix, VNO;
        public POMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }

        public IActionResult Index()
        {
            return View("~/Views/Purchase/Master/POMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDocid()
        {
            try
            {
                var CompanyCd = _globalValue.GetGlobalVariables().PubCompCode;
                var YearCd = _globalValue.GetGlobalVariables().PubFYearCode;
                var BranchCd = 1;
                string VType = "POMT";


                string query = @"
            SELECT ISNULL(
                RIGHT('00000' + 
                    CAST(CAST(MAX(SUBSTRING(CONVERT(VARCHAR, V_NO), 5, LEN(CONVERT(VARCHAR, V_NO)))) AS INT) + 1 AS VARCHAR), 5),
            '00001')
            FROM PO_MAST
            WHERE COMP_CODE = @COMP_CODE
              AND BRANCH_CODE = @BRANCH_CODE
              AND YEAR_CODE = @YEAR_CODE
              AND V_TYPE = @V_TYPE;
        ";

                var parameters = new Dictionary<string, object>
        {
            { "@COMP_CODE",  CompanyCd },
            { "@BRANCH_CODE", BranchCd },
            { "@YEAR_CODE", YearCd },
            { "@V_TYPE", VType }
        };

                string newVNo = await _dbHelper.GetExecuteScalarAsync<string>(query, parameters);
                yearPrefix = await _dbHelper.GetExecuteScalarAsync<string>(
                  $"SELECT PREFIXYR as PrefixYr FROM YEAR_MAST WHERE CODE = '{YearCd}'");

                var DocId = _dbHelper.Xnull(VType).ToString() + _dbHelper.Xnull(yearPrefix) + _dbHelper.Xnull(newVNo);

                return Json(new { status = true, DocId = DocId });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        public class PoMastModel
        {
            public int? VNo { get; set; }
            public string? DocId { get; set; }
            public string? DocDate { get; set; }
            public string? FromDate { get; set; }
            public string? ToDate { get; set; }
            public decimal? StoreAmt { get; set; }
            public decimal? CapitalAmt { get; set; }
            public string? Remarks { get; set; }
            public int? Status { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SavePOMast([FromBody] PoMastModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { status = false, message = "Data Save Failed" });
                }

                using (var con = _dbcontext.GetErpConnection())
                {
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    string DocId = _dbHelper.Xnull(model.DocId).ToString();
                    if (DocId == null || DocId.Length > 5)
                    {
                        VNO = (DocId).Substring(4);
                    }
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PurchaseOrderMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@branchCd", 1);
                        cmd.Parameters.AddWithValue("@yearCd", usersessionDt.PubFYearCode);
                        cmd.Parameters.AddWithValue("@VNo", VNO);
                        cmd.Parameters.AddWithValue("@VType", "POMT");
                        cmd.Parameters.AddWithValue("@Docid", _dbHelper.Xnull(model.DocId));
                        //cmd.Parameters.AddWithValue("@DocDate", _dbHelper.Xnull(model.DocDate));
                        //cmd.Parameters.AddWithValue("@FromDt", _dbHelper.Xnull(model.FromDate));
                        //cmd.Parameters.AddWithValue("@ToDate", _dbHelper.Xnull(model.ToDate));
                        cmd.Parameters.AddWithValue("@DocDate", DbHelper.DbHelper.ConvertToDate(model.DocDate) ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDt", DbHelper.DbHelper.ConvertToDate(model.FromDate) ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", DbHelper.DbHelper.ConvertToDate(model.ToDate) ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@storeAmt", _dbHelper.Xnull(model.StoreAmt));
                        cmd.Parameters.AddWithValue("@CapitalAmt", _dbHelper.Xnull(model.CapitalAmt));
                        cmd.Parameters.AddWithValue("@Remark", _dbHelper.Xnull(model.Remarks));
                        cmd.Parameters.AddWithValue("@status", _dbHelper.Xnull(model.Status));
                        cmd.Parameters.AddWithValue("@Faprov_Remarks", "");
                        cmd.Parameters.AddWithValue("@Faprov_Status", "");
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        await con.OpenAsync();
                        int x = await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { status = true, message = "Data Save Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Save Failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetPODetailsById(string id)
        {
            try
            {
                string query = $"SELECT V_NO, V_TYPE, DOC_ID, V_DATE, FROM_DATE, TO_DATE, STORE_AMT, CAPITAL_AMT, REMARK, FAPROV_REMARKS, FAPROV_STATUS, STATUS  FROM po_mast WHERE DOC_ID = '{id}'  and COMP_CODE='{_globalValue.GetGlobalVariables().PubCompCode}'  ";
                var data = await _dbHelper.GetJsonDataAsync(query);

                if (data.Count > 0)
                    return Json(new { status = true, data = data[0] });

                return Json(new { status = false, message = "Not found" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> UpdatePOMast([FromBody] PoMastModel model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new { status = false, message = "Data update Failed" });
                }

                using (var con = _dbcontext.GetErpConnection())
                {
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PurchaseOrderMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                        cmd.Parameters.AddWithValue("@branchCd", 1);
                        cmd.Parameters.AddWithValue("@yearCd", usersessionDt.PubFYearCode);
                        cmd.Parameters.AddWithValue("@VNo", _dbHelper.Xnull(model.VNo));
                        cmd.Parameters.AddWithValue("@VType", "POMT");
                        cmd.Parameters.AddWithValue("@Docid", _dbHelper.Xnull(model.DocId));
                        //cmd.Parameters.AddWithValue("@DocDate", _dbHelper.Xnull(model.DocDate));
                        //cmd.Parameters.AddWithValue("@FromDt", _dbHelper.Xnull(model.FromDate));
                        //cmd.Parameters.AddWithValue("@ToDate", _dbHelper.Xnull(model.ToDate));
                        cmd.Parameters.AddWithValue("@DocDate", DbHelper.DbHelper.ConvertToDate(model.DocDate) ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FromDt", DbHelper.DbHelper.ConvertToDate(model.FromDate) ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ToDate", DbHelper.DbHelper.ConvertToDate(model.ToDate) ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@storeAmt", _dbHelper.Xnull(model.StoreAmt));
                        cmd.Parameters.AddWithValue("@CapitalAmt", _dbHelper.Xnull(model.CapitalAmt));
                        cmd.Parameters.AddWithValue("@Remark", _dbHelper.Xnull(model.Remarks));
                        cmd.Parameters.AddWithValue("@status", _dbHelper.Xnull(model.Status));
                        cmd.Parameters.AddWithValue("@Faprov_Remarks", "");
                        cmd.Parameters.AddWithValue("@Faprov_Status", "");
                        cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                        cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                        await con.OpenAsync();
                        int x = await cmd.ExecuteNonQueryAsync();

                    }
                }

                return Json(new { status = true, message = "Data update Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data update Failed" });
            }

        }

    }
}
