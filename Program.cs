using POC;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder.Configuration);
// Add services to the container.
builder.Services.AddRazorPages();

//configure services
startup.ConfigureServices(builder.Services);

var app = builder.Build();

//configure app
startup.Configure(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
