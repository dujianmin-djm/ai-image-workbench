using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace AI.Image.Books;

[Mapper]
public partial class BookAndBookDtoMapper : TwoWayMapperBase<Book, BookDto>
{
	public override partial BookDto Map(Book source);
	public override partial void Map(Book source, BookDto destination);
	public override partial Book ReverseMap(BookDto source);
	public override partial void ReverseMap(BookDto source, Book destination);
}


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class CreateUpdateBookMapper : MapperBase<CreateUpdateBookDto, Book>
{
	public override partial Book Map(CreateUpdateBookDto source);
	public override partial void Map(CreateUpdateBookDto source, Book destination);
}
