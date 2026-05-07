using travelexpensemanagement.Models.GateEntry;

namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IOutwardEntryRepository
    {
        Task<string> GetVNoAsync(string vType, string tableName = "GATE2");
        List<object> GetDataByPartyandAddressidCodeAsync(int partyId, int addressId);
        List<object> GetDataByPartyCodeAsync(int partyId);           
        string SaveOutwardEntry(OutWordEntry_Header header, List<DetailsOutwardEntry> details, string action);
   


    }
}
