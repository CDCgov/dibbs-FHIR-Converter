using System.Net;

public class UserFacingException: Exception
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