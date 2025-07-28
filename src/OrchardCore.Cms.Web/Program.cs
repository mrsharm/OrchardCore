using OrchardCore.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNLogHost();

builder.Services
    .AddOrchardCms()
    .AddSetupFeatures("OrchardCore.AutoSetup");

var app = builder.Build();

Task.Run(async () =>
{
    Console.WriteLine("Starting memory allocation thread...");
    await Task.Delay(5000); // Wait 5 seconds before starting allocation
    
    var allocations = new List<byte[]>();
    var random = new Random();
    
    try
    {
        while (true)
        {
            // Allocate increasingly large chunks of memory
            var size = 100 * 1024 * 1024; // Start with 100MB chunks
            var buffer = new byte[size];
            
            // Fill with random data to ensure it's actually allocated
            random.NextBytes(buffer);
            allocations.Add(buffer);
            
            Console.WriteLine($"DEMO: Allocated {allocations.Count * size / (1024 * 1024)}MB total");
            
            // Small delay to see progress
            await Task.Delay(500);
            
            // Increase allocation size progressively
            size = (int)(size * 1.2);
        }
    }
    catch (OutOfMemoryException ex)
    {
        Console.WriteLine($"DEMO: OutOfMemoryException caught: {ex.Message}");
        // This will likely crash the entire application
        throw;
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseOrchardCore();

await app.RunAsync();
