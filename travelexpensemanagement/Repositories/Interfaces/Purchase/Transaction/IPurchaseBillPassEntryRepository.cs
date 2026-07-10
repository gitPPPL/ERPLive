using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseBillPassEntryRepository
    {
        public Task<DebitNoteResponse> CalculateFrieghtPay(DebitNoteRequest request);
        Task<DebitNoteResponse> CalculateDebitNote(DebitNoteRequest request);
    }
}
