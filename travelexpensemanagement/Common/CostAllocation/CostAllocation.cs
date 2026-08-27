using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Reflection.Emit;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Common.CostAllocation
{
    public class CostAllocation : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly GlobalValidationdate _globalValidationdate;
        private readonly DbHelper.DbHelper _dbHelper;
        private readonly GlobalVariableService _globalVariableService;
        public CostAllocation(DataBaseConnection dbConnection, GlobalValidationdate globalValidationdate, DbHelper.DbHelper dbHelper,
            GlobalVariableService globalVariableService)
        {
            _dbConnection = dbConnection;
            _globalValidationdate = globalValidationdate;
            _dbHelper = dbHelper;
            _globalVariableService = globalVariableService;
        }

        [HttpGet]
        public JsonResult GetVNo(string vType)
        {
            var result = _globalValidationdate.GetVNo(vType, "COST_ALLOCATION");
            return Json(new { status = true, V_NO = result });
        }
        
        [HttpGet]
        public async Task<IActionResult> GetDropDowns()
        {
            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                con.Open();

                var result = new
                {
                    costCategories = await GetCostCategories(),
                    costSubCategories = await GetCostSubCategories(),
                    costCenters = await GetCostCenters()
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private async Task<object> GetCostCategories()
        {
            string query = @"SELECT code, costcode, name FROM COSTCAT_MAST WHERE ACTIVE = 1 ORDER BY NAME";
            return await _dbHelper.GetJsonDataAsync(query);
        }
        
        private async Task<object> GetCostSubCategories()
        {
            string query = @"SELECT code, costcode, name FROM COSTSUBCAT_MAST WHERE ACTIVE = 1 ORDER BY NAME";
            return await _dbHelper.GetJsonDataAsync(query);
        }

        private async Task<object> GetCostCenters()
        {
            string query = @"select code, costcode, name from COSTCENTER_MAST where ACTIVE = 1 Order by name";
            return await _dbHelper.GetJsonDataAsync(query);
        }

        [HttpGet]
        public async Task<IActionResult> IsCostAllocationExists(string refType, int refNo, int drAc = 0)
        {
            if (string.IsNullOrWhiteSpace(refType) || refNo <= 0)
            {
                return Json(new{status = false, message = "Invalid request."});
            }

            var gv = _globalVariableService.GetGlobalVariables();
            var data = new CostAllocationRowDto();
            try
            {
                string qry = $@"select 1 from COST_ALLOCATION Where Ref_Type=@Ref_Type and Ref_No=@Ref_No and Comp_code=@Comp_code and 
                                Branch_code=@Branch_code and Year_code=@Year_code";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Ref_Type", refType),
                    new SqlParameter("@Ref_No", refNo),
                    new SqlParameter("@Comp_code", gv.PubCompCode),
                    new SqlParameter("@Branch_code", gv.PubBranchCode),
                    new SqlParameter("@Year_code", gv.PubFYearCode)
                };

                bool exists = await _dbHelper.IsDataExist(qry, parameters);

                if (exists)
                {
                    string query = $@"Select sum(allocation_amt) from cost_allocation where Ref_Type=@Ref_Type and Ref_No=@Ref_No and 
                                        Comp_code=@Comp_code and Branch_code=@Branch_code and Year_code=@Year_code";
                    var parameter = new Dictionary<string, object>
                    {
                        {"@Ref_Type", refType},
                        {"@Ref_No", refNo},
                        {"@Comp_code", gv.PubCompCode},
                        {"@Branch_code", gv.PubBranchCode},
                        {"@Year_code", gv.PubFYearCode},
                    };

                    var result = await _dbHelper.GetExecuteScalarAsync<decimal>(query, parameter);
                    decimal balAmt = result;

                    return Json(new { status = true, exists, balAmt });
                }
                // Load default Cost Category / Cost Center
                string defaultQry = @"Select a.COSTCAT_CODE, a.COSTCENTER_CODE, a.COSTSCAT_CODE, c.Name as CostCatgeory, b.Name as CostCenter,
                                    d.Name as CostSubCatgeory from DOC_GLMAST A 
                                    LEFT JOIN COSTCENTER_MAST B ON A.COSTCENTER_CODE=B.CODE AND A.COMP_CODE=B.COMP_CODE 
                                    LEFT JOIN COSTCAT_MAST C ON C.CODE = A.COSTCAT_CODE AND A.COMP_CODE = C.COMP_CODE 
                                    LEFT JOIN COSTSUBCAT_MAST D ON D.CODE = A.COSTSCAT_CODE AND A.COMP_CODE = D.COMP_CODE 
                                    Where a.DOC_CODE=@DOC_CODE and a.AC_CODE=@AC_CODE and a.Comp_Code=@Comp_Code";

                using(SqlConnection con = _dbConnection.GetErpConnection())
                {
                    using(SqlCommand cmd = new SqlCommand(defaultQry, con))
                    {
                        cmd.Parameters.AddWithValue("@DOC_CODE", refType);
                        cmd.Parameters.AddWithValue("@AC_CODE", drAc);
                        cmd.Parameters.AddWithValue("@Comp_Code", gv.PubCompCode);
                        
                        await con.OpenAsync();

                        using(SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if(await reader.ReadAsync())
                            {
                                data.CostCategoryId = reader["COSTCAT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COSTCAT_CODE"]) : 0;
                                data.CostSubCategoryId = reader["COSTSCAT_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COSTSCAT_CODE"]) : 0;
                                data.CostCenterId = reader["COSTCENTER_CODE"] != DBNull.Value ? Convert.ToInt32(reader["COSTCENTER_CODE"]) : 0;
                                data.CostCategory = reader["CostCatgeory"].ToString();
                                data.CostSubCategory = reader["CostSubCatgeory"].ToString();
                                data.CostCenter = reader["CostCenter"].ToString();
                            }
                        }
                    }

                    return Json(new { status = true, data });
                }
            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetAllocation(string refType, int refNo)
        {
            if (string.IsNullOrWhiteSpace(refType) || refNo <= 0)
            {
                return Json(new { status = false, message = "Invalid request." });
            }

            var gv = _globalVariableService.GetGlobalVariables();

            try
            {
                using SqlConnection con = _dbConnection.GetErpConnection();

                string sql = @"SELECT a.V_TYPE, a.V_NO, a.V_DATE, a.REF_TYPE, a.REF_NO, a.COST_CENTER, B.NAME COSTCENTER, a.COST_CATEGORY, 
                                c.NAME as CostCategory, a.COST_SUBCAT, D.Name as CostSubCategory, a.ALLOCATION_AMT, a.NARRATION 
                                FROM COST_ALLOCATION A 
                                LEFT JOIN COSTCENTER_MAST B ON A.COST_CENTER=B.CODE AND A.COMP_CODE=B.COMP_CODE 
                                LEFT JOIN COSTCAT_MAST C ON C.CODE = A.COST_CATEGORY AND A.COMP_CODE = C.COMP_CODE 
                                LEFT JOIN COSTSUBCAT_MAST D ON D.CODE = A.COST_SUBCAT AND A.COMP_CODE = D.COMP_CODE 
                                WHERE A.V_TYPE='COAL' AND A.REF_TYPE = @REF_TYPE AND A.REF_NO = @REF_NO AND A.COMP_CODE = @COMP_CODE AND 
                                A.BRANCH_CODE=@BRANCH_CODE AND A.YEAR_CODE=@YEAR_CODE";

                using SqlCommand cmd = new SqlCommand(sql, con);

                cmd.Parameters.AddWithValue("@REF_TYPE", refType);
                cmd.Parameters.AddWithValue("@REF_NO", refNo);
                cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);

                con.Open();

                using SqlDataReader dr = cmd.ExecuteReader();

                CostAllocationDto dto = new();

                while (dr.Read())
                {
                    if (dto.Rows.Count == 0)
                    {
                        dto.VoucherNo = Convert.ToInt32(dr["V_NO"]);
                        dto.VoucherDate = Convert.ToDateTime(dr["V_DATE"]);
                        dto.RefType = dr["REF_TYPE"].ToString();
                        dto.RefNo = Convert.ToInt32(dr["REF_NO"]);
                    }
                    dto.Rows.Add(new CostAllocationRowDto
                    {
                        SrNo = dr["V_NO"] == DBNull.Value ? 0 : Convert.ToInt32(dr["V_NO"]),
                        CostCategoryId = dr["COST_CATEGORY"] == DBNull.Value ? 0 : Convert.ToInt32(dr["COST_CATEGORY"]),
                        CostCategory = dr["CostCategory"]?.ToString(),
                        CostSubCategoryId = dr["COST_SUBCAT"] == DBNull.Value ? 0 : Convert.ToInt32(dr["COST_SUBCAT"]),
                        CostSubCategory = dr["CostSubCategory"]?.ToString(),
                        CostCenterId = dr["COST_CENTER"] == DBNull.Value ? 0 : Convert.ToInt32(dr["COST_CENTER"]),
                        CostCenter = dr["COSTCENTER"]?.ToString(),
                        AllocationAmount = dr["ALLOCATION_AMT"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["ALLOCATION_AMT"]),
                        Narration = dr["NARRATION"]?.ToString()
                    });
                }

                return Json(new {status = true, data = dto});
            }
            catch (Exception ex)
            {
                return Json(new {status = false, message = ex.Message});
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveCA([FromBody] CostAllocationDto model)
        {
            if (model == null)
            {
                return Json(new { status = false, message = "Invalid request." });
            }

            if (model.Rows == null || model.Rows.Count == 0)
            {
                return Json(new { status = false, message = "Grid is empty." });
            }

            var gv = _globalVariableService.GetGlobalVariables();

            using SqlConnection con = _dbConnection.GetErpConnection();

            await con.OpenAsync();

            SqlTransaction tran = con.BeginTransaction();

            try
            {
                // Delete existing records

                string RefDOCID = string.Concat(model.RefType, model.RefNo.ToString());

                string deleteQry = @"Delete from COST_ALLOCATION Where V_TYPE='COAL' and Concat(Ref_type,Ref_No)=@RefDOCID and 
                                    Comp_code=@Comp_code and Branch_code=@Branch_code";

                SqlCommand cmd = new(deleteQry, con, tran);

                cmd.Parameters.AddWithValue("@RefDOCID", RefDOCID);
                cmd.Parameters.AddWithValue("@Comp_code", gv.PubCompCode);
                cmd.Parameters.AddWithValue("@Branch_code", gv.PubBranchCode);

                await cmd.ExecuteNonQueryAsync();

                // Insert

                string insertQry = @"INSERT INTO COST_ALLOCATION(COMP_CODE, BRANCH_CODE, YEAR_CODE, V_TYPE, V_NO, V_DATE, REF_TYPE, REF_NO, COST_CENTER, 
                                        COST_CATEGORY, COST_SUBCAT, ALLOCATION_AMT, NARRATION, SNO, UUSER, UDATE, AED, WSID, LIP, LID)
                                        VALUES(@COMP_CODE, @BRANCH_CODE, @YEAR_CODE, @V_TYPE, @V_NO, @V_DATE, @REF_TYPE, @REF_NO, @COST_CENTER, 
                                        @COST_CATEGORY, @COST_SUBCAT, @ALLOCATION_AMT, @NARRATION, @SNO, @UUSER, GETDATE(), 'A', @WSID, @LIP, @LID)";

                int sno = 1;

                foreach (var row in model.Rows)
                {
                    cmd = new SqlCommand(insertQry, con, tran);

                    cmd.Parameters.AddWithValue("@COMP_CODE", gv.PubCompCode);
                    cmd.Parameters.AddWithValue("@BRANCH_CODE", gv.PubBranchCode);
                    cmd.Parameters.AddWithValue("@YEAR_CODE", gv.PubFYearCode);
                    cmd.Parameters.AddWithValue("@V_NO", model.VoucherNo);
                    cmd.Parameters.AddWithValue("@V_TYPE", "COAL");
                    cmd.Parameters.AddWithValue("@V_DATE", model.VoucherDate);
                    cmd.Parameters.AddWithValue("@REF_TYPE", model.RefType);
                    cmd.Parameters.AddWithValue("@REF_NO", model.RefNo);

                    cmd.Parameters.AddWithValue("@COST_CATEGORY", row.CostCategoryId);
                    cmd.Parameters.AddWithValue("@COST_SUBCAT", row.CostSubCategoryId);
                    cmd.Parameters.AddWithValue("@COST_CENTER", row.CostCenterId);
                    cmd.Parameters.AddWithValue("@ALLOCATION_AMT", row.AllocationAmount);
                    cmd.Parameters.AddWithValue("@NARRATION", row.Narration ?? "");

                    cmd.Parameters.AddWithValue("@SNO", sno++);
                    
                    cmd.Parameters.AddWithValue("@UUSER", gv.PubUserId);
                    cmd.Parameters.AddWithValue("@WSID", gv.PubWorkStationID);
                    cmd.Parameters.AddWithValue("@LIP", gv.PubLocalId);
                    cmd.Parameters.AddWithValue("@LID", Environment.MachineName);

                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();

                return Json(new { status = true, message = "Data saved successfully."});
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();

                return Json(new { status = false, message = ex.Message});
            }
        }

        public class CostAllocationDto
        {
            public string RefType { get; set; }
            public int RefNo { get; set; }
            public string VoucherType { get; set; }
            public int VoucherNo { get; set; }
            public DateTime VoucherDate { get; set; }
            public decimal TotalAmount { get; set; }
            public List<CostAllocationRowDto> Rows { get; set; } = new List<CostAllocationRowDto>();
        }

        public class CostAllocationRowDto
        {
            public int SrNo { get; set; }
            public int CostCategoryId { get; set; }
            public string CostCategory { get; set; }
            public int CostSubCategoryId { get; set; }
            public string CostSubCategory { get; set; }
            public int CostCenterId { get; set; }
            public string CostCenter { get; set; }
            public decimal AllocationAmount { get; set; }
            public string Narration { get; set; }
        }

    }
}
