namespace ErrandFlow.Application.Places.Exceptions;

public sealed class PlaceSearchUnavailableException : Exception
{
    public PlaceSearchUnavailableException()
        : base("Place search is temporarily unavailable.")
    {
    }
}
