using Microsoft.Data.SqlClient;
using System.Data;
using travelexpensemanagement.Dbconnection;

namespace travelexpensemanagement.Common.GlobalExcel
{
    public class GlobalExcelExport
    {
        private readonly DataBaseConnection _dbConnection;
        public GlobalExcelExport(DataBaseConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        //==================Export Excel===================
        public byte[] ExportToExcel(string spName, string sheetName, Dictionary<string, object> parameters = null)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = _dbConnection.GetErpConnection())
            {
                using (SqlCommand cmd = new SqlCommand(spName, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(
                                param.Key,
                                param.Value ?? DBNull.Value
                            );
                        }
                    }

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }

            using var workbook = new ClosedXML.Excel.XLWorkbook();

            var worksheet = workbook.Worksheets.Add(sheetName);

            worksheet.Cell(1, 1).InsertTable(dt);

            worksheet.Row(1).Style.Font.Bold = true;

            worksheet.Row(1).Style.Fill.BackgroundColor =
                ClosedXML.Excel.XLColor.LightBlue;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}
