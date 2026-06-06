using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Inventory.Master;
using travelexpensemanagement.Models.QualityControl.Master;
using static iTextSharp.text.pdf.AcroFields;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class QCDiscMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        private int? userLevel;
        public QCDiscMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/QCDiscMaster/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetDropdown(string type)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            string query = "";

            switch (type)
            {
                case "ITEM":
                    query = @"SELECT CODE, NAME FROM ITEM_MAST WHERE COMP_CODE = '" + compCode + @"' AND ACTIVE = 1 ORDER BY NAME";
                    break;

                case "PARAMETER":
                    query = @"SELECT CODE, NAME FROM QCP_MAST WHERE COMP_CODE = '" + compCode + @"' AND ACTIVE = 1  ORDER BY NAME";
                    break;

                default:
                    return Json(new List<object>());
            }

            var list = _dropdownService.GetDropdownList(query);
            return Json(list);
        }

        [HttpPost]
        public IActionResult SaveAndUpdateData([FromBody] QCDISC_MAST model)
        {
            var globalVaribales = _globalVariableService.GetGlobalVariables();
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    con.Open();

                    cmd.Parameters.AddWithValue("@COMP_CODE", globalVaribales.PubCompCode);
                    cmd.Parameters.AddWithValue("@V_TYPE", "QDIS");
                    cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE);
                    cmd.Parameters.AddWithValue("@ITEM_NAME", model.ITEM_NAME ?? string.Empty);
                    cmd.Parameters.AddWithValue("@QCP_CODE", model.QCP_CODE);
                    cmd.Parameters.AddWithValue("@QCP_DIFF", model.QCP_DIFF);
                    cmd.Parameters.AddWithValue("@UUSER", globalVaribales.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", globalVaribales.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", globalVaribales.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@Action", model.ACTION ?? "INSERT");

                    cmd.ExecuteNonQuery();

                }
                return Json(new { success = true, message = "Data saved successfully." });   
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving data", error = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetQcDiscOnChange(int itemCode)
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
            var discList = new List<QCDISC_MAST>();
            int totalCount = 0;
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Action", "ONLOAD");
                        cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
                        cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

                        con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                discList.Add(new QCDISC_MAST
                                {
                                    COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
                                    V_TYPE = reader["V_TYPE"]?.ToString(),
                                    ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
                                    ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
                                    QCP_CODE = reader["QCP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["QCP_CODE"]) : 0,
                                    QCP_DIFF = reader["QCP_DIFF"] != DBNull.Value ? Convert.ToDecimal(reader["QCP_DIFF"]) : 0,
                                    UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
                                    UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
                                    EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
                                    EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
                                    AED = reader["AED"]?.ToString(),
                                    WSID = reader["WSID"]?.ToString(),
                                    LIP = reader["LIP"]?.ToString(),
                                    LID = reader["LID"]?.ToString(),
                                    SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0,
                                    ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
                                });
                            }

                            if (reader.NextResult() && reader.Read())
                            {
                                totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
                            }
                        }
                    }
                }

                return Json(new { success = true, lists = discList, totalCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching Qc discount ", error = ex.Message });
            }
        }

        //public IActionResult GetItemToList()
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        //    string query = "SELECT CODE,NAME FROM ITEM_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 ORDER BY NAME DESC";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}

        //public IActionResult GetItemFromList()
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        //    string query = "SELECT CODE,NAME FROM ITEM_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 ORDER BY NAME DESC";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}

        //public IActionResult GetParameterList()
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        //    string query = "SELECT CODE,NAME FROM QCP_MAST WHERE COMP_CODE='" + compCode + "' AND ACTIVE=1 ORDER BY NAME DESC";
        //    var moduelList = _dropdownService.GetDropdownList(query);
        //    return Json(moduelList);
        //}


        //public JsonResult GetQcDiscOnChange(int itemCode)
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
        //    var discList = new List<QCDISC_MAST>();
        //    int totalCount = 0;
        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@Action", "ONLOAD");
        //                cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

        //                con.Open();

        //                using (SqlDataReader reader = cmd.ExecuteReader())
        //                {
        //                    while (reader.Read())
        //                    {
        //                        discList.Add(new QCDISC_MAST
        //                        {
        //                            COMP_CODE = reader["COMP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COMP_CODE"]) : 0,
        //                            V_TYPE = reader["V_TYPE"]?.ToString(),
        //                            ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
        //                            ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
        //                            QCP_CODE = reader["QCP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["QCP_CODE"]) : 0,
        //                            QCP_DIFF = reader["QCP_DIFF"] != DBNull.Value ? Convert.ToDecimal(reader["QCP_DIFF"]) : 0,
        //                            UUSER = reader["UUSER"] != DBNull.Value ? Convert.ToInt32(reader["UUSER"]) : 0,
        //                            UDATE = reader["UDATE"] != DBNull.Value ? Convert.ToDateTime(reader["UDATE"]) : DateTime.MinValue,
        //                            EUSER = reader["EUSER"] != DBNull.Value ? Convert.ToInt32(reader["EUSER"]) : 0,
        //                            EDATE = reader["EDATE"] != DBNull.Value ? Convert.ToDateTime(reader["EDATE"]) : DateTime.MinValue,
        //                            AED = reader["AED"]?.ToString(),
        //                            WSID = reader["WSID"]?.ToString(),
        //                            LIP = reader["LIP"]?.ToString(),
        //                            LID = reader["LID"]?.ToString(),
        //                            SRNO = reader["SRNO"] != DBNull.Value ? Convert.ToInt32(reader["SRNO"]) : 0,
        //                            ACTIVE = reader["ACTIVE"] != DBNull.Value ? Convert.ToInt32(reader["ACTIVE"]) : 0,
        //                        });
        //                    }

        //                    if (reader.NextResult() && reader.Read())
        //                    {
        //                        totalCount = reader["TotalCount"] != DBNull.Value ? Convert.ToInt32(reader["TotalCount"]) : 0;
        //                    }
        //                }
        //            }
        //        }

        //        return Json(new { success = true, lists = discList, totalCount });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Error fetching Qc discount ", error = ex.Message });
        //    }
        //}

        //[HttpPost]
        //public JsonResult ImportData([FromBody] ImportQcDiscDto request)
        //{
        //    //var itemN = getItemNameByCode(request.ItemCode);
        //    //if (IsDuplicateQcDesc(itemN))
        //    //{
        //    //    return Json(new { success = false, message = "Item Qc desc already exists. You need to edit." });
        //    //}
        //    //var itemN = request.QcDiscList.FirstOrDefault().ITEM_NAME;
        //    //if (string.IsNullOrWhiteSpace(itemN))
        //    //{
        //    //    return Json(new { success = false, message = "Item name cannot be blank." });
        //    //}

        //    var itemCode = request.ItemCode;
        //    var globalVar = _globalVariableService.GetGlobalVariables();
        //    try
        //    {
        //        DataTable qcDiscTable = new DataTable();
        //        qcDiscTable.Columns.Add("COMP_CODE", typeof(int));
        //        qcDiscTable.Columns.Add("V_TYPE", typeof(string));
        //        qcDiscTable.Columns.Add("ITEM_CODE", typeof(int));
        //        qcDiscTable.Columns.Add("ITEM_NAME", typeof(string));
        //        qcDiscTable.Columns.Add("QCP_CODE", typeof(int));
        //        qcDiscTable.Columns.Add("QCP_DIFF", typeof(decimal));
        //        qcDiscTable.Columns.Add("UUSER", typeof(int));
        //        qcDiscTable.Columns.Add("UDATE", typeof(DateTime));
        //        qcDiscTable.Columns.Add("EUSER", typeof(int));
        //        qcDiscTable.Columns.Add("EDATE", typeof(DateTime));
        //        qcDiscTable.Columns.Add("AED", typeof(string));
        //        qcDiscTable.Columns.Add("WSID", typeof(string));
        //        qcDiscTable.Columns.Add("LIP", typeof(string));
        //        qcDiscTable.Columns.Add("LID", typeof(string));
        //        qcDiscTable.Columns.Add("ACTIVE", typeof(int));

        //        DateTime now = DateTime.Now;
        //        DateTime safeNow = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

        //        foreach (var item in request.QcDiscList)
        //        {
        //            qcDiscTable.Rows.Add(
        //                globalVar.PubCompCode,
        //                "QDIS",
        //                itemCode,
        //                item.ITEM_NAME,
        //                item.QCP_CODE,
        //                item.QCP_DIFF,
        //                globalVar.PubUserId,
        //                safeNow,
        //                item.EUSER,
        //                safeNow,
        //                item.AED,
        //                globalVar.PubWorkStationID ?? "WEB",
        //                globalVar.PubLocalId ?? "127.0.0.1",
        //                Environment.MachineName ?? "WEB",
        //                1
        //            );
        //        }

        //        using (SqlConnection conn = _dbConnection.GetErpConnection())
        //        using (SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", conn))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@Action", "INSERT");
        //            cmd.Parameters.AddWithValue("@ITEM_CODE", itemCode);
        //            cmd.Parameters.AddWithValue("@QCDISC_TABLE", qcDiscTable);

        //            conn.Open();
        //            cmd.ExecuteNonQuery();
        //        }

        //        return Json(new { success = true, message = "Data imported successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        //public JsonResult DeleteQcDiscByCode(int code)
        //{
        //    var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

        //    try
        //    {
        //        using (SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            using (SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con))
        //            {
        //                cmd.CommandType = CommandType.StoredProcedure;
        //                cmd.Parameters.AddWithValue("@Action", "DELETE");
        //                cmd.Parameters.AddWithValue("@ITEM_CODE", code);
        //                cmd.Parameters.AddWithValue("@COMP_CODE", compCode);

        //                con.Open();
        //                cmd.ExecuteNonQuery();
        //            }
        //        }

        //        return Json(new { success = true, message = " deleted successfully." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}

        //private string getItemNameByCode(int Code)  
        //{
        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        using (SqlCommand cmd = new SqlCommand("SELECT ITEM_NAME FROM QCDISC_MAST WHERE ITEM_CODE= @ITEM_CODE", con))
        //        {
        //            cmd.Parameters.AddWithValue("@ITEM_CODE", Code);

        //            con.Open();
        //            object result = cmd.ExecuteScalar();
        //            if (result != null && result != DBNull.Value)
        //                return result.ToString();
        //            else
        //                return null;
        //        }
        //    }
        //}

        //private bool IsDuplicateQcDesc(string itemName)
        //{
        //    if (string.IsNullOrWhiteSpace(itemName))
        //    {
        //        return false;
        //    }

        //    using (SqlConnection con = _dbConnection.GetErpConnection())
        //    {
        //        using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM MACHINE_MAST WHERE ITEM_NAME = @ITEM_NAME", con))
        //        {
        //            cmd.Parameters.AddWithValue("@ITEM_NAME", itemName.Trim());

        //            con.Open();
        //            int count = (int)cmd.ExecuteScalar();
        //            return count > 0;
        //        }
        //    }
        //}


    }
}
