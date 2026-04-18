using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using travelexpensemanagement.Controllers.Globalvariable;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.DbHelper;
using travelexpensemanagement.GlobalErrorHandlingMiddleware;
using travelexpensemanagement.Models.Payroll.Master;

namespace travelexpensemanagement.Controllers.Payroll.Master
{
    public class EmployeeMasterController : Controller
    {
        private readonly travelexpensemanagement.DbHelper.DbHelper _dbHelper;
        private readonly DataBaseConnection _dbcontext;
        private readonly GlobalVariableService _globalValue;
        int x = 0;
        public EmployeeMasterController(DataBaseConnection dbcontext, travelexpensemanagement.DbHelper.DbHelper dbHelper, GlobalVariableService globalValue)
        {
            _dbHelper = dbHelper;
            _dbcontext = dbcontext;
            _globalValue = globalValue;
        }
        public IActionResult Index()
        {
            return View("~/Views/Payroll/Master/EmployeeMaster/Index.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetDesignationList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from DESG_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartmentList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from DEPT_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlaceList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from PLACE_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@" select distinct CategoryID as CODE,CategoryName as NAME from ExpenseCategoryMaster order by NAME ");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetShiftList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, SHIFT as NAME from SHIFT_MAST where COMP_CODE={_globalValue.GetGlobalVariables().PubCompCode} order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetbankList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from BANK_MAST order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCaderList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from CADER_MAST  order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCityList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from CITY_MAST  order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGradeList()
        {
            try
            {
                var dataList = await _dbHelper.GetJsonDataAsync($@"select distinct CODE, NAME from GRADE_MAST  order by NAME");
                return Json(new { status = true, data = dataList });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Data Load Failed" });
            }
        }

        [HttpGet]
        public JsonResult getExistOrNot(string inputdata)
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
                        FROM EMP_MAST 
                        WHERE ISNULL(EMP_ID, '') = UPPER(@EmpId) 
                        AND COMP_CODE = @CompCode
                        ) 
                        THEN 1 ELSE 0 
                        END";

                        cmd.Parameters.AddWithValue("@EmpId", inputdata);
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

        [HttpGet]
        public async Task<IActionResult> GetEmpMastDetailsById(string id)
        {
            try
            {
                string compCode = _globalValue.GetGlobalVariables().PubCompCode;

                // --- Header: EMP_MAST ---
                DataTable Empdt = new DataTable();
                string strqry = $@"SELECT 
                  COMP_CODE, EMP_ID, CODE, MAC_CODE, M_TYPE, TITLE, FIRSTNAME, MIDDLENAME, LASTNAME, NAME, FATHER_NAME,
                REJOIN_CODE, PADD1, PADD2, PADD3, PCITY_CODE, TADD1, TADD2, TADD3, TCITY_CODE, SEX, AGE, DOB, DOM,
                DESG_CODE, DEPT_CODE, PLACE_CODE, INCPLACE_CODE, TYPE, CADER_CODE, CAT_CODE, GRADE_CODE,
                JOIN_DATE, RESIGN_DATE, PERMANENT_DATE, RIS_ZONE,
                PF_APPL, PF_NO, PF_DATE, ESI_APPL, ESI_NO, ESI_DATE,
                OFFDAY, SHIFT, REF_BY, SPOUSE, CONTACT, MOBILE, EMAIL, UAN, PAN, AADAR, VOTORID, DL,
                AC_NO, BANK_CODE, BANK_NAME, IFSC_CODE, BRANCH, AC_TYPE, BANK_VERIFY, REMARKS,
                LEAVE_APPL, HOLIDAY_APPL, BONUS_APPL, BLOOD_GROUP, RELIGION, CARD_NO,
                CTC_PROD_INC, CTC_OTHER_ALLOW, CTC_MISC_BENEFITS, CTC_CAR_BENEFITS, 
                CTC_RENT_REIMB, CTC_MOB_EXPS, CTC_TEA_EXPS,
                FAPROV_STATUS, FAPROV_REMARKS, MIN_SALARY, MAX_SALARY,
                A, B, C, D, E, D1, D2, D3,
                JOB_RES, EXPERIENCE, imagepath, imagebyte, ABRY, GRATUITY_APPL,
                RETIREMENT_DATE, SALARY_EFFDATE, HRA_DEDUCT, FIXED_SALARY,
                EMP_TYPE, FITMENT, PMRPY, BANK_LOAN, CARD_ISSUE, QUARTER_NO, LINE_NO,
                COLONY_APPL, ROOM_NO, PPF_ONFULLBASIC, M_STATUS, VPF_APPL, VPF_DATE, RETR_DATE, ACTIVE
                FROM EMP_MAST WHERE COMP_CODE = '{compCode}' AND CODE = {id}";
                SqlDataAdapter ds = new SqlDataAdapter(new SqlCommand(strqry, _dbcontext.GetErpConnection()));
                ds.Fill(Empdt);

                if (Empdt.Rows.Count == 0)
                    return Json(new { status = false, message = "Not found" });

                var empHeader = Empdt.Rows[0].Table.Columns.Cast<DataColumn>().ToDictionary(
                col => col.ColumnName,
                col => Empdt.Rows[0][col] == DBNull.Value ? "" : Empdt.Rows[0][col]
                );


                // --- Detail: EMP_EXPERIENCE ---
                DataTable EmpExpdt = new DataTable();
                string stremp = $@"SELECT EMPLOYER, ADDRESS, DOJ, DOR, PERIOD, LAST_SALARY, DEPT, DESG, JOB_PROFILE 
                 FROM EMP_EXPERIENCE WHERE COMP_CODE = '{compCode}' AND CODE = {id}";
                new SqlDataAdapter(new SqlCommand(stremp, _dbcontext.GetErpConnection())).Fill(EmpExpdt);

                // --- Detail: EMP_FAMILY ---
                DataTable EmpFamdt = new DataTable();
                string strempfamily = $@"SELECT SEQ_NO, MEMBER, AGE, CONTACT, RELATION, MINOR, NOMINEE, SHARE, ADDRESS, REMARKS
                FROM EMP_FAMILY WHERE COMP_CODE = '{compCode}' AND CODE = {id}";
                new SqlDataAdapter(new SqlCommand(strempfamily, _dbcontext.GetErpConnection())).Fill(EmpFamdt);

                // --- Detail: EMP_QUALIFICATION ---
                DataTable EmpQualdt = new DataTable();
                string strempqual = $@"SELECT SEQ_NO, DEGREE, BOARD, YEAR, MARKS, REMARKS
                FROM EMP_QUALIFICATION WHERE COMP_CODE = '{compCode}' AND CODE = {id}";
                new SqlDataAdapter(new SqlCommand(strempqual, _dbcontext.GetErpConnection())).Fill(EmpQualdt);

                // --- Detail: EMP_RELATIVE ---
                DataTable EmpReldt = new DataTable();
                string stremprelatives = $@"SELECT SEQ_NO, NAME, DEPT_CODE, CONTACT, RELATION, ADDRESS, REMARKS
                FROM EMP_RELATIVES WHERE COMP_CODE = '{compCode}' AND CODE = {id}";
                new SqlDataAdapter(new SqlCommand(stremprelatives, _dbcontext.GetErpConnection())).Fill(EmpReldt);

                // --- Detail: EMP_ATTACHMENT ---
                DataTable EmpAtchdt = new DataTable();
                string strempattachment = $@"SELECT ATTACHID, FILENAME, FILEPATH, ATTACHDATE, FILE_RDATA
                FROM EMP_ATTACHMENT WHERE COMP_CODE = '{compCode}' AND CODE = {id}";
                new SqlDataAdapter(new SqlCommand(strempattachment, _dbcontext.GetErpConnection())).Fill(EmpAtchdt);

                // Convert all detail tables to List<Dictionary<string, object>>            

                var experience = EmpExpdt.Rows.Count > 0 ? ConvertToList(EmpExpdt) : new object { };
                var family = EmpFamdt.Rows.Count > 0 ? ConvertToList(EmpFamdt) : new object { };
                var qualification = EmpQualdt.Rows.Count > 0 ? ConvertToList(EmpQualdt) : new object { };
                var relatives = EmpReldt.Rows.Count > 0 ? ConvertToList(EmpReldt) : new object { };
                var attachments = EmpAtchdt.Rows.Count > 0 ? ConvertToList(EmpAtchdt) : new object { };


                return Json(new
                {
                    status = true,
                    header = empHeader,
                    experience = experience,
                    family = family,
                    qualification = qualification,
                    relatives = relatives,
                    attachments = attachments
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        private List<Dictionary<string, object>> ConvertToList(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0)
                return new List<Dictionary<string, object>>();

            return dt.AsEnumerable()
                     .Select(row =>
                         dt.Columns.Cast<DataColumn>()
                           .ToDictionary(
                               col => col.ColumnName,
                               col =>
                               {
                                   var value = row[col];
                                   return value == DBNull.Value ? "" : value;
                               }))
                     .ToList();
        }


        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateEmployeeMast([FromBody] EmployeeMaster empmodel)
        {
            if (empmodel == null)
                return Json(new { status = false, message = " data save failed." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {

                    if (!string.IsNullOrWhiteSpace(empmodel.ImageByteBase64))
                    {                       
                        var base64 = empmodel.ImageByteBase64;
                        var commaIndex = base64.IndexOf(',');

                        if (commaIndex >= 0)
                            base64 = base64.Substring(commaIndex + 1);

                        empmodel.ImageByte = Convert.FromBase64String(base64);
                    }
                    else
                    {
                        empmodel.ImageByte = null;
                    }


                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();
                   
                    var EmpCode= empmodel.Code ;
                    DataTable experienceTable = FillDataTable(empmodel.Experiences, "dbo.EmployeeExperience_Type");
                    DataTable familyTable = FillDataTable(empmodel.Family, "dbo.EmployeeFamily_Type");
                    DataTable qualificationTable = FillDataTable(empmodel.Qualifications, "dbo.EmployeeQualification_Type");
                    DataTable relativeTable = FillDataTable(empmodel.Relatives, "dbo.EmployeeRelatives_Type");
                    DataTable attachmentTable = FillDataTable(empmodel.Attachments, "dbo.EmployeeAttachment_Type");

                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;

                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_EmployeeMast_AED]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Transaction = transaction; 
                                if(empmodel.SaveOrUpdate== "Save")
                                {
                                    cmd.Parameters.AddWithValue("@AED", "A");
                                    cmd.Parameters.AddWithValue("@EMP_ID", _dbHelper.Xnull(empmodel.EmpId));
                                    cmd.Parameters.AddWithValue("@CODE", _dbHelper.Xnull(empmodel.EmpId));
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@AED", "E");
                                    cmd.Parameters.AddWithValue("@EMP_ID", _dbHelper.Xnull(empmodel.EmpId));
                                    cmd.Parameters.AddWithValue("@CODE", _dbHelper.Xnull(empmodel.Code));
                                }

                                cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                                cmd.Parameters.AddWithValue("@MAC_CODE", _dbHelper.Xnull(empmodel.MacCode));
                                cmd.Parameters.AddWithValue("@M_TYPE", _dbHelper.Xnull(empmodel.MType));
                                cmd.Parameters.AddWithValue("@TITLE", _dbHelper.Xnull(empmodel.Title));
                                cmd.Parameters.AddWithValue("@FIRSTNAME", _dbHelper.Xnull(empmodel.FirstName));
                                cmd.Parameters.AddWithValue("@MIDDLENAME", _dbHelper.Xnull(empmodel.MiddleName));
                                cmd.Parameters.AddWithValue("@LASTNAME", _dbHelper.Xnull(empmodel.LastName));
                                cmd.Parameters.AddWithValue("@NAME", _dbHelper.Xnull(empmodel.Name));
                                cmd.Parameters.AddWithValue("@FATHER_NAME", _dbHelper.Xnull(empmodel.FatherName));
                                cmd.Parameters.AddWithValue("@REJOIN_CODE", _dbHelper.Xnull(empmodel.RejoinCode));
                                cmd.Parameters.AddWithValue("@PADD1", _dbHelper.Xnull(empmodel.PAdd1));
                                cmd.Parameters.AddWithValue("@PADD2", _dbHelper.Xnull(empmodel.PAdd2));
                                cmd.Parameters.AddWithValue("@PADD3", _dbHelper.Xnull(empmodel.PAdd3));
                                cmd.Parameters.AddWithValue("@PCITY_CODE", _dbHelper.Xnull(empmodel.PCityCode));
                                cmd.Parameters.AddWithValue("@TADD1", _dbHelper.Xnull(empmodel.TAdd1));
                                cmd.Parameters.AddWithValue("@TADD2", _dbHelper.Xnull(empmodel.TAdd2));
                                cmd.Parameters.AddWithValue("@TADD3", _dbHelper.Xnull(empmodel.TAdd3));
                                cmd.Parameters.AddWithValue("@TCITY_CODE", _dbHelper.Xnull(empmodel.TCityCode));
                                cmd.Parameters.AddWithValue("@SEX", _dbHelper.Xnull(empmodel.Sex));
                                cmd.Parameters.AddWithValue("@AGE", _dbHelper.Vnull(empmodel.Age));
                                cmd.Parameters.AddWithValue("@DOB", _dbHelper.FGetSmallDateTime(empmodel.Dob));
                                cmd.Parameters.AddWithValue("@DOM", _dbHelper.FGetSmallDateTime(empmodel.Dom));
                                cmd.Parameters.AddWithValue("@DESG_CODE", _dbHelper.Xnull(empmodel.DesgCode));
                                cmd.Parameters.AddWithValue("@DEPT_CODE", _dbHelper.Xnull(empmodel.DeptCode));
                                cmd.Parameters.AddWithValue("@PLACE_CODE", _dbHelper.Xnull(empmodel.PlaceCode));
                                cmd.Parameters.AddWithValue("@INCPLACE_CODE", _dbHelper.Xnull(empmodel.IncPlaceCode));
                                cmd.Parameters.AddWithValue("@TYPE", _dbHelper.Xnull(empmodel.Type));
                                cmd.Parameters.AddWithValue("@CADER_CODE", _dbHelper.Xnull(empmodel.CaderCode));
                                cmd.Parameters.AddWithValue("@CAT_CODE", _dbHelper.Xnull(empmodel.CatCode));
                                cmd.Parameters.AddWithValue("@GRADE_CODE", _dbHelper.Xnull(empmodel.GradeCode));
                                cmd.Parameters.AddWithValue("@JOIN_DATE", _dbHelper.FGetSmallDateTime(empmodel.JoinDate));
                                cmd.Parameters.AddWithValue("@RESIGN_DATE", _dbHelper.FGetSmallDateTime(empmodel.ResignDate));
                                cmd.Parameters.AddWithValue("@PERMANENT_DATE", _dbHelper.FGetSmallDateTime(empmodel.PermanentDate));
                                cmd.Parameters.AddWithValue("@RIS_ZONE", _dbHelper.Xnull(empmodel.RisZone));
                                cmd.Parameters.AddWithValue("@PF_APPL", _dbHelper.Xnull(empmodel.PfAppl));
                                cmd.Parameters.AddWithValue("@PF_NO", _dbHelper.Xnull(empmodel.PfNo));
                                cmd.Parameters.AddWithValue("@PF_DATE", _dbHelper.FGetSmallDateTime(empmodel.PfDate));
                                cmd.Parameters.AddWithValue("@ESI_APPL", _dbHelper.Xnull(empmodel.EsiAppl));
                                cmd.Parameters.AddWithValue("@ESI_NO", _dbHelper.Xnull(empmodel.EsiNo));
                                cmd.Parameters.AddWithValue("@ESI_DATE", _dbHelper.FGetSmallDateTime(empmodel.EsiDate));
                                cmd.Parameters.AddWithValue("@OFFDAY", _dbHelper.Xnull(empmodel.OffDay));
                                cmd.Parameters.AddWithValue("@SHIFT", _dbHelper.Xnull(empmodel.Shift));
                                cmd.Parameters.AddWithValue("@REF_BY", _dbHelper.Xnull(empmodel.RefBy));
                                cmd.Parameters.AddWithValue("@SPOUSE", _dbHelper.Xnull(empmodel.Spouse));
                                cmd.Parameters.AddWithValue("@CONTACT", _dbHelper.Xnull(empmodel.Contact));
                                cmd.Parameters.AddWithValue("@MOBILE", _dbHelper.Vnull(empmodel.Mobile));
                                cmd.Parameters.AddWithValue("@EMAIL", _dbHelper.Xnull(empmodel.Email));
                                cmd.Parameters.AddWithValue("@UAN", _dbHelper.Xnull(empmodel.Uan));
                                cmd.Parameters.AddWithValue("@PAN", _dbHelper.Xnull(empmodel.Pan));
                                cmd.Parameters.AddWithValue("@AADAR", _dbHelper.Xnull(empmodel.Aadar));
                                cmd.Parameters.AddWithValue("@VOTORID", _dbHelper.Xnull(empmodel.VotorId));
                                cmd.Parameters.AddWithValue("@DL", _dbHelper.Xnull(empmodel.Dl));
                                cmd.Parameters.AddWithValue("@AC_NO", _dbHelper.Xnull(empmodel.AcNo));
                                cmd.Parameters.AddWithValue("@BANK_CODE", _dbHelper.Xnull(empmodel.BankCode));
                                cmd.Parameters.AddWithValue("@BANK_NAME", _dbHelper.Xnull(empmodel.BankName));
                                cmd.Parameters.AddWithValue("@IFSC_CODE", _dbHelper.Xnull(empmodel.IfscCode));
                                cmd.Parameters.AddWithValue("@BRANCH", _dbHelper.Xnull(empmodel.Branch));
                                cmd.Parameters.AddWithValue("@AC_TYPE", _dbHelper.Xnull(empmodel.AcType));
                                cmd.Parameters.AddWithValue("@BANK_VERIFY", _dbHelper.Xnull(empmodel.BankVerify));
                                cmd.Parameters.AddWithValue("@REMARKS", _dbHelper.Xnull(empmodel.Remarks));
                                cmd.Parameters.AddWithValue("@LEAVE_APPL", _dbHelper.Xnull(empmodel.LeaveAppl));
                                cmd.Parameters.AddWithValue("@HOLIDAY_APPL", _dbHelper.Xnull(empmodel.HolidayAppl));
                                cmd.Parameters.AddWithValue("@BONUS_APPL", _dbHelper.Xnull(empmodel.BonusAppl));
                                cmd.Parameters.AddWithValue("@BLOOD_GROUP", _dbHelper.Xnull(empmodel.BloodGroup));
                                cmd.Parameters.AddWithValue("@RELIGION", _dbHelper.Xnull(empmodel.Religion));
                                cmd.Parameters.AddWithValue("@CARD_NO", _dbHelper.Xnull(empmodel.CardNo));
                                cmd.Parameters.AddWithValue("@CTC_PROD_INC", _dbHelper.Vnull(empmodel.CtcProdInc));
                                cmd.Parameters.AddWithValue("@CTC_OTHER_ALLOW", _dbHelper.Vnull(empmodel.CtcOtherAllow));
                                cmd.Parameters.AddWithValue("@CTC_MISC_BENEFITS", _dbHelper.Vnull(empmodel.CtcMiscBenefits));
                                cmd.Parameters.AddWithValue("@CTC_CAR_BENEFITS", _dbHelper.Vnull(empmodel.CtcCarBenefits));
                                cmd.Parameters.AddWithValue("@CTC_RENT_REIMB", _dbHelper.Vnull(empmodel.CtcRentReimb));
                                cmd.Parameters.AddWithValue("@CTC_MOB_EXPS", _dbHelper.Vnull(empmodel.CtcMobExps));
                                cmd.Parameters.AddWithValue("@CTC_TEA_EXPS", _dbHelper.Vnull(empmodel.CtcTeaExps));
                                cmd.Parameters.AddWithValue("@FAPROV_STATUS", _dbHelper.Xnull(empmodel.FaProvStatus));
                                cmd.Parameters.AddWithValue("@FAPROV_REMARKS", _dbHelper.Xnull(empmodel.FaProvRemarks));
                                cmd.Parameters.AddWithValue("@MIN_SALARY", _dbHelper.Vnull(empmodel.MinSalary));
                                cmd.Parameters.AddWithValue("@MAX_SALARY", _dbHelper.Vnull(empmodel.MaxSalary));
                                cmd.Parameters.AddWithValue("@D1", _dbHelper.Xnull(empmodel.D1));
                                cmd.Parameters.AddWithValue("@D2", _dbHelper.Xnull(empmodel.D2));
                                cmd.Parameters.AddWithValue("@D3", _dbHelper.Xnull(empmodel.D3));
                                cmd.Parameters.AddWithValue("@JOB_RES", _dbHelper.Xnull(empmodel.JobRes));
                                cmd.Parameters.AddWithValue("@EXPERIENCE", _dbHelper.Vnull(empmodel.Experience));
                                cmd.Parameters.AddWithValue("@ABRY", _dbHelper.Xnull(empmodel.Abry));
                                cmd.Parameters.AddWithValue("@GRATUITY_APPL", _dbHelper.Xnull(empmodel.GratuityAppl));
                                cmd.Parameters.AddWithValue("@RETIREMENT_DATE", _dbHelper.FGetSmallDateTime(empmodel.RetirementDate));
                                cmd.Parameters.AddWithValue("@SALARY_EFFDATE", _dbHelper.FGetSmallDateTime(empmodel.SalaryEffDate));
                                cmd.Parameters.AddWithValue("@HRA_DEDUCT", _dbHelper.Xnull(empmodel.HraDeduct));
                                cmd.Parameters.AddWithValue("@FIXED_SALARY", _dbHelper.Vnull(empmodel.FixedSalary));
                                cmd.Parameters.AddWithValue("@EMP_TYPE", _dbHelper.Xnull(empmodel.EmpType));
                                cmd.Parameters.AddWithValue("@PMRPY", _dbHelper.Xnull(empmodel.Pmrpy));
                                cmd.Parameters.AddWithValue("@BANK_LOAN", _dbHelper.Xnull(empmodel.BankLoan));
                                cmd.Parameters.AddWithValue("@CARD_ISSUE", _dbHelper.Xnull(empmodel.CardIssue));
                                cmd.Parameters.AddWithValue("@QUARTER_NO", _dbHelper.Xnull(empmodel.QuarterNo));
                                cmd.Parameters.AddWithValue("@LINE_NO", _dbHelper.Xnull(empmodel.LineNo));
                                cmd.Parameters.AddWithValue("@COLONY_APPL", _dbHelper.Xnull(empmodel.ColonyAppl));
                                cmd.Parameters.AddWithValue("@ROOM_NO", _dbHelper.Xnull(empmodel.RoomNo));
                                cmd.Parameters.AddWithValue("@PPF_ONFULLBASIC", _dbHelper.Xnull(empmodel.PpfOnFullBasic));
                                cmd.Parameters.AddWithValue("@M_STATUS", _dbHelper.Xnull(empmodel.MStatus));
                                cmd.Parameters.AddWithValue("@VPF_APPL", _dbHelper.Xnull(empmodel.VpfAppl));
                                cmd.Parameters.AddWithValue("@VPF_DATE", _dbHelper.FGetSmallDateTime(empmodel.VpfDate));
                                cmd.Parameters.AddWithValue("@RETR_DATE", _dbHelper.FGetSmallDateTime(empmodel.RetrDate));
                                cmd.Parameters.AddWithValue("@imagepath", string.IsNullOrEmpty(empmodel.ImagePath)
                                                                            ? DBNull.Value
                                                                            : (object)empmodel.ImagePath);

                                cmd.Parameters.Add("@imagebyte", SqlDbType.VarBinary).Value =
                                    empmodel.ImageByte != null ? (object)empmodel.ImageByte : DBNull.Value;

                                cmd.Parameters.AddWithValue("@ACTIVE", _dbHelper.Xnull(empmodel.active));
                                cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId);
                                cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);

                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                cmd.Parameters.Add(returnParam);
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);
                                await cmd.ExecuteNonQueryAsync();
                                string errorMessage = errorParam.Value?.ToString();
                                if ((int)returnParam.Value <= 0)
                                    success = false;
                            }

                            async Task<bool> CallProcedure(string spName, string paramName, DataTable data, string typeName)
                            {
                                if (data.Rows.Count == 0) return true;
                                using (var cmd = new SqlCommand(spName, con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                                    cmd.Parameters.AddWithValue("@Code", EmpCode);
                                    cmd.Parameters.AddWithValue("@Lip", usersessionDt.PubLocalId ?? (object)DBNull.Value);
                                    cmd.Parameters.AddWithValue("@User", usersessionDt.PubUserId);
                                    cmd.Parameters.AddWithValue("@ACTIVE", empmodel.active);

                                    var tvp = cmd.Parameters.AddWithValue(paramName, data);
                                    tvp.SqlDbType = SqlDbType.Structured;
                                    tvp.TypeName = typeName;

                                    var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                    cmd.Parameters.Add(returnParam);

                                    var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
                                    {
                                        Direction = ParameterDirection.Output
                                    };
                                    cmd.Parameters.Add(errorParam);

                                    await cmd.ExecuteNonQueryAsync();
                                    int result = (int)(returnParam.Value ?? 0);
                                    return result > 0;
                                }
                            }

                            success &= await CallProcedure("[dbo].[sp_EmpExperience_AED]", "@EXPERIENCES", experienceTable, "dbo.EmployeeExperience_Type");
                            success &= await CallProcedure("[dbo].[sp_EmpFamily_AED]", "@FAMILY", familyTable, "dbo.EmployeeFamily_Type");
                            success &= await CallProcedure("[dbo].[sp_EmpQualification_AED]", "@Qualifications", qualificationTable, "dbo.EmployeeQualification_Type");
                            success &= await CallProcedure("[dbo].[sp_EmpRelative_AED]", "@Relatives", relativeTable, "dbo.EmployeeRelatives_Type");
                            success &= await CallProcedure("[dbo].[sp_EmpAttachment_AED]", "@Attachments", attachmentTable, "dbo.EmployeeAttachment_Type");

                            if (success)
                                transaction.Commit();
                            else
                                transaction.Rollback();

                            return Json(new
                            {
                                status = success,
                                message = success ? "Data save/update successfully." : "Failed to save or update some employee details."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction?.Rollback();
                            return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

 
        [HttpPost]
        public async Task<IActionResult> DeleteEmployeeMaster(string code)
        {
            if (code == null)
                return Json(new { status = false, message = " data delete failed." });

            try
            {
                using (var con = _dbcontext.GetErpConnection())
                {                    

                    await con.OpenAsync();
                    var usersessionDt = _globalValue.GetGlobalVariables();


                    DataTable experienceTable = ToEmptyDataTable("dbo.EmployeeExperience_Type");
                    DataTable familyTable = ToEmptyDataTable("dbo.EmployeeFamily_Type");
                    DataTable qualificationTable = ToEmptyDataTable("dbo.EmployeeQualification_Type");
                    DataTable relativeTable = ToEmptyDataTable("dbo.EmployeeRelatives_Type");
                    DataTable attachmentTable = ToEmptyDataTable("dbo.EmployeeAttachment_Type");

                    using (var transaction = con.BeginTransaction())
                    {
                        bool success = true;

                        try
                        {
                            using (SqlCommand cmd = new SqlCommand("[dbo].[sp_EmployeeMast_AED]", con, transaction))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Transaction = transaction;
                                 
                                cmd.Parameters.AddWithValue("@AED", "D");
                                cmd.Parameters.AddWithValue("@companyCd", _globalValue.GetGlobalVariables().PubCompCode);
                                cmd.Parameters.AddWithValue("@CODE", _dbHelper.Xnull(code));

                                var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                cmd.Parameters.Add(returnParam);
                                var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
                                {
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);
                                await cmd.ExecuteNonQueryAsync();
                                string errorMessage = errorParam.Value?.ToString();
                                if ((int)returnParam.Value <= 0)
                                    success = false;
                            }

                            async Task<bool> CallProcedure(string spName, string paramName, DataTable data, string typeName)
                            {
                              
                                using (var cmd = new SqlCommand(spName, con, transaction))
                                {
                                    cmd.CommandType = CommandType.StoredProcedure;
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.AddWithValue("@companyCd", usersessionDt.PubCompCode);
                                    cmd.Parameters.AddWithValue("@Code", code);
                                    var tvp = cmd.Parameters.AddWithValue(paramName, data);
                                    tvp.SqlDbType = SqlDbType.Structured;
                                    tvp.TypeName = typeName;

                                    var returnParam = new SqlParameter("@ReturnVal", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
                                    cmd.Parameters.Add(returnParam);

                                    var errorParam = new SqlParameter("@ErrorMessage", SqlDbType.NVarChar, 4000)
                                    {
                                        Direction = ParameterDirection.Output
                                    };
                                    cmd.Parameters.Add(errorParam);

                                    await cmd.ExecuteNonQueryAsync();
                                    int result = (int)(returnParam.Value ?? 0);
                                    return true;
                                }
                            }

                            success &= await CallProcedure("[dbo].[sp_EmpExperience_AED]", "@EXPERIENCES", experienceTable, "dbo.EmployeeExperience_Type");
                            success &= await CallProcedure("[dbo].[sp_EmpFamily_AED]", "@FAMILY", familyTable, "dbo.EmployeeFamily_Type");
                            success &= await CallProcedure("[dbo].[sp_EmpQualification_AED]", "@Qualifications", qualificationTable, "dbo.EmployeeQualification_Type");
                            success &= await CallProcedure("[dbo].[sp_EmpRelative_AED]", "@Relatives", relativeTable, "dbo.EmployeeRelatives_Type");
                            success &= await CallProcedure("[dbo].[sp_EmpAttachment_AED]", "@Attachments", attachmentTable, "dbo.EmployeeAttachment_Type");

                            if (success)
                                transaction.Commit();
                            else
                                transaction.Rollback();

                            return Json(new
                            {
                                status = success,
                                message = success ? "Data delete successfully." : "Failed to save or delete some employee details."
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction?.Rollback();
                            return Json(new { status = false, message = "Transaction failed: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        private DataTable FillDataTable<T>(List<T> data, string typeName)
        {
            int i = 1;
            int SeqNo = 1;
            DataTable employeeTable = ToEmptyDataTable(typeName);
            switch (typeName)
            {
                case "dbo.EmployeeExperience_Type":
                    foreach (var item in data.Cast<EmployeeExperience>())
                    {
                        employeeTable.Rows.Add(
                            SeqNo++,
                            item.Employer,
                            item.Address,
                            item.Doj,
                            item.Dor,
                            item.Period,
                            item.LastSalary,
                            item.Dept,
                            item.Desg,
                            item.JobProfile
                        );
                    }
                    break;

                case "dbo.EmployeeFamily_Type":
                    foreach (var item in data.Cast<EmployeeFamily>())
                    {
                        employeeTable.Rows.Add(
                                            SeqNo++,
                                            item.Member,
                                            item.Age,
                                            item.Contact,
                                            item.Relation,
                                            item.Minor,
                                            item.Nominee,
                                            item.Share,
                                            item.Address,
                                            item.Remarks
                                        );
                    }
                    break;

                case "dbo.EmployeeQualification_Type":
                    foreach (var item in data.Cast<EmployeeQualification>())
                    {
                        employeeTable.Rows.Add(
                                                SeqNo++,
                                                item.Degree,
                                                item.Board,
                                                item.Year,
                                                item.Marks,
                                                item.Remarks
                                        );
                    }
                    break;

                case "dbo.EmployeeRelatives_Type":
                    foreach (var item in data.Cast<EmployeeRelative>())
                    {
                        employeeTable.Rows.Add(
                                 SeqNo++,
                                item.Name,
                                item.DeptCode,
                                item.Contact,
                                item.Relation,
                                item.Address,
                                item.Remarks,
                                 i++
                        );
                    }
                    break;

                case "dbo.EmployeeAttachment_Type":
                    foreach (var item in data.Cast<EmployeeAttachment>())
                    {
                        employeeTable.Rows.Add(
                             item.AttachId,
                             item.FileName,
                             item.FilePath,
                             item.AttachDate,
                             item.FileRData
                        );
                    }
                    break;

                default:
                    employeeTable = null;
                    break;
            }

            return employeeTable;
        }

        private DataTable ToEmptyDataTable(string typeName)
        {
            var dt = new DataTable();
            switch (typeName)
            {
                case "dbo.EmployeeExperience_Type":
                    dt.Columns.Add("SEQ_NO", typeof(int));
                    dt.Columns.Add("EMPLOYER", typeof(string));
                    dt.Columns.Add("ADDRESS", typeof(string));
                    dt.Columns.Add("DOJ", typeof(DateTime));
                    dt.Columns.Add("DOR", typeof(DateTime));
                    dt.Columns.Add("PERIOD", typeof(string));
                    dt.Columns.Add("LAST_SALARY", typeof(decimal));
                    dt.Columns.Add("DEPT", typeof(string));
                    dt.Columns.Add("DESG", typeof(string));
                    dt.Columns.Add("JOB_PROFILE", typeof(string));
                    break;

                case "dbo.EmployeeFamily_Type":
                    dt.Columns.Add("SEQ_NO", typeof(int));
                    dt.Columns.Add("MEMBER", typeof(string));
                    dt.Columns.Add("AGE", typeof(string));
                    dt.Columns.Add("CONTACT", typeof(string));
                    dt.Columns.Add("RELATION", typeof(string));
                    dt.Columns.Add("MINOR", typeof(string));
                    dt.Columns.Add("NOMINEE", typeof(string));
                    dt.Columns.Add("SHARE", typeof(decimal));
                    dt.Columns.Add("ADDRESS", typeof(string));
                    dt.Columns.Add("REMARKS", typeof(string));
                    break;

                case "dbo.EmployeeQualification_Type":
                    dt.Columns.Add("SEQ_NO", typeof(int));
                    dt.Columns.Add("DEGREE", typeof(string));
                    dt.Columns.Add("BOARD", typeof(string));
                    dt.Columns.Add("YEAR", typeof(string));
                    dt.Columns.Add("MARKS", typeof(string));
                    dt.Columns.Add("REMARKS", typeof(string));
                    break;

                case "dbo.EmployeeRelatives_Type":
                    dt.Columns.Add("SEQ_NO", typeof(int));
                    dt.Columns.Add("NAME", typeof(string));
                    dt.Columns.Add("DEPT_CODE", typeof(int));
                    dt.Columns.Add("CONTACT", typeof(string));
                    dt.Columns.Add("RELATION", typeof(string));
                    dt.Columns.Add("ADDRESS", typeof(string));
                    dt.Columns.Add("REMARKS", typeof(string));
                    dt.Columns.Add("SrNO", typeof(int));
                    break;

                case "dbo.EmployeeAttachment_Type":
                    dt.Columns.Add("ATTACHID", typeof(int));
                    dt.Columns.Add("FILENAME", typeof(string));
                    dt.Columns.Add("FILEPATH", typeof(string));
                    dt.Columns.Add("ATTACHDATE", typeof(DateTime));
                    dt.Columns.Add("FILE_RDATA", typeof(byte[]));
                    break;

                default:
                    throw new ArgumentException("Unknown table type: " + typeName);
            }
            return dt;
        }


    }
}

