using travelexpensemanagement.Models.Purchase.Transiction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface ITradingDirectPurchaseRepository
    {
        public Task<decimal> CheckExistingTDS(string billNo, int drCode);
        public Task<DebitNoteResponse> CalculateFrieghtPay(DebitNoteRequest request);
        Task<DebitNoteResponse> CalculateDebitNote(DebitNoteRequest request);
        public Task<PurchaseRowValidationResult> ValidatePurchaseRow(string vType, int itemCode, string itemName, string billHsnCode,
            decimal qty, decimal freightAmount, string poType, int poNo, string mrnType, int mrnNo);
        public Task<ValidationResult> ValidatePoSaudaApproval(int itemCode, string itemName, string poType, int poNo);
        public Task<ValidationResult> ValidatePartyGst(string gstType, string partyCode, string gstNo);
        public Task<RepositoryResponse> SavePurchaseBillPassEntry(PurchaseWrapper data);
        public Task<RepositoryResponseData<FullPurchaseBillResponse>> GetFullQuotationByVno(int vNo, string vType);
        public AddressDetails GetAddByParty(int code, int addressId);
        public Task<PurchaseDetailsDto> GetPurchaseDetailsByMRN(string vType, int vNo);
        public RepositoryResponseData<string> ValidateMRN(string mrnTypeNo, string vType, int vNo);
        public RepositoryResponseList<PurchaseItemDto> GetPurchaseItemsByMRN(string vType, int vNo);
        public OrderRateDetailsDto GetItemOrderRatesByPO(string poType, int poNo, int itemCode);
        public Task<RepositoryResponseData<int>> GetPackOnBasic(int code);
        public Task<(int PartyCode, string PartyName)> GetFrtCrAcByTransCodeAsync(int transportCode);
        public Task<RepositoryResponseData<string>> GetPurchaseOrSaleVoucherNo(string transportName, string grNo, string currentVoucher, string purchaseOrSale);
        public Task<RepositoryResponseData<bool>> CheckPaymentExists(string docType, int docNo);
        public Task<(bool Exists, string DocId, DateTime? VDate)> CheckDuplicateBill(int partyCode, string billNo, int currentVNo);
        public Task<RepositoryResponseData<bool>> ValidateTaxType(int cityCode, decimal totalIGST, decimal totalCGST, decimal totalSGST);
    }
}
