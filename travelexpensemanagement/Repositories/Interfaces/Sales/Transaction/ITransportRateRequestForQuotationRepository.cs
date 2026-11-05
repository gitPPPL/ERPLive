
    using travelexpensemanagement.Models.Sales.Transaction;

    namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
    {
        public interface ITransportRateRequestForQuotationRepository
        {
            Task<RepositoryResponseData<List<TransportRateRequestImportModel>>> ImportDispatch(int refNo);
            Task<RepositoryResponse> Save(TransportRateRequestForQuotationModel model);
            Task<RepositoryResponseData<TransportRateRequestForQuotationModel>> GetById(string vType, int vNo);
            Task<RepositoryResponse> SendTransportRequestEmail(TransportEmailRequestModel model);
            Task<RepositoryResponse> SendTransportRequestWhatsApp(TransportWhatsAppRequestModel model);
        }
    }
