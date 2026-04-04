using AI.Image.Services.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AI.Image.ImageSets;

public interface IImageItemAppService : IApplicationService
{
    Task<ImageItemDto> GetAsync(Guid id);
    Task<PagedResponseDto<ImageItemDto>> GetListAsync(ImageItemQueryDto input);
    Task<ImageItemDto> UpdateAsync(Guid id, UpdateImageItemDto input);
    Task DeleteAsync(Guid id);
    Task DeleteByProjectAsync(Guid projectId);
}
