using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IItemMarketRateListRepository
    {
        (List<MARKET_RATE1> itemRates, int totalCount) GetAllItemRateList(string searchTerm, int pageNumber, int pageSize);

        bool DeleteItemMarketRateByCode(int code, string vType, int compCode, int branchCode,int yearCode);
    }
}
