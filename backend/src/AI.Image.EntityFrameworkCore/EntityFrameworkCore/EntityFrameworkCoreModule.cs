using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;

namespace AI.Image.EntityFrameworkCore;

[DependsOn(
    typeof(DomainModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule),
	typeof(AbpEntityFrameworkCoreSqliteModule)
	)]
public class EntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<ImageDbContext>(options =>
        {
            /* Remove "includeAllEntities: true" to create
                * default repositories only for aggregate roots */
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        Configure<AbpDbContextOptions>(options =>
        {
			/* The main point to change your DBMS.
             * See also DbContextFactory for EF Core tooling. */

			//options.UseSqlServer();
			options.UseSqlite();
		});

		// 使用Sqlite时，禁用工作单元事务机制
		context.Services.AddAlwaysDisableUnitOfWorkTransaction();
        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });
    }
}
