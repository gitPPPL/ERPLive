using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Purchase.EPR
{
    public class EPRReconciliationController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public EPRReconciliationController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,DropdownService dropdownService,
            DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate, LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _logService = logService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Purchase/EPR/EPRReconciliation/Index.cshtml");
        }
        [HttpGet]
        public JsonResult GetDropdown(string type)
        {
            try
            {
                var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;
                string query = "";
                switch (type.ToLower())
                {
                    case "doctype":
                        query = @"SELECT CODE, NAME FROM DOCTYPE_MAST WHERE DOCTYPE = 'EPRRECO' ORDER BY CODE";
                        break;
                    case "city":
                        query = @"SELECT CODE, NAME FROM CITY_MAST WHERE Active = 1 ORDER BY NAME";
                        break;
                    case "state":
                        query = @"SELECT CODE, NAME FROM STATE_MAST WHERE Active = 1 ORDER BY NAME";
                        break;
                    case "partyname":
                        query = $@"SELECT CODE, NAME FROM SUBGROUP_MAST WHERE COMP_CODE = {compCode} AND Active = 1 ORDER BY NAME";
                        break;
                    case "allotparty":
                        query = $@"SELECT DISTINCT ISNULL(ALLOTED_PARTY, 0) AS CODE, ISNULL(ALLOTED_PARTYNAME, '') AS NAME
                        FROM EPR_RECO WHERE COMP_CODE = {compCode} ORDER BY ISNULL(ALLOTED_PARTYNAME, '')";
                        break;
                    case "ref1":
                        query = $@"SELECT DISTINCT ISNULL(REF_NO1, '') AS CODE, ISNULL(REF_NO1, '') AS NAME FROM EPR_RECO
                        WHERE COMP_CODE = {compCode} ORDER BY ISNULL(REF_NO1, '')";
                        break;
                    case "ref2":
                        query = $@" SELECT DISTINCT ISNULL(REF_NO2, '') AS CODE, ISNULL(REF_NO2, '') AS NAME FROM EPR_RECO
                        WHERE COMP_CODE = {compCode} ORDER BY ISNULL(REF_NO2, '')";
                        break;
                    default:
                        return Json(new
                        {
                            success = false,
                            message = "Invalid dropdown type."
                        });
                }
                var list = _dropdownService.GetDropdownList(query);
                return Json(list);
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
        public JsonResult GetNextDocNo(string vType)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                string query = @"SELECT ISNULL(MAX(V_NO), 0) + 1 AS NextVNo FROM EPR_RECO  WHERE V_TYPE = @VType
                AND COMP_CODE = @CompCode AND BRANCH_CODE = @BranchCode AND YEAR_CODE = @YearCode";
                int nextVNo = 1;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@VType", SqlDbType.NVarChar, 10).Value = vType ?? "";
                    cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = gv.PubCompCode;
                    cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = gv.PubBranchCode;
                    cmd.Parameters.Add("@YearCode", SqlDbType.Int).Value = gv.PubFYearCode;
                    con.Open();
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        nextVNo = Convert.ToInt32(result);
                    }
                }
                return Json(new
                {
                    success = true,
                    nextVNo = nextVNo
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
        public JsonResult GetEPRList(string? fromDate, string? toDate, string? hsn, string? partyCode, string? cityCode, string? stateCode,
        string? docType, string? allotParty, bool ignorePartyShip = false, string? itemType = "", int actionTag = 0, int vNo = 0)
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                if (!DateTime.TryParse(fromDate, out DateTime dtFrom))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid From Date."
                    });
                }
                if (!DateTime.TryParse(toDate, out DateTime dtTo))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid To Date."
                    });
                }
                if (dtFrom.Date > dtTo.Date)
                {
                    return Json(new
                    {
                        success = false,
                        message = "From Date cannot be greater than To Date."
                    });
                }
                docType = "EPRR";
                int? party = null;
                int? city = null;
                int? state = null;
                int? allot = null;

                if (!string.IsNullOrWhiteSpace(partyCode) && partyCode != "0" && partyCode != "-1")
                {
                    if (int.TryParse(partyCode, out int partyValue))
                    {
                        party = partyValue;
                    }
                }
                if (!string.IsNullOrWhiteSpace(cityCode) && cityCode != "0" && cityCode != "-1")
                {
                    if (int.TryParse(cityCode, out int cityValue))
                    {
                        city = cityValue;
                    }
                }
                if (!string.IsNullOrWhiteSpace(stateCode) && stateCode != "0" &&  stateCode != "-1")
                {
                    if (int.TryParse(stateCode, out int stateValue))
                    {
                        state = stateValue;
                    }
                }
                if (!string.IsNullOrWhiteSpace(allotParty) && allotParty != "0" && allotParty != "-1")
                {
                    if (int.TryParse(allotParty, out int allotValue))
                    {
                        allot = allotValue;
                    }
                }
                var list = new List<object>();

                using (SqlConnection con = _dbConnection.GetErpConnection())
                using (SqlCommand cmd = new SqlCommand("Sp_GetEPRList", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = dtFrom.Date;
                    cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = dtTo.Date;
                    cmd.Parameters.Add("@HSN", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(hsn) ? DBNull.Value : hsn.Trim();
                    cmd.Parameters.Add("@PartyCode", SqlDbType.Int).Value = party.HasValue ? party.Value : DBNull.Value;
                    cmd.Parameters.Add("@CityCode", SqlDbType.Int).Value = city.HasValue ? city.Value : DBNull.Value;
                    cmd.Parameters.Add("@StateCode", SqlDbType.Int).Value = state.HasValue ? state.Value : DBNull.Value;
                    cmd.Parameters.Add("@DocType", SqlDbType.NVarChar, 50).Value = "EPRR";
                    cmd.Parameters.Add("@AllotParty", SqlDbType.Int).Value = allot.HasValue ? allot.Value : DBNull.Value;
                    cmd.Parameters.Add("@IgnorePartyShip", SqlDbType.Bit).Value = ignorePartyShip;
                    cmd.Parameters.Add("@ItemType", SqlDbType.NVarChar, 20).Value = string.IsNullOrWhiteSpace(itemType) ? DBNull.Value : itemType.Trim();
                    cmd.Parameters.Add("@ActionTag", SqlDbType.Int).Value = actionTag;
                    cmd.Parameters.Add("@VNo", SqlDbType.Int).Value = vNo;
                    cmd.Parameters.Add("@CompCode", SqlDbType.Int).Value = gv.PubCompCode;
                    cmd.Parameters.Add("@BranchCode", SqlDbType.Int).Value = gv.PubBranchCode;
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new
                            {
                                vType = reader["V_TYPE"] == DBNull.Value ? "" : Convert.ToString(reader["V_TYPE"]),
                                vNo = reader["V_NO"] == DBNull.Value ? "" : Convert.ToString(reader["V_NO"]),
                                vDate = reader["V_DATE"] == DBNull.Value ? "" : Convert.ToString(reader["V_DATE"]),
                                billFrom = reader["BillFrom"] == DBNull.Value ? "" : Convert.ToString(reader["BillFrom"]),
                                shipFrom = reader["ShipFrom"] == DBNull.Value ? "" : Convert.ToString(reader["ShipFrom"]),
                                itemCode = reader["ITEM_CODE"] == DBNull.Value ? "" : Convert.ToString(reader["ITEM_CODE"]),
                                itemName = reader["ITEM_NAME"] == DBNull.Value ? "" : Convert.ToString(reader["ITEM_NAME"]),
                                hsnCode = reader["HSN_CODE"] == DBNull.Value ? "" : Convert.ToString(reader["HSN_CODE"]),
                                recdQty = reader["RECD_QTY"] == DBNull.Value ? "" : Convert.ToString(reader["RECD_QTY"]),
                                billQty = reader["BILL_QTY"] == DBNull.Value ? "" : Convert.ToString(reader["BILL_QTY"]),
                                city = reader["City"] == DBNull.Value ? "" : Convert.ToString(reader["City"]),
                                state = reader["State"] == DBNull.Value ? "" : Convert.ToString(reader["State"]),
                                waybillNo = reader["WAYBILL_NO"] == DBNull.Value ? "" : Convert.ToString(reader["WAYBILL_NO"]),
                                billGst = reader["BILL_GST"] == DBNull.Value ? "" : Convert.ToString(reader["BILL_GST"]),
                                partyCode = reader["PARTY_CODE"] == DBNull.Value ? "" : Convert.ToString(reader["PARTY_CODE"]),
                                shipCode = reader["SHIP_CODE"] == DBNull.Value ? "" : Convert.ToString(reader["SHIP_CODE"]),
                                billNo = reader["BILL_NO"] == DBNull.Value ? "" : Convert.ToString(reader["BILL_NO"]),
                                billDate = reader["BILL_DATE"] == DBNull.Value ? "" : Convert.ToString(reader["BILL_DATE"]),
                                truckNo = reader["TRUCK_NO"] == DBNull.Value ? "" : Convert.ToString(reader["TRUCK_NO"]),
                                allotedParty = reader["ALLOTED_PARTY"] == DBNull.Value ? "" : Convert.ToString(reader["ALLOTED_PARTY"]),
                                allotedPartyName = reader["ALLOTED_PARTYNAME"] == DBNull.Value ? "" : Convert.ToString(reader["ALLOTED_PARTYNAME"]),
                                dispState = reader["ShipState"] == DBNull.Value ? "" : Convert.ToString(reader["ShipState"]),
                                dispCity = reader["ShipCity"] == DBNull.Value ? "" : Convert.ToString(reader["ShipCity"]),
                                remarks = "",
                                compWbCopy = "",
                                partyWbCopy = "",
                                partyInvCopy = "",
                                grCopy = "",
                                ewbCopy = "",
                                othCopy1 = "",
                                othCopy2 = "",
                                othCopy3 = "",
                                othCopy4 = "",
                                othCopy5 = ""
                            });
                        }
                    }
                }
                return Json(new
                {
                    success = true,
                    data = list,
                    totalRecords = list.Count
                });
            }
            catch (SqlException ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Database error: " + ex.Message
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
        [HttpPost]
        public async Task<IActionResult> GetEPRReportData([FromBody] EPRReportRequest model)
        {
            try
            {
                if (model == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid report request."
                    });
                }
                if (string.IsNullOrWhiteSpace(model.ReportType))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Report type is required."
                    });
                }
                if (string.IsNullOrWhiteSpace(model.FromDate))
                {
                    return Json(new
                    {
                        success = false,
                        message = "From date is required."
                    });
                }
                if (string.IsNullOrWhiteSpace(model.ToDate))
                {
                    return Json(new
                    {
                        success = false,
                        message = "To date is required."
                    });
                }
                var gv = _globalVariableService.GetGlobalVariables();
                string reportName;
                string rptTitle;
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    string selectionFormula = "";
                    if (model.ReportType.Equals("purchase", StringComparison.OrdinalIgnoreCase))
                    {
                        reportName = "EPR1";
                        rptTitle = "Purchase List";

                        selectionFormula = "{Purchase1.COMP_CODE} = " + gv.PubCompCode + " AND {Purchase1.BRANCH_CODE} = " +
                            gv.PubBranchCode + " AND {Purchase1.V_TYPE} = 'RMPB'";
                        // HSN
                        if (!string.IsNullOrWhiteSpace(model.Hsn))
                        {
                            selectionFormula += " AND LEFT({Purchase2.HSN_CODE},4) = '" + EscapeCrystalString(model.Hsn.Trim()) + "'";
                        }
                        // Ignore Party / Ship
                        if (!model.IgnorePartyShip)
                        {
                            selectionFormula += " AND {Purchase1.PARTY_CODE} = " + "{Purchase1.SHIP_CODE}";
                        }
                        // Date
                        selectionFormula += BuildDateFormula("{Purchase1.BILL_DATE}", model.FromDate, model.ToDate);
                        // Party
                        if (IsValidDropdownValue(model.PartyCode))
                        {
                            selectionFormula += " AND {Purchase1.PARTY_CODE} = " + Convert.ToInt32(model.PartyCode);
                        }
                        // City
                        if (IsValidDropdownValue(model.CityCode))
                        {
                            selectionFormula += " AND {CITY_MAST.CODE} = " + Convert.ToInt32(model.CityCode);
                        }

                        // State
                        if (IsValidDropdownValue(model.StateCode))
                        {
                            selectionFormula += " AND {STATE_MAST.CODE} = " + Convert.ToInt32(model.StateCode);
                        }

                        // Bottle / Other
                        selectionFormula += BuildItemTypeFormula(model.ItemType);
                    }
                    else if (model.ReportType.Equals("purchase2", StringComparison.OrdinalIgnoreCase))
                    {
                        reportName = "EPR6";
                        rptTitle = "Purchase List";

                        selectionFormula = "{Purchase1.COMP_CODE} = " + gv.PubCompCode + " AND {Purchase1.BRANCH_CODE} = " +
                            gv.PubBranchCode + " AND {Purchase1.V_TYPE} = 'RMPB'";

                        // HSN
                        if (!string.IsNullOrWhiteSpace(model.Hsn))
                        {
                            selectionFormula += " AND LEFT({Purchase2.HSN_CODE},4) = '" + EscapeCrystalString(model.Hsn.Trim()) + "'";
                        }
                        // Ignore Party / Ship
                        if (!model.IgnorePartyShip)
                        {
                            selectionFormula += " AND {Purchase1.PARTY_CODE} = " + "{Purchase1.SHIP_CODE}";
                        }
                        // Date
                        selectionFormula += BuildDateFormula("{Purchase1.BILL_DATE}", model.FromDate, model.ToDate);

                        // Party
                        if (IsValidDropdownValue(model.PartyCode))
                        {
                            selectionFormula += " AND {Purchase1.PARTY_CODE} = " + Convert.ToInt32(model.PartyCode);
                        }
                        // City
                        if (IsValidDropdownValue(model.CityCode))
                        {
                            selectionFormula += " AND {CITY_MAST.CODE} = " + Convert.ToInt32(model.CityCode);
                        }
                        // State
                        if (IsValidDropdownValue(model.StateCode))
                        {
                            selectionFormula += " AND {STATE_MAST.CODE} = " + Convert.ToInt32(model.StateCode);
                        }
                        // Bottle / Other
                        selectionFormula += BuildItemTypeFormula(model.ItemType);
                    }
                    else if (model.ReportType.Equals("pending",StringComparison.OrdinalIgnoreCase))
                    {
                        reportName = "EPR1";
                        rptTitle = "Pending EPR List";

                        selectionFormula = "{Purchase1.COMP_CODE} = " + gv.PubCompCode +
                        " AND {Purchase1.BRANCH_CODE} = " + gv.PubBranchCode + " AND {Purchase1.V_TYPE} = 'RMPB'";

                        // HSN
                        if (!string.IsNullOrWhiteSpace(model.Hsn))
                        {
                            selectionFormula += " AND LEFT({Purchase2.HSN_CODE},4) = '" + EscapeCrystalString(model.Hsn.Trim()) + "'";
                        }

                        // Ignore Party / Ship
                        if (!model.IgnorePartyShip)
                        {
                            selectionFormula += " AND {Purchase1.PARTY_CODE} = " + "{Purchase1.SHIP_CODE}";
                        }
                        // Date
                        selectionFormula += BuildDateFormula("{Purchase1.BILL_DATE}", model.FromDate, model.ToDate);
                        // Party
                        if (IsValidDropdownValue(model.PartyCode))
                        {
                            selectionFormula += " AND {Purchase1.PARTY_CODE} = " + Convert.ToInt32(model.PartyCode);
                        }

                        // City
                        if (IsValidDropdownValue(model.CityCode))
                        {
                            selectionFormula += " AND {CITY_MAST.CODE} = " + Convert.ToInt32(model.CityCode);
                        }

                        // State
                        if (IsValidDropdownValue(model.StateCode))
                        {
                            selectionFormula += " AND {STATE_MAST.CODE} = " + Convert.ToInt32(model.StateCode);
                        }

                        // Bottle / Other
                        selectionFormula += BuildItemTypeFormula(model.ItemType);

                        // IMPORTANT:
                        // VB:
                        // SelForMul = SelForMul & " AND {@EPR}<1"
                        selectionFormula += " AND {@EPR} < 1";
                    }
                    else if (model.ReportType.Equals("alloted", StringComparison.OrdinalIgnoreCase))
                    {
                        reportName = "EPR2";
                        rptTitle = "Alloted EPR List";

                        selectionFormula =
                            "{EPR_RECO.COMP_CODE} = " + gv.PubCompCode + " AND {EPR_RECO.BRANCH_CODE} = " + gv.PubBranchCode;

                        // Date
                        selectionFormula += BuildDateFormula("{Purchase1.BILL_DATE}", model.FromDate, model.ToDate);

                        // Party
                        if (IsValidDropdownValue(model.PartyCode))
                        {
                            selectionFormula +=
                                " AND {Purchase1.PARTY_CODE} = " +
                                Convert.ToInt32(model.PartyCode);
                        }

                        // Alloted Party
                        if (IsValidDropdownValue(model.AllotParty))
                        {
                            selectionFormula += " AND {EPR_RECO.ALLOTED_PARTY} = " + Convert.ToInt32(model.AllotParty);
                        }

                        // City
                        if (IsValidDropdownValue(model.CityCode))
                        {
                            selectionFormula += " AND {CITY_MAST.CODE} = " + Convert.ToInt32(model.CityCode);
                        }

                        // State
                        if (IsValidDropdownValue(model.StateCode))
                        {
                            selectionFormula += " AND {STATE_MAST.CODE} = " + Convert.ToInt32(model.StateCode);
                        }

                        // Ref 1
                        if (!string.IsNullOrWhiteSpace(model.Ref1))
                        {
                            selectionFormula += " AND {EPR_RECO.REF_NO1} = '" + EscapeCrystalString(model.Ref1.Trim()) + "'";
                        }

                        // Ref 2
                        if (!string.IsNullOrWhiteSpace(model.Ref2))
                        {
                            selectionFormula += " AND {EPR_RECO.REF_NO2} = '" + EscapeCrystalString(model.Ref2.Trim()) + "'";
                        }
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Invalid report type."
                        });
                    }
                    var dateText = "From " + FormatReportDate(model.FromDate) + " To " + FormatReportDate(model.ToDate);

                    var requestData = new
                    {
                        Reportname = reportName,
                        Database = "ERPDB",
                        selectionFormula = selectionFormula,

                        Parameters = new
                        {
                            comp_name = gv.CompanyName,
                            comp_add1 = gv.Address1,
                            comp_add2 = gv.Address2,

                            RPTNAME = rptTitle,

                            f1 = dateText
                        }
                    };

                    return Json(new
                    {
                        success = true,
                        message = "Report data generated successfully.",
                        reportType = model.ReportType,
                        reportName = reportName,
                        report = requestData
                    });
                }
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
        private string BuildDateFormula(string fieldName, string fromDate, string toDate)
        {
            DateTime from;
            DateTime to;

            if (!DateTime.TryParse(fromDate, out from))
            {
                throw new Exception("Invalid From Date.");
            }

            if (!DateTime.TryParse(toDate, out to))
            {
                throw new Exception("Invalid To Date.");
            }
            return
            " AND " + fieldName + " IN DATE(" + from.Year + "," + from.Month + "," + from.Day + ") TO DATE(" + to.Year + "," + to.Month + "," + to.Day +")";
        }


        private string BuildItemTypeFormula(string itemType)
        {
            if (string.IsNullOrWhiteSpace(itemType))
            {
                return "";
            }

            // Adjust these values according to your radio button values.
            if (itemType.Equals("bottle", StringComparison.OrdinalIgnoreCase))
            {
                return
                    " AND (" +
                    "{ITEM_MAST.GROUP_CODE} = 201 OR " +
                    "{ITEM_MAST.GROUP_CODE} = 202 OR " +
                    "{ITEM_MAST.GROUP_CODE} = 203 OR " +
                    "{ITEM_MAST.GROUP_CODE} = 214 OR " +
                    "{ITEM_MAST.GROUP_CODE} = 215" +
                    ")";
            }

            if (itemType.Equals("other", StringComparison.OrdinalIgnoreCase))
            {
                return
                    " AND (" +
                    "{ITEM_MAST.GROUP_CODE} <> 201 AND " +
                    "{ITEM_MAST.GROUP_CODE} <> 202 AND " +
                    "{ITEM_MAST.GROUP_CODE} <> 203 AND " +
                    "{ITEM_MAST.GROUP_CODE} <> 214 AND " +
                    "{ITEM_MAST.GROUP_CODE} <> 215" +
                    ")";
            }

            return "";
        }


        private bool IsValidDropdownValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value == "0")
                return false;

            if (value == "-1")
                return false;

            return int.TryParse(value, out _);
        }
        private string FormatReportDate(string date)
        {
            if (DateTime.TryParse(date, out DateTime dt))
            {
                return dt.ToString("dd/MMM/yyyy");
            }

            return date;
        }

        private string EscapeCrystalString(string value)
        {
            return value?.Replace("'", "''") ?? "";
        }

        public class EPRReportRequest
        {
            public string ReportType { get; set; }

            public string FromDate { get; set; }
            public string ToDate { get; set; }

            public string Hsn { get; set; }

            public string PartyCode { get; set; }
            public string CityCode { get; set; }
            public string StateCode { get; set; }

            public string AllotParty { get; set; }

            public string Ref1 { get; set; }
            public string Ref2 { get; set; }

            public bool IgnorePartyShip { get; set; }

            public string ItemType { get; set; }
        }



    }
}