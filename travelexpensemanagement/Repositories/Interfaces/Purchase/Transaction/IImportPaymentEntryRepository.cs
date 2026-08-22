using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Models.Purchase.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IImportPaymentEntryRepository
    {
        Task<(bool Success, string Message)> SaveImportPaymentEntryAsync(ImportPaymentEntryModel.SaveImportPaymentEntry model);

        Task<(Dictionary<string, object> Header,List<Dictionary<string, object>> Footer)> LoadEditDataAsync(string docId);
    }
}
