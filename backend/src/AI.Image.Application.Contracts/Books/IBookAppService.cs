using AI.Image.Services;
using AI.Image.Services.Dtos;
using System;

namespace AI.Image.Books;

public interface IBookAppService : ICrudAppService<BookDto, Guid, PagedRequestDto, CreateUpdateBookDto> 
{

}