using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text;
using travelexpensemanagement.Controllers.AddAttachmentService;
using travelexpensemanagement.Controllers.DropdownService;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace travelexpensemanagement.Controllers.Purchase.Transaction
{
    public class PurchaseBillPassEntryDirectController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly FileHelper _filehelper;
        public PurchaseBillPassEntryDirectController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Controllers.DropdownService.DropdownService dropdownService, travelexpensemanagement.DbHelper.DbHelper dbHelper,
    ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            ViewBag.CompCode = globalVar.PubCompCode;
            ViewBag.BranchCode = 1;
            ViewBag.YearCode = globalVar.PubFYearCode;
            return View("~/Views/Purchase/Transaction/PurchaseBillPassEntryDirect/Index.cshtml");
        }

        public int GetNextV_NO(string yearCode)
        {
            string newV_NO = "00000";

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                con.Open();

                // Execute query to get PREFIXYR
                string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = '" + yearCode + "'";
                SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";

                // Execute query to get last V_NO
                string lastV_NO_Query = "SELECT TOP 1 V_NO FROM PURCHASE1 ORDER BY V_NO DESC";
                SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                string lastV_NO = lastVnoCmd.ExecuteScalar()?.ToString();

                int lastNumber = 0;
                if (!string.IsNullOrEmpty(lastV_NO) && lastV_NO.Length >= 9)
                {
                    string numericPart = lastV_NO.Substring(lastV_NO.Length - 5);
                    int.TryParse(numericPart, out lastNumber);
                }

                // Increment and format the new V_NO
                string newRunningNo = (lastNumber + 1).ToString("D5");
                newV_NO = prefixYR + newRunningNo;
            }

            return Convert.ToInt32(newV_NO);
        }

        public IActionResult GetDocTypeList()
        {
            string query = "SELECT CODE,NAME FROM DOCTYPE_MAST WHERE DOCTYPE= 'HighSeaPurchase' ORDER BY NAME DESC";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult GetStatusList()
        {
            string query = "SELECT CODE,NAME FROM DOCSTATUS_MAST WHERE V_TYPE = 'Document' ORDER BY NAME";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult GetPoList(int cCode, int yCode, int bCode)
        {
            string query = "SELECT V_TYPE,DOC_ID FROM PO_MAST WHERE COMP_CODE = '" + cCode + "' AND YEAR_CODE='" + yCode + "' AND BRANCH_CODE='" + bCode + "'";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult GetBillToList(int cCode)
        {
            string query = "SELECT CODE,NAME FROM SUBGROUP_MAST WHERE COMP_CODE='" + cCode + "' AND NATURE='Supplier' AND ACTIVE=1 ORDER BY NAME ";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetShipToList(int cCode)
        {
            string query = "SELECT CODE,NAME FROM SUBGROUP_MAST WHERE COMP_CODE='" + cCode + "' AND NATURE='Supplier' AND ACTIVE=1 ORDER BY NAME ";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }

        public IActionResult GetTransitNoByParty(int cCode, int bCode, int yCode, int pCode)
        {
            //var query = "SELECT V_NO,DOC_ID FROM WAYBILL1 WHERE  COMP_CODE='" + cCode + "' AND YEAR_CODE='" + yCode + "' AND BRANCH_CODE='" + bCode + "' AND PARTY_CODE='" + pCode + "' ";
            var queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT V_NO,DOC_ID FROM WAYBILL1 ");
            queryBuilder.Append("WHERE COMP_CODE='").Append(cCode).Append("' ");
            queryBuilder.Append("AND YEAR_CODE='").Append(yCode).Append("' ");
            queryBuilder.Append("AND BRANCH_CODE='").Append(bCode).Append("' ");
            queryBuilder.Append("AND PARTY_CODE='").Append(pCode).Append("'");
            string query = queryBuilder.ToString();

            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetAddressListByBillToParty(int cCode, int pCode)
        {
            var query = "SELECT ADDRESS_ID,ADD1 FROM [SUBGROUP_ADDRESS] WHERE  COMP_CODE='" + cCode + "' AND CODE='" + pCode + "'";
            var moduelList = _dropdownService.GetDropdownList(query);
            return Json(moduelList);
        }
        public IActionResult GetAddressByBillToParty(int cCode, int pCode, int addressId)
        {
            var addressDetails = new
            {
                add1 = "",
                add2 = "",
                add3 = "",
                pincode = "",
                gstin = ""
            };
            try
            {
                using (SqlConnection connection = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("Select ADD1,ADD2,ADD3,PINCODE,GSTIN from SUBGROUP_ADDRESS where COMP_CODE = @COMP_CODE AND Code = @PCODE AND ADDRESS_ID = @ADDRESSID", connection))
                    {
                        cmd.Parameters.AddWithValue("@COMP_CODE", cCode);
                        cmd.Parameters.AddWithValue("@PCODE", pCode);
                        cmd.Parameters.AddWithValue("@ADDRESSID", addressId);
                        connection.Open();
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                addressDetails = new
                                {
                                    add1 = reader["ADD1"].ToString(),
                                    add2 = reader["ADD2"].ToString(),
                                    add3 = reader["ADD3"].ToString(),
                                    pincode = reader["PINCODE"].ToString(),
                                    gstin = reader["GSTIN"].ToString(),
                                };

                            }
                        }
                    }
                }
                return Json(new { success = true, addressDetails });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error retrieving the address by specfic address id" });
            }
        }
    }
}
