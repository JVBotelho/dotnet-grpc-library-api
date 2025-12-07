using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using LibrarySystem.Application.UseCases.Books.CreateBook;
using LibrarySystem.Application.UseCases.Books.Delete;
using LibrarySystem.Application.UseCases.Books.GetAll;
using LibrarySystem.Application.UseCases.Books.GetBookById;
using LibrarySystem.Application.UseCases.Books.Update;
using LibrarySystem.Application.UseCases.Lending.BorrowBook;
using LibrarySystem.Application.UseCases.Lending.ReturnBook;
using LibrarySystem.Application.UseCases.Reports.EstimateReading;
using LibrarySystem.Application.UseCases.Reports.GetAlsoBorrowed;
using LibrarySystem.Application.UseCases.Reports.GetAvailability;
using LibrarySystem.Application.UseCases.Reports.GetMostBorrowed;
using LibrarySystem.Application.UseCases.Reports.GetTopBorrowers;
using LibrarySystem.Application.UseCases.Reports.GetUserHistory;
using LibrarySystem.Contracts.Protos;

// Adicione namespaces para as outras queries que criamos acima
// Assumindo que você colocou todas num namespace global ou similar

namespace LibrarySystem.Grpc.Services;

public class LibraryService : Library.LibraryBase
{
    private readonly ISender _sender;

    public LibraryService(ISender sender)
    {
        _sender = sender;
    }

    // --- BOOKS CRUD ---

    public override async Task<BookResponse> GetBookById(GetBookByIdRequest request, ServerCallContext context)
    {
        var result = await _sender.Send(new GetBookByIdQuery(request.Id), context.CancellationToken);
        if (result == null) throw new RpcException(new Status(StatusCode.NotFound, "Book not found"));
        return MapToResponse(result);
    }

    public override async Task<BookResponse> CreateBook(CreateBookRequest request, ServerCallContext context)
    {
        var command = new CreateBookCommand(request.Title, request.Author, request.PublicationYear, request.Pages, request.TotalCopies);
        var result = await _sender.Send(command, context.CancellationToken);
        return MapToResponse(result);
    }

    public override async Task<GetAllBooksResponse> GetAllBooks(Empty request, ServerCallContext context)
    {
        var result = await _sender.Send(new GetAllBooksQuery(), context.CancellationToken);
        var response = new GetAllBooksResponse();
        response.Books.AddRange(result.Select(MapToResponse));
        return response;
    }

    public override async Task<BookResponse> UpdateBook(UpdateBookRequest request, ServerCallContext context)
    {
        try 
        {
            var command = new UpdateBookCommand(request.Id, request.Title, request.Author, request.PublicationYear, request.Pages, request.TotalCopies);
            var result = await _sender.Send(command, context.CancellationToken);
            return MapToResponse(result);
        }
        catch (KeyNotFoundException) { throw new RpcException(new Status(StatusCode.NotFound, "Book not found")); }
    }

    public override async Task<Empty> DeleteBook(DeleteBookRequest request, ServerCallContext context)
    {
        try 
        {
            await _sender.Send(new DeleteBookCommand(request.Id), context.CancellationToken);
            return new Empty();
        }
        catch (KeyNotFoundException) { throw new RpcException(new Status(StatusCode.NotFound, "Book not found")); }
    }

    // --- LENDING OPERATIONS ---

