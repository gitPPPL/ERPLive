
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.ModuleService;

namespace travelexpensemanagement.Controllers
{
    public class PermissionController : Controller
    {
        private readonly ModuleService.ModuleService _moduleService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;

        public PermissionController(
            ModuleService.ModuleService moduleService,
            IHttpContextAccessor httpContextAccessor, GlobalVariableService globalVariableService, DataBaseConnection dbConnection)
        {
            _moduleService = moduleService;
            _httpContextAccessor = httpContextAccessor;
            _globalVariableService = globalVariableService;
            _dbConnection = dbConnection;

        }

        [HttpGet]
        public IActionResult GetCurrentMenuPermission(string controllerName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(controllerName))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Controller name is required."
                    });
                }

                controllerName = controllerName.Trim();

                var globalVar = _globalVariableService.GetGlobalVariables();

                int menuId = 0;

                using (SqlConnection conn = _dbConnection.GetErpConnection())
                {
                    conn.Open();

                    const string query = @"SELECT CODE
                                   FROM MENU_MAST
                                   WHERE LTRIM(RTRIM(WebFORM_NAME)) = @WebFORM_NAME";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@WebFORM_NAME", controllerName);

                        var result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                        {
                            return Json(new
                            {
                                success = false,
                                message = "Menu not found."
                            });
                        }

                        menuId = Convert.ToInt32(result);
                    }
                }

                // Admin User
                if (globalVar.PubUserLevel == "1")
                {
                    return Json(new
                    {
                        success = true,
                        add = true,
                        edit = true,
                        delete = true,
                        print = true,
                        export = true,
                        mail = true,
                        approval = true,
                        docdetail = true
                    });
                }

                var permission = _moduleService
                    .GetUserMenuPermissions()
                    .FirstOrDefault(x => x.MENU_CODE == menuId);

                if (permission == null)
                {
                    return Json(new
                    {
                        success = true,
                        add = false,
                        edit = false,
                        delete = false,
                        print = false,
                        export = false,
                        mail = false,
                        approval = false,
                        docdetail = false
                    });
                }

                return Json(new
                {
                    success = true,
                    add = permission._ADD == 1,
                    edit = permission._EDIT == 1,
                    delete = permission._DELETE == 1,
                    print = permission._PRINT == 1,
                    export = permission._EXPORT == 1,
                    mail = permission._MAIL == 1,
                    approval = permission._APPROVAL == 1,
                    docdetail = permission._DOCDETAIL == 1
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

    }
}


