namespace server.Extensions;

public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message, Exception? innerException = null, int? statusCode = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public int? StatusCode { get; }
}
