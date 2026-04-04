using AI.Image.Enums;
using System;
using Volo.Abp.Application.Dtos;

namespace AI.Image.ImageSets;

public class ImageItemDto : AuditedEntityDto<Guid>
{
    public Guid ProjectId { get; set; }
    public required string FileName { get; set; }
    public required string FilePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public long FileSize { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? MimeType { get; set; }
    public int Rating { get; set; }
    public ReviewStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? TagsJson { get; set; }
    public int SortOrder { get; set; }
}
