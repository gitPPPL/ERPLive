using travelexpensemanagement.Models;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IMiscConsumptionRepository
    {
        List<object> GetItemList();
        List<object> GetDeptList();
        List<object> GetUnitList();
        List<object> GetDropdown(string type);
        List<object> GetAddressByPartyCode(int partyId);

        string GenerateVNo(string vType);

        string SaveMiscConsumption(MiscConsumptionEntry_Header header, List<Details> details, string action);

    }
}
