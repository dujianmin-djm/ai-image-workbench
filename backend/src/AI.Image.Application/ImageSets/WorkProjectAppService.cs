using AI.Image.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace AI.Image.ImageSets;

[Route("dapi/project")]
public class WorkProjectAppService : AppService, IWorkProjectAppService
{
    private readonly IRepository<WorkProject, Guid> _projectRepo;
    private readonly IRepository<ImageItem, Guid> _imageRepo;

    public WorkProjectAppService(
        IRepository<WorkProject, Guid> projectRepo,
        IRepository<ImageItem, Guid> imageRepo)
    {
        _projectRepo = projectRepo;
        _imageRepo = imageRepo;
    }

    [HttpGet("{id}")]
    public async Task<WorkProjectDto> GetAsync(Guid id)
    {
        var project = await _projectRepo.GetAsync(id);
        return ObjectMapper.Map<WorkProject, WorkProjectDto>(project);
    }

    [HttpGet("query")]
    public async Task<PagedResponseDto<WorkProjectDto>> GetListAsync(PagedRequestDto input)
    {
        var queryable = await _projectRepo.GetQueryableAsync();
        var total = await AsyncExecuter.CountAsync(queryable);
        var list = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(input.Sorts.IsNullOrWhiteSpace() ? "CreationTime desc" : input.Sorts)
                .PageBy((input.Current - 1) * input.Size, input.Size));

        return new PagedResponseDto<WorkProjectDto>(
            total,
            ObjectMapper.Map<List<WorkProject>, List<WorkProjectDto>>(list),
            input.Current, input.Size, input.Sorts);
    }

    [HttpPost("add")]
    public async Task<WorkProjectDto> CreateAsync(CreateUpdateWorkProjectDto input)
    {
        var project = ObjectMapper.Map<CreateUpdateWorkProjectDto, WorkProject>(input);
        await _projectRepo.InsertAsync(project);
        return ObjectMapper.Map<WorkProject, WorkProjectDto>(project);
    }

    [HttpPut("edit/{id}")]
    public async Task<WorkProjectDto> UpdateAsync(Guid id, CreateUpdateWorkProjectDto input)
    {
        var project = await _projectRepo.GetAsync(id);
        ObjectMapper.Map(input, project);
        await _projectRepo.UpdateAsync(project);
        return ObjectMapper.Map<WorkProject, WorkProjectDto>(project);
    }

    [HttpDelete("delete/{id}")]
    public async Task DeleteAsync(Guid id)
    {
        // 级联删除图片（物理文件由 ImageItemAppService 负责）
        var images = await _imageRepo.GetListAsync(x => x.ProjectId == id);
        foreach (var img in images)
        {
            await _imageRepo.DeleteAsync(img);
        }
        await _projectRepo.DeleteAsync(id);
    }
}
