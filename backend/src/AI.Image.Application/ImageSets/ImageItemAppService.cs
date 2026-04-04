using AI.Image.ApiResponse;
using AI.Image.Services.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace AI.Image.ImageSets;

[Route("dapi/image")]
public class ImageItemAppService : AppService, IImageItemAppService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".tiff" };

    private readonly IRepository<ImageItem, Guid> _imageRepo;
    private readonly IRepository<WorkProject, Guid> _projectRepo;
    private readonly IHostEnvironment _env;

    public ImageItemAppService(
        IRepository<ImageItem, Guid> imageRepo,
        IRepository<WorkProject, Guid> projectRepo,
        IHostEnvironment env)
    {
        _imageRepo = imageRepo;
        _projectRepo = projectRepo;
        _env = env;
    }

    [HttpGet("{id}")]
    public async Task<ImageItemDto> GetAsync(Guid id)
    {
        var item = await _imageRepo.GetAsync(id);
        return ObjectMapper.Map<ImageItem, ImageItemDto>(item);
    }

    [HttpGet("query")]
    public async Task<PagedResponseDto<ImageItemDto>> GetListAsync([FromQuery] ImageItemQueryDto input)
    {
        var queryable = (await _imageRepo.GetQueryableAsync())
            .Where(x => x.ProjectId == input.ProjectId);

        if (input.Status.HasValue)
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        if (input.MinRating.HasValue)
            queryable = queryable.Where(x => x.Rating >= input.MinRating.Value);
        if (!string.IsNullOrWhiteSpace(input.Tag))
            queryable = queryable.Where(x => x.TagsJson != null && x.TagsJson.Contains(input.Tag));

        var total = await AsyncExecuter.CountAsync(queryable);
        var list = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(input.Sorts.IsNullOrWhiteSpace() ? "SortOrder asc, CreationTime asc" : input.Sorts)
                .PageBy((input.Current - 1) * input.Size, input.Size));

        return new PagedResponseDto<ImageItemDto>(
            total,
            ObjectMapper.Map<List<ImageItem>, List<ImageItemDto>>(list),
            input.Current, input.Size, input.Sorts);
    }

    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<List<ImageItemDto>> UploadAsync([FromQuery] Guid projectId, IFormFileCollection files)
    {
        var project = await _projectRepo.GetAsync(projectId);

        var uploadsRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
        var thumbsRoot = Path.Combine(_env.ContentRootPath, "wwwroot", "thumbs");
        Directory.CreateDirectory(uploadsRoot);
        Directory.CreateDirectory(thumbsRoot);

        var result = new List<ImageItemDto>();
        var sortBase = await AsyncExecuter.CountAsync(
            (await _imageRepo.GetQueryableAsync()).Where(x => x.ProjectId == projectId));

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                throw new SysBusinessException(ApiResponseCode.BadRequest, $"不支持的文件类型: {ext}");

            var fileId = Guid.NewGuid().ToString("N");
            var relativeDir = Path.Combine(projectId.ToString("N"));
            var uploadsDir = Path.Combine(uploadsRoot, relativeDir);
            var thumbsDir = Path.Combine(thumbsRoot, relativeDir);
            Directory.CreateDirectory(uploadsDir);
            Directory.CreateDirectory(thumbsDir);

            var storedName = fileId + ext;
            var fullPath = Path.Combine(uploadsDir, storedName);
            var thumbPath = Path.Combine(thumbsDir, fileId + ".jpg");

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            int width = 0, height = 0;
            try
            {
                using var img = await SixLabors.ImageSharp.Image.LoadAsync(fullPath);
                width = img.Width;
                height = img.Height;

                // 生成 320px 宽缩略图
                img.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(320, 0),
                    Mode = ResizeMode.Max
                }));
                await img.SaveAsJpegAsync(thumbPath);
            }
            catch
            {
                // 无法解析则跳过缩略图
            }

            var entity = new ImageItem
            {
                ProjectId = projectId,
                FileName = file.FileName,
                FilePath = $"uploads/{relativeDir}/{storedName}",
                ThumbnailPath = File.Exists(thumbPath) ? $"thumbs/{relativeDir}/{fileId}.jpg" : null,
                FileSize = file.Length,
                Width = width,
                Height = height,
                MimeType = file.ContentType,
                SortOrder = sortBase++
            };

            await _imageRepo.InsertAsync(entity);
            result.Add(ObjectMapper.Map<ImageItem, ImageItemDto>(entity));
        }
		if (CurrentUnitOfWork != null)
		{
			await CurrentUnitOfWork.SaveChangesAsync();
		}

		// 更新项目封面与计数
		project.ImageCount = await AsyncExecuter.CountAsync(
            (await _imageRepo.GetQueryableAsync()).Where(x => x.ProjectId == projectId));
        if (project.CoverPath == null && result.Count > 0)
            project.CoverPath = result[0].ThumbnailPath ?? result[0].FilePath;
        await _projectRepo.UpdateAsync(project);

        return result;
    }

    [HttpPut("edit/{id}")]
    public async Task<ImageItemDto> UpdateAsync(Guid id, UpdateImageItemDto input)
    {
        var item = await _imageRepo.GetAsync(id);
        item.Rating = input.Rating;
        item.Status = input.Status;
        item.Notes = input.Notes;
        item.TagsJson = input.TagsJson;
        await _imageRepo.UpdateAsync(item);
        return ObjectMapper.Map<ImageItem, ImageItemDto>(item);
    }

    [HttpDelete("delete/{id}")]
    public async Task DeleteAsync(Guid id)
    {
        var item = await _imageRepo.GetAsync(id);
        DeletePhysicalFiles(item);
        await _imageRepo.DeleteAsync(item);
		if (CurrentUnitOfWork != null)
		{
			await CurrentUnitOfWork.SaveChangesAsync();
		}

		// 更新项目封面与计数
		var project = await _projectRepo.FindAsync(item.ProjectId);
        if (project != null)
        {
            project.ImageCount = Math.Max(0, project.ImageCount - 1);
            if (project.CoverPath?.Equals(item.ThumbnailPath) == true)
            {
				var imageItem = (await _imageRepo.GetQueryableAsync())
			        .Where(x => x.ProjectId == project.Id).FirstOrDefault();
                project.CoverPath = imageItem?.ThumbnailPath;
			}
			await _projectRepo.UpdateAsync(project);
        }
    }

    [HttpDelete("deleteByProject/{projectId}")]
    public async Task DeleteByProjectAsync(Guid projectId)
    {
        var items = await _imageRepo.GetListAsync(x => x.ProjectId == projectId);
        foreach (var item in items)
        {
            DeletePhysicalFiles(item);
            await _imageRepo.DeleteAsync(item);
        }
    }

    private void DeletePhysicalFiles(ImageItem item)
    {
        TryDelete(Path.Combine(_env.ContentRootPath, "wwwroot", item.FilePath.Replace('/', Path.DirectorySeparatorChar)));
        if (item.ThumbnailPath != null)
            TryDelete(Path.Combine(_env.ContentRootPath, "wwwroot", item.ThumbnailPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }
}
