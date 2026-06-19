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

    /// <summary>
    /// Non-generic response for endpoints with no data payload
    /// </summary>
    public class ResponseBase
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }

        public static ResponseBase Ok(string? message = null) => new ResponseBase { Success = true, Message = message };
        public static ResponseBase Fail(string message) => new ResponseBase { Success = false, Message = message };
    }
}
