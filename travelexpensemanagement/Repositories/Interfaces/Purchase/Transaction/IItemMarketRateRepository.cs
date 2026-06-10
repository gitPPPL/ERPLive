using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IItemMarketRateRepository
    {
        Task<ItemMarketRateWrapper?> GetItemMarketRateByVnoAsync(int vNo);

        Task<(bool Success, string Message, int VNo)> SaveItemMarketRateAsync(ItemMarketRateWrapper data);

        bool IsDuplicateMarketRateEntry(int vNo, int compCode, int yearCode, int branchCode);
    }
}
