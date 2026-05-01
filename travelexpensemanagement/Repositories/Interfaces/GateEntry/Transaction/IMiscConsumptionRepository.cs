using travelexpensemanagement.Models;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IMiscConsumptionRepository
    {
        string GenerateVNo(string vType);

        string SaveMiscConsumption(MiscConsumptionEntry_Header header, List<Details> details, string action);
    }
}
