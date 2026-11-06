namespace ErrandFlow.Application.Planning.Exceptions;

public sealed class RoutePathUnavailableException : Exception
{
    public RoutePathUnavailableException()
        : base("The route path could not be retrieved.")
    {
    }
}