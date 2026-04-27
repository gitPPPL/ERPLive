using System.Data;
using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Dbconnection;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Asn1.X509;
using travelexpensemanagement.Models.FincialAccounting.Master;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Common.DbHelper;
namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    public class PartyQCMasterController : Controller
    {
        private readonly DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x;
        public PartyQCMasterController(DataBaseConnection dbcontext, DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/PartyQCMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<JsonResult> GetSupplierList()
        {
            try
            {
                var supplierList = await _dbHelper.GetJsonDataAsync("select distinct code, Name from SUBGROUP_MAST where COMP_CODE='"+ _globalValue.GetGlobalVariables().PubCompCode +"' and NATURE in ('Supplier') order by NAME");
                return Json(new { status = true, data = supplierList });
            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "supplier name load failed" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetQCNameList()
        {
            try
            {
                var QCList = await _dbHelper.GetJsonDataAsync("select distinct code, Name from QC_MAST where COMP_CODE='" + _globalValue.GetGlobalVariables().PubCompCode + "' order by NAME");
                return Json(new { status = true, data = QCList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "QC name load failed" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetQCDetailList(int Qccode)
        {
            try
            {
                string strqry = $@"select distinct q1.CODE QC_Code, QCP_MAST.NAME QC_Parameter,q1.QCP_CODE,q1.QCP_UNIT Unit, q1.QCP_STD Standard
                from QC_MAST1  q1 left join QCP_MAST on q1.QCP_CODE=QCP_MAST.code and q1.COMP_CODE=QCP_MAST.COMP_CODE
                where q1.COMP_CODE = {_globalValue.GetGlobalVariables().PubCompCode} and q1.CODE = {Qccode} ";
                var QcList = await _dbHelper.GetJsonDataAsync(strqry);
                return Json(new { status = true, data = QcList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data load failed" });
            }
        }

      

        [HttpGet]
        public JsonResult getExistOrNot(int partyCd, int QCcode)
        {
            try
            {
                bool isExist = false;

                using (var con = _dbcontext.GetErpConnection())
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = con;
                        cmd.CommandText = @"
                         SELECT CASE 
                        WHEN EXISTS (
                        SELECT 1 
                        FROM PARTY_QCMAST  WHERE COMP_CODE = @CompCode and PARTY_CODE=@partyCD  and QC_CODE=@QCCode                   
                        ) 
                        THEN 1 ELSE 0 
                        END";

                        cmd.Parameters.AddWithValue("@partyCD", partyCd);
                        cmd.Parameters.AddWithValue("@QCCode", QCcode);                        
                        cmd.Parameters.AddWithValue("@CompCode", _globalValue.GetGlobalVariables().PubCompCode);
                        con.Open();
                        var result = cmd.ExecuteScalar();
                        isExist = Convert.ToInt32(result) == 1;
                    }
                }

                return Json(new { status = true, exists = isExist });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data check failed: " + ex.Message });
            }
        }

        public class PartyQCModel
        {
             
            public int? QCP_Code { get; set; }
            public int? UnitCode { get; set; }
            public decimal? Std { get; set; }
            public decimal? FromResult { get; set; }
            public decimal? ToResult { get; set; }
        }
        public class PartyQCMasterRequest
        {
            public int? code { get; set; }
            public int PartyCd { get; set; }
            public int QC_Code { get; set; }
            public List<PartyQCModel> QCDetails { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SavePartyQCMast([FromBody] PartyQCMasterRequest model)
        {
            try
            {
                if (model == null || model.QCDetails == null || !model.QCDetails.Any())
                    return Json(new { status = false, message = "Invalid or empty data submitted." });

                var usersessionDt = _globalValue.GetGlobalVariables();
                int totalInserted = 0;

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            string query = "SELECT  ISNULL(MAX(CODE), 0) + 1    FROM PARTY_QCMAST    WHERE COMP_CODE ='"+usersessionDt.PubCompCode+"' ";
                            int newVNo = await _dbHelper.GetExecuteScalarAsync<int>(query);

                            foreach (var detail in model.QCDetails)
                            {
                                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PartyQCMast_AED]", con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;

                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@CompanyCd", usersessionDt.PubCompCode);
                                    cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(newVNo));
                                    cmd.Parameters.AddWithValue("@PartyCd", _dbHelper.Xnull(model.PartyCd));
                                    cmd.Parameters.AddWithValue("@QC_Code",  _dbHelper.Xnull(model.QC_Code));
                                    cmd.Parameters.AddWithValue("@QCP_CODE", _dbHelper.Xnull(detail.QCP_Code));
                                    cmd.Parameters.AddWithValue("@UNIT_CODE", _dbHelper.Xnull(detail.UnitCode));
                                    cmd.Parameters.AddWithValue("@STD", _dbHelper.Vnull(detail.Std));
                                    cmd.Parameters.AddWithValue("@FROM_RESULT", _dbHelper.Vnull(detail.FromResult));
                                    cmd.Parameters.AddWithValue("@TO_RESULT", _dbHelper.Vnull(detail.ToResult));
                                    cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                                    cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);

                                    var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                    {
                                        Direction = ParameterDirection.ReturnValue
                                    };
                                    cmd.Parameters.Add(returnParam);

                                    await cmd.ExecuteNonQueryAsync();

                                    int result = (int)cmd.Parameters["@ReturnVal"].Value;
                                    if (result > 0)
                                        totalInserted++;
                                    else
                                        throw new Exception("Stored procedure failed for a row.");
                                }
                            }
 
                            transaction.Commit();
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            return Json(new { status = false, message = "Transaction failed. No records were saved." });
                        }
                    }
                }
                return Json(new { status = true, message = "All records saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Unexpected error occurred while saving data." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetPartyQCDetailsById(string id)
        {
            try
            {
                var UsersessionDt = _globalValue.GetGlobalVariables();
                string strqry = $@"               
SELECT pm.CODE,isnull(sg.name, '') as Party, isnull(qcm.name, '') as QC_Name, pm.PARTY_CODE, isnull(pm.QC_CODE, '') AS QC_CODE, isnull(qcp.NAME, '') AS QC_Parameter, pm.QCP_CODE, pm.UNIT_CODE, pm.STD, pm.FROM_RESULT, pm.TO_RESULT
FROM PARTY_QCMAST pm
LEFT JOIN SUBGROUP_MAST sg ON pm.PARTY_CODE = sg.CODE AND pm.COMP_CODE = sg.COMP_CODE
LEFT JOIN QC_MAST qcm ON pm.QC_CODE = qcm.CODE AND pm.COMP_CODE = qcm.COMP_CODE
LEFT JOIN QCP_MAST qcp ON pm.QCP_CODE = qcp.CODE AND pm.COMP_CODE = qcp.COMP_CODE 
                WHERE pm.COMP_CODE = '{UsersessionDt.PubCompCode}' and pm.code={id}  ";
                var data = await _dbHelper.GetJsonDataAsync(strqry);
                if (data.Count > 0)
                    return Json(new { status = true, data = data });

                return Json(new { status = false, message = "Not found" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePartyQCMast([FromBody] PartyQCMasterRequest model)
        {
            try
            {
                if (model == null || model.QCDetails == null || !model.QCDetails.Any())
                    return Json(new { status = false, message = "Invalid or empty data submitted." });

                var usersessionDt = _globalValue.GetGlobalVariables();
                int totalInserted = 0;

                using (var con = _dbcontext.GetErpConnection())
                {
                    await con.OpenAsync();
                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand sqlcmd = new SqlCommand())
                            {
                                sqlcmd.Connection = con;
                                sqlcmd.Transaction = transaction;
                                sqlcmd.CommandType = CommandType.Text;
                                sqlcmd.CommandText = $@"DELETE FROM PARTY_QCMAST WHERE COMP_CODE = @CompCode AND CODE = @Code";
                                sqlcmd.Parameters.AddWithValue("@CompCode", usersessionDt.PubCompCode);
                                sqlcmd.Parameters.AddWithValue("@Code", model.code);

                                await sqlcmd.ExecuteNonQueryAsync();
                            }

                            foreach (var detail in model.QCDetails)
                            {
                                using (SqlCommand cmd = new SqlCommand("[dbo].[sp_PartyQCMast_AED]", con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@CompanyCd", usersessionDt.PubCompCode);
                                    cmd.Parameters.AddWithValue("@Code", _dbHelper.Xnull(model.code));
                                    cmd.Parameters.AddWithValue("@PartyCd", _dbHelper.Xnull(model.PartyCd));
                                    cmd.Parameters.AddWithValue("@QC_Code", _dbHelper.Xnull(model.QC_Code));
                                    cmd.Parameters.AddWithValue("@QCP_CODE", _dbHelper.Xnull(detail.QCP_Code));
                                    cmd.Parameters.AddWithValue("@UNIT_CODE", _dbHelper.Xnull(detail.UnitCode));
                                    cmd.Parameters.AddWithValue("@STD", _dbHelper.Vnull(detail.Std));
                                    cmd.Parameters.AddWithValue("@FROM_RESULT", _dbHelper.Vnull(detail.FromResult));
                                    cmd.Parameters.AddWithValue("@TO_RESULT", _dbHelper.Vnull(detail.ToResult));
                                    cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                                    cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);

                                    var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int)
                                    {
                                        Direction = ParameterDirection.ReturnValue
                                    };
                                    cmd.Parameters.Add(returnParam);

                                    await cmd.ExecuteNonQueryAsync();

                                    int result = (int)cmd.Parameters["@ReturnVal"].Value;
                                    if (result > 0)
                                        totalInserted++;
                                    else
                                        throw new Exception("Stored procedure failed for a row.");
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Json(new { status = false, message = "Transaction failed. No records were updated. Reason: " + ex.Message });
                        }
                    }
                }

                return Json(new { status = true, message = "All records updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Unexpected error occurred: " + ex.Message });
            }
        }


    }
}
