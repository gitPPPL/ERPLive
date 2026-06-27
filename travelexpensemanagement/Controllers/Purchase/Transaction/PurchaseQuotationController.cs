using DocumentFormat.OpenXml.Office.Word;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Net.Mail;
using System.Reflection.Emit;
using System.Text.Json;
using System.Text.RegularExpressions;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.AddAttachmentService;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models;
using travelexpensemanagement.Models.Purchase.Transiction;
using travelexpensemanagement.Repositories.Implementations.Purchase.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseQuotationController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly FileHelper _filehelper;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IPurchaseQuotationRepository _purchaseQuotation;
        public PurchaseQuotationController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, GlobalValidationdate globalValidationdate, ModuleService.ModuleService moduleService, IPurchaseQuotationRepository purchaseQuotation)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _purchaseQuotation = purchaseQuotation;

        }

        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.GlobalVariables = globalVar;
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = globalVar.PubBranchCode;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/Purchase/Transaction/PurchaseQuotation/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetNextV_NO(string vType)
        {
            try
            {
                string vNo = _purchaseQuotation.GenerateVNo(vType);

                return Json(new
                {
                    v_NO = vNo
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message
                });
            }
        }

        //==========Global Dropdown============
        [HttpGet]
        public IActionResult GetDropdown(string type)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            string query = "";

            switch (type)
            {
                case "PARTY":
                    query = @"SELECT 
                            a.CODE,
                            a.NAME,
                            b.ADD1,
                            b.ADD2,
                            b.CITY_CODE,
                            b.GSTIN
                        FROM SUBGROUP_MAST a
                        LEFT JOIN SUBGROUP_ADDRESS b
                            ON a.COMP_CODE = b.COMP_CODE
                            AND a.CODE = b.CODE
                            AND b.IS_DEFAULT = 1
                        WHERE a.NATURE IN ('supplier')
                            AND a.COMP_CODE ='" + compCode +"' AND a.ACTIVE = 1 ORDER BY a.NAME";
                    break;

                case "STATUS":
                    query = @"SELECT CODE,NAME FROM DOCSTATUS_MAST WHERE V_TYPE = 'Document' ORDER BY CODE";
                    break;
                 
                case "PAYMENTTERM":
                    query = @"SELECT CODE,NAME FROM PAYTERM_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 ORDER BY NAME";
                    break;

                case "Currency":
                    query = @"Select Code,CURR_CODE + ' ' + SHORTNAME 'Name' from CURRENCY_MAST where ACTIVE=1";
                    break;

                default:
                    return Json(new List<object>());
            }

            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }

        public IActionResult GetDocTypeList()
        {
            string query = "SELECT CODE,NAME FROM DOCTYPE_MAST WHERE DOCTYPE= 'PurchaseQuotation' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult GetItemList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM ITEM_MAST WHERE COMP_CODE = '" + compCode + "' AND ACTIVE=1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);

            return Json(moduelList);
        }

        public IActionResult GetItemMakeList()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            string query = "SELECT CODE,NAME FROM ITEMMAKE_MAST WHERE COMP_CODE = '" + compCode + "' AND ACTIVE=1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);

            return Json(moduelList);
        }

        public IActionResult GetUOMList()
        {
            string query = "SELECT CODE,NAME FROM ITEMUNIT_MAST WHERE ACTIVE=1 ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public IActionResult GetMakeItemsByItemCode(int itemCode)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var makeItems = new List<object>();

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    string query = @"
                   SELECT DISTINCT
                        IMK.MAKE_CODE,
                        IMM.NAME AS MAKE_NAME,
                        IU.CODE AS UNIT_CODE,
                        IU.NAME AS UNIT_NAME
                    FROM ITEM_MAST IM

                    LEFT JOIN ITEM_MAKE IMK
                        ON IM.CODE = IMK.ITEM_CODE
                        AND IM.COMP_CODE = IMK.COMP_CODE

                    LEFT JOIN ITEMMAKE_MAST IMM
                        ON IMM.CODE = IMK.MAKE_CODE
                        AND IMM.COMP_CODE = IMK.COMP_CODE

                    LEFT JOIN ITEMUNIT_MAST IU
                        ON IU.CODE = IM.UNIT_CODE
                        AND IU.COMP_CODE = IM.COMP_CODE

                    WHERE IM.CODE = @ITEM_CODE
                        AND IM.COMP_CODE = @COMP_CODE

                    ORDER BY IMM.NAME";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                makeItems.Add(new
                                {
                                    MakeCode = reader["MAKE_CODE"] == DBNull.Value ? "" : reader["MAKE_CODE"].ToString(),
                                    MakeName = reader["MAKE_NAME"] == DBNull.Value ? "" : reader["MAKE_NAME"].ToString(),
                                    UnitCode = reader["UNIT_CODE"] == DBNull.Value ? "" : reader["UNIT_CODE"].ToString(),
                                    UnitName = reader["UNIT_NAME"] == DBNull.Value ? "" : reader["UNIT_NAME"].ToString()
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, data = makeItems });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving make items", error = ex.Message });
            }
        }
        
        [HttpGet]
        public IActionResult GetTextCodeList()
        {
            string query = "SELECT CODE,NAME FROM TAX_MAST WHERE ACTIVE=1 AND NAME<>'' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        [HttpGet]
        public IActionResult GetTaxDetails(int taxCode)
        {
            string query = @"
            SELECT
                PACK_ONBASIC,
                CGST_PER,
                SGST_PER,
                IGST_PER,
                VAT_PER
            FROM TAX_MAST
            WHERE CODE = @TaxCode";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@TaxCode", taxCode);

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return Json(new
                        {
                            success = true,
                            data = new
                            {
                                packOnBasic = Convert.ToInt32(reader["PACK_ONBASIC"]),
                                cgstPer = Convert.ToDecimal(reader["CGST_PER"]),
                                sgstPer = Convert.ToDecimal(reader["SGST_PER"]),
                                igstPer = Convert.ToDecimal(reader["IGST_PER"]),
                                vatPer = Convert.ToDecimal(reader["VAT_PER"])
                            }
                        });
                    }
                }
            }

            return Json(new { success = false });
        }

        public IActionResult GetTextRelatedDetailsTaxCode(int taxCode)
        {
            var taxDetails = new
            {
                TaxType = "",
                CgstPer = "",
                SgstPer = "",
                IgstPer = "",
                TdsPer = "",
                TcsPer = "",
                VatPer = "",
                OthPer = ""
            };

            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("Select TAX_TYPE,CGST_PER,SGST_PER,IGST_PER,TDS_PER,TCS_PER,VAT_PER,OTH_PER from TAX_MAST WHERE CODE = @taxCode", con))
                    {
                        cmd.Parameters.AddWithValue("@taxCode", taxCode);

                        con.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                taxDetails = new
                                {
                                    TaxType = reader["TAX_TYPE"]?.ToString(),
                                    CgstPer = reader["CGST_PER"]?.ToString(),
                                    SgstPer = reader["SGST_PER"]?.ToString(),
                                    IgstPer = reader["IGST_PER"]?.ToString(),
                                    TdsPer = reader["TDS_PER"]?.ToString(),
                                    TcsPer = reader["TCS_PER"]?.ToString(),
                                    VatPer = reader["VAT_PER"]?.ToString(),
                                    OthPer = reader["OTH_PER"]?.ToString()
                                };
                            }
                        }
                    }
                }

                return Json(new { success = true, taxDetails });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error retrieving next ID", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFullQuotationByVno(int vNo, string vType)
        {
            var result = await _purchaseQuotation.GetFullQuotationByVno(vNo, vType);

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuotation([FromBody] QuotationWrapper data)
        {
            var result = await _purchaseQuotation.SaveQuotation(data);

            return Json(new { success = result.Success, action = result.Message });
        }
         
        [HttpPost]
        public JsonResult DeletePurchaseQuotationByCode(int code, string vType, int compCode, int branchCode, int yearCode)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QUOTATION1_MGMT", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "DELETE");
                        cmd.Parameters.AddWithValue("@V_NO", code);
                        cmd.Parameters.AddWithValue("@V_TYPE", vType);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);
                        cmd.Parameters.AddWithValue("@BRANCH_CODE", branchCode);
                        cmd.Parameters.AddWithValue("@YEAR_CODE", yearCode);
                        cmd.Parameters.AddWithValue("@USERPC", Environment.MachineName);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Quotation deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CopyData(string actionType,DateTime? vDate)
        {
            var sw = Stopwatch.StartNew();
            var result = await _purchaseQuotation.CopyData(actionType,vDate);
            sw.Stop();
            Console.WriteLine($"Repository Time = {sw.ElapsedMilliseconds} ms");
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> CheckValidDate([FromBody] JsonElement data)
        {
            var global = _globalVariableService.GetGlobalVariables();
            DateTime vdate = data.GetProperty("vdate").GetDateTime();
            string vtype = data.GetProperty("vtype").GetString();
            string vno = data.GetProperty("vno").GetString();
            var result = await _globalValidationdate.CheckValidDate("QUOTATION1", vdate, vtype, vno);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseHistory(int itemcode)
        {
            var result = await _purchaseQuotation.GetPurchaseHistory(itemcode);

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseQuotation(int itemcode)
        {
            var result = await _purchaseQuotation.GetPurchaseQuotation(itemcode);

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> OrderHistory(int itemcode, DateTime? vDate)
        {
            try
            {
                var result = await _purchaseQuotation.OrderHistory(itemcode, vDate);

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int vNo, string vType)
        {
            try
            {
                byte[] fileBytes = await _purchaseQuotation.ExportToExcel(vNo, vType);

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Quotation_{vType}_{vNo}.xlsx");
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult GetLastOrderRate(int itemCode, DateTime vDate)
        {
            try
            {
                decimal rate = _purchaseQuotation.GetLastOrderRate(itemCode, vDate);

                return Json(new
                {
                    success = true,
                    lastRate = rate
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
