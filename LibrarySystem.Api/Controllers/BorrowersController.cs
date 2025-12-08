using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using LibrarySystem.Contracts.Protos;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BorrowersController : ControllerBase
{
    private readonly Library.LibraryClient _grpcClient;

    public BorrowersController(Library.LibraryClient grpcClient)
    {
        _grpcClient = grpcClient;
    }

    [HttpGet("most-active")]
    public async Task<IActionResult> GetTopBorrowers(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int count = 5)
    {
        if (startDate >= endDate || count <= 0)
            return BadRequest("Invalid query parameters. Ensure startDate is before endDate and count is positive.");

        try
        {
            var request = new GetTopBorrowersRequest
            {
                Count = count,
                StartDate = Timestamp.FromDateTime(startDate.ToUniversalTime()),
                EndDate = Timestamp.FromDateTime(endDate.ToUniversalTime())
            };
            var response = await _grpcClient.GetTopBorrowersAsync(request);
            return Ok(response.TopBorrowers);
        }
        catch (RpcException ex)
        {
            return StatusCode(500, "An error occurred while communicating with the service.");
        }
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetLendingHistory(
        int id,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate >= endDate) return BadRequest("Start date must be before end date.");

        try
        {
            var request = new GetUserLendingHistoryRequest
            {
                BorrowerId = id,
                StartDate = Timestamp.FromDateTime(startDate.ToUniversalTime()),
                EndDate = Timestamp.FromDateTime(endDate.ToUniversalTime())
            };
            var response = await _grpcClient.GetUserLendingHistoryAsync(request);
            return Ok(response.History);
        }
        catch (RpcException ex)
        {
            return StatusCode(500, "An error occurred while communicating with the service.");
        }
    }
}