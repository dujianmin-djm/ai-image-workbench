using AI.Image.Localization;
using Volo.Abp.Application.Services;

namespace AI.Image;

public abstract class AppService : ApplicationService
{
    protected AppService()
    {
        LocalizationResource = typeof(ImageResource);
    }
}
