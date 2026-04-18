using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Payroll.Transaction;
using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Controllers.Payroll.Transaction
{
    public class EmployeeCurrentStatusController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private readonly travelexpensemanagement.Services.IMasterDataService _masterDataService;
        public EmployeeCurrentStatusController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue, ModuleService.ModuleService moduleService, Services.IMasterDataService masterDataService)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
            _moduleService = moduleService;
            _masterDataService = masterDataService;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Transaction/EmployeeCurrentStatus/Index.cshtml");
        }
               
        [HttpGet]
        public async Task<IActionResult> GetMaxVNo()
        {
            var maxVnoData = await _masterDataService.GetMaxVNoAsync("EMPS", "PAY_EMPSTATUS");
            return Json(maxVnoData);

        }

        [HttpGet]
        public async Task<IActionResult> GetDocType()
        {
            var doctype = await _masterDataService.GetDocTypeAsync("EMPSTATUS");
            return Json(doctype);

        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeMast()
        {
            var employeeList = await _masterDataService.GetEmployeeMastAsync();
            return Json(employeeList);
        }
        [HttpGet]
        public async Task<IActionResult> GetDesignMastList()
        {
            var designList = await _masterDataService.GetDesignationMastAsync();
            return Json(designList);
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentMast()
        {
            var dataList = await _masterDataService.GetEmployeeDepartMastAsync();
            return Json(dataList);
        }

        [HttpGet]
        public async Task<IActionResult> GetshiftList()
        {
            var shiftList = await _masterDataService.GetShiftMastAsync();
            return Json(shiftList);
        }

        [HttpGet]
        public async Task<IActionResult> LoaddataByFilter(string departmentId, string categoryId, string date, string shiftData)
        {
            try
            {
                var strCondition = "";                
                if (categoryId != "" && categoryId != null)
                {
                    strCondition += @$"and b.cat_Code={categoryId}";
                }
                if (shiftData !="" && shiftData != null)
                {
                    if (shiftData == "A")
                    {
                        strCondition += " and a.In_Time <= 1100";
                    }
                    else
                    {
                        strCondition += " and a.In_Time > 1900";
                    }
                }
                var compCode= _globalValue.GetGlobalVariables().PubCompCode;
                string strqry = @$"
                Select a.Emp_code,b.Name Emp_name,b.Dept_Code,c.name Deptname,b.Desg_Code,d.name Desgname,format(a.V_Date,'dd/MM/yyyy')In_Date,Max(a.In_Time)In_Time 
                 from PAY_TIMEDATA a  
                 Left Join Emp_mast b on a.Emp_code=b.code and a.Comp_code=b.Comp_Code  
                 Left Join Dept_mast c on b.Dept_Code=c.code and b.comp_code=c.comp_code  
                 Left join Desg_mast d on b.Desg_code=d.code and b.comp_code=d.Comp_code  
                 Where a.V_type='MACD' and a.Comp_Code={compCode} and b.Dept_Code={departmentId} and a.V_Date='{date}' {strCondition}
                group by a.Emp_code,b.Name,b.Dept_Code,c.name,b.Desg_Code,d.name,a.v_date Order by b.Dept_Code,a.Emp_code";

                var dataList = await _dbHelper.GetJsonDataAsync(strqry);

                return Json(new
                {
                    status=true,
                    data=dataList
                });

            }
            catch(Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = "data load failed"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmpCurrentStatusById(string id)
        {
            try
            {
                var usersession = _globalValue.GetGlobalVariables();               
                var parameter1 = new Dictionary<string, object> {
                    {"@COMP_CODE", usersession.PubCompCode},
                    {"@YEAR_CODE", usersession.PubFYearCode},
                    {"@BRANCH_CODE", 1},
                    {"@DOC_ID", id},
                    {"@Action", "EmpCurrentStatusForUpdate"}
                };          
                var detaillist = await _dbHelper.GetJsonFromProcedureAsync("[dbo].[sp_GetEmpStatusListEntry]", parameter1);
                return Json(new { status = true,  data = detaillist });

            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "data load failed" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateEmpCurrentStatusEntry([FromBody] PayEmpStatus model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request: Model is null." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();

                    
                        try
                        {
                            DataTable prod2Table = await ToDataTable(model.payEmpStatusDetails);
 
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PayEmpStatus]", con))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                 
                                cmd.Parameters.AddWithValue("@YEAR_CODE", usersessionDt.PubFYearCode);
                                cmd.Parameters.AddWithValue("@COMP_CODE", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@BRANCH_CODE", 1);
                                cmd.Parameters.AddWithValue("@V_TYPE", model.V_TYPE);
                                cmd.Parameters.AddWithValue("@V_NO", model.V_NO);
                                cmd.Parameters.AddWithValue("@V_DATE", model.V_DATE);
                                cmd.Parameters.AddWithValue("@DOC_ID", model.DOC_ID ?? (object)DBNull.Value); 
                                cmd.Parameters.AddWithValue("@SHIFT", model.SHIFT ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@USER", usersessionDt.PubUserId);
                                cmd.Parameters.AddWithValue("@WSID", Environment.MachineName);
                                cmd.Parameters.AddWithValue("@LIP", usersessionDt.PubLocalId);                          
                                cmd.Parameters.AddWithValue("@EmpStatusTable", prod2Table);

                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 5000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);

                                var returnParam = new SqlParameter
                                {
                                    Direction = ParameterDirection.ReturnValue,
                                    SqlDbType = SqlDbType.Int
                                };
                                cmd.Parameters.Add(returnParam);
                                await cmd.ExecuteNonQueryAsync();

                                var returnValue = (int)returnParam.Value;
                                string errorMsg = errorParam.Value?.ToString();

                                if (returnValue > 0)
                                {
                                    
                                    return Json(new { status = true, message = "Data saved/updated successfully." });
                                }
                                else
                                {
                                    
                                    return Json(new { status = false, message = errorMsg ?? "Operation failed." });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            
                            return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Unexpected error: " + ex.Message });
            }
        }

        private async Task<DataTable> ToDataTable(List<PayEmpStatusDetail> data)
        {
            var table = new DataTable();
            table.Columns.Add("SNO", typeof(int));
            table.Columns.Add("EMP_CODE", typeof(int));
            table.Columns.Add("EMP_NAME", typeof(string));             
            table.Columns.Add("DEPT_CODE", typeof(int));
            table.Columns.Add("DESG_CODE", typeof(int));
            table.Columns.Add("TDEPT_CODE", typeof(int));
            table.Columns.Add("IN_DATE", typeof(DateTime));
            table.Columns.Add("IN_TIME", typeof(string));
            table.Columns.Add("STATUS", typeof(string));
            table.Columns.Add("REMARKS", typeof(string));

            int x = 1;
            foreach (var row in data)
            {
      table.Rows.Add(
      x,
      row.EMP_CODE ?? (object)DBNull.Value,
      row.EMP_NAME ?? (object)DBNull.Value,       
      row.DEPT_CODE ?? (object)DBNull.Value,
      row.DESG_CODE ?? (object)DBNull.Value,
      row.TDEPT_CODE ?? (object)DBNull.Value,
      row.IN_DATE ?? (object)DBNull.Value,
      row.IN_TIME ?? (object)DBNull.Value,
      row.STATUS ?? (object)DBNull.Value,
      row.REMARKS ?? (object)DBNull.Value
  );
                x++;
            }

            return table;
        }
 

    }
}
