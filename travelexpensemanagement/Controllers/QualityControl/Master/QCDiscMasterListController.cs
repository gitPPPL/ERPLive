using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.DbHelper;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Controllers.Travelexpense;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Repositories.Implementations.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class QCDiscMasterListController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        private int? userLevel;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly IQCDiscMasterListRepository _qcDiscMasterListRepository;
        public QCDiscMasterListController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
        DropdownService dropdownService, DbHelper dbHelper, ModuleService.ModuleService moduleService, GlobalValidationdate globalValidationdate ,IQCDiscMasterListRepository qcDiscMasterListRepository)
        {
            _dbConnection = dbConnection;
            _globalVariableService = globalVariableService;
            _dropdownService = dropdownService;
            _dbHelper = dbHelper;
            _moduleService = moduleService;
            _globalValidationdate = globalValidationdate;
            _qcDiscMasterListRepository = qcDiscMasterListRepository;
        }

        public IActionResult Index()
        {
            string databaseName;
            using (var connection = _dbConnection.GetErpConnection())
            {
                databaseName = connection.Database;
            }
            ViewBag.DatabaseName = databaseName;
            var globalVariables = _globalVariableService.GetGlobalVariables();
            ViewBag.GlobalVariables = globalVariables;
            return View("~/Views/QualityControl/Master/QCDiscMasterList/Index.cshtml");
        }

        [HttpGet]
        public IActionResult GetAllListData(string searchTerm = "", int pageNumber = 1,int pageSize = 10)
        {
            try
            {
                var result = _qcDiscMasterListRepository.GetAllListData(searchTerm, pageNumber, pageSize);

                return Json(new
                {
                    success = true,
                    data = result.Data,
                    totalCount = result.TotalCount
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
        public IActionResult ExportAllDocs()
        {
            try
            {
                var fileBytes = _qcDiscMasterListRepository.ExportAllDocs();

                return File(fileBytes,"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"QCDiskMaster_{DateTime.Now:ddMMyyyy}.xlsx");
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

        //[HttpGet]
        //public IActionResult GetAllListData(string searchTerm = "", int pageNumber = 1, int pageSize = 10)
        //{
        //    var globalVariable= _globalVariableService.GetGlobalVariables();
        //    List<object> list = new List<object>();
        //    int totalCount = 0;

        //    try
        //    {
        //        using(SqlConnection con = _dbConnection.GetErpConnection())
        //        {
        //            SqlCommand cmd = new SqlCommand("sp_QCDISC_MAST", con);
        //            cmd.CommandType = CommandType.StoredProcedure;

        //            con.Open();

        //            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
        //            cmd.Parameters.AddWithValue("@V_TYPE", "QDIS");
        //            cmd.Parameters.AddWithValue("@SearchTerm", string.IsNullOrWhiteSpace(searchTerm) ? (object)DBNull.Value : searchTerm);
        //            cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
        //            cmd.Parameters.AddWithValue("@PageSize", pageSize);
        //            cmd.Parameters.AddWithValue("@Action", "SELECT");

        //            SqlDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read()) 
        //            {
        //                list.Add(new
        //                {
        //                    ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? Convert.ToInt32(reader["ITEM_CODE"]) : 0,
        //                    ITEM_NAME = reader["ITEM_NAME"]?.ToString(),
        //                    QCP_CODE = reader["QCP_CODE"] != DBNull.Value ? Convert.ToInt32(reader["QCP_CODE"]) : 0,
        //                    PARAMETER_NAME = reader["PARAMETER_NAME"].ToString(),
        //                    QCP_DIFF = reader["QCP_DIFF"] != DBNull.Value ? Convert.ToInt32(reader["QCP_DIFF"]) : 0,
        //                });

        //            }
        //            if (reader.NextResult() && reader.Read())
        //            {
        //              totalCount = Convert.ToInt32(reader["TotalCount"]);
        //            }
        //            return Json(new { success = true, data = list, totalCount });

        //        }

        //    }catch(Exception ex)
        //    {
        //        return Json(new {success= false, message= ex.Message });    
        //    }
        //}

        //[HttpGet]
        //public IActionResult ExportAllDocs()
        //{
        //    try
        //    {
        //        var gv = _globalVariableService.GetGlobalVariables();

        //        var parameters = new Dictionary<string, object>
        //        {
        //            { "@COMP_CODE", gv.PubCompCode },
        //            { "@Action", "Excel" }
        //        };

        //        var fileBytes = _globalValidationdate.ExportToExcel("sp_QCDISC_MAST", "QCDisk Master", parameters);

        //        return File(
        //            fileBytes,
        //            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //            $"QCDiskMaster_{DateTime.Now:ddMMyyyy}.xlsx"
        //        );
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}

    }
} 
