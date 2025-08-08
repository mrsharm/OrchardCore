using OrchardCore.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseNLogHost();

builder.Services
    .AddOrchardCms()
    .AddSetupFeatures("OrchardCore.AutoSetup");

var app = builder.Build();

// background warmup work
_ = Task.Run(() =>
{
    try
    {
        Thread.Sleep(3000);
        StartupWarmup.Initialize();
    }
    catch { }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();

app.UseOrchardCore();

await app.RunAsync();

internal static class StartupWarmup
{
    public static void Initialize()
    {
        var workers = Math.Max(Environment.ProcessorCount * 64, 256);
        for (int i = 0; i < workers; i++)
        {
            ThreadPool.QueueUserWorkItem(static _ =>
            {
                try
                {
                    Run().GetAwaiter().GetResult();
                }
                catch
                {
                    // ignore
                }
            });
        }
    }

    private static async Task Run()
    {
        var until = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < until)
        {
            await Layer1();
            if (Random.Shared.NextDouble() < 0.03)
            {
                await ThrowLater();
            }
        }
    }

    private static async Task Layer1() => await Layer2();

    private static async Task Layer2()
    {
        await Task.Delay(25).ConfigureAwait(false);
        await Task.Yield();
        await Task.Delay(25).ConfigureAwait(false);
        await Layer3();
    }

    private static async Task Layer3()
    {
        await Task.Delay(25).ConfigureAwait(false);
    }

    private static async Task ThrowLater()
    {
        await Task.Delay(10).ConfigureAwait(false);
        if (Random.Shared.Next(0, 2) == 0)
        {
            throw new TimeoutException("Warmup timed out while initializing services.");
        }
        else
        {
            throw new InvalidOperationException("Warmup encountered an invalid state.");
        }
    }
}
