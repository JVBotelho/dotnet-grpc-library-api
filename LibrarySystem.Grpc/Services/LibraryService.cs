using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Persistence;
using LibrarySystem.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using LibrarySystem.Grpc.Protos;

namespace LibrarySystem.Grpc.Services;

public class LibraryService : Library.LibraryBase
{
    private readonly LibraryDbContext _context;

    public LibraryService(LibraryDbContext context)
    {
        _context = context;
    }

    public override async Task<BookResponse> GetBookById(GetBookByIdRequest request, ServerCallContext context)
    {
        var book = await _context.Books.FindAsync(request.Id);

        if (book == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Book with ID {request.Id} not found."));

        return new BookResponse
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            PublicationYear = book.PublicationYear,
            Pages = book.Pages,
            TotalCopies = book.TotalCopies
        };
    }

    public override async Task<BookResponse> CreateBook(CreateBookRequest request, ServerCallContext context)
    {
        var newBook = new Book
        {
            Title = request.Title,
            Author = request.Author,
            PublicationYear = request.PublicationYear,
            Pages = request.Pages,
            TotalCopies = request.TotalCopies
        };

        _context.Books.Add(newBook);
        await _context.SaveChangesAsync();

        return new BookResponse
        {
            Id = newBook.Id,
            Title = newBook.Title,
            Author = newBook.Author,
            PublicationYear = newBook.PublicationYear,
            Pages = newBook.Pages,
            TotalCopies = newBook.TotalCopies
        };
    }

    public override async Task<GetAllBooksResponse> GetAllBooks(Empty request, ServerCallContext context)
    {
        var books = await _context.Books
            .Select(book => new BookResponse
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                PublicationYear = book.PublicationYear,
                Pages = book.Pages,
                TotalCopies = book.TotalCopies
            })
            .ToListAsync();

