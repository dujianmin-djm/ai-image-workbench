using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using AI.Image.Services.Dtos;

namespace AI.Image.Services;

public interface IReadOnlyAppService<TGetOutputDto, TGetListOutputDto, in TKey, in TGetListInput> : IApplicationService, IRemoteService
{
	Task<TGetOutputDto> GetAsync(TKey id);
	Task<PagedResponseDto<TGetListOutputDto>> GetListAsync(TGetListInput input);
}
