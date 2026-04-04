using AI.Image.Enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AI.Image.ImageSets;

/// <summary>
/// 图片条目
/// </summary>
public class ImageItem : AuditedAggregateRoot<Guid>
{
    /// <summary>所属项目 ID</summary>
    public Guid ProjectId { get; set; }

    /// <summary>原始文件名</summary>
    public required string FileName { get; set; }

    /// <summary>存储相对路径（uploads/...）</summary>
    public required string FilePath { get; set; }

    /// <summary>缩略图相对路径</summary>
    public string? ThumbnailPath { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long FileSize { get; set; }

    /// <summary>图片宽度（像素）</summary>
    public int Width { get; set; }

    /// <summary>图片高度（像素）</summary>
    public int Height { get; set; }

    /// <summary>MIME 类型</summary>
    public string? MimeType { get; set; }

    /// <summary>星级评分 0-5</summary>
    public int Rating { get; set; }

    /// <summary>评审状态</summary>
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    /// <summary>备注 / 评审意见</summary>
    public string? Notes { get; set; }

    /// <summary>标签（JSON 数组字符串）</summary>
    public string? TagsJson { get; set; }

    /// <summary>显示排序</summary>
    public int SortOrder { get; set; }
}
