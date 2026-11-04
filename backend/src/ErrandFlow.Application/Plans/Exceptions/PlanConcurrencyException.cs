namespace ErrandFlow.Application.Plans.Exceptions;

public sealed class PlanConcurrencyException : Exception
{
    public PlanConcurrencyException(
        Guid planId,
        ulong expectedRevision,
        Exception? innerException = null)
        : base(
            "The plan was changed by another request.",
            innerException)
    {
        PlanId = planId;
        ExpectedRevision = expectedRevision;
    }

    public Guid PlanId { get; }

    public ulong ExpectedRevision { get; }
}