using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder.Extensions;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<FirestoreService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddHttpClient<FunctionsService>();

// Configure session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; 
});

builder.Services.AddSingleton(sp =>
{
    var googleCredential = GoogleCredential.FromFile("Secrets/bumble-bee-foundation-firebase-adminsdk.json");
    var firestoreDbBuilder = new FirestoreDbBuilder
    {
        ProjectId = "bumble-bee-foundation",
        Credential = googleCredential
    };
    return firestoreDbBuilder.Build();
});

// Configure Firebase
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("Secrets/bumble-bee-foundation-firebase-adminsdk.json"),
});

var app = builder.Build();

// HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession(); 
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}");

app.Run();
