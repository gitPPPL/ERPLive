using Microsoft.AspNetCore.Mvc;
using System.Data;
using travelexpensemanagement.Models.Purchase.Transiction;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseBillPassEntryModel;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseBillPassEntryRepository
    {
        public Task<decimal> CheckExistingTDS(string billNo, int drCode);
        public Task<(int DebitAc, string DebitAcName)> GetLatestDebitAccount(string vType);
        public Task<DebitNoteResponse> CalculateFrieghtPay(DebitNoteRequest request);
        Task<DebitNoteResponse> CalculateDebitNote(DebitNoteRequest request);
        public Task<PurchaseQtyValidationResult> CheckPurchaseQtyExcess(int vNo, decimal currentRecQty);
        public Task<PurchaseRowValidationResult> ValidatePurchaseRow(string vType, int itemCode, string itemName, string billHsnCode,
            decimal qty, decimal freightAmount, string poType, int poNo, string mrnType, int mrnNo);
        public Task<ValidationResult> ValidatePoSaudaApproval(int itemCode, string itemName, string poType, int poNo);
        public Task<ValidationResult> ValidatePartyGst(string gstType, string partyCode, string gstNo);
        public Task<RepositoryResponse> SavePurchaseBillPassEntry(PurchaseWrapper data);
        public Task<RepositoryResponseData<FullPurchaseBillResponse>> GetFullQuotationByVno(int vNo, string vType);
        public Task<PBTdsCalculation> CalculateTDS(PURCHASE1 model);
        public RepositoryResponseList<CopyFromMenuItem> GetCopyFromMenu(string docType);
        public RepositoryResponseData<CopyFromGridResponse> GetCopyFromData(CopyFromRequest request);
        public RepositoryResponseData<List<PendingApprovalModel>> GetPendingApprovalList();
        public RepositoryResponseData<List<AdvanceTdsModel>> GetAdvanceTdsList(string billNo, int drCode);
        public Task<RepositoryResponseData<DrCrCalculationResponse>> CalculateRowDrCrAmount(CalculateDrCrRequest request);
        public Task<bool> UpdatePassDetails();
        public Task<bool> GenerateImportBillAsync(int vNo, string vType, DateTime vDate, int plNo);
        public AddressDetails GetAddByParty(int code, int addressId);
        public Task<PurchaseDetailsDto> GetPurchaseDetailsByMRN(string vType, int vNo);
        public RepositoryResponseData<string> ValidateMRN(string mrnTypeNo, string vType, int vNo);
        public RepositoryResponseList<PurchaseItemDto> GetPurchaseItemsByMRN(string vType, int vNo);
        public OrderRateDetailsDto GetItemOrderRatesByPO(string poType, int poNo, int itemCode);
        public Task<RepositoryResponseData<int>> GetPackOnBasic(int code);
        public Task<RepositoryResponseData<(int HsnCode, decimal Qty)>> GetHsnCodeAndQty(int itemCode, string poType, int poNo);
        public Task<(int PartyCode, string PartyName)> GetFrtCrAcByTransCodeAsync(int transportCode);
        public Task<RepositoryResponseData<DateTime>> GetPurchaseDate(string vType, int vNo);

        public Task<RepositoryResponseData<decimal>> GetPartyPurchaseAmount(int partyCode, string vType, int? vNo = null,
            decimal? currentAmount = null);
        public Task<RepositoryResponseData<string>> GetTDS206Apply(int partyCode);
        public Task<RepositoryResponseData<bool>> IsPostingExist(string vType);
        public Task<RepositoryResponseData<string>> GetPLDocId(int vNo, int plNo);
        public Task<RepositoryResponseData<string>> GetDebitAcType(int code);
        public Task<RepositoryResponseData<string>> GetPOType(string poType, int poNo);
        public Task<RepositoryResponseData<string>> GetPurchaseOrSaleVoucherNo(string transportName, string grNo, string currentVoucher, string purchaseOrSale);
        public Task<RepositoryResponseData<bool>> CheckPaymentExists(string docType, int docNo);
        public Task<(bool Exists, string DocId, DateTime? VDate)> CheckDuplicateBill(int partyCode, string billNo, int currentVNo);
        public Task<RepositoryResponseData<bool>> ValidateTaxType(int cityCode, decimal totalIGST, decimal totalCGST, decimal totalSGST);
        public Task<RepositoryResponseData<decimal>> GetCrDrNoteAmtForReport(string vType, int vNo, string drOrCr);

    }
}
