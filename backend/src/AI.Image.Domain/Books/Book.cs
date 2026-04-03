using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AI.Image.Books;

public class Book : AuditedAggregateRoot<Guid>
{
    public required string Name { get; set; }

    public int Type { get; set; }

    public DateTime PublishDate { get; set; }

    public float Price { get; set; }
}
