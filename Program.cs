using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<FirestoreService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddHttpClient<FunctionsService>();

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("Secrets/bumble-bee-foundation-firebase-adminsdk.json"),
});

var app = builder.Build();

//--- HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
