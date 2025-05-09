using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using FFB.ContentTransformation.Data;
using FFB.ContentTransformation.Services.DocumentProcessing;
using FFB.ContentTransformation.Services.AI;
using FFB.ContentTransformation.Services.AI.ContentGeneration;
using FFB.ContentTransformation.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Fluent UI components
builder.Services.AddFluentUIComponents();

// Configure database context
builder.Services.AddDbContextPool<AppDbContext>(options =>
{
    // For development, we'll use an in-memory database
    options.UseInMemoryDatabase("FFBContentDb");

    // For production, use PostgreSQL - uncomment the following
    /*
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly("FFB.ContentTransformation");
        npgsqlOptions.EnableRetryOnFailure(5);
    });
    */
});

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

// Register services
builder.Services.AddScoped<DocumentTextExtractor>();
builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
builder.Services.AddScoped<IAIService, AzureOpenAIService>();  // Real Azure OpenAI service
builder.Services.AddScoped<IContentGenerationService, ContentGenerationService>();
builder.Services.AddScoped<FFB.ContentTransformation.Services.ErrorHandling.IErrorHandlingService, FFB.ContentTransformation.Services.ErrorHandling.ErrorHandlingService>();

// Add Http client for Azure OpenAI
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<FFB.ContentTransformation.Components.App>()
    .AddInteractiveServerRenderMode();

// Ensure the database is created (for in-memory DB)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();