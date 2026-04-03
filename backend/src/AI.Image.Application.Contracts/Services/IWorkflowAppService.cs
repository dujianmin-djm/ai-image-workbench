using System.Threading.Tasks;

namespace AI.Image.Services;

public interface IWorkflowAppService<Guid>
{
	Task SubmitAsync(Guid id);
	Task CancelAsync(Guid id);
	Task AuditAsync(Guid id);
	Task UnAuditAsync(Guid id);
}
