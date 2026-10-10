using travelexpensemanagement.Models.Sales.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sales.Transaction
{
    public interface IEInvoiceUtilityRepository
    {
        RepositoryResponseList<EInvoiceUtilityModel> GetEInvoiceList(string vType);
        Task<RepositoryResponseData<EInvoiceStatusModel>> GetInvoiceStatus(string vType, long vNo);
        Task<RepositoryResponseData<string>> GetSignedJson(string vType, long vNo);
        Task<RepositoryResponseData<string>> GetEWayBillNo(string vType, long vNo);
        Task<RepositoryResponseData<string>> GetIRN(string vType, long vNo);
        Task<RepositoryResponseData<ApiModeModel>> GetApiMode();

        Task<EInvoiceResultModel> EInvoice(int vNo, string vType, bool cessNAValue);
        Task<IrnResult> GenerateIRNFromJson(int vNo, string vType, string jsonText);
        Task<RepositoryResponseData<string>> CreateJsonOnly(int vNo, string vType, bool cessNAValue);
        Task<RepositoryResponseData<string>> CreateQrFromSignedJson(int vNo, string vType, string signedJson);
        Task<RepositoryResponseData<string>> CreateEwbJsonOnly(int vNo, string vType);
        Task<RepositoryResponseData<EwayBillResultModel>> EWayBill(int vNo, string vType, bool useEwbJson, string jsonText, bool cbEwayBill, bool takeConfirmation = false);
        Task<RepositoryResponseData<CancelResultModel>> CancelIRN(int vNo, string vType, string irn, bool confirmed);
        Task<RepositoryResponseData<CancelResultModel>> CancelEwayBill(int vNo, string vType, string ewbNo);

        Task<RepositoryResponseData<string>> Authenticate();
    }
}
