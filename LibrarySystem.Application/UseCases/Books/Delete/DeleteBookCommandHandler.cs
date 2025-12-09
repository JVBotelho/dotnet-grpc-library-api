using LibrarySystem.Application.Abstractions.Repositories;
using MediatR;

namespace LibrarySystem.Application.UseCases.Books.Delete;

public class DeleteBookCommandHandler : IRequestHandler<DeleteBookCommand, Unit>
{
    private readonly IBookRepository _bookRepository;

    public DeleteBookCommandHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<Unit> Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.Id, cancellationToken);

        if (book is null)
            throw new KeyNotFoundException($"Book with ID {request.Id} not found.");

        _bookRepository.Remove(book);
        await _bookRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}