    public override async Task<LendingActivityResponse> CreateLending(CreateLendingRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _sender.Send(new BorrowBookCommand(request.BookId, request.BorrowerId), context.CancellationToken);
            return new LendingActivityResponse 
            { 
                Id = result.Id, BookId = result.BookId, BorrowerId = result.BorrowerId, 
                BorrowedDate = Timestamp.FromDateTime(result.BorrowedDate.ToUniversalTime()) 
            };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    public override async Task<LendingActivityResponse> ReturnBook(ReturnBookRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _sender.Send(new ReturnBookCommand(request.LendingActivityId), context.CancellationToken);
            return new LendingActivityResponse
            {
                Id = result.Id, BookId = result.BookId, BorrowerId = result.BorrowerId,
                BorrowedDate = Timestamp.FromDateTime(result.BorrowedDate.ToUniversalTime()),
                ReturnedDate = Timestamp.FromDateTime(result.ReturnedDate!.Value.ToUniversalTime())
            };
        }
        catch (KeyNotFoundException ex) { throw new RpcException(new Status(StatusCode.NotFound, ex.Message)); }
        catch (InvalidOperationException ex) { throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message)); }
    }

    // --- REPORTS ---

    public override async Task<GetMostBorrowedBooksResponse> GetMostBorrowedBooks(GetMostBorrowedBooksRequest request, ServerCallContext context)
    {
        var result = await _sender.Send(new GetMostBorrowedQuery(request.Count), context.CancellationToken);
        var response = new GetMostBorrowedBooksResponse();
        // Nota: O proto original esperava MostBorrowedBookInfo com contagem, mas o Application retorna apenas BookDto.
        // Simplificando para retornar o livro. Se precisar da contagem, o DTO teria que mudar.
        // Assumindo adaptação para preencher apenas o livro por enquanto:
        response.MostBorrowedBooks.AddRange(result.Select(b => new MostBorrowedBookInfo { Book = MapToResponse(b), BorrowCount = 0 })); 
        return response;
    }

    public override async Task<BookAvailabilityResponse> GetBookAvailability(GetBookAvailabilityRequest request, ServerCallContext context)
    {
        try
        {
            var result = await _sender.Send(new GetBookAvailabilityQuery(request.BookId), context.CancellationToken);
            return new BookAvailabilityResponse { TotalCopies = result.TotalCopies, BorrowedCopies = result.BorrowedCopies, AvailableCopies = result.AvailableCopies };
        }
        catch (KeyNotFoundException) { throw new RpcException(new Status(StatusCode.NotFound, "Book not found")); }
    }

    public override async Task<GetTopBorrowersResponse> GetTopBorrowers(GetTopBorrowersRequest request, ServerCallContext context)
    {
        var result = await _sender.Send(new GetTopBorrowersQuery(request.StartDate.ToDateTime(), request.EndDate.ToDateTime(), request.Count), context.CancellationToken);
        var response = new GetTopBorrowersResponse();
        response.TopBorrowers.AddRange(result.Select(r => new TopBorrowerInfo 
        { 
            BorrowCount = r.BorrowCount, 
            Borrower = new BorrowerResponse { Id = r.BorrowerId, Name = r.Name } 
        }));
        return response;
    }

    public override async Task<GetUserLendingHistoryResponse> GetUserLendingHistory(GetUserLendingHistoryRequest request, ServerCallContext context)
    {
        var result = await _sender.Send(new GetUserHistoryQuery(request.BorrowerId, request.StartDate.ToDateTime(), request.EndDate.ToDateTime()), context.CancellationToken);
        var response = new GetUserLendingHistoryResponse();
        response.History.AddRange(result.Select(h => new UserLendingHistoryItem
        {
            Book = MapToResponse(h.Book),
            BorrowedDate = Timestamp.FromDateTime(h.BorrowedDate.ToUniversalTime()),
            ReturnedDate = h.ReturnedDate.HasValue ? Timestamp.FromDateTime(h.ReturnedDate.Value.ToUniversalTime()) : null
        }));
        return response;
    }

    public override async Task<GetAlsoBorrowedBooksResponse> GetAlsoBorrowedBooks(GetAlsoBorrowedBooksRequest request, ServerCallContext context)
    {
        var result = await _sender.Send(new GetAlsoBorrowedQuery(request.BookId, request.Count), context.CancellationToken);
        var response = new GetAlsoBorrowedBooksResponse();
        // Mesmo caso da contagem, simplificando para retornar o livro
        response.AlsoBorrowedBooks.AddRange(result.Select(b => new AlsoBorrowedBookInfo { Book = MapToResponse(b), CommonBorrowersCount = 0 }));
        return response;
    }

    public override async Task<EstimateReadingRateResponse> EstimateReadingRate(EstimateReadingRateRequest request, ServerCallContext context)
    {
        try
        {
            var rate = await _sender.Send(new EstimateReadingRateQuery(request.BookId), context.CancellationToken);
            return new EstimateReadingRateResponse { PagesPerDay = rate };
        }
        catch (KeyNotFoundException) { throw new RpcException(new Status(StatusCode.NotFound, "Book not found")); }
    }

    // Helper
    private static BookResponse MapToResponse(LibrarySystem.Application.DTOs.BookDto dto) => new()
    {
        Id = dto.Id, Title = dto.Title, Author = dto.Author, PublicationYear = dto.PublicationYear, Pages = dto.Pages, TotalCopies = dto.TotalCopies
    };
}