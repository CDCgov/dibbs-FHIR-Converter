using System.Net;

/// <summary>
/// Allows customization of exception message, specifying status code, and optionally includes the original exception.
/// </summary>
public class UserFacingException : Exception
{
  public UserFacingException(string message, HttpStatusCode statusCode)
  : base(message)
  {
    StatusCode = statusCode;
  }

  public UserFacingException(string message, HttpStatusCode statusCode, Exception? innerException = null)
  : base(message, innerException)
  {
    StatusCode = statusCode;
  }

  public HttpStatusCode StatusCode { get; set; }
}