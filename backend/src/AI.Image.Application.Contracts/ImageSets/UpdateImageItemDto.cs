using AI.Image.Enums;
using System.ComponentModel.DataAnnotations;

namespace AI.Image.ImageSets;

/// <summary>
/// 更新图片元数据（评分 / 状态 / 标签 / 备注）
/// </summary>
public class UpdateImageItemDto
{
    [Range(0, 5)]
    public int Rating { get; set; }

    public ReviewStatus Status { get; set; }

    [MaxLength(1024)]
    public string? Notes { get; set; }

    /// <summary>JSON 数组，如 ["人像","自然"]</summary>
    public string? TagsJson { get; set; }
}
