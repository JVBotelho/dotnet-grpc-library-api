using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Contracts.Protos;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly Library.LibraryClient _grpcClient;

    public BooksController(Library.LibraryClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    [HttpGet("{id:int}", Name = "GetBookById")]
    public async Task<IActionResult> GetBook(int id)
    {
        try
        {
            var request = new GetBookByIdRequest { Id = id };
            var reply = await _grpcClient.GetBookByIdAsync(request);
            return Ok(reply);
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            return NotFound($"Book not found id: {id}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Erro ao comunicar com o serviço gRPC: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookDto createBookDto)
    {
        var request = new CreateBookRequest
        {
            Title = createBookDto.Title,
            Author = createBookDto.Author,
            PublicationYear = createBookDto.PublicationYear,
            Pages = createBookDto.Pages,
            TotalCopies = createBookDto.TotalCopies
        };

        var newBookResponse = await _grpcClient.CreateBookAsync(request);

        return CreatedAtAction(nameof(GetBook), new { id = newBookResponse.Id }, newBookResponse);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllBooks()
    {
        var response = await _grpcClient.GetAllBooksAsync(new Empty());
        return Ok(response.Books);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDto updateBookDto)
    {
        var request = new UpdateBookRequest
        {
            Id = id,
            Title = updateBookDto.Title,
            Author = updateBookDto.Author,
            PublicationYear = updateBookDto.PublicationYear,
            Pages = updateBookDto.Pages,
            TotalCopies = updateBookDto.TotalCopies
        };

        try
        {
            var updatedBook = await _grpcClient.UpdateBookAsync(request);
            return Ok(updatedBook);
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(ex.Status.Detail);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        try
        {
            await _grpcClient.DeleteBookAsync(new DeleteBookRequest { Id = id });
            return NoContent();
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(ex.Status.Detail);
        }
    }

    [HttpGet("most-borrowed")]
    public async Task<IActionResult> GetMostBorrowed([FromQuery] int count = 5)
    {
        if (count <= 0) return BadRequest("Count must be a positive number.");

        var request = new GetMostBorrowedBooksRequest { Count = count };
        var response = await _grpcClient.GetMostBorrowedBooksAsync(request);
        return Ok(response.MostBorrowedBooks);
    }

    [HttpGet("{id:int}/availability")]
    public async Task<IActionResult> GetBookAvailability(int id)
    {
        try
        {
            var request = new GetBookAvailabilityRequest { BookId = id };
            var response = await _grpcClient.GetBookAvailabilityAsync(request);
            return Ok(response);
        }
        catch (RpcException ex)
        {
            return NotFound(ex.Status.Detail);
        }
    }

    [HttpGet("{id:int}/also-borrowed")]
    public async Task<IActionResult> GetAlsoBorrowed(int id, [FromQuery] int count = 5)
    {
        if (count <= 0) return BadRequest("Count must be a positive number.");

        try
        {
            var request = new GetAlsoBorrowedBooksRequest { BookId = id, Count = count };
            var response = await _grpcClient.GetAlsoBorrowedBooksAsync(request);
            return Ok(response.AlsoBorrowedBooks);
        }
        catch (RpcException ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Status.Detail}");
        }
    }

    [HttpGet("{id:int}/reading-rate")]
    public async Task<IActionResult> GetReadingRate(int id)
    {
        try
        {
            var request = new EstimateReadingRateRequest { BookId = id };
            var response = await _grpcClient.EstimateReadingRateAsync(request);
            return Ok(response);
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            return NotFound(ex.Status.Detail);
        }
    }
}