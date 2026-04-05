using AI.Image.Books;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace AI.Image.HealthChecks;

public class DatabaseCheck : IHealthCheck, ITransientDependency
{
	private readonly IRepository<Book, Guid> _repository;

	public DatabaseCheck(IRepository<Book, Guid> repository)
    {
		_repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _repository.GetCountAsync(cancellationToken);

            return HealthCheckResult.Healthy($"Could connect to database and get record.");
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy($"Error when trying to get database record. ", e);
        }
    }
}
