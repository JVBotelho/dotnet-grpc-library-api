using LibrarySystem.Application.Abstractions.Repositories;
using LibrarySystem.Application.DTOs;
using LibrarySystem.Domain.Entities;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.Update;

public class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, BookDto>
{
    private readonly IBookRepository _bookRepository;

    public UpdateBookCommandHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookDto> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);

        if (book is null)
            throw new KeyNotFoundException($"Book with ID {request.Id} not found.");

        SetProperty(book, nameof(Book.Title), request.Title);
        SetProperty(book, nameof(Book.Author), request.Author);
        SetProperty(book, nameof(Book.PublicationYear), request.PublicationYear);
        SetProperty(book, nameof(Book.Pages), request.Pages);
        SetProperty(book, nameof(Book.TotalCopies), request.TotalCopies);

        _bookRepository.Update(book);
        await _bookRepository.SaveChangesAsync(cancellationToken);

        return new BookDto(
            book.Id, book.Title, book.Author, book.PublicationYear,
            book.Pages, book.TotalCopies,
            book.TotalCopies - book.LendingActivities.Count(la => la.ReturnedDate == null)
        );
    }

    private void SetProperty(object target, string propName, object value)
    {
        var prop = target.GetType().GetProperty(propName);
        prop?.SetValue(target, value);
    }
}