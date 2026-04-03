using AI.Image.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AI.Image.Controllers;

public abstract class ControllerBage : AbpControllerBase
{
    protected ControllerBage()
    {
        LocalizationResource = typeof(ImageResource);
    }
}
