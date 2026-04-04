using AI.Image.Enums;
using AI.Image.Services.Dtos;
using System;

namespace AI.Image.ImageSets;

public class ImageItemQueryDto : PagedRequestDto
{
    public Guid ProjectId { get; set; }
    public ReviewStatus? Status { get; set; }
    public int? MinRating { get; set; }
    public string? Tag { get; set; }
}
