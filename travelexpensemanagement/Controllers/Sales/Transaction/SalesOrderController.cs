using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.Cmp;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class SalesOrderController : Controller
    {        
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataservice;
        public SalesOrderController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataservice = masterDataService;
        }
        public IActionResult Index()
        {
            TempData["LoginDate"]=_globalValue.GetGlobalVariables().PubLoginDate;
            ViewBag.CurrentMenu = "Sales Order";
            var permissions = _moduleService.GetUserMenuPermissions();
            var userLevel = _moduleService.GetUserLevel();

            var model = new UserMenuPermissionsViewModel
            {
                UserMenuPermissions = permissions,
                UserLevel = userLevel
            };
            return View("~/Views/Sales/Transaction/SalesOrder/Index.cshtml", model);             
        }
        [HttpGet]
        public async Task<IActionResult> GetMaxVNo(string V_type)
        {
            var dataList = await _masterDataservice.GetMaxVNoAsync(V_type, "ORDER1");
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetSaudaDataList(string saudaNo)
        {           
            try
            {
                var userSessionDt = _globalValue.GetGlobalVariables();
                var companyCd = userSessionDt.PubCompCode;
                var yearCd = userSessionDt.PubFYearCode;
                string strqry = $@"
               SELECT YEAR_CODE, COMP_CODE, BRANCH_CODE, V_TYPE, V_NO, V_DATE, DOC_ID, STATUS, PARTY_CODE, AGENT_CODE, PARTY_TO, ADD1, ADD2, ADD3, CITY_CODE, PHONE, ITEM_TYPE, ITEM_CODE, TRUCK_NO, QTY, RATE, DEFECTIVE_GOODS, DISC_PER, FRT_TERM, TAX_TERM, FRT_RATE, TAX_RATE, NET_RATE, PAYTERM_CODE, CD_DAYS
              ,DEL_TERM, REMARK, SALE_REMARK, SALE_QTY, ORD_QTY, GATE_QTY, SAUDA_TRUCK, SALE_TRUCK, WASTE_PER, FAPROV_STATUS, FAPROV_REMARKS, HOLD_PAY, TENACITY_GRPCODE, TENACITY_GRP, PINO, OFFERNO, GRADE, BROKER, BROKER_RATE, DELIVERY_TERMIMP, DISPATCH_FROM, PACK_TYPE, PAYMENT_STATUS, SBLC_DUEDATE, LC_DUEDATE, ITEM_REMARKS, DEAL_THROUGH, UUSER, UDATE, EUSER, EDATE, AED, SRNO, REF_TYPE, REF_NO, WSID, LIP, LID, FLAKES_SIZE, FLAKES_PVCPPM, FLAKES_PPMALL, FLAKES_SIZEMAX, FLAKES_USETYPE, DELIVERY_DAYS, ATTACHMENT_PATH, DEL_STATION, DEL_PORT, SIZE
              ,INCOTERM, CURRENCY, REF_QCTYPE, REF_QCNO, REF_REQTYPE, REF_REQNO, REF_ASTYPE, REF_ASNO, END_APPL, SHIP_CODE, SHIP_FROM, CDISC_PER, DISC_TYPE, ONLY_NATURAL, SAUDA_TYPE, SHIP_TYPE, EXPV_TYPE, EXPV_NO, CONT_STUFFWT, CONTAINER_NOS, STUFFING_WT, EXRATE, PIDATE, TAX_CODE FROM ERPDB.dbo.SAUDA
              where COMP_CODE={companyCd} and YEAR_CODE={yearCd} and BRANCH_CODE=1 and DOC_ID='{saudaNo}' ";
                var dataList = await _dbHelper.GetJsonDataAsync(strqry);

                string strqry1 = $@"select a.ITEM_CODE,b.SHORTNAME,b.Name ItemName,a.QTY,a.v_type,a.v_no,a.rate,c.name 'Party',c.Tenacity_type,a.Tenacity_Grp
                     from sauda a  
                     left join ITEM_mast b on b.CODE = a.ITEM_CODE and b.COMP_CODE =a.COMP_CODE  
                     left join subgroup_mast c on c.CODE = a.Party_Code and c.COMP_CODE =a.COMP_CODE  
                     where a.v_Type='SAUD' and a.DOC_ID='{saudaNo}'
                     and a.COMP_CODE=  {companyCd}  
                     and a.BRANCH_CODE=  1";
                var dataList1 = await _dbHelper.GetJsonDataAsync(strqry1);
                
                return Json(new { status = true, data = dataList, saudaDetails=dataList1 });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message ="data load failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSaudaNoList()
        {
            var dataList = await _masterDataservice.GetSaudaNoListAsync("SAUD");
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetItemList()
        {
            var dataList = await _masterDataservice.GetItemListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetTenacityList()
        {
            var dataList = await _masterDataservice.GetTenaCityListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyList()
        {
            var dataList = await _masterDataservice.GetPartyListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetCityList()
        {
            var dataList = await _masterDataservice.GetCityListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentTermList()
        {
            var dataList = await _masterDataservice.GetPaymentTermListAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetPartyAddress(int partyCd)
        {
            try
            {
                var UserLoginData = _globalValue.GetGlobalVariables();
                var PartyAddList = await _dbHelper.GetJsonDataAsync($@"select distinct sg.code, sg.ADD1,sg.ADD2,sg.ADD3,sg.PINCODE, isnull(cm.NAME, '') as CityName ,sg.CITY_CODE,sg.GSTIN from SUBGROUP_ADDRESS sg left join CITY_MAST cm on sg.CITY_CODE=cm.CODE  where sg.COMP_CODE={UserLoginData.PubCompCode}  and sg.code={partyCd} order by ADD1  ");
                return Json(new { status = true, data = PartyAddList });
            }
            catch (Exception ex)
            {
                return Json(new { status = true, message = "data load failed" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderRecordsById(string id)
        {
            try
            {

                if (string.IsNullOrEmpty(id))
                {
                    return Json(new { status = false, message = "Invalid ID" });
                }
                var userSession = _globalValue.GetGlobalVariables();
                string VType = id.Substring(0, 4);
                string VNo = id.Substring(4);
                var parametersHeader = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", VType},
                { "@V_NO", int.Parse(VNo) },
                { "@Action", "PurchaseOrderHeader" }
                };

                var parametersDetail = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", VType},
                { "@V_NO", int.Parse(VNo) },
                { "@Action", "PurchaseOrderDetail" }
                };

                var parametersAttachment = new Dictionary<string, object>
                {
                { "@COMP_CODE", int.Parse(userSession.PubCompCode) },
                { "@YEAR_CODE", int.Parse(userSession.PubFYearCode) },
                { "@BRANCH_CODE", 1 },
                { "@V_TYPE", VType},
                { "@V_NO", int.Parse(VNo) },
                { "@Action", "PurchaseOrderAttachment" }
                };

                var header = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersHeader);
                var detail = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersDetail);
                var attachment = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_PurchaseOrder]", parametersAttachment);

                return Json(new
                {
                    status = true,
                    header = header,
                    detail = detail,
                    attachment = attachment
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
        public async Task<IActionResult> SaveOrUpdateSalesOrder([FromBody] PurchaseOrder POmodel)
        {
            if (POmodel == null)
                return Json(new { status = false, message = " data save failed." });
            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                    DataTable purchaseOrderTable = FillDataTable(POmodel.ItemRecords, "[dbo].[Type_Order2]");
                    //DataTable purchaseOrderAttachmentTable = FillDataTable(POmodel.Attachments, "[dbo].[Type_order3]");
                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;
                        try
                        {
                            var docid = POmodel.DocId;
                            var vNo = docid.Substring(4);
                            var vtype = docid.Substring(0, 4);
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PurchaseOrder_AE]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Transaction = transaction;
                                
                                if (POmodel.SaveOrUpdate == "Save")
                                    cmd.Parameters.AddWithValue("@Action", "Add");
                                else
                                    cmd.Parameters.AddWithValue("@Action", "Edit");

                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@V_NO", vNo);
                                cmd.Parameters.AddWithValue("@V_TYPE", vtype);
                                cmd.Parameters.AddWithValue("@V_DATE", _dbHelper.Xnull(POmodel.VDate));
                                cmd.Parameters.AddWithValue("@DOC_ID", _dbHelper.Xnull(POmodel.DocId));
                                cmd.Parameters.AddWithValue("@PLACE_CODE", _dbHelper.Xnull(POmodel.PlaceCode));
                                cmd.Parameters.AddWithValue("@WB_TYPE", _dbHelper.Xnull(POmodel.WbType));
                                cmd.Parameters.AddWithValue("@WB_NO", _dbHelper.Xnull(POmodel.WbNo));
                                cmd.Parameters.AddWithValue("@PARTY_CODE", _dbHelper.Xnull(POmodel.PartyCode));
                                cmd.Parameters.AddWithValue("@SHIP_CODE", _dbHelper.Xnull(POmodel.ShipCode));
                                cmd.Parameters.AddWithValue("@SHIP_FROM", _dbHelper.Xnull(POmodel.ShipFrom));
                                cmd.Parameters.AddWithValue("@PRICE_TYPE", _dbHelper.Xnull(POmodel.PriceType));
                                cmd.Parameters.AddWithValue("@PARTY_REF", _dbHelper.Xnull(POmodel.PartyRef));
                                cmd.Parameters.AddWithValue("@IMPORT_CURRENCY", _dbHelper.Xnull(POmodel.ImportCurrency));
                                cmd.Parameters.AddWithValue("@EXRATE", _dbHelper.Xnull(POmodel.ExRate));                               
                                cmd.Parameters.AddWithValue("@DELIVERY_TO", _dbHelper.Xnull(POmodel.DeliveryTo));
                                cmd.Parameters.AddWithValue("@REMARKS", _dbHelper.Xnull(POmodel.Remarks));
                                cmd.Parameters.AddWithValue("@POTYPE", _dbHelper.Xnull(POmodel.PoType));
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", _dbHelper.Xnull(POmodel.FAProvStatus));
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", _dbHelper.Xnull(POmodel.FAProvRemarks));
                                cmd.Parameters.AddWithValue("@MAILSEND", _dbHelper.Xnull(POmodel.MailSend));
                                cmd.Parameters.AddWithValue("@CDISC_AMT", _dbHelper.Xnull(POmodel.CDiscAmt));
                                cmd.Parameters.AddWithValue("@AUTOGEN_PO", _dbHelper.Xnull(POmodel.AutoGenPo));
                                cmd.Parameters.AddWithValue("@POACCEPT_FLG", _dbHelper.Xnull(POmodel.PoAcceptFlg));
                                cmd.Parameters.AddWithValue("@POATTACH_PATH", _dbHelper.Xnull(POmodel.PoAttachPath));
                                cmd.Parameters.AddWithValue("@POATTCH_DATE", _dbHelper.Xnull(POmodel.PoAttachDate));
                                cmd.Parameters.AddWithValue("@BILL_ADD1", _dbHelper.Xnull(POmodel.BillAdd1));
                                cmd.Parameters.AddWithValue("@BILL_ADD2", _dbHelper.Xnull(POmodel.BillAdd2));
                                cmd.Parameters.AddWithValue("@BILL_ADD3", _dbHelper.Xnull(POmodel.BillAdd3));
                                cmd.Parameters.AddWithValue("@BILL_CITY", _dbHelper.Xnull(POmodel.BillCity));
                                cmd.Parameters.AddWithValue("@BILL_PINCODE", _dbHelper.Xnull(POmodel.BillPincode));
                                cmd.Parameters.AddWithValue("@BILL_GST", _dbHelper.Xnull(POmodel.BillGst));
                                cmd.Parameters.AddWithValue("@SHIP_ADD1", _dbHelper.Xnull(POmodel.ShipAdd1));
                                cmd.Parameters.AddWithValue("@SHIP_ADD2", _dbHelper.Xnull(POmodel.ShipAdd2));
                                cmd.Parameters.AddWithValue("@SHIP_ADD3", _dbHelper.Xnull(POmodel.ShipAdd3));
                                cmd.Parameters.AddWithValue("@SHIP_CITY", _dbHelper.Xnull(POmodel.ShipCity));
                                cmd.Parameters.AddWithValue("@SHIP_PINCODE", _dbHelper.Xnull(POmodel.ShipPincode));
                                cmd.Parameters.AddWithValue("@SHIP_GST", _dbHelper.Xnull(POmodel.ShipGst));
                                cmd.Parameters.AddWithValue("@TAX_CODE", _dbHelper.Xnull(POmodel.TaxCode));
                                cmd.Parameters.AddWithValue("@ITEM_TYPE", _dbHelper.Xnull(POmodel.ItemType));
                                cmd.Parameters.AddWithValue("@SUPPLY_TYPE", _dbHelper.Xnull(POmodel.SupplyType));
                                cmd.Parameters.AddWithValue("@TRAN_TYPE", _dbHelper.Xnull(POmodel.TranType));
                                cmd.Parameters.AddWithValue("@FORM_CODE", _dbHelper.Xnull(POmodel.FormCode));
                                cmd.Parameters.AddWithValue("@VEHICLE_NO", _dbHelper.Xnull(POmodel.VehicleNo));
                                cmd.Parameters.AddWithValue("@INV_TYPE", _dbHelper.Xnull(POmodel.InvType));
                                cmd.Parameters.AddWithValue("@INV_NO", _dbHelper.Xnull(POmodel.InvNo));
                                cmd.Parameters.AddWithValue("@PARTY_NAME", _dbHelper.Xnull(POmodel.PartyName));
                                cmd.Parameters.AddWithValue("@SHIP_NAME", _dbHelper.Xnull(POmodel.ShipName));
                                cmd.Parameters.AddWithValue("@status", 1);
                                cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);                             
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
            DataTable PurchaseOrderTbl = ToEmptyDataTable(typeName);

            switch (typeName)
            {
                case "[dbo].[Type_Order2]":
                    foreach (var detail in data.Cast<Order2>())
                    {
                        PurchaseOrderTbl.Rows.Add(
                        detail.SNO,
                        detail.ItemName,
                        detail.ItemCode,
                        detail.NOS,
                        detail.Qty,
                        detail.Rate,
                        detail.Amount,
                        detail.Status,
                        detail.Remarks,
                        detail.DeliveryDate,
                        detail.SaudaType,
                        detail.SaudaNo,
                        detail.TenacityGrpCode,
                        detail.TenacityType,
                        detail.TenacityCode,
                        detail.TenacityName
                    );
                    }
                    break;

                default:
                    PurchaseOrderTbl = null;
                    break;
            }
            return PurchaseOrderTbl;

        }

        private DataTable ToEmptyDataTable(string typeName)
        {
            var dt = new DataTable();
            switch (typeName)
            {

                case "[dbo].[Type_Order2]":
                    dt.Columns.Add("SNO", typeof(int));
                    dt.Columns.Add("ITEM_NAME", typeof(string));
                    dt.Columns.Add("ITEM_CODE", typeof(int));
                    dt.Columns.Add("NOS", typeof(int));
                    dt.Columns.Add("QTY", typeof(decimal));
                    dt.Columns.Add("RATE", typeof(decimal));
                    dt.Columns.Add("AMOUNT", typeof(decimal));
                    dt.Columns.Add("STATUS", typeof(int));
                    dt.Columns.Add("REMARKS", typeof(string));
                    dt.Columns.Add("DELIVERY_DATE", typeof(DateTime));
                    dt.Columns.Add("SAUDA_TYPE", typeof(string));
                    dt.Columns.Add("SAUDA_NO", typeof(int));
                    dt.Columns.Add("TENACITY_GRPCODE", typeof(int));
                    dt.Columns.Add("TENACITY_TYPE", typeof(string));
                    dt.Columns.Add("TENACITY_CODE", typeof(int));
                    dt.Columns.Add("TENACITY_NAME", typeof(string));
                    break;

                default:
                    throw new ArgumentException("Unknown table type: " + typeName);
            }
            return dt;
        }


    }
}
 