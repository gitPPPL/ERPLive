namespace travelexpensemanagement.Repositories.Interfaces.Purchase.Transaction
{
    public interface IPurchaseReceiptEntryListRepository
    {
        (List<object> Items, int TotalCount) GetPurchaseReceiptEntryList(string searchTerm, int pageNumber = 1, int pageSize = 10);

        (bool Success, string Message) DeleteDocByCode(string vType, string vNo);

    }
}
