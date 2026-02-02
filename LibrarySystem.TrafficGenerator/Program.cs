using Grpc.Core;
using Grpc.Net.Client;
using LibrarySystem.Contracts.Protos; // Verify this matches your project
using Google.Protobuf.WellKnownTypes;

namespace LibrarySystem.TrafficGenerator;

class Program
{
    // CONFIGURATION: Ensure this matches your launchSettings.json
    private const string ServerUrl = "http://localhost";
    
    static async Task Main(string[] args)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        Console.Title = "☠️ TRAFFIC GENERATOR (Red Team)";
        Console.WriteLine($"Targeting: {ServerUrl}");

        using var channel = GrpcChannel.ForAddress(ServerUrl);
        
        // ⚠️ IMPORTANT: Check your .proto file for the service name.
        // If your proto says "service Inventory", use "Inventory.InventoryClient"
        // If your proto says "service Library", use "Library.LibraryClient"
        var client = new Library.LibraryClient(channel); 

        var random = new Random();
        var payloads = GetMaliciousPayloads();

        Console.WriteLine("Press Ctrl+C to stop...");

        while (true)
        {
            try
            {
                // 50% Attack, 50% Valid
                bool isAttack = random.NextDouble() > 0.5;

                if (isAttack)
                {
                    // Select random attack from the List
                    var attack = payloads[random.Next(payloads.Count)];
                    await SendMaliciousRequest(client, attack.Type, attack.Payload);
                }
                else
                {
                    await SendLegitimateRequest(client);
                }
            }
            catch (RpcException ex)
            {
                // This is GOOD. It means the WAF or Server rejected the request.
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[BLOCKED]: {ex.Status.StatusCode} - {ex.Status.Detail}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[ERROR]: {ex.Message}");
                Console.ResetColor();
            }

            // Small delay to make the WPF graph readable
            await Task.Delay(random.Next(200, 800));
        }
    }

    private static async Task SendLegitimateRequest(Library.LibraryClient client)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[INFO] Reading Book (ID 1)... ");
        
        // Valid Read Request
        await client.GetBookByIdAsync(new GetBookByIdRequest { Id = 1 }); 
        
        Console.WriteLine("OK.");
        Console.ResetColor();
    }

    private static async Task SendMaliciousRequest(Library.LibraryClient client, string type, string payload)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"[ATTACK] {type} on 'Title'... ");

        // We use AddBook because 'Title' is a string.
        // We inject the payload into the Title field.
        await client.CreateBookAsync(new CreateBookRequest 
        { 
            Title = payload,
            Author = "Unknown Attacker",
            PublicationYear = 2025,
            TotalCopies = 2
        });

        // If we reach here, the WAF FAILED to block it.
        Console.WriteLine("SENT (Not Blocked).");
        Console.ResetColor();
    }

    // Using List<Tuple> to fix the "Key/Value" error and allow index access
    private static List<(string Type, string Payload)> GetMaliciousPayloads()
    {
        return new List<(string, string)>
        {
            ("SQL Injection", "' OR '1'='1"),
            ("SQL Injection", "'; DROP TABLE Books; --"),
            ("XSS (Script)", "<script>alert('HACKED')</script>"),
            ("XSS (Image)", "<img src=x onerror=alert(1)>"),
            ("Path Traversal", "../../etc/passwd"),
            ("Command Inj", "; shutdown -h now")
        };
    }
}