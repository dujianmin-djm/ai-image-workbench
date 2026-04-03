using AI.Image.Books;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace AI.Image.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class ImageDbContext : AbpDbContext<ImageDbContext>
{
	/* Add DbSet properties for your Aggregate Roots / Entities here. */

	public DbSet<Book> Books { get; set; }

    public ImageDbContext(DbContextOptions<ImageDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

		/* Configure your own tables/entities inside here */

		builder.Entity<Book>(b =>
		{
			b.ToTable(DomainConsts.DbTablePrefix.BaseData + "Books");
			b.ConfigureByConvention();
			b.Property(x => x.Name).IsRequired().HasMaxLength(128);
		});
	}
}
