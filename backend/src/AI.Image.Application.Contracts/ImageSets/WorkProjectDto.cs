using System;
using Volo.Abp.Application.Dtos;

namespace AI.Image.ImageSets;

public class WorkProjectDto : AuditedEntityDto<Guid>
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Template { get; set; }
    public string? CoverPath { get; set; }
    public int ImageCount { get; set; }
}
