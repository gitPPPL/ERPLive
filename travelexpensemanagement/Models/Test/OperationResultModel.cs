namespace travelexpensemanagement.Models.Test
{
    public class OperationResultModel
    {
        public bool Success { get; init; }

        public string? Message { get; init; }

        public int? Id { get; init; }

        public Dictionary<string, string[]> Errors { get; init; }
            = new();

        public static OperationResultModel SuccessResult(
            string message,
            int? id = null)
        {
            return new OperationResultModel
            {
                Success = true,
                Message = message,
                Id = id
            };
        }

        public static OperationResultModel Failed(
            Dictionary<string, string[]> errors)
        {
            return new OperationResultModel
            {
                Success = false,
                Errors = errors
            };
        }
    }
}
