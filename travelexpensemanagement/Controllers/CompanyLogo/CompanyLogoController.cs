using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Controllers.CompanyLogo
{
    public class CompanyLogoController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly travelexpensemanagement.Common.DropdownService.DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        public CompanyLogoController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
    travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
        }
        public IActionResult Index()
        {
            return View("~/Views/CompanyLogo/Index.cshtml");
        }

        [HttpGet]
        public IActionResult CompanyDDL()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "select CODE, NAME from COMP_MAST";
            var company = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, list = company });
        }

        [HttpPost]
        public IActionResult UploadLogo(int compCode, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "No file selected" });

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    byte[] imageBytes;

                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        imageBytes = ms.ToArray();
                    }

                    SqlCommand cmd = new SqlCommand(@"
                        UPDATE COMP_MAST 
                        SET COMP_LOGO = @COMP_LOGO
                        WHERE CODE = @CODE", con);

                    cmd.Parameters.Add("@COMP_LOGO", System.Data.SqlDbType.VarBinary).Value = imageBytes;
                    cmd.Parameters.AddWithValue("@CODE", compCode);

                    cmd.ExecuteNonQuery();
                }

                return Json(new { success = true, message = "Logo uploaded successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCompanyLogo(int compCode)
        {
            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    SqlCommand cmd = new SqlCommand(@"
                        SELECT COMP_LOGO 
                        FROM COMP_MAST 
                        WHERE CODE = @CODE", con);

                    cmd.Parameters.AddWithValue("@CODE", compCode);

                    var result = cmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                        return NotFound();

                    byte[] imageBytes = (byte[])result;

                    return File(imageBytes, "image/png");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
