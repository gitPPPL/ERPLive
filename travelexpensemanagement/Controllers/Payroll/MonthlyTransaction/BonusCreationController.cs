using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.Payroll.MonthlyTransaction
{
    public class BonusCreationController : Controller
    {

        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Controllers.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        public BonusCreationController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
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
            return View("~/Views/Payroll/MonthlyTransaction/BonusCreation/Index.cshtml");
        }


        [HttpPost]
        public IActionResult CreateBonus([FromBody] ParamBonus bonus)
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();
                var companyCode = g.PubCompCode;
                var yearCode = g.PubFYearCode;
                var branchCode = "1";
                
                var fys = new DateTime(DateTime.Now.Year, 4, 1);
                var fye = new DateTime((DateTime.Now.Year +1) , 3, 31);

                using var con = _dbConnection.GetErpConnection();
                con.Open();
                using var cmd = new Microsoft.Data.SqlClient.SqlCommand("usp_CreateBonus", con);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@pubCompCode", companyCode);
                cmd.Parameters.AddWithValue("@pubFYearCode", yearCode);
                cmd.Parameters.AddWithValue("@pubFYStartDate",fys );
                cmd.Parameters.AddWithValue("@pubFYEndDate", fye);
                cmd.Parameters.AddWithValue("@pubBranchCode", branchCode);
                cmd.Parameters.AddWithValue("@pubUserId", g.PubUserId );
                cmd.Parameters.AddWithValue("@txtBonusLimit", bonus.BonusLimit);
                cmd.Parameters.AddWithValue("@txtWagesLimit", bonus.WagesLimitMonthly);
                cmd.Parameters.AddWithValue("@txtBonusAppl", bonus.BonusApplYearly );
                cmd.Parameters.AddWithValue("@txtBonusPer", bonus.BonusPer);
                var r=  cmd.ExecuteNonQuery();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false });
            }

            
        }


        [HttpPost]
        public IActionResult DecreateBonus()
        {
            try
            {
                var g = _globalVariableService.GetGlobalVariables();

                var companyCode = g.PubCompCode;
                var yearCode = g.PubFYearCode;
                var branchCode = "1";
            
                using var con = _dbConnection.GetErpConnection();
                con.Open();

                string qry = " DELETE FROM PAY_BONUS WHERE  COMP_CODE = @pubCompCode and BRANCH_CODE = @pubBranchCode  and YEAR_CODE=  @pubFYearCode ";

                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(qry, con);
                cmd.CommandType = System.Data.CommandType.Text;

                cmd.Parameters.AddWithValue("@pubCompCode", companyCode);
                cmd.Parameters.AddWithValue("@pubFYearCode", yearCode);
                cmd.Parameters.AddWithValue("@pubBranchCode", branchCode);
               

                var r = cmd.ExecuteNonQuery();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false , message = ex.Message});
            }
        }


    }


    public class ParamBonus
    {
        public decimal WagesLimitMonthly { get; set; }
        public decimal BonusApplYearly { get; set; }
        public decimal BonusLimit { get; set; }
        public decimal BonusPer { get; set; }
    }

}
