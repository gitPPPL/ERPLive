using travelexpensemanagement.Models.Test;

namespace travelexpensemanagement.Repositories.Interfaces.Test
{
    public interface IDatabaseMetadataService
    {
        Task<IReadOnlyDictionary<string, ColumnMetadataModel>>
            GetColumnsAsync(string tableName);
    }
}
