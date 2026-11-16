using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Sales.Transaction;
using travelexpensemanagement.Repositories.Interfaces.Sales.Transaction;

namespace travelexpensemanagement.Controllers.Sales.Transaction
{
    public class DocumentEmailingController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DbHelper _dbHelper;
        private readonly IDocumentEmailingRepository _repository;

        public DocumentEmailingController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DbHelper dbHelper, IDocumentEmailingRepository repository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dbHelper = dbHelper;
            _repository = repository;
        }
        public IActionResult Index()
        {
            var gv = _globalVariableService.GetGlobalVariables();
            ViewBag.UserLevel = gv.PubUserLevel;
            return View("~/Views/Sales/Transaction/DocumentEmailing/Index.cshtml");
        }

        // ======================DROPDOWN======================
        public async Task<IActionResult> GetDropdown(string type, string data = "")
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string qry = "";

            switch (type.ToLower())
            {
                case "mail":
                    qry = $@"select code as value, USER_ID as text from EMAIL_SETTING1 where COMP_CODE={gv.PubCompCode} group by code, USER_ID order by user_id";
                    break;

                //case "documenttype":
                //    qry = $@"Select a.V_TYPE as value, b.NAME as text from email_setting1 a left join DOCTYPE_MAST b on a.V_Type=b.CODE where a.COMP_CODE=
                //            {gv.PubCompCode} group by a.V_TYPE, b.NAME order by b.NAME";
                //    break;
                case "documenttype":
                    {
                        // 'data' = selected Mail Type; empty = all document types
                        string where = (data ?? "").ToLower() switch
                        {
                            "salesinvoicecustomer" or "salesinvoiceagent" => "and b.doctype='SalesInvoice'",
                            "drnote" or "crnote" => "and b.doctype='PurchaseInvoice'",
                            "packingslip" or "packingslipexcel" => "and b.doctype='FinishIssue'",
                            "partybalance" => "and a.v_type='BPMS'",
                            _ => ""
                        };

                        qry = $@"Select a.V_TYPE as value, b.NAME as text from email_setting1 a left join DOCTYPE_MAST b on a.V_Type=b.CODE
                                where a.COMP_CODE={gv.PubCompCode} {where} group by a.V_TYPE, b.NAME order by b.NAME";
                        break;
                    }

                case "party":
                    qry = $@"Select code as value, NAME as text from SUBGROUP_MAST where ACTIVE=1 and NATURE not in ('CASH','BANK','OTHERS') and COMP_CODE={gv.PubCompCode} 
                            order by NAME";
                    break;

                case "agent":
                    qry = $@"Select code as value, NAME as text from SUBGROUP_MAST where ACTIVE=1 and NATURE in ('BROKER') and COMP_CODE={gv.PubCompCode} order by NAME";
                    break;

                // Sales Invoice Agent Checked
                case "agentbysales":
                    qry = $@"Select a.AGENT_CODE as value, b.NAME as text from SALE1 a left join SUBGROUP_MAST b on a.AGENT_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE
                            where b.ACTIVE=1 and b.NATURE in ('BROKER') and a.COMP_CODE={gv.PubCompCode} group by a.AGENT_CODE, b.NAME";
                    break;

                // Sales Invoice Customer Checked
                case "customerbysales":
                    qry = $@"Select a.BILL_CODE as value, b.NAME as text from SALE1 a left join SUBGROUP_MAST b on a.AGENT_CODE=b.CODE and a.COMP_CODE=b.COMP_CODE
                            where b.ACTIVE=1 and b.NATURE in ('CUSTOMER') and a.COMP_CODE={gv.PubCompCode} group by a.BILL_CODE, b.NAME";
                    break;

                case "acgroup":
                    qry = $@"Select code as value, NAME as text from mgroup_mast where ACTIVE=1 and NATURE in ('customer','supplier','Broker') and COMP_CODE={gv.PubCompCode}
                            order by NAME";
                    break;

                case "itemtype":
                    qry = $@"Select Distinct SALE_GROUP as value, SALE_GROUP as text from ITEM_GROUP where comp_code={gv.PubCompCode} and SALE_GROUP <> '' order by SALE_GROUP";
                    break;

                default:
                    return Json(new { success = false, message = "Invalid dropdown type." });
            }

            var result = await _dbHelper.GetJsonDataAsync(qry);
            return Json(result);
        }


        // ======================MAIL TYPE======================
        [HttpGet]
        public IActionResult GetMailTypeInfo(string mailType)
        {
            var gv = _globalVariableService.GetGlobalVariables();

            string footer = $"<BR><BR><BR>Regards,<BR>{gv.CompanyName}<BR>{gv.Address1}<BR>{gv.Address2}";
            string docType, message;

            switch ((mailType ?? "").ToLower())
            {
                case "salesinvoicecustomer":
                case "salesinvoiceagent":
                    docType = "SaleInvoice";
                    message = "Please find attached invoice(s) for product dispatches." + footer;
                    break;

                case "drnote":
                    docType = "DrNote";
                    message = "Please find attached Debit Note." + footer;
                    break;

                case "crnote":
                    docType = "CrNote";
                    message = "Please find attached Credit Note." + footer;
                    break;

                case "packingslip":
                case "packingslipexcel":
                    docType = "SaleInvoice";
                    message = "Please find attached packing slip for product dispatches." + footer;
                    break;

                case "partybalance":
                    docType = "Approval";
                    message = "Please find attached document(s) for Confirmation of account balance." + footer;
                    break;

                default:
                    return Json(new { success = false, message = "Invalid mail type." });
            }

            return Json(new { success = true, docType, message });
        }


        // ======================FILL DATA======================
        [HttpGet]
        public async Task<IActionResult> FillData(string mailType, string docType, DateTime fromDate, DateTime toDate, int[] partyCodes,
            int agentCode = 0, string itemType = "", int acGroup = 0)
        {
            var res = await _repository.FillData(mailType, docType, fromDate, toDate, partyCodes, agentCode, itemType, acGroup);

            if (!res.status)
                return Json(new { success = false, message = res.message });

            return Json(new { success = true, data = res.data });
        }


        // ======================GLOBAL VARIABLES======================
        [HttpGet]
        public async Task<JsonResult> getGlobalValues()
        {
            try
            {
                var gv = _globalVariableService.GetGlobalVariables();
                var gs = await _globalVariableService.LoadGeneralSetting();
                
                string databaseName;
                using (var connection = _dbConnection.GetErpConnection())
                {
                    databaseName = connection.Database; // Get the database name
                }

                var response = new
                {
                    compCode = gv.PubCompCode,
                    yearCode = gv.PubFYearCode,
                    branchCode = gv.PubBranchCode,
                    add1 = gv.Address1,
                    add2 = gv.Address2,
                    companyName = gv.CompanyName,
                    db = databaseName,
                    phone = gv.Phone,
                    email = gv.Email,
                };

                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // ======================EXPORT INVOICE======================
        [HttpGet]
        public async Task<IActionResult> PrepareInvoiceExport(string vType, long vNo)
        {
            var res = await _repository.PrepareInvoiceExport(vType, vNo);

            if (!res.status)
                return Json(new { success = false, message = res.message });

            dynamic d = res.data;
            return Json(new { success = true, reportName = d.reportName, selectionFormula = d.selectionFormula, parameters = d.parameters });
        }


        // ======================SEND MAIL======================
        [HttpPost]
        public async Task<IActionResult> SendMail([FromBody] SendMailRequest req)
        {
            var res = await _repository.SendMail(req);

            if (!res.status)
                return Json(new { success = false, message = res.message });

            return Json(new { success = true, results = res.data });
        }
    }
}
