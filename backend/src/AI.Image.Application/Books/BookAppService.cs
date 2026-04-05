using AI.Image.ApiResponse;
using AI.Image.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace AI.Image.Books;

[Route("dapi/book")]
[RemoteService(IsMetadataEnabled = false)]
public class BookAppService : AppService, IBookAppService
{
    private readonly IRepository<Book, Guid> _repository;

    public BookAppService(IRepository<Book, Guid> repository)
    {
        _repository = repository;
    }

	[HttpGet("{id}")]
	public async Task<BookDto> GetAsync(Guid id)
    {
        var book = await _repository.GetAsync(id);
        return ObjectMapper.Map<Book, BookDto>(book);
    }

	[HttpGet("query")]
	public async Task<PagedResponseDto<BookDto>> GetListAsync(PagedRequestDto input)
    {
        var queryable = await _repository.GetQueryableAsync();
			//.WhereIf(!input.Filter.IsNullOrWhiteSpace(), book => book.Name.ToLower().Contains(input.Filter!.ToLower()));
        var query = queryable
			.OrderBy(input.Sorts.IsNullOrWhiteSpace() ? "CreationTime desc" : input.Sorts)
			.PageBy((input.Current - 1) * input.Size, input.Size);

        var books = await AsyncExecuter.ToListAsync(query);
		var totalCount = await AsyncExecuter.CountAsync(queryable);

        //throw new SysBusinessException("502", L["Auth:TokenRevokeSuccess"].Value);

		return new PagedResponseDto<BookDto>(
			totalCount,
			ObjectMapper.Map<List<Book>, List<BookDto>>(books),
			input.Current,
			input.Size,
			input.Sorts
		);
    }

	[HttpPost("add")]
	public async Task<BookDto> CreateAsync(CreateUpdateBookDto input)
    {
        var book = ObjectMapper.Map<CreateUpdateBookDto, Book>(input);
        await _repository.InsertAsync(book);
        return ObjectMapper.Map<Book, BookDto>(book);
    }

	[HttpPut("edit/{id}")]
	public async Task<BookDto> UpdateAsync(Guid id, CreateUpdateBookDto input)
    {
        var book = await _repository.GetAsync(id);
        ObjectMapper.Map(input, book);
        await _repository.UpdateAsync(book);
        return ObjectMapper.Map<Book, BookDto>(book);
    }

	[HttpDelete("delete/{id}")]
	public async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}
