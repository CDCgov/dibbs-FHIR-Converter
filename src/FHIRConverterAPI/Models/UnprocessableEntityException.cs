public class UnprocessableEntityException : Exception
{
  public UnprocessableEntityException(string message) : base(message)
  {
    Message = message;
  }

  public string Message { get; set; }
}