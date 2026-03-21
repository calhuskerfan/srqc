namespace Srqc
{
    public class ClaimCheck : IClaimCheck
    {
        public Guid Ticket { get; } = Guid.NewGuid();
        public DateTime Issued { get;} = DateTime.UtcNow;
    }
}
