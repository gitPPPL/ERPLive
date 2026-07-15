using Microsoft.AspNetCore.Mvc;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseReceiptEntry;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseReceiptEntryRepository
    {
        Task<(bool Success, string Message)> SaveAllData(string Header,List<ItemDetailModel> ItemDetails,List<AttachmentModel> Attachments);

        Task<(bool Success,string Message,string WBType,int WBNo, Dictionary<string, object> Header,List<Dictionary<string, object>>? Items)> GetGatDetailsList(string StrVNo, string StrV_type);

        Task<(bool Success, string Message, PurchaseAllDetailsResponse Data)> GetAllDatadetails(GetDetailsRequest request);

    }
}
