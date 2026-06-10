namespace SimpleFileStorage.Api
{
    public class StringMessage
    {
        public string Message { get; set; }
        public DateTime Time { get; set; } = DateTime.UtcNow;

        public StringMessage(string message)
        {
            Message = message;
            Time = DateTime.UtcNow;
        }
    }
}
