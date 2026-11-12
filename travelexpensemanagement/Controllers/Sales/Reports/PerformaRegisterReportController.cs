using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Sales.Reports
{
    public class PerformaRegisterReportController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;

        public PerformaRegisterReportController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
       travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
       ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _globalValidationdate = globalValidationdate;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
        }

        public async Task<IActionResult> Index()
        {
            var globalVariables = _globalVariableService.GetGlobalVariables();

            var loadGeneralSetting = await _globalVariableService.LoadGeneralSetting();

            ViewBag.LoadGeneralSetting = loadGeneralSetting;

            string databaseName;

            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }

            ViewBag.GlobalVariables = globalVariables;
            ViewBag.DatabaseName = databaseName;

            return View("~/Views/Sales/Reports/PerformaRegisterReport/Index.cshtml");
        }

        public JsonResult DDlDocumentType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = "Select Code,Name from DOCTYPE_MAST where doctype in ('SalesInvoice','EComSalesInvoice','TradingSale','EComSalesReturn','SalesReturn') or code in ('CRSL','DNSL') order by name;";
                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlItemType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select DISTINCT ITEM_TYPE as 'No'  , ITEM_TYPE as 'Type'  from sale1 where comp_code={getdata.PubCompCode} AND YEAR_CODE={getdata.PubFYearCode} AND BRANCH_CODE={getdata.PubBranchCode} and ITEM_TYPE != '' order by ITEM_TYPE;";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult OSGroup()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from ACOS_MAST where comp_code={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlTransport()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from TRANSPORT_MAST where comp_code= {getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }

        public JsonResult DDlACGroup()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from MGROUP_MAST where comp_code={getdata.PubCompCode} and nature in ('Customer') order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlSupplyType()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from MGROUP_MAST where comp_code={getdata.PubCompCode} and nature in ('Customer') order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlCountry()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from COUNTRY_MAST order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }

        public JsonResult DDlItem()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, shortname  from ITEM_MAST where comp_code={getdata.PubCompCode} and shortname <>'' order by shortname";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlPartyName()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from SUBGROUP_MAST where comp_code={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }

        public JsonResult DDlcolur()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from COLOR_MAST where comp_code={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlDenierGram()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from ITEMGRAM_MAST where comp_code={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlGodown()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from GODOWN_MAST where comp_code={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlPTypeGroup()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name from ITEMPTYPE_MAST WHERE COMP_CODE={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlPItemMGroup()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select code, name  from ITEM_MGROUP where comp_code={getdata.PubCompCode} order by name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }
        public JsonResult DDlSaleThrough()
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                string query = @$"select distinct code,name from (
                  Select distinct b.code,b.NAME from Sauda a Left Join EMP_MAST b on a.DEAL_THROUGH=b.code and a.comp_code=b.comp_code 
                  where a.V_type='SAUD' and a.COMP_CODE={getdata.PubCompCode} and b.ACTIVE=1
                  union all Select distinct b.code,b.NAME from Sauda a Left Join SALESEXECUTIVE_MAST b on a.DEAL_THROUGH=b.code and 
                  a.comp_code=b.comp_code where a.V_type='SAUD' and a.COMP_CODE={getdata.PubCompCode} and
                  b.ACTIVE=1)x where x.CODE is not null Order by x.Name";

                var Data = _dropdownService.GetDropdownList(query);
                return Json(Data);
            }
        }

        public JsonResult DataInsert(bool SaleRegisterDrCrNote, bool TptFreightReport, DateTime FromDate, DateTime Todate, bool LoadUnLoadReport, bool WBReport, bool GateToInvoiceTime)
        {
            var getdata = _globalVariableService.GetGlobalVariables();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            {

                if (SaleRegisterDrCrNote == true)
                {
                    string query = @$"Truncate table Temp1 ; 

                        with xyz as( select comp_code,branch_code,year_code,doc_no,doc_type, sum(amount-DISC_AMT)CNamt,
                        sum(CGST_AMT+SGST_AMT+IGST_AMT)CNTax,0 DNAmt,0 DNTax from dc_note1 
                        where comp_code=@CompCode and Branch_code=@BranchCode and YEAR_CODE=@YearCode and v_type in ('CRSL')
                        group by comp_code,branch_code,year_code,doc_no,doc_type
                        union all 
                        select comp_code,branch_code,year_code,doc_no,doc_type,0,0, sum(amount-DISC_AMT),sum(CGST_AMT+SGST_AMT+IGST_AMT)
                        from dc_note1 where comp_code=@CompCode and Branch_code=@BranchCode  and YEAR_CODE=@YearCode and v_type in ('DNSL')
                        group by comp_code,branch_code,year_code,doc_no,doc_type)
                       
                        Insert into temp1 (sno1,sno2,sno3,sno4,item1,value1,value2,value3,value4)
                        select comp_code,branch_code,year_code,doc_no,doc_type,sum(DNAmt)DNAmt,sum(DNTax)DNTax,sum(CNamt)CNamt,
                        sum(CNTax)CNTax from xyz group by comp_code,branch_code,year_code,doc_no,doc_type ";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);

                        if (con.State != ConnectionState.Open)
                            con.Open();

                        int rowsAffected = cmd.ExecuteNonQuery();

                    }

                }

                if (TptFreightReport == true)
                {
                    string query = @$"delete from temp_gst ; 

                   insert into temp_gst(comp_code,v_type,v_no,v_date,form_code,bill_type) select comp_code,v_type,v_no,v_date,form_code,item_type 
                   from sale1 where v_date between @FromDate  and @Todate and comp_code=@CompCode and branch_code=@BranchCode 

                   update temp_gst set party_code=p.freight_ac from temp_gst s, posting_mast p where s.v_type=p.v_type and s.form_code=p.form_code 
                   and s.bill_type=p.post_type and p.doc_type='SALE' and s.comp_code=p.comp_code and s.comp_code=@CompCode
                  
                   update temp_gst set qty=transport_qt1.qty from temp_gst,do1,transport_qt1 where temp_gst.v_type=do1.ref_type and 
                   temp_gst.v_no=do1.ref_no and temp_gst.comp_code=do1.comp_code and do1.v_type=transport_qt1.do_type and do1.v_no=transport_qt1.do_no
                   and do1.comp_code=transport_qt1.comp_code and transport_qt1.v_date 
                   between @TransportFromDate and @Todate and transport_qt1.qty>0

                   update sale1 set incoterm=sauda.frt_term from sale1,sale2,sauda where sale1.comp_code = sale2.comp_code And 
                   sale1.v_type = sale2.v_type And sale1.v_no = sale2.v_no And sale1.year_code = sale2.YEAR_CODE and 
                   sale1.branch_code=sale2.BRANCH_CODE and sauda.COMP_CODE=sale2.COMP_CODE and sauda.V_NO=sale2.SAUDA_NO and 
                   SAUDA.V_TYPE=sale2.SAUDA_TYPE and SAUDA.BRANCH_CODE=sale2.BRANCH_CODE and (sale1.INCOTERM is null or sale1.INCOTERM='') and 
                   sale1.COMP_CODE=@CompCode and sale2.SAUDA_TYPE='SAUD' AND SALE1.V_TYPE='SAGT' AND SALE2.SAUDA_NO>0 AND 
                   SALE1.V_DATE BETWEEN @FromDate AND @Todate ";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate);
                        cmd.Parameters.AddWithValue("@Todate", Todate);
                        cmd.Parameters.Add("@TransportFromDate", SqlDbType.DateTime).Value = Convert.ToDateTime(FromDate).AddDays(-10);

                        if (con.State != ConnectionState.Open)
                            con.Open();

                        int rowsAffected = cmd.ExecuteNonQuery();
                    }
                }

                if (LoadUnLoadReport == true)
                {
                    string query = @$"delete from temp_gst ;  
                    insert into temp_gst(comp_code,v_type,v_no,v_date,form_code,bill_type) select comp_code,v_type,v_no,
                    v_date,form_code,item_type from sale1 where 
                    v_date between @FromDate and @Todate and comp_code=@CompCode and branch_code=@BranchCode
                     
                    update temp_gst set party_code=p.loading_ac from temp_gst s, posting_mast p where s.v_type=p.v_type and 
                    s.form_code=p.form_code and s.bill_type=p.post_type and p.doc_type='SALE' and s.comp_code=p.comp_code and 
                    s.comp_code=@CompCode ";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate);
                        cmd.Parameters.AddWithValue("@Todate", Todate);
                        cmd.Parameters.Add("@TransportFromDate", SqlDbType.DateTime).Value = Convert.ToDateTime(FromDate).AddDays(-10);

                        if (con.State != ConnectionState.Open)
                            con.Open();

                        int rowsAffected = cmd.ExecuteNonQuery();
                    }
                }

                if (WBReport == true)
                {

                    string query = @$"delete from temp_gst ; 
                    insert into temp_gst(comp_code,v_type,v_no,v_date,form_code,bill_type) select comp_code,v_type,v_no,v_date,form_code,
                    item_type from sale1 where v_date between @FromDate and @Todate and 
                    comp_code=@CompCode and branch_code=@BranchCode
                  
                    update temp_gst set party_code=p.wb_ac from temp_gst s, posting_mast p where s.v_type=p.v_type and s.form_code=p.form_code 
                    and s.bill_type=p.post_type and p.doc_type='SALE' and s.comp_code=p.comp_code and s.comp_code=@CompCode ";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate);
                        cmd.Parameters.AddWithValue("@Todate", Todate);
                        cmd.Parameters.Add("@TransportFromDate", SqlDbType.DateTime).Value = Convert.ToDateTime(FromDate).AddDays(-10);

                        if (con.State != ConnectionState.Open)
                            con.Open();

                        int rowsAffected = cmd.ExecuteNonQuery();
                    }



                }

                if (GateToInvoiceTime == true)
                {
                    string query = @$"delete from workflow ; 
                    insert into workflow(comp_code,branch_code,year_code,v_type,v_no,pass_time,pq_time) select comp_code,branch_code,year_code,
                    v_type,v_no,udate,udate from sale1 where V_DATE between @FromDate and @Todate
                    AND COMP_CODE= @CompCode and BRANCH_CODE= @BranchCode and year_code=@YearCode
 
                    update workflow set mrn_type=do1.v_type,mrn_no=do1.v_no from workflow, do1 where workflow.comp_code=do1.comp_code 
                    and workflow.branch_code=do1.branch_code and workflow.year_code=do1.year_code and workflow.v_type=do1.ref_type and 
                    workflow.v_no=do1.ref_no and do1.v_type in ('DOGT')""
                    
                    update workflow set gate_time=gate1.udate   from workflow, gate1 where  workflow.comp_code=gate1.comp_code 
                    and workflow.branch_code=gate1.branch_code and workflow.year_code=gate1.year_code and workflow.mrn_type=gate1.disp_plan_type
                    and workflow.mrn_no=gate1.disp_plan_no and gate1.v_type in ('TRGI')

                    update workflow set gateout_time=gate1.out_time  from workflow, gate1 where  workflow.comp_code=gate1.comp_code and 
                    workflow.branch_code=gate1.branch_code and workflow.year_code=gate1.year_code and workflow.mrn_type=gate1.disp_plan_type 
                    and workflow.mrn_no=gate1.disp_plan_no and gate1.v_type in ('TRGI')

                    update workflow set wb_time=wb1.udate from workflow, wb1 where workflow.comp_code=wb1.comp_code and 
                    workflow.branch_code=wb1.branch_code and workflow.year_code=wb1.year_code and workflow.mrn_type=wb1.gate_type and 
                    workflow.mrn_no=wb1.gate_no and wb1.v_type in ('KANT') 

                    update workflow set mrn_time=wb1.status_date  from workflow, wb1 where workflow.comp_code=wb1.comp_code 
                    and workflow.branch_code=wb1.branch_code and workflow.year_code=wb1.year_code and workflow.mrn_type=wb1.gate_type 
                    and workflow.mrn_no=wb1.gate_no and wb1.v_type in ('KANT') and wb1.status in (3) ";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@CompCode", getdata.PubCompCode);
                        cmd.Parameters.AddWithValue("@BranchCode", getdata.PubBranchCode);
                        cmd.Parameters.AddWithValue("@YearCode", getdata.PubFYearCode);
                        cmd.Parameters.AddWithValue("@FromDate", FromDate);
                        cmd.Parameters.AddWithValue("@Todate", Todate);
                        cmd.Parameters.Add("@TransportFromDate", SqlDbType.DateTime).Value = Convert.ToDateTime(FromDate).AddDays(-10);

                        if (con.State != ConnectionState.Open)
                            con.Open();

                        int rowsAffected = cmd.ExecuteNonQuery();
                    }

                }

                return Json(new { success = true });
            }
        }


    }
}













