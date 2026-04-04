using System.ComponentModel.DataAnnotations;

namespace AI.Image.ImageSets;

public class CreateUpdateWorkProjectDto
{
    [Required]
    [MaxLength(128)]
    public required string Name { get; set; }

    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>ai-gen / design / ecommerce / other</summary>
    [MaxLength(32)]
    public string? Template { get; set; }
}
