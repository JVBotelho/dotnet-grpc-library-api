using Grpc.Core;
using LibrarySystem.Api.Dtos;
using LibrarySystem.Contracts.Protos;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LendingController : ControllerBase
{
    private readonly Library.LibraryClient _grpcClient;

    public LendingController(Library.LibraryClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    [HttpPost]
    public async Task<IActionResult> CreateLending([FromBody] CreateLendingDto createLendingDto)
    {
        try
        {
            var request = new CreateLendingRequest
            {
                BookId = createLendingDto.BookId,
                BorrowerId = createLendingDto.BorrowerId
            };
            var response = await _grpcClient.CreateLendingAsync(request);
            return CreatedAtAction(nameof(CreateLending), new { id = response.Id }, response);
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound ||
                                      ex.StatusCode == global::Grpc.Core.StatusCode.FailedPrecondition)
        {
            return BadRequest(ex.Status.Detail);
        }
    }

    [HttpPut("{id:int}/return")]
    public async Task<IActionResult> ReturnBook(int id)
    {
        try
        {
            var request = new ReturnBookRequest { LendingActivityId = id };
            var response = await _grpcClient.ReturnBookAsync(request);
            return Ok(response);
        }
        catch (RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound ||
                                      ex.StatusCode == global::Grpc.Core.StatusCode.FailedPrecondition)
        {
            return BadRequest(ex.Status.Detail);
        }
    }
}