using AI.Image.Books;
using AI.Image.ImageSets;
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
	public DbSet<WorkProject> WorkProjects { get; set; }
	public DbSet<ImageItem> ImageItems { get; set; }

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

		builder.Entity<WorkProject>(b =>
		{
			b.ToTable(DomainConsts.DbTablePrefix.Business + "WorkProjects");
			b.ConfigureByConvention();
			b.Property(x => x.Name).IsRequired().HasMaxLength(128);
			b.Property(x => x.Description).HasMaxLength(512);
			b.Property(x => x.Template).HasMaxLength(32);
			b.Property(x => x.CoverPath).HasMaxLength(512);
		});

		builder.Entity<ImageItem>(b =>
		{
			b.ToTable(DomainConsts.DbTablePrefix.Business + "ImageItems");
			b.ConfigureByConvention();
			b.Property(x => x.FileName).IsRequired().HasMaxLength(256);
			b.Property(x => x.FilePath).IsRequired().HasMaxLength(512);
			b.Property(x => x.ThumbnailPath).HasMaxLength(512);
			b.Property(x => x.MimeType).HasMaxLength(64);
			b.Property(x => x.Notes).HasMaxLength(1024);
			b.HasIndex(x => x.ProjectId);
		});
	}
}
