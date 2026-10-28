namespace WordWise.Domain.Entities
{
    /// <summary>Records one successful authentication, used as a development-safe session activity signal.</summary>
    public class UserLoginEvent : BaseEntity
    {
        public Guid UserId { get; set; }
        public DateTime LoggedInAt { get; set; } = DateTime.UtcNow;
    }
}
