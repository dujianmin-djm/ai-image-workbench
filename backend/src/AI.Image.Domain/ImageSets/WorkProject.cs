using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AI.Image.ImageSets;

/// <summary>
/// 图片项目（工作集）
/// </summary>
public class WorkProject : AuditedAggregateRoot<Guid>
{
    /// <summary>项目名称</summary>
    public required string Name { get; set; }

    /// <summary>项目描述</summary>
    public string? Description { get; set; }

	/// <summary>
    /// 项目模板：ai-gen / design / ecommerce / other / heart-valve
    /// </summary>
	public string? Template { get; set; }

    /// <summary>封面图片相对路径</summary>
    public string? CoverPath { get; set; }

    /// <summary>图片数量（缓存）</summary>
    public int ImageCount { get; set; }
}
