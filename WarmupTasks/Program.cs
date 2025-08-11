using System.Text;
using AutoFixture;

Console.WriteLine("Testing the IsPowerOfTwo method:");
var fixture = new Fixture();

var specificTestCases = new[] { 0, 1, 2, 3, 16, -8, 128 };
var randomTestCases = fixture.CreateMany<int>(10);
var allTestNumbers = specificTestCases.Concat(randomTestCases).Distinct();

foreach (var number in allTestNumbers) Console.WriteLine($"Is {number} a power of two? -> {IsPowerOfTwo(number)}");

Console.WriteLine("\nWarm-up Task 2: Reverse a Book Title.");
Console.WriteLine("\nTesting the ReverseTitle method:");

var specificTitleCases = new[]
{
    "1984",
    "Demônios",
    "Teeteto",
    "A Metamorfose",
    "Moby Dick",
    "A",
    "",
    null
};
var randomTitleCases = fixture.CreateMany<string>(5);
var allTestTitles = specificTitleCases.Concat(randomTitleCases);

foreach (var title in allTestTitles) Console.WriteLine($"Original: '{title}' -> Reversed: '{ReverseTitle(title)}'");

Console.WriteLine("\nWarm-up Task 3: Generate Book Title Replicas.");
Console.WriteLine("\nTesting the GenerateReplicas method:");

var testCases = new[]
{
    new { Title = "Read", Count = 3 },
    new { Title = "1984", Count = 2 },
    new { Title = "Echo", Count = 5 },
    new { Title = "Single", Count = 1 },
    new { Title = "ZeroCount", Count = 0 },
    new { Title = "NegativeCount", Count = -5 },
    new { Title = fixture.Create<string>(), Count = fixture.Create<int>() },
    new { Title = "Empty", Count = 100 },
    new { Title = (string?)null, Count = 10 }
};
foreach (var testCase in testCases)
{
    var result = GenerateReplicas(testCase.Title, testCase.Count);
    Console.WriteLine($"Repeating '{testCase.Title}' {testCase.Count} times -> '{result}'");
}

Console.WriteLine("\nWarm-up Task 4: List Odd-Numbered Book IDs.");
var random = new Random();
fixture.Register(() => random.Next(1, 101));
ListOddIds(0, fixture.Create<int>());

static bool IsPowerOfTwo(int n)
{
    return n > 0 && (n & (n - 1)) == 0;
}

static string ReverseTitle(string? title)
{
    return string.IsNullOrEmpty(title) ? string.Empty : new string(title.Reverse().ToArray());
}

static string GenerateReplicas(string? title, int count)
{
    if (string.IsNullOrEmpty(title) || count <= 0) return string.Empty;

    var stringBuilder = new StringBuilder(title.Length * count);
    for (var i = 0; i < count; i++) stringBuilder.Append(title);

    return stringBuilder.ToString();
}

static void ListOddIds(int start, int end)
{
    Console.WriteLine($"\nListing odd numbers between {start} and {end}:");

    for (var i = start; i <= end; i++)
        if (i % 2 != 0)
            Console.Write($"{i} ");

    Console.WriteLine();
}