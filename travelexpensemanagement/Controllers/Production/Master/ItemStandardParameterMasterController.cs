using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Production.Master.ItemStandardParameterMaster;

namespace travelexpensemanagement.Controllers.Production.Master
{
    public class ItemStandardParameterMasterController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalVariableService _globalVariableService;
        private readonly DropdownService _dropdownService;
        private readonly travelexpensemanagement.Common.DbHelper.DbHelper _dbHelper;
        private readonly travelexpensemanagement.ModuleService.ModuleService _moduleService;
        public ItemStandardParameterMasterController(DataBaseConnection dbConnection, GlobalVariableService globalVariableService,
     travelexpensemanagement.Common.DropdownService.DropdownService dropdownService, travelexpensemanagement.Common.DbHelper.DbHelper dbHelper,
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
            return View("~/Views/Production/Master/ItemStandardParameterMaster/Index.cshtml");
        }
        
        [HttpGet]
        public IActionResult ItemNameDropdown()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE ,NAME  FROM ITEM_MAST  WHERE COMP_CODE = 1 ORDER BY NAME";
            var itemName = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = itemName });
        }

        [HttpGet]
        public IActionResult MeshDropdown()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE, NAME FROM MESH_MAST WHERE COMP_CODE =1 ORDER BY NAME";
            var mesh = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = mesh });
        }

        [HttpGet]
        public IActionResult ColorDropdown()
        {
            var getData = _globalVariableService.GetGlobalVariables();
            string query = "SELECT CODE, NAME FROM COLOR_MAST WHERE COMP_CODE =5 ORDER BY NAME";
            var color = _dropdownService.GetDropdownList(query);
            return Json(new { success = true, data = color });
        }
        [HttpPost]
        public IActionResult SaveAndUpdateItemStandardMaster([FromBody] ItemStandardParameterMaster model)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            if (model == null)
                return Json(new { success = false, message = "Model is NULL" });

            try
            {
                using (SqlConnection con = _dbConnection.GetErpConnection())
                {
                    con.Open();

                    foreach (var d in model.Details)
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_ItemStandard_ParameterMaster", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);
                            cmd.Parameters.AddWithValue("@CODE", model.CODE == 0 ? DBNull.Value : model.CODE);
                            cmd.Parameters.AddWithValue("@ITEM_CODE", model.ITEM_CODE);
                            cmd.Parameters.AddWithValue("@SUB_ITEM", d.SUB_ITEM);
                            cmd.Parameters.AddWithValue("@STD_WT", d.STD_WT);
                            cmd.Parameters.AddWithValue("@SRNO", d.SRNO);
                            cmd.Parameters.AddWithValue("@UUSER", globalVariable.PubUserId);
                            cmd.Parameters.AddWithValue("@WSID", globalVariable.PubWorkStationID);
                            cmd.Parameters.AddWithValue("@LID", Environment.MachineName);
                            cmd.Parameters.AddWithValue("@BAG_TYPE", model.BAG_TYPE);
                            cmd.Parameters.AddWithValue("@CUTTING_STD_WT", model.CUTTING_STD_WT);
                            cmd.Parameters.AddWithValue("@TOP_S", model.TOP_S);
                            cmd.Parameters.AddWithValue("@BOTTOM_S", model.BOTTOM_S);
                            cmd.Parameters.AddWithValue("@PRINTING_STD_WT", model.PRINTING_STD_WT);
                            cmd.Parameters.AddWithValue("@THREAD_STD_WT", model.THREAD_STD_WT);
                            cmd.Parameters.AddWithValue("@MINSTD_WT", model.MINSTD_WT);
                            cmd.Parameters.AddWithValue("@MAXSTD_WT", model.MAXSTD_WT);
                            cmd.Parameters.AddWithValue("@MESH_CODE", model.MESH_CODE);
                            cmd.Parameters.AddWithValue("@COLOR_CODE", model.COLOR_CODE);
                            cmd.Parameters.AddWithValue("@DENIER", model.DENIER);
                            cmd.Parameters.AddWithValue("@LINER_MICRONE", model.LINER_MICRONE);
                            cmd.Parameters.AddWithValue("@CAPACITY", model.CAPACITY);
                            cmd.Parameters.AddWithValue("@LINER_WT", model.LINER_WT);
                            cmd.Parameters.AddWithValue("@PRINTING_TYPE", model.PRINTING_TYPE);
                            cmd.Parameters.AddWithValue("@GMG_REQ", model.GMG_REQ);
                            cmd.Parameters.AddWithValue("@PACKING", model.PACKING);
                            cmd.Parameters.AddWithValue("@GRAM_WITHLAM", model.GRAM_WITHLAM);
                            cmd.Parameters.AddWithValue("@BAG_SIZE", model.BAG_SIZE);
                            cmd.Parameters.AddWithValue("@WIDTH", model.WIDTH);
                            cmd.Parameters.AddWithValue("@LINER_SIZE", model.LINER_SIZE);
                            cmd.Parameters.AddWithValue("@GSM", model.GSM);
                            cmd.Parameters.AddWithValue("@BAG_WT", model.BAG_WT);
                            cmd.Parameters.AddWithValue("@NOS", model.NOS);
                            cmd.Parameters.AddWithValue("@BALING_INST", model.BALING_INST);
                            cmd.Parameters.AddWithValue("@LABELING_INST", model.LABELING_INST);
                            cmd.Parameters.AddWithValue("@WEIGHING_INST", model.WEIGHING_INST);
                            cmd.Parameters.AddWithValue("@LINER", model.LINER);
                           
                            cmd.Parameters.AddWithValue("@Action", string.IsNullOrEmpty(model.Action) ? "Insert" : model.Action);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    return Json(new { success = true, message = model.Action == "Update" ? "Data Updated Successfully !!" : "Data Saved Successfully !!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetDataByCode(int code)
        {
            var globalVariable = _globalVariableService.GetGlobalVariables();
            var model = new ItemStandardParameterMaster();
            var details = new List<ItemStandardParameterDetailModel>();
            using (SqlConnection con = _dbConnection.GetErpConnection())
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = con;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = @"SELECT * FROM ITEM_STDPARAM WHERE CODE = @CODE AND COMP_CODE = @COMP_CODE
                                  SELECT * FROM ITEM_STDPARAM2 WHERE CODE = @CODE AND COMP_CODE = @COMP_CODE";
                cmd.Parameters.AddWithValue("@CODE", code);
                cmd.Parameters.AddWithValue("@COMP_CODE", globalVariable.PubCompCode);

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.CODE = reader["CODE"] != DBNull.Value ? int.Parse(reader["CODE"].ToString()) : 0;
                        model.ITEM_CODE = reader["ITEM_CODE"] != DBNull.Value ? int.Parse(reader["ITEM_CODE"].ToString()) : 0;

                        model.BAG_TYPE = reader["BAG_TYPE"]?.ToString();
                        model.TOP_S = reader["TOP_S"] != DBNull.Value ? reader["TOP_S"].ToString() : null;
                        model.BOTTOM_S = reader["BOTTOM_S"] != DBNull.Value ? reader["BOTTOM_S"].ToString() : null;

                        model.CUTTING_STD_WT = reader["CUTTING_STD_WT"] != DBNull.Value ? decimal.Parse(reader["CUTTING_STD_WT"].ToString()) : 0;
                        model.THREAD_STD_WT = reader["THREAD_STD_WT"] != DBNull.Value ? decimal.Parse(reader["THREAD_STD_WT"].ToString()) : 0;
                        model.PRINTING_STD_WT = reader["PRINTING_STD_WT"] != DBNull.Value ? decimal.Parse(reader["PRINTING_STD_WT"].ToString()) : 0;
                        model.MINSTD_WT = reader["MINSTD_WT"] != DBNull.Value ? decimal.Parse(reader["MINSTD_WT"].ToString()) : 0;
                        model.MAXSTD_WT = reader["MAXSTD_WT"] != DBNull.Value ? decimal.Parse(reader["MAXSTD_WT"].ToString()) : 0;

                        model.MESH_CODE = reader["MESH_CODE"] != DBNull.Value ? int.Parse(reader["MESH_CODE"].ToString()) : 0;
                        model.COLOR_CODE = reader["COLOR_CODE"] != DBNull.Value ? int.Parse(reader["COLOR_CODE"].ToString()) : 0;

                        model.DENIER = reader["DENIER"]?.ToString();
                        model.LINER_MICRONE = reader["LINER_MICRONE"]?.ToString();
                        model.CAPACITY = reader["CAPACITY"] != DBNull.Value ? Convert.ToDecimal(reader["CAPACITY"]) : (decimal?)null;
                        model.LINER_WT = reader["LINER_WT"] != DBNull.Value ? Convert.ToDecimal(reader["LINER_WT"]) : (decimal?)null;
                        model.PRINTING_TYPE = reader["PRINTING_TYPE"] != DBNull.Value ? reader["PRINTING_TYPE"].ToString() : null;
                        model.GMG_REQ = reader["GMG_REQ"] != DBNull.Value ? Convert.ToDecimal(reader["GMG_REQ"]) : (decimal?)null;
                        model.PACKING = reader["PACKING"] != DBNull.Value ? (int?)Convert.ToInt32(reader["PACKING"]) : null;
                        model.GRAM_WITHLAM = reader["GRAM_WITHLAM"] != DBNull.Value ? Convert.ToDecimal(reader["GRAM_WITHLAM"]) : (decimal?)null;
                        model.BAG_SIZE = reader["BAG_SIZE"] != DBNull.Value ? reader["BAG_SIZE"].ToString() : null;
                        model.WIDTH = reader["WIDTH"] != DBNull.Value ? Convert.ToDecimal(reader["WIDTH"]) : (decimal?)null;
                        model.LINER_SIZE = reader["LINER_SIZE"] != DBNull.Value ? reader["LINER_SIZE"].ToString() : null;
                        model.GSM = reader["GSM"] != DBNull.Value ? Convert.ToDecimal(reader["GSM"]) : (decimal?)null;
                        model.BAG_WT = reader["BAG_WT"] != DBNull.Value ? Convert.ToDecimal(reader["BAG_WT"]) : (decimal?)null;
                        model.NOS = reader["NOS"] != DBNull.Value ? Convert.ToInt32(reader["NOS"]) : (int?)null;
                        model.BALING_INST = reader["BALING_INST"]?.ToString();
                        model.LABELING_INST = reader["LABELING_INST"]?.ToString();
                        model.WEIGHING_INST = reader["WEIGHING_INST"]?.ToString();
                        model.LINER = reader["LINER"]?.ToString();
                    }
                    if (reader.NextResult())
                    {  
                        while (reader.Read())
                        {
                            details.Add(new ItemStandardParameterDetailModel
                            {
                                CODE = reader["CODE"] != DBNull.Value ? int.Parse(reader["CODE"].ToString()) : 0,
                                SUB_ITEM = reader["SUB_ITEM"] != DBNull.Value ? int.Parse(reader["SUB_ITEM"].ToString()) : 0,
                                STD_WT = reader["STD_WT"] != DBNull.Value ? decimal.Parse(reader["STD_WT"].ToString()) : 0,
                                SRNO = reader["SRNO"] != DBNull.Value ? int.Parse(reader["SRNO"].ToString()) : 0
                            });
                        }
                    }
                }
            }
            return Json(new { success = true, data = model, details = details });
        }
  
    }
}
