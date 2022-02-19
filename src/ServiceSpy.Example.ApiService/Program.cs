using ServiceSpy.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();
builder.Services.AddControllers();

// add service discovery and notifications:
//  configuration will only receive health check notifications and send/receive service metadata notifications
builder.Services.AddServiceSpy(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts(); // 30 days
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health-check");
app.MapControllers();
app.Run();
