using Volo.Abp.Modularity;

namespace AI.Image;

[DependsOn(typeof(DomainSharedModule))]
public class ApplicationContractsModule : AbpModule
{

}
