namespace ErrandFlow.Application.Planning.Exceptions;

public sealed class TravelGraphUnavailableException : Exception
{
    public TravelGraphUnavailableException()
        : base("Travel graph is temporarily unavailable.")
    {
    }
}