using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data;
using System.Globalization;
using System.Net.Mail;
using System.Reflection.Emit;
using System.Threading.Tasks;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Master;
using travelexpensemanagement.Models.Purchase.Transaction;
using static iTextSharp.text.pdf.AcroFields;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class QuotationRateApprovalController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public QuotationRateApprovalController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;

        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/Transaction/QuotationRateApproval/Index.cshtml");
        }

        public async Task<IActionResult> GetMaxVNo()
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var companyCode = userSession.PubCompCode;
                var yearCode = userSession.PubFYearCode;
                var branchCode = "1";
                var vType = "STAP";
                var tableName = "QUOTATION2";

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
                var docIdNoList = new { DocId = docId, VNo = newVno };
                return Json(new { status = true, data = docIdNoList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            try
            {
                var Doctype = await _dbHelper.GetJsonDataAsync("select CODE, NAME from DOCTYPE_MAST where isnull(DOCTYPE, '')='RateApproved' ");
                return Json(new { status = true, data = Doctype });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetItemName()
        {
            try
            {
                var parameters = new Dictionary<string, object>
                {
                { "@CompanyCode", _globalValue.GetGlobalVariables().PubCompCode }
                };

                var itemList = await _dbHelper.GetJsonFromProcedureAsync("dbo.Get_DdlList", parameters);

                return Json(new { status = true, data = itemList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVendorName()
        {
            try
            {
                var vendorList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from SUBGROUP_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode}  order by NAME ");
                return Json(new { status = true, data = vendorList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }

        }
 
       [HttpGet]
        public async Task<IActionResult> GetQuotRtApvrlDetailsById(string id)
        {
            try
            {
                var userSession = _globalValue.GetGlobalVariables();
                var parameters = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@DOC_ID", id },              
                { "@Action", "FilterDataByVNo" }
                };
                var result = await _dbHelper.GetJsonFromProcedureAsync("sp_QuotationRateApproval", parameters);
                var attachment = await _dbHelper.GetJsonDataAsync($@"select FILE_NAME,  FILE_Path  from IMG_TABLE  WHERE COMP_CODE = {userSession.PubCompCode} AND YEAR_CODE = {userSession.PubFYearCode} AND BRANCH_CODE = 1 and DOC_ID='{id}'  ");

                return Json(new
                {
                    status = true,
                    detail = result,
                    attachment=attachment
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetFilterItemdetails([FromBody] FilterItemload filtrItmModel)
        {
            try
            {
                var usersessionDt = _globalValue.GetGlobalVariables();
                var parameters = new Dictionary<string, object>
                {
                { "@COMP_CODE", usersessionDt.PubCompCode },             
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", "STQT"},
                { "@Action", "FilterData" }
                };

                if (filtrItmModel.FromDt != null && filtrItmModel.ToDt != null)
                {
                    parameters["@FromDate"] = filtrItmModel.FromDt.ToString();
                    parameters["@ToDate"] = filtrItmModel.ToDt.ToString();
                }
               
                if (filtrItmModel.groupCode > 0)
                    parameters["@GroupCode"] = filtrItmModel.groupCode;

                if (filtrItmModel.VendorList?.Any() == true)
                    parameters["@VendorList"] = string.Join(",", filtrItmModel.VendorList);

                if (filtrItmModel.ItemList?.Any() == true)
                    parameters["@ItemList"] = string.Join(",", filtrItmModel.ItemList);

                if (!string.IsNullOrWhiteSpace(filtrItmModel.SortBy))
                    parameters["@SortBy"] = filtrItmModel.SortBy;

                var result = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_QuotationRateApproval]", parameters);

                return Json(new
                {
                    status = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }
                
        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateQuotRateApproval([FromBody] QuotationRateApproval equotmodel)
        {
            if (equotmodel == null)
                return Json(new { status = false, message = " data save failed." });
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();               
                    DataTable equotationRtAprovlTable = FillDataTable(equotmodel.quotationRateApprovalDetail, "dbo.Type_Quotation2");
                    DataTable equotationRtAprovlAttachTable = FillDataTable(equotmodel.quotatRateApprovalAttachment, "[dbo].[IMG_TABLE]");

                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_QuotationRateApproval_AE]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Transaction = transaction;
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@V_NO", _dbHelper.Xnull(equotmodel.V_NO));
                                cmd.Parameters.AddWithValue("@V_TYPE", "STAP");
                                cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(equotmodel.V_DATE));
                                cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(equotmodel.V_DOCID));
                                cmd.Parameters.AddWithValue("@status", 1);
                                cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);

                                var tvp = cmd.Parameters.AddWithValue("@QuotationTable", equotationRtAprovlTable);
                                tvp.SqlDbType = SqlDbType.Structured;
                                tvp.TypeName = "dbo.Type_Quotation2";

                                var tvpI = cmd.Parameters.AddWithValue("@ImgTable", equotationRtAprovlAttachTable);
                                tvpI.SqlDbType = SqlDbType.Structured;
                                tvpI.TypeName = "[dbo].[IMG_TABLE]";

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
                                transaction.Commit();
                            else
                                transaction.Rollback();

                            return Json(new
                            {
                                status = success,
                                message = success ? "Data save/update successfully." : "Failed to save or update some employee details."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction?.Rollback();
                            return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        private DataTable FillDataTable<T>(List<T> data, string typeName)
        {
            int x = 1;
            DataTable QuotationRtApvlAttachTbl = ToEmptyDataTable(typeName);

            switch (typeName)
            {

                case "[dbo].[IMG_TABLE]":
                    var attachmentData = data as List<QuotatRateApprovalAttachment>;
                    if (attachmentData == null || !attachmentData.Any())
                    {
                        return QuotationRtApvlAttachTbl;
                    }

                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "attachments", "Purchase");

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    foreach (var attachment in attachmentData)
                    {
                        if (attachment.FileName != null && attachment.FileContentBase64 != null)
                        {
                            string sanitizedFileName = Path.GetFileName(attachment.FileName);
                            string fullPath = Path.Combine(folderPath, sanitizedFileName);
                            string relativePath = $"/attachments/Purchase/{sanitizedFileName}";

                            byte[] fileBytes = Convert.FromBase64String(attachment.FileContentBase64);
                            System.IO.File.WriteAllBytes(fullPath, fileBytes); 
                            attachment.FilePath = $"/attachments/Purchase/{sanitizedFileName}";

                            QuotationRtApvlAttachTbl.Rows.Add(
                                x,
                                relativePath,
                                sanitizedFileName,
                                x++
                            );
                           
                        }
                    }

                    break;

                case "dbo.Type_Quotation2":
                    foreach (var detail in data.Cast<QuotationRateApprovalDetail>())
                    {
                        QuotationRtApvlAttachTbl.Rows.Add(
                    _dbHelper.Xnull(detail.PARTY_CODE),
                    detail.ITEM_CODE,
                    detail.MAKE_CODE,
                    detail.TECH_DESC,
                    detail.UOM_CODE,
                    detail.REF_NO,
                    _dbHelper.FGetSmallDateTime(detail.REF_DATE),
                    detail.REF_TYPE,
                    detail.REF_DOCID,
                    detail.QTY,
                    detail.RATE,
                    detail.AMOUNT,
                    detail.PACK_PER,
                    detail.PACK_AMT,
                    detail.DISC_PER,
                    detail.DISC_AMT,
                    detail.FREIGHT,
                    detail.TAX_CODE,
                    detail.CGST_PER,
                    detail.CGST_AMT,
                    detail.SGST_PER,
                    detail.SGST_AMT,
                    detail.IGST_PER,
                    detail.IGST_AMT,
                    detail.VAT_PER,
                    detail.VAT_AMT,
                    detail.CESS_PER,
                    detail.CESS_AMT,
                    detail.OTH_EXPS,
                    detail.LD_RATE,
                    detail.NET_AMT,
                    detail.BULK_QTY,
                    detail.BULK_RATE,
                    detail.BULK_DISC_PER,
                    detail.BULK_DISC_AMT,
                    detail.WARRANTY,
                    detail.LEADTIME_DAYS,
                    detail.PURCHASER_REMARKS,
                    detail.PREORITY_LEVEL,
                    detail.RATE_MONTHLY,
                    detail.RATE_QUARTERLY,
                    detail.RATE_ANNUALY,
                    detail.RATE_SPECIAL,
                    detail.REQ_TYPE,
                    detail.REQ_NO,
                    detail.APROV_CODE,
                    detail.APROV_STATUS,
                    detail.APROV_REMARKS,
                    detail.FAPROV_STATUS,
                    detail.FAPROV_REMARKS,
                    _dbHelper.Xnull(detail.PACK_UR),
                    _dbHelper.Xnull(detail.DISC_UR),
                    detail.FREIGHT_UR,
                    detail.CGST_UR,
                    detail.SGST_UR,
                    detail.IGST_UR,
                    detail.OTHEXP_UR,
                    detail.BULKDISC_UR,
                    detail.AUTOPO_FLG,
                    detail.DOC_ID
                        );
                    }
                    break;

                default:
                    QuotationRtApvlAttachTbl = null;
                    break;
            }

            return QuotationRtApvlAttachTbl;

        }

        private DataTable ToEmptyDataTable(string typeName)
        {
            var dt = new DataTable();
            switch (typeName)
            {

                case "dbo.Type_Quotation2":
                    dt.Columns.Add("PARTY_CODE", typeof(int));
                    dt.Columns.Add("ITEM_CODE", typeof(int));
                    dt.Columns.Add("MAKE_CODE", typeof(int));
                    dt.Columns.Add("TECH_DESC", typeof(string));
                    dt.Columns.Add("UOM_CODE", typeof(int));
                    dt.Columns.Add("REF_NO", typeof(int));
                    dt.Columns.Add("REF_DATE", typeof(DateTime));
                    dt.Columns.Add("REF_TYPE", typeof(string));
                    dt.Columns.Add("REF_DOCID", typeof(string));
                    dt.Columns.Add("QTY", typeof(decimal));
                    dt.Columns.Add("RATE", typeof(decimal));
                    dt.Columns.Add("AMOUNT", typeof(decimal));
                    dt.Columns.Add("PACK_PER", typeof(decimal));
                    dt.Columns.Add("PACK_AMT", typeof(decimal));
                    dt.Columns.Add("DISC_PER", typeof(decimal));
                    dt.Columns.Add("DISC_AMT", typeof(decimal));
                    dt.Columns.Add("FREIGHT", typeof(decimal));
                    dt.Columns.Add("TAX_CODE", typeof(int));
                    dt.Columns.Add("CGST_PER", typeof(decimal));
                    dt.Columns.Add("CGST_AMT", typeof(decimal));
                    dt.Columns.Add("SGST_PER", typeof(decimal));
                    dt.Columns.Add("SGST_AMT", typeof(decimal));
                    dt.Columns.Add("IGST_PER", typeof(decimal));
                    dt.Columns.Add("IGST_AMT", typeof(decimal));
                    dt.Columns.Add("VAT_PER", typeof(decimal));
                    dt.Columns.Add("VAT_AMT", typeof(decimal));
                    dt.Columns.Add("CESS_PER", typeof(decimal));
                    dt.Columns.Add("CESS_AMT", typeof(decimal));
                    dt.Columns.Add("OTH_EXPS", typeof(decimal));
                    dt.Columns.Add("LD_RATE", typeof(decimal));
                    dt.Columns.Add("NET_AMT", typeof(decimal));
                    dt.Columns.Add("BULK_QTY", typeof(decimal));
                    dt.Columns.Add("BULK_RATE", typeof(decimal));
                    dt.Columns.Add("BULK_DISC_PER", typeof(decimal));
                    dt.Columns.Add("BULK_DISC_AMT", typeof(decimal));
                    dt.Columns.Add("WARRANTY", typeof(string));
                    dt.Columns.Add("LEADTIME_DAYS", typeof(int));
                    dt.Columns.Add("PURCHASER_REMARKS", typeof(string));
                    dt.Columns.Add("PREORITY_LEVEL", typeof(int));
                    dt.Columns.Add("RATE_MONTHLY", typeof(decimal));
                    dt.Columns.Add("RATE_QUARTERLY", typeof(decimal));
                    dt.Columns.Add("RATE_ANNUALY", typeof(decimal));
                    dt.Columns.Add("RATE_SPECIAL", typeof(decimal));
                    dt.Columns.Add("REQ_TYPE", typeof(string));
                    dt.Columns.Add("REQ_NO", typeof(int));
                    dt.Columns.Add("APROV_CODE", typeof(int));
                    dt.Columns.Add("APROV_STATUS", typeof(string));
                    dt.Columns.Add("APROV_REMARKS", typeof(string));
                    dt.Columns.Add("FAPROV_STATUS", typeof(string));
                    dt.Columns.Add("FAPROV_REMARKS", typeof(string));
                    dt.Columns.Add("PACK_UR", typeof(string));
                    dt.Columns.Add("DISC_UR", typeof(string));
                    dt.Columns.Add("FREIGHT_UR", typeof(string));
                    dt.Columns.Add("CGST_UR", typeof(string));
                    dt.Columns.Add("SGST_UR", typeof(string));
                    dt.Columns.Add("IGST_UR", typeof(string));
                    dt.Columns.Add("OTHEXP_UR", typeof(string));
                    dt.Columns.Add("BULKDISC_UR", typeof(string));
                    dt.Columns.Add("AUTOPO_FLG", typeof(int));
                    dt.Columns.Add("DOC_ID", typeof(string));
                    break;

                case "[dbo].[IMG_TABLE]":
                    dt.Columns.Add("ROWID", typeof(int));
                    dt.Columns.Add("FILE_Path", typeof(string));
                    dt.Columns.Add("FILE_NAME", typeof(string));
                    dt.Columns.Add("SRNO", typeof(int));
                    break;

                default:
                    throw new ArgumentException("Unknown table type: " + typeName);
            }
            return dt;
        }

    }
}
