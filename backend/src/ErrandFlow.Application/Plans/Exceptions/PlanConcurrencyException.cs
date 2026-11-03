namespace ErrandFlow.Application.Plans.Exceptions;

public sealed class PlanConcurrencyException : Exception
{
    public PlanConcurrencyException(Guid planId, ulong expectedRevision)
        : base("The plan was changed by another request.")
    {
        PlanId = planId;
        ExpectedRevision = expectedRevision;
    }

    public Guid PlanId { get; }

    public ulong ExpectedRevision { get; }
}
