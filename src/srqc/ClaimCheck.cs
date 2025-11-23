namespace Srqc
{
    public class ClaimCheck : IClaimCheck
    {
        public Guid Ticket { get; set; }
        public DateTime Issued { get;} = DateTime.UtcNow;
    }
}
