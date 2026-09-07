using AIDrivenFrequencyAllocation.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    policy.AllowAnyHeader().AllowAnyMethod();

    if (builder.Environment.IsDevelopment())
    {
        // Next.js dev sunucusu bir port çakışmasında farklı bir porta
        // (3001, 3002, ...) geçebilir; geliştirmede bunu elle senkronize
        // etmek yerine herhangi bir localhost/127.0.0.1 origin'ine izin ver.
        policy.SetIsOriginAllowed(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            (uri.Host is "localhost" or "127.0.0.1"));
    }
    else
    {
        policy.WithOrigins(
            builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>());
    }
}));

builder.Services.AddSingleton<AllocationSessionService>();

var app = builder.Build();

app.UseCors();

const string notInitializedMessage = "Önce /api/session ile aralık ayarlayın";

app.MapPost("/api/session", (RangeRequest req, AllocationSessionService svc) =>
{
    if (req.MinFreqGHz >= req.MaxFreqGHz)
        return Results.BadRequest(new ErrorResponse("minFreqGHz, maxFreqGHz'den küçük olmalı"));

    return Results.Ok(svc.Initialize(req.MinFreqGHz, req.MaxFreqGHz));
});

app.MapGet("/api/session", (AllocationSessionService svc) =>
    svc.GetRange() is { } range
        ? Results.Ok(range)
        : Results.NotFound(new ErrorResponse("Oturum başlatılmadı")));

app.MapPost("/api/allocations", (AllocateRequest req, AllocationSessionService svc) =>
{
    if (!svc.IsInitialized)
        return Results.BadRequest(new ErrorResponse(notInitializedMessage));

    var block = svc.Allocate(req.BandwidthMHz);
    return block is null
        ? Results.UnprocessableEntity(new ErrorResponse("Uygun frekans bulunamadı"))
        : Results.Ok(block);
});

app.MapPost("/api/allocations/batch", (BatchAllocateRequest req, AllocationSessionService svc) =>
{
    if (!svc.IsInitialized)
        return Results.BadRequest(new ErrorResponse(notInitializedMessage));

    return Results.Ok(svc.AllocateBatch(req.BandwidthsMHz));
});

app.MapGet("/api/allocations", (AllocationSessionService svc) =>
    svc.IsInitialized
        ? Results.Ok(svc.GetAllocations())
        : Results.BadRequest(new ErrorResponse(notInitializedMessage)));

app.MapDelete("/api/allocations/{id}", (string id, AllocationSessionService svc) =>
{
    if (!svc.IsInitialized)
        return Results.BadRequest(new ErrorResponse(notInitializedMessage));

    return svc.Deallocate(id) ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/utilization", (AllocationSessionService svc) =>
    svc.IsInitialized
        ? Results.Ok(new UtilizationResponse(svc.GetUtilization()))
        : Results.BadRequest(new ErrorResponse(notInitializedMessage)));

app.Run();
