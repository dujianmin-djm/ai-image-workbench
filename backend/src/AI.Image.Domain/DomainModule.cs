using Volo.Abp.Modularity;
using Volo.Abp.Caching;

namespace AI.Image;

[DependsOn(
    typeof(DomainSharedModule),
    typeof(AbpCachingModule)
	)]
public class DomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {

    }
}
