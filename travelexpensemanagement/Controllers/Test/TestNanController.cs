using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Test;
using travelexpensemanagement.Repositories.Interfaces.Test;

namespace travelexpensemanagement.Controllers.Test
{
    public class TestNanController : Controller
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly IDataConversionService _dataConversionService;

        public TestNanController(DataBaseConnection dbConnection, IDataConversionService dataConversionService)
        {
            _dbConnection = dbConnection;
            _dataConversionService = dataConversionService;
        }

        public IActionResult Index()
        {
            return View("~/Views/TestNan/Index.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] JsonElement request)
        {
            try
            {
                // ==========================================
                // Header
                // ==========================================

                var headerData = request.GetProperty("Header");
                dynamic employee = await _dataConversionService.ConvertAsync("Employee", headerData);


                // ==========================================
                // Footer
                // ==========================================

                var detailData = request.GetProperty("Details");
                dynamic footer = await _dataConversionService.ConvertAsync("TestFooter", detailData);

                // ==========================================
                // Connection
                // ==========================================

                using SqlConnection con = _dbConnection.GetErpConnection();
                await con.OpenAsync();


                // ==========================================
                // Transaction
                // ==========================================

                await using var transaction = await con.BeginTransactionAsync();
                try
                {
                    // ======================================
                    // INSERT EMPLOYEE
                    // ======================================

                    const string employeeSql = """
                INSERT INTO dbo.Employee
                (
                    Name,
                    Salary,
                    JoiningDate,
                    IsActive
                )
                VALUES
                (
                    @Name,
                    @Salary,
                    @JoiningDate,
                    @IsActive
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;


                    int newId;


                    await using (var command = new SqlCommand(employeeSql, con, (SqlTransaction)transaction))
                    {
                        command.Parameters.AddWithValue("@Name", employee.Name);
                        command.Parameters.AddWithValue("@Salary", employee.Salary);
                        command.Parameters.AddWithValue("@JoiningDate", employee.JoiningDate);
                        command.Parameters.AddWithValue("@IsActive", employee.IsActive);

                        var result = await command.ExecuteScalarAsync();
                        newId = Convert.ToInt32(result);
                    }


                    // ======================================
                    // INSERT FOOTER
                    // ======================================

                    const string footerSql = """
                INSERT INTO dbo.TestFooter
                (
                    Id,
                    Name
                )
                VALUES
                (
                    @Id,
                    @Name
                );
                """;


                    foreach (dynamic d in footer)
                    {
                        await using var footerCommand = new SqlCommand(footerSql, con, (SqlTransaction)transaction);

                        footerCommand.Parameters.AddWithValue("@Id", d.Id);
                        footerCommand.Parameters.AddWithValue("@Name", d.Name);

                        await footerCommand.ExecuteNonQueryAsync();
                    }


                    // ======================================
                    // COMMIT
                    // ======================================

                    await transaction.CommitAsync();

                    return Json(new {success = true, message = "Employee and footer saved successfully.", id = newId});
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (FormatException ex)
            {
                return BadRequest(new {success = false, message = ex.Message});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error while saving employee.", detail = ex.Message});
            }
        }

    }
}
