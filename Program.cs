var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = ExpenseManagementPdfGenerator.Services.SharedPlaywrightBrowser.InitializeAsync();
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    ExpenseManagementPdfGenerator.Services.SharedPlaywrightBrowser.CloseAsync().GetAwaiter().GetResult();
});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
