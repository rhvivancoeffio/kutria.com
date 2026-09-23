namespace Commerce.Domain.Common;

public interface ITenantScoped
{
    string TenantId { get; set; }
}
