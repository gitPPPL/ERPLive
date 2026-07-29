using Microsoft.AspNetCore.Mvc;
using static travelexpensemanagement.Common.DropdownService.DropdownService;
using static travelexpensemanagement.Models.Purchase.Transaction.PurchaseReturnEntry;

namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseReturnEntryRepository
    {
        List<object> GetddlDocType();
        List<object> GetddlRefType();
        int GetDocNo(string docType);
        List<object> GetddlRefNo(string vType);
        List<object> GetddlDocStatus();
        List<object> GetMakeListByItem();
        List<object> GetDepartmentList();
        List<object> GetddlReturnTo();
        List<object> GetddlCreditAC();
        List<object> GetddlDebitAC();
        List<object> GetddlFreightCreditAC();
        List<object> GetddlFreightDebitAC();
        object GetBillDetails(int code);
        List<object> GetddlCityBillDetails();
        List<object> GetddlstateBillDetails();
        List<object> GetddlCityShipDetails();
        List<object> GetddlstateShipDetails();
        List<object> GetddlShipDetails();
        List<DropdownModel> GetTransportName(string term);
        List<object> GetddlTransportAc();
        List<object> GetItemList();
        object GetHSNCode(int code);
        List<object> GetTaxTypeList();
        object GetTaxTypeDetails(string code);
        Task<object> SaveAllData(PurchaseReturnHeaderModel headerObj, List<ItemDetailModel> itemDetails, List<AttachmentModel> attachments);
        Task<GatePurchaseDetailsResponse> GetRefNoList(string strVNo, string strVType);
        Task<PurchaseAllDetailsResponse> GetAllDataDetails(GetDetailsRequest request);
        Task<object> PrintPurchaseReturnEntryReport(PrintReportModelPurchaseReturnEntry model);
    }
}
