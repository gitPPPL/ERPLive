using System.Text.Json;
using travelexpensemanagement.Models.Test;

namespace travelexpensemanagement.Repositories.Interfaces.Test
{
    public interface ISqlValueConverter
    {
        object Convert(JsonElement value, ColumnMetadataModel column);
    }
}
