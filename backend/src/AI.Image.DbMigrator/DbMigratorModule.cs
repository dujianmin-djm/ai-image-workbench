using AI.Image.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AI.Image.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(EntityFrameworkCoreModule),
    typeof(ApplicationContractsModule)
)]
public class DbMigratorModule : AbpModule
{
}
