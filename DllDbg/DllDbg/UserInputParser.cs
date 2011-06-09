namespace DllDbg
{
    public class UserInputParser : IResponseParser
    {
        public bool Yes(string response)
        {
            return response.ToLower().Contains("y");
        }
    }
}