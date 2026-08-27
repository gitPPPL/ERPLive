using System.Text.Json;

namespace travelexpensemanagement.Repositories.Interfaces.Test
{
    public interface IDataConversionService
    {
        Task<dynamic> ConvertAsync(string tableName, JsonElement data);
    }
}