using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sale.Transaction
{
    public interface ISalesInVoice
    {

        Task<(string Status, string Message)> SubmitRequest(SalesInvoiceModel_Header header, List<SalesInvoiceModel_Detail> details, string action);

    }
}
