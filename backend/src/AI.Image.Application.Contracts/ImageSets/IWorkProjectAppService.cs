using AI.Image.Services.Dtos;
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace AI.Image.ImageSets;

public interface IWorkProjectAppService : IApplicationService
{
    Task<WorkProjectDto> GetAsync(Guid id);
    Task<PagedResponseDto<WorkProjectDto>> GetListAsync(PagedRequestDto input);
    Task<WorkProjectDto> CreateAsync(CreateUpdateWorkProjectDto input);
    Task<WorkProjectDto> UpdateAsync(Guid id, CreateUpdateWorkProjectDto input);
    Task DeleteAsync(Guid id);
}
