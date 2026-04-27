using System.Data;
using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;

namespace travelexpensemanagement.Controllers.Admin.SystemInitilization
{
    public class DocumentTypeMasterController : Controller
    {
        
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalVal;

        public DocumentTypeMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalVal)
        {
            _dbcontext = dbcontext;
            _dbHelper = dbHelper;
            _globalVal = globalVal;
        }
        public IActionResult Index()
        {
            return View("~/Views/Admin/SystemInitilization/DocumentTypeMaster/Index.cshtml");
        }

        [HttpGet]
        public JsonResult getdocTypeExitOrNot(string doctype)
        {
            try
            {
                bool isExist = false;
                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = @"
                       SELECT CASE 
                        WHEN EXISTS (
                            SELECT 1 
                            FROM DOCTYPE_MAST 
                            WHERE UPPER(ISNULL(CODE, '')) = UPPER(@doctype)
                        ) 
                        THEN 1 ELSE 0 END";
                        cmd.Parameters.AddWithValue("@doctype", doctype);
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


        [HttpGet]
        public async Task<JsonResult> GettransactionDt()
        {
            try
            {
                var transDt = new List<object>();
                DataTable dt = new DataTable();
                var con = _dbcontext.GetErpConnection();
                dt = await _dbHelper.ExecuteQueryAsync("select distinct code, NAME from DOCTYPE_MAST order by name");
                foreach (DataRow row1 in dt.Rows)
                {
                    transDt.Add(new { code = row1["code"].ToString(), name = row1["name"].ToString() });
                }

                return Json(new { status = true, data = transDt });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, data = "Data Load Failed" + ex.Message });
            }
        }

        public class DocumentType
        {

            public string DocType { get; set; }

            public string DocName { get; set; }

            public string TransactionType { get; set; }

            public string? StockMethod { get; set; }

            public int? Sno { get; set; }

            public int Active { get; set; }
        }

        [HttpPost]
        public JsonResult SaveDocumentMastDt([FromBody] DocumentType doctype)
        {
            try
            {

                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[DocumentTypeMast_AED]", con))
                    {

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "A");
                        cmd.Parameters.AddWithValue("@doctype", _dbHelper.Xnull(doctype.DocType));
                        cmd.Parameters.AddWithValue("@docname", _dbHelper.Xnull(doctype.DocName));
                        cmd.Parameters.AddWithValue("@transtype", _dbHelper.Xnull(doctype.TransactionType));
                        cmd.Parameters.AddWithValue("@stock", _dbHelper.Xnull(doctype.StockMethod));
                        cmd.Parameters.AddWithValue("@sno", _dbHelper.Xnull(doctype.Sno));
                        cmd.Parameters.AddWithValue("@lip", _globalVal.GetGlobalVariables().PubLocalId);
                        cmd.Parameters.AddWithValue("@active", doctype.Active);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { status = true, message = "Data Save Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data not saved" + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateDoctypemastDt([FromBody] DocumentType doctype)
        {
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("[dbo].[DocumentTypeMast_AED]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@AED", "E");
                        cmd.Parameters.AddWithValue("@doctype", _dbHelper.Xnull(doctype.DocType));
                        cmd.Parameters.AddWithValue("@docname", _dbHelper.Xnull(doctype.DocName));
                        cmd.Parameters.AddWithValue("@transtype", _dbHelper.Xnull(doctype.TransactionType));
                        cmd.Parameters.AddWithValue("@stock", _dbHelper.Xnull(doctype.StockMethod));
                        cmd.Parameters.AddWithValue("@sno", _dbHelper.Xnull(doctype.Sno));
                        cmd.Parameters.AddWithValue("@lip", _globalVal.GetGlobalVariables().PubLocalId);                     
                        cmd.Parameters.AddWithValue("@active", doctype.Active);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { status = true, message = "Data Update Successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Not Update" + ex.Message });
            }
        }


    }
}
