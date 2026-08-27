using travelexpensemanagement.Models.Purchase.Transiction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IImportExportExpensesEntryListRepository
    {
        public RepositoryResponse DeletePurchaseBillPass(int docId, string docType);
        public PurchaseEditStatus GetPurchaseEditStatus(string vType, int vNo);
        public PurchaseDeleteStatus GetPurchaseDeleteStatus(string vType, int vNo);
        public RepositoryResponseList<PURCHASE1> GetAllPurchaseBillPassEntry(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
    }
}
