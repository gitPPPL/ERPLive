using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ITransportRateRequestForQuotationListRepository
    {
        public RepositoryResponse DeleteTransportRateQuotationEntry(int docId, string docType);
        public RepositoryResponseList<TransportRateRequestForQuotationListModel> GetAllTransportRateQuotationList(string searchTerm = "", int pageNumber = 1, int pageSize = 10);
    }
}
