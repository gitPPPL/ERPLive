namespace travelexpensemanagement.Repositories
{
    public class RepositoryResponse
    {
        public bool status { get; set; } = false;
        public string? message { get; set; }
    }
    public class RepositoryResponseData<T>
    {
        public bool status { get; set; } = false;
        public string? message { get; set; }
        public T? data { get; set; }
    }
    public class RepositoryResponseList<T>
    {
        public bool status { get; set; } = false;
        public string? message { get; set; }
        public int totalCount { get; set; }
        public List<T>? data { get; set; }
    }
    public class DocInfo
    {
        public string? DocId { get; set; }
        public string? VNo { get; set; }
    }
}
