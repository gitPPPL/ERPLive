using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.LogService;
using travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction;
using static travelexpensemanagement.Common.DropdownService.DropdownService;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseReturnEntry;

namespace travelexpensemanagement.Repositories.Implementations.Purchase.Transaction
{
    public class PurchaseReturnEntryRepository : IPurchaseReturnEntryRepository
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.LogService.LogService _logService;

        public PurchaseReturnEntryRepository(
            DataBaseConnection dbConnection,
            GlobalVariableService globalVariableService,
            DropdownService dropdownService,
            LogService.LogService logService)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _logService = logService;
        }

        #region 1. Get Doc Type

        public List<object> GetddlDocType()
        {
            string query = @"SELECT Code,Name
                             FROM DOCTYPE_MAST
                             WHERE DOCTYPE='PurchaseReturn'";

            return _dropdownService.GetDropdownList(query);
        }
        #endregion

        #region 2. Get Ref Type
        public List<object> GetddlRefType()
        {
            string query = @"SELECT Code,Name
                             FROM DOCTYPE_MAST
                             WHERE Code IN ('BFRC','RCPI','RCPT','SRPU')";

            return _dropdownService.GetDropdownList(query);
        }
        #endregion

        #region 3. Get Doc No

        public int GetDocNo(string docType)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = @"SELECT ISNULL(MAX(V_NO),0)+1 FROM PURCHASE1  WHERE V_TYPE=@VType AND COMP_CODE=@CompCode
             AND BRANCH_CODE=@BranchCode AND YEAR_CODE=@YearCode";

            using SqlConnection con = _dbConnection.GetErpConnection();
            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@VType", docType);
            cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);
            cmd.Parameters.AddWithValue("@BranchCode", 1);
            cmd.Parameters.AddWithValue("@YearCode", gv.PubFYearCode);

            con.Open();

            object result = cmd.ExecuteScalar();

            return result == DBNull.Value ? 1 : Convert.ToInt32(result);
        }
        #endregion

        #region 4. Get Ref No
        public List<object> GetddlRefNo(string vType)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = $@" SELECT V_NO,DOC_ID FROM PURCHASE1 WHERE COMP_CODE={gv.PubCompCode}
                    AND BRANCH_CODE=1 AND YEAR_CODE='{gv.PubFYearCode}' AND V_TYPE='{vType}' ORDER BY V_NO";
            return _dropdownService.GetDropdownList(query);
        }
        #endregion

        #region 5. Document Status
        public List<object> GetddlDocStatus()
        {
            string query = @"SELECT Code,Name FROM DOCSTATUS_MAST WHERE V_TYPE='Document' ORDER BY CODE";
            return _dropdownService.GetDropdownList(query);
        }
        #endregion

        #region 6. Make List
        public List<object> GetMakeListByItem()
        {
            var compCode = _globalVariableService.GetGlobalVariables().PubCompCode;

            string query = $@"
            SELECT DISTINCT
                   IMM.CODE,
                   IMM.NAME
            FROM ITEM_MAKE IM
            LEFT JOIN ITEMMAKE_MAST IMM
                 ON IM.MAKE_CODE=IMM.CODE
            WHERE IM.COMP_CODE='{compCode}'
            AND IMM.NAME<>''
            ORDER BY IMM.NAME";

            return _dropdownService.GetDropdownList(query);
        }

        #endregion

        #region 7. Department

        public List<object> GetDepartmentList()
        {
            string query = @"SELECT CODE,NAME
                             FROM DEPT_MAST
                             ORDER BY NAME";

            return _dropdownService.GetDropdownList(query);
        }

        #endregion

        #region 8. Return To

        public List<object> GetddlReturnTo()
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string query = $@"
            SELECT DISTINCT
                   A.CODE,
                   A.NAME
            FROM SUBGROUP_MAST A
            LEFT JOIN SUBGROUP_ADDRESS B
                 ON A.COMP_CODE=B.COMP_CODE
                 AND A.CODE=B.CODE
                 AND B.IS_DEFAULT=1
            WHERE A.COMP_CODE={gv.PubCompCode}
            AND A.ACTIVE=1
            ORDER BY A.NAME";

            return _dropdownService.GetDropdownList(query);
        }

        #endregion





        public List<object> GetddlCreditAC()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT  a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
            return _dropdownService.GetDropdownList(query);
            
        }


        public List<object> GetddlDebitAC()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT  a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
            return _dropdownService.GetDropdownList(query);
        }


        public List<object> GetddlFreightCreditAC()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT  a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
            return _dropdownService.GetDropdownList(query);
        }


        public List<object> GetddlFreightDebitAC()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT  a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
            return _dropdownService.GetDropdownList(query);
        }
        [HttpPost]
        public object GetBillDetails(int code)
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = @" SELECT TOP 1 a.code, a.name, a.ADD1, a.ADD2, a.Add3, a.CITY_CODE, b.name AS City, c.code AS StateCode, c.name AS State, a.GSTIN, a.PINCODE 
                FROM SUBGROUP_MAST a LEFT JOIN CITY_MAST b ON a.CITY_CODE = b.CODE LEFT JOIN STATE_MAST c ON b.STATE_CODE = c.CODE WHERE a.NATURE = 'Supplier' 
                AND a.COMP_CODE = @CompCode AND a.ACTIVE = 1 AND a.CODE = @Code";

            object billDetails = null;
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Code", code);
                    cmd.Parameters.AddWithValue("@CompCode", globalVar.PubCompCode);

                    con.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            billDetails = new
                            {
                                Code = rdr["code"].ToString(),
                                Name = rdr["name"].ToString(),
                                Address1 = rdr["ADD1"].ToString(),
                                Address2 = rdr["ADD2"].ToString(),
                                Address3 = rdr["Add3"].ToString(),
                                CityCode = rdr["CITY_CODE"].ToString(),
                                City = rdr["City"].ToString(),
                                StateCode = rdr["StateCode"].ToString(),
                                State = rdr["State"].ToString(),
                                GSTIN = rdr["GSTIN"].ToString(),
                                Pincode = rdr["PINCODE"].ToString()
                            };
                        }
                    }
                }
            }
            return (billDetails);
        }

        [HttpGet]
        public List<object> GetddlCityBillDetails()
        {
            string query = $@" Select a.CODE, a.NAME from CITY_MAST a left join STATE_MAST b on a.STATE_CODE=b.CODE 
            left join COUNTRY_MAST c on a.COUNTRY_CODE=c.CODE where a.ACTIVE=1 and b.ACTIVE=1 and c.ACTIVE=1 Order by a.NAME";
            return _dropdownService.GetDropdownList(query);
            
        }
        [HttpGet]
        public List<object> GetddlstateBillDetails()
        {
            string query = $@" select a.CODE, a.NAME from STATE_MAST a left join COUNTRY_MAST b on a.COUNTRY_CODE=b.CODE where a.ACTIVE=1 and b.ACTIVE=1  Order by a.NAME";
            return _dropdownService.GetDropdownList(query);
            
        }
        [HttpGet]
        public List<object> GetddlCityShipDetails()
        {
            string query = $@" Select a.CODE, a.NAME from CITY_MAST a left join STATE_MAST b on a.STATE_CODE=b.CODE 
            left join COUNTRY_MAST c on a.COUNTRY_CODE=c.CODE where a.ACTIVE=1 and b.ACTIVE=1 and c.ACTIVE=1 Order by a.NAME";
            return _dropdownService.GetDropdownList(query);
            
        }
        [HttpGet]
        public List<object> GetddlstateShipDetails()
        {
            string query = $@" select a.CODE, a.NAME from STATE_MAST a left join COUNTRY_MAST b on a.COUNTRY_CODE=b.CODE where a.ACTIVE=1 and b.ACTIVE=1  Order by a.NAME";
            return _dropdownService.GetDropdownList(query);
        }
        public List<object> GetddlShipDetails()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            //string query = $@" Select Code, Name from SUBGROUP_MAST where NATURE in('Supplier') and COMP_CODE={globalVar.PubCompCode} and ACTIVE=1";
            string query = $@" select DISTINCT a.code,a.name from SUBGROUP_MAST a
            left join SUBGROUP_ADDRESS b on a.COMP_CODE=b.COMP_CODE and a.CODE=b.code and b.IS_DEFAULT=1
            where a.COMP_CODE={globalVar.PubCompCode} and ACTIVE=1 order by a.NAME asc";
            return _dropdownService.GetDropdownList(query);
        }

        public List<DropdownModel> GetTransportName(string term)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            return _dropdownService.GetTransportName(gv.PubCompCode, term);
        }

        //Banding Tab1 Item Name List

        public List<object> GetddlTransportAc()
        {
            var globalVar = _globalVariableService.GetGlobalVariables();
            string query = $@" select Code, NAME From TRANSPORT_MAST where COMP_CODE={globalVar.PubCompCode} order by name asc";
            return _dropdownService.GetDropdownList(query);
        }
        [HttpGet]
        public List<object> GetItemList()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            string sql = @"SELECT a.CODE AS Code, a.NAME AS Name FROM ITEM_MAST a LEFT JOIN ITEM_MAKE b ON a.code = b.ITEM_CODE AND b.COMP_CODE = @Comp
            LEFT JOIN ITEMUNIT_MAST c ON a.UNIT_CODE = c.CODE AND c.COMP_CODE = @Comp
            LEFT JOIN ITEM_MGROUP d ON a.MGROUP_CODE = d.CODE AND d.COMP_CODE = @Comp
            WHERE a.COMP_CODE = @Comp GROUP BY a.NAME, a.CODE ORDER BY a.NAME";
            var list = new List<object>();
            using (var con = _dbConnection.GetErpConnection())
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Comp", gv.PubCompCode);
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new
                        {
                            Code = rdr["Code"].ToString(),
                            Name = rdr["Name"].ToString()
                        });
                    }
                }
            }
            return (list);
        }
        [HttpGet]
        public object GetHSNCode(int code)
        {
            var result = new { hsnCode = "", unit = "" };
            string sql = @"SELECT a.HSN_CODE, b.NAME AS UNIT_NAME
            FROM ITEM_MAST a LEFT JOIN ITEMUNIT_MAST b ON a.UNIT_CODE = b.CODE AND b.COMP_CODE = a.COMP_CODE
            WHERE a.CODE = @Code AND a.COMP_CODE = @CompCode";

            var gv = _globalVariableService.GetGlobalVariables(); 
            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Code", code);
                cmd.Parameters.AddWithValue("@CompCode", gv.PubCompCode);

                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        result = new
                        {
                            hsnCode = rdr["HSN_CODE"]?.ToString() ?? "",
                            unit = rdr["UNIT_NAME"]?.ToString() ?? ""
                        };
                    }
                }
            }
            return (result);
        }
        public List<object> GetTaxTypeList()
        {
            string sql = @"Select Code, NAME From TAX_MAST";
            var list = new List<object>();
            using (var con = _dbConnection.GetErpConnection())
            using (var cmd = new SqlCommand(sql, con))
            {
                con.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        list.Add(new
                        {
                            Code = rdr["Code"].ToString(),
                            Name = rdr["Name"].ToString()
                        });
                    }
                }
            }
            return (list);
        }
        [HttpGet]
        public object GetTaxTypeDetails(string code)
        {
            bool isNumeric = int.TryParse(code, out int codeValue);
            string sql;
            SqlCommand cmd;

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                if (isNumeric)
                {
                    sql = @" SELECT CODE, CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 
                FROM TAX_MAST WHERE CODE = @Code";

                    cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@Code", codeValue);
                }
                else
                {
                    sql = @" SELECT CODE, CGST_PER, SGST_PER, IGST_PER, TDS_PER, TCS_PER, VAT_PER, OTH_PER, OTH_PER2 
                FROM TAX_MAST WHERE NAME = @Name";

                    cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@Name", code);
                }

                con.Open();

                using (var rdr = cmd.ExecuteReader())
                {
                    if (rdr.Read())
                    {
                        var result = new
                        {
                            Code = rdr["CODE"],
                            CGST_PER = rdr["CGST_PER"],
                            SGST_PER = rdr["SGST_PER"],
                            IGST_PER = rdr["IGST_PER"],
                            TDS_PER = rdr["TDS_PER"],
                            TCS_PER = rdr["TCS_PER"],
                            VAT_PER = rdr["VAT_PER"],
                            OTH_PER = rdr["OTH_PER"],
                            OTH_PER2 = rdr["OTH_PER2"]
                        };

                        return (result);
                    }
                    else
                    {
                        return (new { success = false, message = "No record found" });
                    }
                }
            }
        }
        private static void AddParameterSafe(SqlCommand cmd, string parameterName, object value)
        {
            cmd.Parameters.AddWithValue(parameterName, value ?? DBNull.Value);
        }
        public async Task<object> SaveAllData(
            PurchaseReturnHeaderModel headerObj1,
            List<ItemDetailModel> ItemDetails,
            List<AttachmentModel> Attachments)
        {
            //var headerObj = JsonConvert.DeserializeObject<PurchaseReturnHeaderModel>(Header);
            var headerObj = headerObj1;
            var globalVar = _globalVariableService.GetGlobalVariables();
            string V_NO = "";
            string DOC_ID = "";
            DOC_ID = headerObj.DocType + headerObj.Vno;
            if (headerObj.ACTION == "INSERT")
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {

                            // Insert Header
                            using (var cmdHeader = new SqlCommand("InsertPurchaseReturnEntryHeader", con, transaction))
                            {
                                cmdHeader.CommandType = CommandType.StoredProcedure;

                                AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
                                AddParameterSafe(cmdHeader, "@BRANCH_CODE", globalVar.PubBranchCode);
                                AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
                                // Document Header
                                AddParameterSafe(cmdHeader, "@V_NO", headerObj.Vno);
                                AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
                                AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                //AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WbNo);
                                AddParameterSafe(cmdHeader, "@REF_TYPE", headerObj.RefType);
                                AddParameterSafe(cmdHeader, "@REF_NO", headerObj.RefNo);

                                // Return To Details
                                AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.ReturnTo);
                                AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.ReturnAddLine1);
                                AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.ReturnAddLine2);
                                AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.ReturnAddLine3);
                                AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.ReturnCity);
                                AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.ReturnCity);
                                AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.ReturnGST);

                                // Ship To Details
                                AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipTo);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
                                AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
                                AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);
                                AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.ShipCity);

                                // Accounting
                                AddParameterSafe(cmdHeader, "@CREDIT_AC", headerObj.CreditAC);
                                AddParameterSafe(cmdHeader, "@DEBIT_AC", headerObj.DebitAC);

                                // Document Details 
                                AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
                                AddParameterSafe(cmdHeader, "@BILL_DATE", string.IsNullOrWhiteSpace(headerObj.BillDate) ? DBNull.Value : DateTime.Parse(headerObj.BillDate));
                                AddParameterSafe(cmdHeader, "@BL_NO", headerObj.BLNo);
                                AddParameterSafe(cmdHeader, "@BL_DATE", string.IsNullOrWhiteSpace(headerObj.BLDate) ? DBNull.Value : DateTime.Parse(headerObj.BLDate));
                                AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);
                                AddParameterSafe(cmdHeader, "@INPUT_TYPE", headerObj.InputType);
                                AddParameterSafe(cmdHeader, "@EXPS_TYPE", headerObj.ExpensesType);
                                AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);
                                AddParameterSafe(cmdHeader, "@STATUS", 1);

                                // Transport
                                AddParameterSafe(cmdHeader, "@TRANSPORT_NAME", headerObj.TransportName);
                                AddParameterSafe(cmdHeader, "@TRANSPORT_CODE", headerObj.TransportCode);
                                AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
                                AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
                                AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
                                AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
                                AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo ?? "");
                                AddParameterSafe(cmdHeader, "@GR_DATE", string.IsNullOrWhiteSpace(headerObj.GRDate) ? DBNull.Value : DateTime.Parse(headerObj.GRDate));
                                AddParameterSafe(cmdHeader, "@TRANSPORT_AC", headerObj.TransportAC);
                                AddParameterSafe(cmdHeader, "@FRTPAY_DRAC", headerObj.FreightDebit);
                                AddParameterSafe(cmdHeader, "@FRTPAY_CRAC", headerObj.FreightCredit);
                                AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);

                                // Amount Breakdown
                                AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty ?? 0);
                                AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty ?? 0);
                                AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount ?? 0);
                                AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount ?? 0);
                                AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking ?? (object)DBNull.Value);
                                AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST ?? 0);
                                AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST ?? 0);
                                AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST ?? 0);
                                AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS ?? 0);
                                AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT ?? 0);
                                AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt ?? 0);
                                AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1 ?? 0);
                                AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2 ?? 0);
                                AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff ?? 0);

                                AddParameterSafe(cmdHeader, "@UUSER", globalVar.PubUserId);
                                AddParameterSafe(cmdHeader, "@UDATE", DateTime.Now);
                                AddParameterSafe(cmdHeader, "@EUSER", "");
                                AddParameterSafe(cmdHeader, "@EDATE", "");
                                AddParameterSafe(cmdHeader, "@AED", "A");
                                AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
                                AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
                                AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);
                                AddParameterSafe(cmdHeader, "@Action", "Insert");
                                await cmdHeader.ExecuteNonQueryAsync();
                            }
                            // Insert Items 
                            int serialNo = 1;

                            foreach (var item in ItemDetails)
                            {
                                using (var cmdItem = new SqlCommand("InsertPurchaseReturnEntryItemDetail", con, transaction))
                                {
                                    cmdItem.CommandType = CommandType.StoredProcedure;

                                    AddParameterSafe(cmdItem, "@V_NO", headerObj.Vno);
                                    AddParameterSafe(cmdItem, "@DOC_ID", headerObj.DocType + headerObj.Vno ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                    AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdItem, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                    AddParameterSafe(cmdItem, "@SNO", serialNo++);
                                    AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemCode);
                                    AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@UOM_NAME", item.Unit ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@NOS", item.Nos ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RECD_QTY", item.ReturnQty ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RATE", item.Rate ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@AMOUNT", item.Amount ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RCM_YN", item.RCMYN ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@INPUT_YN", item.InputYN ?? (object)DBNull.Value);

                                    // Parse string to int or pass DBNull
                                    if (int.TryParse(item.TaxType, out int taxCode))
                                        AddParameterSafe(cmdItem, "@TAX_CODE", taxCode);
                                    else
                                        AddParameterSafe(cmdItem, "@TAX_CODE", DBNull.Value);

                                    AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt ?? (object)DBNull.Value);

                                    // Handle MAKE_CODE (string to int or DBNull)
                                    if (int.TryParse(item.Make, out int makeCode))
                                        AddParameterSafe(cmdItem, "@MAKE_CODE", makeCode);
                                    else
                                        AddParameterSafe(cmdItem, "@MAKE_CODE", DBNull.Value);

                                    // Handle DEPT_CODE (string to int or DBNull)
                                    if (int.TryParse(item.Department, out int deptCode))
                                        AddParameterSafe(cmdItem, "@DEPT_CODE", deptCode);
                                    else
                                        AddParameterSafe(cmdItem, "@DEPT_CODE", DBNull.Value);

                                    AddParameterSafe(cmdItem, "@REMARKS", item.Remarks ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt ?? (object)DBNull.Value);
                                    // WBType/WBNo are not being sent, so omitted


                                    AddParameterSafe(cmdItem, "@KANTA_TYPE", item.WBType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@KANTA_NO", item.WBNo ?? (object)DBNull.Value);

                                    AddParameterSafe(cmdItem, "@REF_TYPE", item.RefType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@REF_NO", item.RefNo);

                                    AddParameterSafe(cmdItem, "@BATCH_NO", string.IsNullOrWhiteSpace(item.RefBatchNo) ? DBNull.Value : item.RefBatchNo);
                                    AddParameterSafe(cmdItem, "@BAG_NO", string.IsNullOrWhiteSpace(item.RefBagNo) ? DBNull.Value : item.RefBagNo);

                                    AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
                                    AddParameterSafe(cmdItem, "@EUSER", DBNull.Value);
                                    AddParameterSafe(cmdItem, "@EDATE", DBNull.Value);
                                    AddParameterSafe(cmdItem, "@AED", "A");
                                    AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdItem, "@LID", Environment.MachineName);
                                    AddParameterSafe(cmdItem, "@Action", "Insert");

                                    await cmdItem.ExecuteNonQueryAsync();
                                }

                                if (headerObj.DocType == "RRET")
                                {
                                    using (SqlCommand cmdBatch = new SqlCommand(@"
                                        INSERT INTO PROD_BATCH
                                        (COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, V_DATE, BATCH_NO, BAG_NO, ITEM_CODE, GROSS_QTY, QTY,
                                        REMARKS, SNO, UUSER, UDATE, AED, WSID, LIP, LID        )
                                        VALUES (@COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @V_DATE, @BATCH_NO, @BAG_NO, @ITEM_CODE, @GROSS_QTY,
                                        @QTY, @REMARKS, @SNO, @UUSER, GETDATE(), @AED, @WSID, @LIP, @LID )", con, transaction))
                                    {
                                        cmdBatch.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                        cmdBatch.Parameters.AddWithValue("@BRANCH_CODE", globalVar.PubBranchCode);
                                        cmdBatch.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                        cmdBatch.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                        cmdBatch.Parameters.AddWithValue("@V_NO", headerObj.Vno);
                                        cmdBatch.Parameters.AddWithValue("@V_DATE", DateTime.Parse(headerObj.DocDate));
                                        cmdBatch.Parameters.AddWithValue("@BATCH_NO", string.IsNullOrWhiteSpace(item.RefBatchNo) ? DBNull.Value : item.RefBatchNo);
                                        cmdBatch.Parameters.AddWithValue("@BAG_NO", string.IsNullOrWhiteSpace(item.RefBagNo) ? DBNull.Value : item.RefBagNo);
                                        cmdBatch.Parameters.AddWithValue("@ITEM_CODE", Convert.ToInt32(item.ItemCode));
                                        cmdBatch.Parameters.AddWithValue("@GROSS_QTY", item.ReturnQty ?? 0);
                                        cmdBatch.Parameters.AddWithValue("@QTY", item.ReturnQty ?? 0);
                                        cmdBatch.Parameters.AddWithValue("@REMARKS", string.IsNullOrWhiteSpace(item.Remarks) ? DBNull.Value : item.Remarks);
                                        cmdBatch.Parameters.AddWithValue("@SNO", serialNo++);
                                        cmdBatch.Parameters.AddWithValue("@UUSER", globalVar.PubUserId);
                                        cmdBatch.Parameters.AddWithValue("@AED", "A");
                                        cmdBatch.Parameters.AddWithValue("@WSID", globalVar.PubWorkStationID);
                                        cmdBatch.Parameters.AddWithValue("@LIP", globalVar.PubLocalId);
                                        cmdBatch.Parameters.AddWithValue("@LID", Environment.MachineName);
                                        await cmdBatch.ExecuteNonQueryAsync();
                                    }
                                }

                            }

                            int rowId = 1;

                            if (Attachments != null && Attachments.Any())
                            {
                                foreach (var attachment in Attachments)
                                {
                                    if (attachment?.File == null || attachment.File.Length == 0)
                                        continue;

                                    byte[] fileBytes;

                                    using (var ms = new MemoryStream())
                                    {
                                        await attachment.File.CopyToAsync(ms);
                                        fileBytes = ms.ToArray();
                                    }

                                    using (var cmdImage = new SqlCommand("InsertPURCHASEReturnEntryAttachment", con, transaction))
                                    {
                                        cmdImage.CommandType = CommandType.StoredProcedure;

                                        AddParameterSafe(cmdImage, "@COMP_CODE", globalVar.PubCompCode);
                                        AddParameterSafe(cmdImage, "@BRANCH_CODE", globalVar.PubBranchCode);
                                        AddParameterSafe(cmdImage, "@YEAR_CODE", globalVar.PubFYearCode);

                                        AddParameterSafe(cmdImage, "@DOC_ID", DOC_ID);
                                        AddParameterSafe(cmdImage, "@V_NO", headerObj.Vno);
                                        AddParameterSafe(cmdImage, "@V_TYPE", headerObj.DocType);
                                        AddParameterSafe(cmdImage, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                        AddParameterSafe(cmdImage, "@ROWID", rowId++);
                                        AddParameterSafe(cmdImage, "@IMG_FILE", fileBytes);
                                        AddParameterSafe(cmdImage, "@FILE_NAME", attachment.File.FileName);
                                        AddParameterSafe(cmdImage, "@FILE_TYPE", Path.GetExtension(attachment.File.FileName));

                                        AddParameterSafe(cmdImage, "@UUSER", globalVar.PubUserId);
                                        AddParameterSafe(cmdImage, "@WSID", globalVar.PubWorkStationID);
                                        AddParameterSafe(cmdImage, "@LIP", globalVar.PubLocalId);
                                        AddParameterSafe(cmdImage, "@LID", Environment.MachineName);

                                        AddParameterSafe(cmdImage, "@Action", "ImageInsert");

                                        await cmdImage.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            string action = "INSERT";
                            _logService.InsertLog("PURCHASE1", "purchase Return Entry", "TRANSACTION", action, headerObj.DocType, headerObj.Vno, DateTime.Parse(headerObj.DocDate));
                            transaction.Commit();
                            //return Ok(new { status = "success", message = "Saved successfully" });
                            return (new
                            {
                                success = true,
                                action = "INSERT",
                                message = "Data saved successfully."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return(new { status = "error", message = ex.Message });
                        }
                    }
                }
            }
            else if (headerObj.ACTION == "UPDATE")
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Insert Header
                            using (var cmdHeader = new SqlCommand("InsertPurchaseReturnEntryHeader", con, transaction))
                            {
                                cmdHeader.CommandType = CommandType.StoredProcedure;

                                AddParameterSafe(cmdHeader, "@COMP_CODE", globalVar.PubCompCode);
                                AddParameterSafe(cmdHeader, "@BRANCH_CODE", globalVar.PubBranchCode);
                                AddParameterSafe(cmdHeader, "@YEAR_CODE", globalVar.PubFYearCode);
                                // Document Header
                                AddParameterSafe(cmdHeader, "@V_NO", headerObj.Vno);
                                AddParameterSafe(cmdHeader, "@V_TYPE", headerObj.DocType);
                                AddParameterSafe(cmdHeader, "@DOC_ID", DOC_ID);
                                AddParameterSafe(cmdHeader, "@V_DATE", DateTime.Parse(headerObj.DocDate));
                                //AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WbNo);
                                AddParameterSafe(cmdHeader, "@REF_TYPE", headerObj.RefType);
                                AddParameterSafe(cmdHeader, "@REF_NO", headerObj.RefNo);

                                // Return To Details
                                AddParameterSafe(cmdHeader, "@PARTY_CODE", headerObj.ReturnTo);
                                AddParameterSafe(cmdHeader, "@BILL_ADD1", headerObj.ReturnAddLine1);
                                AddParameterSafe(cmdHeader, "@BILL_ADD2", headerObj.ReturnAddLine2);
                                AddParameterSafe(cmdHeader, "@BILL_ADD3", headerObj.ReturnAddLine3);
                                AddParameterSafe(cmdHeader, "@BILL_CITY", headerObj.ReturnCity);
                                AddParameterSafe(cmdHeader, "@BILL_ADDRESSID", headerObj.ReturnCity);
                                AddParameterSafe(cmdHeader, "@BILL_GST", headerObj.ReturnGST);

                                // Ship To Details
                                AddParameterSafe(cmdHeader, "@SHIP_CODE", headerObj.ShipTo);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD1", headerObj.ShipAddLine1);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD2", headerObj.ShipAddLine2);
                                AddParameterSafe(cmdHeader, "@SHIP_ADD3", headerObj.ShipAddLine3);
                                AddParameterSafe(cmdHeader, "@SHIP_CITY", headerObj.ShipCity);
                                AddParameterSafe(cmdHeader, "@SHIP_GST", headerObj.ShipGST);
                                AddParameterSafe(cmdHeader, "@SHIP_ADDRESSID", headerObj.ShipCity);

                                // Accounting
                                AddParameterSafe(cmdHeader, "@CREDIT_AC", headerObj.CreditAC);
                                AddParameterSafe(cmdHeader, "@DEBIT_AC", headerObj.DebitAC);

                                // Document Details 
                                AddParameterSafe(cmdHeader, "@BILL_NO", headerObj.BillNo);
                                AddParameterSafe(cmdHeader, "@BILL_DATE", string.IsNullOrWhiteSpace(headerObj.BillDate) ? DBNull.Value : DateTime.Parse(headerObj.BillDate));
                                AddParameterSafe(cmdHeader, "@BL_NO", headerObj.BLNo);
                                AddParameterSafe(cmdHeader, "@BL_DATE", string.IsNullOrWhiteSpace(headerObj.BLDate) ? DBNull.Value : DateTime.Parse(headerObj.BLDate));
                                AddParameterSafe(cmdHeader, "@WAYBILL_NO", headerObj.WaybillNo);
                                AddParameterSafe(cmdHeader, "@INPUT_TYPE", headerObj.InputType);
                                AddParameterSafe(cmdHeader, "@EXPS_TYPE", headerObj.ExpensesType);
                                AddParameterSafe(cmdHeader, "@NAMOUNT", headerObj.NumFinalNetAmt);
                                AddParameterSafe(cmdHeader, "@STATUS", 1);

                                // Transport
                                //AddParameterSafe(cmdHeader, "@TRANSPORT_CODE", headerObj.TransportName);
                                AddParameterSafe(cmdHeader, "@TRANSPORT_NAME", headerObj.TransportName);
                                AddParameterSafe(cmdHeader, "@TRANSPORT_CODE", headerObj.TransportCode);
                                AddParameterSafe(cmdHeader, "@TRUCK_NO", headerObj.VehicleNo);
                                AddParameterSafe(cmdHeader, "@CONTAINER_NO", headerObj.ContainerNo);
                                AddParameterSafe(cmdHeader, "@FRTPAY_AMT", headerObj.FreightPay);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAXPER", headerObj.FrtTax1);
                                AddParameterSafe(cmdHeader, "@FRTPAY_TAX", headerObj.FrtTax2);
                                AddParameterSafe(cmdHeader, "@FRTPAY_NAR", headerObj.FrtPayNarr);
                                AddParameterSafe(cmdHeader, "@GR_NO", headerObj.GRNo ?? "");
                                AddParameterSafe(cmdHeader, "@GR_DATE", string.IsNullOrWhiteSpace(headerObj.GRDate) ? DBNull.Value : DateTime.Parse(headerObj.GRDate));
                                AddParameterSafe(cmdHeader, "@TRANSPORT_AC", headerObj.TransportAC);
                                AddParameterSafe(cmdHeader, "@FRTPAY_DRAC", headerObj.FreightDebit);
                                AddParameterSafe(cmdHeader, "@FRTPAY_CRAC", headerObj.FreightCredit);
                                AddParameterSafe(cmdHeader, "@REMARKS", headerObj.Remarks);

                                // Amount Breakdown
                                AddParameterSafe(cmdHeader, "@RECD_QTY", headerObj.NumReceivedQty ?? 0);
                                AddParameterSafe(cmdHeader, "@BILL_QTY", headerObj.NumBillQty ?? 0);
                                AddParameterSafe(cmdHeader, "@AMOUNT", headerObj.NumAmount ?? 0);
                                AddParameterSafe(cmdHeader, "@DISC_AMT", headerObj.NumDiscount ?? 0);
                                AddParameterSafe(cmdHeader, "@PACK_AMT", headerObj.NumPacking ?? (object)DBNull.Value);
                                AddParameterSafe(cmdHeader, "@CGST_AMT", headerObj.NumCGST ?? 0);
                                AddParameterSafe(cmdHeader, "@SGST_AMT", headerObj.NumSGST ?? 0);
                                AddParameterSafe(cmdHeader, "@IGST_AMT", headerObj.NumIGST ?? 0);
                                AddParameterSafe(cmdHeader, "@CESS_AMT", headerObj.NumCESS ?? 0);
                                AddParameterSafe(cmdHeader, "@VAT_AMT", headerObj.NumVAT ?? 0);
                                AddParameterSafe(cmdHeader, "@OTH_AMT", headerObj.NumOtherAmt ?? 0);
                                AddParameterSafe(cmdHeader, "@TCS_PER", headerObj.NumTCSPer1 ?? 0);
                                AddParameterSafe(cmdHeader, "@TCS_AMT", headerObj.NumTCSPer2 ?? 0);
                                AddParameterSafe(cmdHeader, "@ROUND_OFF", headerObj.NumRoundOff ?? 0);

                                AddParameterSafe(cmdHeader, "@UUSER", globalVar.PubUserId);
                                AddParameterSafe(cmdHeader, "@UDATE", DateTime.Now);
                                AddParameterSafe(cmdHeader, "@EUSER", "");
                                AddParameterSafe(cmdHeader, "@EDATE", "");
                                AddParameterSafe(cmdHeader, "@AED", "A");
                                AddParameterSafe(cmdHeader, "@WSID", globalVar.PubWorkStationID);
                                AddParameterSafe(cmdHeader, "@LIP", globalVar.PubLocalId);
                                AddParameterSafe(cmdHeader, "@LID", Environment.MachineName);
                                AddParameterSafe(cmdHeader, "@Action", "Update");
                                await cmdHeader.ExecuteNonQueryAsync();
                            }
                            // Insert Items
                            string action = "Update";
                            _logService.InsertLog("PURCHASE1", "purchase Return Entry", "TRANSACTION", action, headerObj.DocType, headerObj.Vno, DateTime.Parse(headerObj.DocDate));

                            using (SqlCommand ItemDetailDelete = new SqlCommand("DELETE FROM PURCHASE2 WHERE COMP_CODE = @COMP_CODE AND V_NO = @V_NO and V_TYPE= @V_TYPE and YEAR_CODE= @YEAR_CODE ", con, transaction))
                            {
                                ItemDetailDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                ItemDetailDelete.Parameters.AddWithValue("@V_NO", headerObj.Vno);
                                ItemDetailDelete.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                ItemDetailDelete.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                ItemDetailDelete.ExecuteNonQuery();
                            }
                            int serialNo = 1;
                            foreach (var item in ItemDetails)
                            {
                                using (var cmdItem = new SqlCommand("InsertPurchaseReturnEntryItemDetail", con, transaction))
                                {
                                    cmdItem.CommandType = CommandType.StoredProcedure;

                                    AddParameterSafe(cmdItem, "@V_NO", headerObj.Vno);
                                    AddParameterSafe(cmdItem, "@DOC_ID", headerObj.DocType + headerObj.Vno ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@V_TYPE", headerObj.DocType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                    AddParameterSafe(cmdItem, "@COMP_CODE", globalVar.PubCompCode);
                                    AddParameterSafe(cmdItem, "@BRANCH_CODE", globalVar.PubBranchCode);
                                    AddParameterSafe(cmdItem, "@YEAR_CODE", globalVar.PubFYearCode);
                                    AddParameterSafe(cmdItem, "@SNO", serialNo++);
                                    AddParameterSafe(cmdItem, "@ITEM_CODE", item.ItemCode);
                                    AddParameterSafe(cmdItem, "@ITEM_NAME", item.ItemName ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@HSN_CODE", item.HSNCode ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@UOM_NAME", item.Unit ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@NOS", item.Nos ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RECD_QTY", item.ReturnQty ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@BILL_QTY", item.BillQty ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RATE", item.Rate ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@AMOUNT", item.Amount ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@RCM_YN", item.RCMYN ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@INPUT_YN", item.InputYN ?? (object)DBNull.Value);

                                    // Parse string to int or pass DBNull
                                    if (int.TryParse(item.TaxType, out int taxCode))
                                        AddParameterSafe(cmdItem, "@TAX_CODE", taxCode);
                                    else
                                        AddParameterSafe(cmdItem, "@TAX_CODE", DBNull.Value);

                                    AddParameterSafe(cmdItem, "@PACK_PER", item.PackPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@PACK_AMT", item.PackAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@DISC_PER", item.DiscPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@DISC_AMT", item.DiscAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CGST_PER", item.CGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CGST_AMT", item.CGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@SGST_PER", item.SGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@SGST_AMT", item.SGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@IGST_PER", item.IGSTPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@IGST_AMT", item.IGSTAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CESS_PER", item.CESSPer ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@CESS_AMT", item.CESSAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@OTH_AMT", item.OthAmt ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@NET_AMT", item.NetAmt ?? (object)DBNull.Value);

                                    // Handle MAKE_CODE (string to int or DBNull)
                                    if (int.TryParse(item.Make, out int makeCode))
                                        AddParameterSafe(cmdItem, "@MAKE_CODE", makeCode);
                                    else
                                        AddParameterSafe(cmdItem, "@MAKE_CODE", DBNull.Value);

                                    // Handle DEPT_CODE (string to int or DBNull)
                                    if (int.TryParse(item.Department, out int deptCode))
                                        AddParameterSafe(cmdItem, "@DEPT_CODE", deptCode);
                                    else
                                        AddParameterSafe(cmdItem, "@DEPT_CODE", DBNull.Value);

                                    AddParameterSafe(cmdItem, "@REMARKS", item.Remarks ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@LAND_RATE", item.LDRate ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@LAND_AMT", item.LDAmt ?? (object)DBNull.Value);
                                    // WBType/WBNo are not being sent, so omitted


                                    AddParameterSafe(cmdItem, "@KANTA_TYPE", item.WBType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@KANTA_NO", item.WBNo ?? (object)DBNull.Value);

                                    AddParameterSafe(cmdItem, "@REF_TYPE", item.RefType ?? (object)DBNull.Value);
                                    AddParameterSafe(cmdItem, "@REF_NO", item.RefNo);
                                    AddParameterSafe(cmdItem, "@BATCH_NO", string.IsNullOrWhiteSpace(item.RefBatchNo) ? DBNull.Value : item.RefBatchNo);
                                    AddParameterSafe(cmdItem, "@BAG_NO", string.IsNullOrWhiteSpace(item.RefBagNo) ? DBNull.Value : item.RefBagNo);

                                    AddParameterSafe(cmdItem, "@UUSER", globalVar.PubUserId);
                                    AddParameterSafe(cmdItem, "@UDATE", DateTime.Now);
                                    AddParameterSafe(cmdItem, "@EUSER", DBNull.Value);
                                    AddParameterSafe(cmdItem, "@EDATE", DBNull.Value);
                                    AddParameterSafe(cmdItem, "@AED", "A");
                                    AddParameterSafe(cmdItem, "@WSID", globalVar.PubWorkStationID);
                                    AddParameterSafe(cmdItem, "@LIP", globalVar.PubLocalId);
                                    AddParameterSafe(cmdItem, "@LID", Environment.MachineName);
                                    AddParameterSafe(cmdItem, "@Action", "Insert");

                                    await cmdItem.ExecuteNonQueryAsync();
                                }
                            }
                            using (SqlCommand ImageDetailDelete = new SqlCommand("DELETE FROM IMG_TABLE WHERE COMP_CODE = @COMP_CODE AND V_NO = @V_NO and V_TYPE= @V_TYPE and YEAR_CODE= @YEAR_CODE ", con, transaction))
                            {
                                ImageDetailDelete.Parameters.AddWithValue("@COMP_CODE", globalVar.PubCompCode);
                                ImageDetailDelete.Parameters.AddWithValue("@V_NO", headerObj.Vno);
                                ImageDetailDelete.Parameters.AddWithValue("@V_TYPE", headerObj.DocType);
                                ImageDetailDelete.Parameters.AddWithValue("@YEAR_CODE", globalVar.PubFYearCode);
                                ImageDetailDelete.ExecuteNonQuery();
                            }

                            int rowId = 1;

                            if (Attachments != null && Attachments.Any())
                            {
                                foreach (var attachment in Attachments)
                                {
                                    if (attachment?.File == null || attachment.File.Length == 0)
                                        continue;

                                    byte[] fileBytes;

                                    using (var ms = new MemoryStream())
                                    {
                                        await attachment.File.CopyToAsync(ms);
                                        fileBytes = ms.ToArray();
                                    }

                                    using (var cmdImage = new SqlCommand("InsertPURCHASEReturnEntryAttachment", con, transaction))
                                    {
                                        cmdImage.CommandType = CommandType.StoredProcedure;

                                        AddParameterSafe(cmdImage, "@COMP_CODE", globalVar.PubCompCode);
                                        AddParameterSafe(cmdImage, "@BRANCH_CODE", globalVar.PubBranchCode);
                                        AddParameterSafe(cmdImage, "@YEAR_CODE", globalVar.PubFYearCode);

                                        AddParameterSafe(cmdImage, "@DOC_ID", DOC_ID);
                                        AddParameterSafe(cmdImage, "@V_NO", headerObj.Vno);
                                        AddParameterSafe(cmdImage, "@V_TYPE", headerObj.DocType);
                                        AddParameterSafe(cmdImage, "@V_DATE", DateTime.Parse(headerObj.DocDate));

                                        AddParameterSafe(cmdImage, "@ROWID", rowId++);
                                        AddParameterSafe(cmdImage, "@IMG_FILE", fileBytes);
                                        AddParameterSafe(cmdImage, "@FILE_NAME", attachment.File.FileName);
                                        AddParameterSafe(cmdImage, "@FILE_TYPE", Path.GetExtension(attachment.File.FileName));

                                        AddParameterSafe(cmdImage, "@UUSER", globalVar.PubUserId);
                                        AddParameterSafe(cmdImage, "@WSID", globalVar.PubWorkStationID);
                                        AddParameterSafe(cmdImage, "@LIP", globalVar.PubLocalId);
                                        AddParameterSafe(cmdImage, "@LID", Environment.MachineName);

                                        AddParameterSafe(cmdImage, "@Action", "ImageInsert");

                                        await cmdImage.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                            transaction.Commit();
                            //return Ok(new { status = "success", message = "Update successfully" });
                            return(new
                            {
                                success = true,
                                action = "UPDATE",
                                message = "Data updated successfully."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return (new { status = "error", message = ex.Message });
                        }
                    }
                }
            }
            else
            {
                return (new { success = false, message = "Invalid action specified." });
            }
        }

        public async Task<GatePurchaseDetailsResponse> GetRefNoList(string strVNo, string strVType)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            var response = new GatePurchaseDetailsResponse();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand command = new SqlCommand("usp_GetRefNoPurchaseReturnEntry", con))
            {
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@V_TYPE", strVType);
                command.Parameters.AddWithValue("@V_NO", Convert.ToInt32(strVNo));
                command.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                command.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                command.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                await con.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var header = new Dictionary<string, object>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            header[reader.GetName(i)] =
                                reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }

                        response.Header.Add(header);
                    }

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new Dictionary<string, object>();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                item[reader.GetName(i)] =
                                    reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }

                            response.Items.Add(item);
                        }
                    }

                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var obj = new WeightSummary();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var prop = typeof(WeightSummary)
                                    .GetProperty(reader.GetName(i));

                                if (prop != null && !reader.IsDBNull(i))
                                {
                                    prop.SetValue(obj,
                                        ChangeType(reader.GetValue(i), prop.PropertyType));
                                }
                            }

                            response.WeightSummary.Add(obj);
                        }
                    }
                }
            }

            return response;
        }
        private object ChangeType(object value, Type conversionType)
        {
            if (value == null || value == DBNull.Value)
                return null;

            var targetType = Nullable.GetUnderlyingType(conversionType) ?? conversionType;

            return Convert.ChangeType(value, targetType);
        }

        public async Task<PurchaseAllDetailsResponse> GetAllDataDetails(GetDetailsRequest request)
        {
            var gv = _globalVariableService.GetGlobalVariables();
            var response = new PurchaseAllDetailsResponse();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand("sp_GetPurchaseReturnAllDetails", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@VNO", request.VNO);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@V_TYPE", request.vType);

                await con.OpenAsync();

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    // PURCHASE1
                    while (await reader.ReadAsync())
                    {
                        var obj = new Purchase1List();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var prop = typeof(Purchase1List).GetProperty(reader.GetName(i));

                            if (prop != null && !reader.IsDBNull(i))
                            {
                                prop.SetValue(obj,
                                    ChangeType(reader.GetValue(i), prop.PropertyType));
                            }
                        }

                        response.Purchase1.Add(obj);
                    }

                    // PURCHASE2
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var obj = new Purchase2List();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var prop = typeof(Purchase2List).GetProperty(reader.GetName(i));

                                if (prop != null && !reader.IsDBNull(i))
                                {
                                    prop.SetValue(obj,
                                        ChangeType(reader.GetValue(i), prop.PropertyType));
                                }
                            }

                            response.Purchase2.Add(obj);
                        }
                    }

                    // PURCHASE3
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var obj = new Purchase3List();

                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var column = reader.GetName(i);

                                var prop = typeof(Purchase3List).GetProperty(column);

                                if (prop == null || reader.IsDBNull(i))
                                    continue;

                                if (column == "IMG_FILE")
                                {
                                    prop.SetValue(obj, (byte[])reader["IMG_FILE"]);
                                }
                                else
                                {
                                    prop.SetValue(obj,
                                        ChangeType(reader.GetValue(i), prop.PropertyType));
                                }
                            }

                            response.Purchase3.Add(obj);
                        }
                    }
                }
            }

            return response;
        }

        public async Task<object> PrintPurchaseReturnEntryReport(PrintReportModelPurchaseReturnEntry model)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                await con.OpenAsync();

                string sql = @"SELECT 1
                       FROM Ledger2
                       WHERE V_TYPE=@VTYPE
                       AND V_NO=@VNO
                       AND COMP_CODE=@COMP
                       AND BRANCH_CODE=@BRANCH
                       AND YEAR_CODE=@YEAR";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@VTYPE", model.VType);
                    cmd.Parameters.AddWithValue("@VNO", model.VNo);
                    cmd.Parameters.AddWithValue("@COMP", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR", gv.PubFYearCode);

                    var exist = await cmd.ExecuteScalarAsync();

                    if (exist == null)
                    {
                        return new
                        {
                            success = false,
                            message = $"Voucher not posted of VType:{model.VType} and VNo:{model.VNo}"
                        };
                    }
                }

                string reportName = gv.PubCompCode == "7"
                    ? "INVOICE_PRETK"
                    : "INVOICE_PRET";

                var requestData = new
                {
                    Reportname = reportName,
                    Database = "ERPDB",

                    selectionFormula =
                        "{PURCHASE1.V_TYPE} = '" + model.VType + "'" +
                        " AND {PURCHASE1.V_NO} = " + model.VNo +
                        " AND {PURCHASE1.COMP_CODE} = " + gv.PubCompCode +
                        " AND {PURCHASE1.YEAR_CODE} = " + gv.PubFYearCode +
                        " AND {PURCHASE1.BRANCH_CODE} = " + gv.PubBranchCode,

                    Parameters = new
                    {
                        RPTNAME = model.VType + "/Debit Note",
                        comp_name = gv.CompanyName,
                        comp_name1 = gv.PubCompCode == "3" ? "" : gv.CompanyName,
                        comp_add1 = gv.Address1,
                        comp_add2 = gv.Address2,
                        comp_phone = "Mobile :" + gv.Phone,
                        GST = "GSTIN :" + gv.gstin,
                        TIN = "TIN NO. :" + gv.PAN,
                        Website = "Web :" + gv.Website,
                        EMAIL = "Email :" + gv.Email,
                        INWORD = model.Amount
                    }
                };

                return new
                {
                    success = true,
                    report = requestData
                };
            }
        }

       

    }
}