using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;

namespace AI.Image;

[DependsOn(
    typeof(DomainModule),
    typeof(ApplicationContractsModule),
	typeof(AbpMapperlyModule)
	)]
public class ApplicationModule : AbpModule
{

}