        var response = new GetAllBooksResponse();
        response.Books.AddRange(books);
        return response;
    }

    public override async Task<BookResponse> UpdateBook(UpdateBookRequest request, ServerCallContext context)
    {
        var bookToUpdate = await _context.Books.FindAsync(request.Id);

        if (bookToUpdate == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Book with ID {request.Id} not found."));

        bookToUpdate.Title = request.Title;
        bookToUpdate.Author = request.Author;
        bookToUpdate.PublicationYear = request.PublicationYear;
        bookToUpdate.Pages = request.Pages;
        bookToUpdate.TotalCopies = request.TotalCopies;

        await _context.SaveChangesAsync();

        return new BookResponse
        {
            Id = bookToUpdate.Id,
            Title = bookToUpdate.Title,
            Author = bookToUpdate.Author,
            PublicationYear = bookToUpdate.PublicationYear,
            Pages = bookToUpdate.Pages,
            TotalCopies = bookToUpdate.TotalCopies
        };
    }

    public override async Task<Empty> DeleteBook(DeleteBookRequest request, ServerCallContext context)
    {
        var bookToDelete = await _context.Books.FindAsync(request.Id);

        if (bookToDelete == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Book with ID {request.Id} not found."));

        _context.Books.Remove(bookToDelete);
        await _context.SaveChangesAsync();

        return new Empty();
    }

    public override async Task<GetMostBorrowedBooksResponse> GetMostBorrowedBooks(GetMostBorrowedBooksRequest request,
        ServerCallContext context)
    {
        if (request.Count <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Count must be a positive number."));

        var mostBorrowed = await _context.LendingActivities
            .GroupBy(la => la.BookId)
            .Select(g => new
            {
                BookId = g.Key,
                BorrowCount = g.Count()
            })
            .OrderByDescending(x => x.BorrowCount)
            .Take(request.Count)
            .Join(
                _context.Books,
                borrowInfo => borrowInfo.BookId,
                book => book.Id,
                (borrowInfo, book) => new MostBorrowedBookInfo
                {
                    BorrowCount = borrowInfo.BorrowCount,
                    Book = new BookResponse
                    {
                        Id = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        PublicationYear = book.PublicationYear,
                        Pages = book.Pages,
                        TotalCopies = book.TotalCopies
                    }
                })
            .ToListAsync();

        var response = new GetMostBorrowedBooksResponse();
        response.MostBorrowedBooks.AddRange(mostBorrowed);
        return response;
    }

    public override async Task<LendingActivityResponse> CreateLending(CreateLendingRequest request,
        ServerCallContext context)
    {
        var book = await _context.Books.FindAsync(request.BookId);
        if (book == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Book with ID {request.BookId} not found."));

        var borrower = await _context.Borrowers.FindAsync(request.BorrowerId);
        if (borrower == null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Borrower with ID {request.BorrowerId} not found."));

        var borrowedCopies = await _context.LendingActivities
            .CountAsync(la => la.BookId == request.BookId && la.ReturnedDate == null);

        if (borrowedCopies >= book.TotalCopies)
            throw new RpcException(new Status(StatusCode.FailedPrecondition,
                $"No available copies for book '{book.Title}'."));

        var newLending = new LendingActivity
        {
            BookId = request.BookId,
            BorrowerId = request.BorrowerId,
            BorrowedDate = DateTime.UtcNow
        };

        _context.LendingActivities.Add(newLending);
        await _context.SaveChangesAsync();

        return new LendingActivityResponse
        {
            Id = newLending.Id,
            BookId = newLending.BookId,
            BorrowerId = newLending.BorrowerId,
            BorrowedDate = Timestamp.FromDateTime(newLending.BorrowedDate)
        };
    }

    public override async Task<BookAvailabilityResponse> GetBookAvailability(GetBookAvailabilityRequest request,
        ServerCallContext context)
    {
        var book = await _context.Books.FindAsync(request.BookId);
        if (book == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Book with ID {request.BookId} not found."));

        var borrowedCopies = await _context.LendingActivities
            .CountAsync(la => la.BookId == request.BookId && la.ReturnedDate == null);

        return new BookAvailabilityResponse
        {
            TotalCopies = book.TotalCopies,
            BorrowedCopies = borrowedCopies,
            AvailableCopies = book.TotalCopies - borrowedCopies
        };
    }

    public override async Task<GetTopBorrowersResponse> GetTopBorrowers(GetTopBorrowersRequest request,
        ServerCallContext context)
    {
        var startDate = request.StartDate.ToDateTime();
        var endDate = request.EndDate.ToDateTime();

        var topBorrowers = await _context.LendingActivities
            .Where(la => la.BorrowedDate >= startDate && la.BorrowedDate <= endDate)
            .GroupBy(la => la.BorrowerId)
            .Select(g => new
            {
                BorrowerId = g.Key,
                BorrowCount = g.Count()
            })
            .OrderByDescending(x => x.BorrowCount)
            .Take(request.Count)
            .Join(
                _context.Borrowers,
                activityInfo => activityInfo.BorrowerId,
                borrower => borrower.Id,
                (activityInfo, borrower) => new TopBorrowerInfo
                {
                    BorrowCount = activityInfo.BorrowCount,
                    Borrower = new BorrowerResponse
                    {
                        Id = borrower.Id,
                        Name = borrower.Name,
                        Email = borrower.Email
                    }
                })
            .ToListAsync(context.CancellationToken);

        var response = new GetTopBorrowersResponse();
        response.TopBorrowers.AddRange(topBorrowers);
        return response;
    }

    public override async Task<GetUserLendingHistoryResponse> GetUserLendingHistory(
        GetUserLendingHistoryRequest request, ServerCallContext context)
    {
        try
        {
            var startDate = request.StartDate.ToDateTime();
            var endDate = request.EndDate.ToDateTime();

            var activities = await _context.LendingActivities
                .Where(la => la.BorrowerId == request.BorrowerId &&
                             la.BorrowedDate >= startDate &&
                             la.BorrowedDate <= endDate)
                .Include(la => la.Book) 
                .OrderByDescending(la => la.BorrowedDate)
                .ToListAsync(context.CancellationToken);

            var historyItems = activities.Select(la => new UserLendingHistoryItem
            {
                BorrowedDate = Timestamp.FromDateTime(la.BorrowedDate),
                ReturnedDate = la.ReturnedDate.HasValue ? Timestamp.FromDateTime(la.ReturnedDate.Value) : null,
                Book = new BookResponse
                {
                    Id = la.Book!.Id,
                    Title = la.Book.Title,
                    Author = la.Book.Author,
                    PublicationYear = la.Book.PublicationYear,
                    Pages = la.Book.Pages,
                    TotalCopies = la.Book.TotalCopies
                }
            }).ToList();

            var response = new GetUserLendingHistoryResponse();
            response.History.AddRange(historyItems);
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public override async Task<GetAlsoBorrowedBooksResponse> GetAlsoBorrowedBooks(GetAlsoBorrowedBooksRequest request,
        ServerCallContext context)
    {
        var borrowersOfMainBook = await _context.LendingActivities
            .Where(la => la.BookId == request.BookId)
            .Select(la => la.BorrowerId)
            .Distinct()
            .ToListAsync(context.CancellationToken);

        if (!borrowersOfMainBook.Any())
            return new GetAlsoBorrowedBooksResponse(); 

        var alsoBorrowedBooks = await _context.LendingActivities
            .Where(la => borrowersOfMainBook.Contains(la.BorrowerId) && 
                         la.BookId != request.BookId) 
            .GroupBy(la => la.Book) 
            .Select(g => new AlsoBorrowedBookInfo
            {
                Book = new BookResponse
                {
                    Id = g.Key!.Id,
                    Title = g.Key.Title,
                    Author = g.Key.Author,
                    PublicationYear = g.Key.PublicationYear,
                    Pages = g.Key.Pages,
                    TotalCopies = g.Key.TotalCopies
                },
                CommonBorrowersCount = g.Select(la => la.BorrowerId).Distinct().Count()
            })
            .OrderByDescending(b => b.CommonBorrowersCount)
            .Take(request.Count)
            .ToListAsync(context.CancellationToken);

        var response = new GetAlsoBorrowedBooksResponse();
        response.AlsoBorrowedBooks.AddRange(alsoBorrowedBooks);
        return response;
    }

    public override async Task<LendingActivityResponse> ReturnBook(ReturnBookRequest request, ServerCallContext context)
    {
        var lendingActivity = await _context.LendingActivities.FindAsync(request.LendingActivityId);
        if (lendingActivity == null)
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Lending activity with ID {request.LendingActivityId} not found."));

        if (lendingActivity.ReturnedDate.HasValue)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "This book has already been returned."));

        lendingActivity.ReturnedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new LendingActivityResponse
        {
            Id = lendingActivity.Id,
            BookId = lendingActivity.BookId,
            BorrowerId = lendingActivity.BorrowerId,
            BorrowedDate = Timestamp.FromDateTime(lendingActivity.BorrowedDate),
            ReturnedDate = Timestamp.FromDateTime(lendingActivity.ReturnedDate.Value)
        };
    }

    public override async Task<EstimateReadingRateResponse> EstimateReadingRate(EstimateReadingRateRequest request,
        ServerCallContext context)
    {
        var book = await _context.Books.FindAsync(request.BookId);
        if (book == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Book with ID {request.BookId} not found."));

        var completedLoans = await _context.LendingActivities
            .Where(la => la.BookId == request.BookId && la.ReturnedDate.HasValue)
            .ToListAsync(context.CancellationToken);

        if (!completedLoans.Any()) return new EstimateReadingRateResponse { PagesPerDay = 0 };

        double totalDaysBorrowed = completedLoans.Sum(la => (la.ReturnedDate!.Value - la.BorrowedDate).TotalDays);

        if (totalDaysBorrowed < 1) 
            totalDaysBorrowed = 1;

        double totalPagesRead = completedLoans.Count * book.Pages;
        var rate = totalPagesRead / totalDaysBorrowed;

        return new EstimateReadingRateResponse { PagesPerDay = rate };
    }
}