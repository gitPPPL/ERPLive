using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class ScrapSaleSaudaEntryController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ScrapSaleSaudaEntryController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
          DropdownService dropdownService, DbHelper dbHelper,
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
            TempData["LoginDate"] = _globalVariableService.GetGlobalVariables().PubLoginDate;
            return View("~/Views/Sales/Transaction/ScrapSaleSaudaEntry/Index.cshtml");
        }

        public JsonResult GetVNo()
        {
            string newV_NO = "00000";
            try
            {
                var getdata = _globalVariableService.GetGlobalVariables();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                {

                    con.Open();
                    string prefixYRQuery = "SELECT PREFIXYR FROM YEAR_MAST WHERE CODE = @YearCode";
                    SqlCommand prefixCmd = new SqlCommand(prefixYRQuery, con);
                    prefixCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    string prefixYR = prefixCmd.ExecuteScalar()?.ToString() ?? "0000";
                    string lastV_NO_Query = "select max(V_no) from SAUDA where V_TYPE=@V_TYPE and COMP_CODE= @CompCode and BRANCH_CODE= @BRANCH_CODE and YEAR_CODE= @YearCode  ";
                    SqlCommand lastVnoCmd = new SqlCommand(lastV_NO_Query, con);
                    lastVnoCmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                    lastVnoCmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                    lastVnoCmd.Parameters.AddWithValue("@V_TYPE", "SCUD");
                    lastVnoCmd.Parameters.AddWithValue("@BRANCH_CODE", getdata.PubBranchCode);
                    object result = lastVnoCmd.ExecuteScalar();
                    if (result != DBNull.Value && result != null)
                    {
                        int lastV_NO = Convert.ToInt32(result);
                        newV_NO = (lastV_NO + 1).ToString("D5");
                    }
                    else
                    {
                        newV_NO = prefixYR + "00001";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in GetVNo: {ex.Message}");
                return Json(new { error = "An error occurred while generating the V_NO." });
            }

            return Json(new { V_NO = newV_NO });
        }

        public JsonResult DDlRefNo()
        {
            var getdata = _globalVariableService.GetGlobalVariables();

            var refNoList = new List<string>();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                string query = "select distinct a.V_NO from avail_scrapstk1 a " +
                    "left  join avail_scrapstk2 b on a.v_type=b.v_type and a.v_no=b.v_no and a.comp_code=b.comp_code " +
                    "and a.branch_code=b.branch_code and a.year_code=b.year_code where  a.comp_code="+ getdata.PubCompCode + "" +
                    "and a.branch_code="+ getdata.PubBranchCode + " and a.Year_code="+ getdata.PubFYearCode + " order by a.v_no";

                con.Open();

                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        refNoList.Add(reader["V_NO"].ToString());
                    }
                }
            }
            return Json(refNoList);
        }

        public JsonResult GetDatabyCoustomercode(int customercode)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            var result = new object();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @" 
                   SELECT a.Code,  a.NAME,  a.ADD1, a.ADD2,  a.ADD3,  a.CITY_CODE,  b.NAME AS CityName,  a.MOBILE,   a.Nature
                    FROM SUBGROUP_MAST a LEFT JOIN CITY_MAST b ON a.CITY_CODE = b.CODE WHERE a.Active = 1 AND a.comp_code = "+  getdata.PubCompCode +" and  a.Code = "+ customercode + " ; ";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows && reader.Read()) 
                            {
                                result = new
                                {
                                    Code = reader["Code"].ToString(),
                                    Name = reader["NAME"].ToString(),
                                    ADD1 = reader["ADD1"]?.ToString(),
                                    ADD2 = reader["ADD2"]?.ToString(),
                                    ADD3 = reader["ADD3"]?.ToString(),
                                    citycode = reader["CITY_CODE"] != DBNull.Value ? Convert.ToInt32(reader["CITY_CODE"]) : 0,
                                    City = reader["CityName"]?.ToString(),
                                    MOBILE = reader["MOBILE"]?.ToString(),
                                    Nature = reader["Nature"]?.ToString()
                               
                                };
                            }
                            else
                            {
                                result = new { Message = "No data found." };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result = new { Error = ex.Message };
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
            return Json(result);
        }

        public JsonResult DDlcustomerName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select a.Code ,a.NAME from SUBGROUP_MAST a  " +
                    "left join  CITY_MAST b on a.CITY_CODE = b.CODE left join  COUNTRY_MAST c on b.COUNTRY_CODE = c.CODE " +
                    "where a.Active=1 and a.comp_code= " + getdata.PubCompCode + " ";
                var DDlcustomerName = _dropdownService.GetDropdownList(query);
                return Json(DDlcustomerName);
            }
        }

        public JsonResult DDlCityName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE , NAME from CITY_MAST where ACTIVE =1 ";
                var DDlCityName = _dropdownService.GetDropdownList(query);
                return Json(DDlCityName);
            }

        }
        public JsonResult DDlDeliveryStation()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from CITY_MAST Order by NAME ";
                var DDlCityName = _dropdownService.GetDropdownList(query);
                return Json(DDlCityName);
            }

        }
        public JsonResult DDlItemname(int RefNo)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select  b.CODE,b.NAME from AVAIL_SCRAPSTK2 a left join ITEM_MAST b on a.Item_code=b.code and a.comp_code=b.comp_code " +
                    " where a.V_type='ALST' and a.V_No= "+ RefNo + "  and  a.comp_code="+ getdata.PubCompCode +" and  a.Branch_code="+ getdata.PubBranchCode +" ";
                var DDlItemname = _dropdownService.GetDropdownList(query);
                return Json(DDlItemname);
            }
        }

        public JsonResult DDlCurrency()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select code ,NAME from CURRENCY_MAST";
                var DDlCurrency = _dropdownService.GetDropdownList(query);
                return Json(DDlCurrency);
            }
        }

        public JsonResult DDlPaymentTerm()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE, NAME  from PAYTERM_MAST where comp_code="+  getdata.PubCompCode +" ORDER BY NAME";
                var DDlPaymentTerm = _dropdownService.GetDropdownList(query);
                return Json(DDlPaymentTerm);
            }
        }
        public JsonResult ddlFreightTerm()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "select CODE, NAME  from PAYTERM_MAST where comp_code=" + getdata.PubCompCode + " ORDER BY NAME";
                var ddlFreightTerm = _dropdownService.GetDropdownList(query);
                return Json(ddlFreightTerm);
            }
        }

        public JsonResult ddlItemType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCTYPE_MAST where CODE in ('SCUD') order by Name";
                var ddlItemType = _dropdownService.GetDropdownList(query);
                return Json(ddlItemType);
            }
        }
        public JsonResult ddlStatus()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCSTATUS_MAST where V_TYPE='Document' Order by CODE";
                var ddlStatus = _dropdownService.GetDropdownList(query);
                return Json(ddlStatus);
            }
        }
        public JsonResult GetDatabyItemcode( int customercode)
        {
            var result = new object();
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @"SELECT  a.Nature  FROM  SUBGROUP_MAST a  WHERE  a.Active = 1   AND a.comp_code = @comp_code and code =@customercode ;";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                 
                    cmd.Parameters.AddWithValue("@customercode", customercode);
                    cmd.Parameters.AddWithValue("@Branch_code", getdata.PubBranchCode);
                    cmd.Parameters.AddWithValue("@comp_code", getdata.PubCompCode);

                    try
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows && reader.Read())
                            {
                                result = new
                                {
                                    Nature = reader["Nature"].ToString()
                                };
                            }
                            else
                            {
                                result = new { Message = "No data found." };
                            }
                        }
                    }
                    catch (SqlException sqlEx)
                    {                     
                        result = new { Error = "SQL Error: " + sqlEx.Message };
                    }
                    catch (Exception ex)
                    {                      
                        result = new { Error = ex.Message };
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }

            return Json(result);
        }
    }
}
