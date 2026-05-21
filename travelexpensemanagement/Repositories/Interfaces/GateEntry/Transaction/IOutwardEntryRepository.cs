using travelexpensemanagement.Models.GateEntry;
namespace travelexpensemanagement.Repositories.Interfaces.GateEntry.Transaction
{
    public interface IOutwardEntryRepository
    {

        List<object> GetDataByPartyandAddressidCodeAsync(int partyId, int addressId);
        List<object> GetDataByPartyCodeAsync(int partyId);
        RepositoryResponse SaveOutwardEntry(OutWordEntry_Header header, List<DetailsOutwardEntry> details, string action); 
    }
}
