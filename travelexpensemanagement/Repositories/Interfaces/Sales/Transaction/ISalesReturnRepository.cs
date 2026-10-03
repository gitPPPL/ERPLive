namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface ISalesReturnRepository
    {
        
        Task<RepositoryResponseData<object>> GetReferenceDetails(string refValue);
        RepositoryResponse Save(SalesReturn salesReturn);
        RepositoryResponseData<object> GetID(int id, string vType);
        Task<RepositoryResponseList<SaudaItemDetail>> GetSaudaItemDetails(string saudaType, string saudaNo, int partyCode, string itemCodes);
        RepositoryResponseData<object> GetWBWeight(string wbDocId);
        RepositoryResponseData<object> GetPackingData(string packType, int packNo, string vType, int vNo);
        RepositoryResponseData<object> GetGateData(string gateDocId);
        RepositoryResponse ValidateData(ValidateDataRequest model);
        Task<RepositoryResponseList<SaudaItemDetail>> CalculateSaudaRate(int partyCode, string itemCodes);
        RepositoryResponse PostSalesReturn(string vType, int vNo);
        RepositoryResponseData<object> SaveTransport(SaveTransportRequest req);
    }
}
