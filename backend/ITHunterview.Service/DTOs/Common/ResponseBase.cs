namespace ITHunterview.Service.DTOs.Common
{
    public class ResponseBase<T>
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public T? Data { get; set; }

        public ResponseBase() { }

        public ResponseBase(T data, string? message = null)
        {
            Data = data;
            Message = message;
        }

        public ResponseBase(string errorMessage)
        {
            Success = false;
            Message = errorMessage;
        }
    }
}
