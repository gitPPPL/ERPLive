using travelexpensemanagement.Models.Purchase.Transiction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IImportExportExpensesEntryRepository
    {
        public Task<decimal> CheckExistingTDS(string billNo, int drCode);
        public Task<ValidationResult> ValidatePartyGst(string gstType, string partyCode, string gstNo);
        public Task<RepositoryResponse> SavePurchaseBillPassEntry(PurchaseWrapper data);
        public Task<RepositoryResponseData<FullPurchaseBillResponse>> GetFullQuotationByVno(int vNo, string vType);
        public Task<PBTdsCalculation> CalculateTDS(PURCHASE1 model);
        public RepositoryResponseList<CopyFromMenuItem> GetCopyFromMenu();
        public RepositoryResponseData<CopyFromGridResponse> GetCopyFromData(CopyFromRequest request);
        public RepositoryResponseData<List<PendingApprovalModel>> GetPendingApprovalList();
        public AddressDetails GetAddByParty(int code, int addressId);
        public Task<RepositoryResponseData<int>> GetPackOnBasic(int code);
        public Task<(int PartyCode, string PartyName)> GetFrtCrAcByTransCodeAsync(int transportCode);
        public Task<RepositoryResponseData<decimal>> GetPartyPurchaseAmount(int partyCode, string vType, int? vNo = null,
            decimal? currentAmount = null);
        public Task<RepositoryResponseData<string>> GetTDS206Apply(int partyCode);
        public Task<RepositoryResponseData<string>> GetPurchaseOrSaleVoucherNo(string transportName, string grNo, string currentVoucher, string purchaseOrSale);
        public Task<RepositoryResponseData<bool>> CheckPaymentExists(string docType, int docNo);
        public Task<(bool Exists, string DocId, DateTime? VDate)> CheckDuplicateBill(int partyCode, string billNo, int currentVNo);
        public Task<RepositoryResponseData<bool>> ValidateTaxType(int billToCode, decimal totalIGST, decimal totalCGST, decimal totalSGST);
        public Task<RepositoryResponseData<bool>> ValidateFreightExpense(string refType, string refVNo, string expsType);
        public Task<RepositoryResponseData<bool>> ValidateImportTracking(string refVType, string refVNo, string billFromPartyCode, string billNo, string billToName);
        public Task<RepositoryResponseData<bool>> ValidateCostAllocation(ValidateCostAllocationRequest model);
        public RepositoryResponseData<List<ImportInvoiceListModel>> GetImportInvoiceList(int partyCode);
    }
}
