using travelexpensemanagement.Models.Inventory.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.Sale.Transaction
{
    public interface ISalesInvoiceDirect
    {

        Task<(string Status, string Message)> SubmitRequest(SalesInvoiceDirectModel_Header header, List<SalesInvoiceDirectModel_Detail> details, string action);

    }
}
