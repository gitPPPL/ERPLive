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
        public Task<RepositoryResponse> SavePurchaseBillPassEntry([FromBody] PurchaseWrapper data);
        public Task<RepositoryResponseData<FullPurchaseBillResponse>> GetFullQuotationByVno(int vNo, string vType);
        public Task<PBTdsCalculation> CalculateTDS(PURCHASE1 model);
        public RepositoryResponseList<CopyFromMenuItem> GetCopyFromMenu(string docType);
        public RepositoryResponseData<List<Dictionary<string, object?>>> GetCopyFromData(CopyFromRequest request);
    }
}
