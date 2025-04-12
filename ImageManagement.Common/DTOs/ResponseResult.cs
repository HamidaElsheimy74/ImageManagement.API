namespace ImageManagement.Common.DTOs;

public class ResponseResult
{
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; } = null;
    public int StatusCode { get; set; } = 200;
    public ResponseResult() { }
    public ResponseResult(string message, object? data, int statusCode)
    {
        Message = message;
        Data = data;
        StatusCode = statusCode;
    }
}
