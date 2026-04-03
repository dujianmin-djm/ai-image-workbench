using Localization.Resources.AbpUi;
using AI.Image.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Localization;

namespace AI.Image;

 [DependsOn(typeof(ApplicationContractsModule))]
public class HttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources.Get<ImageResource>().AddBaseTypes(typeof(AbpUiResource));
        });
    }
}
