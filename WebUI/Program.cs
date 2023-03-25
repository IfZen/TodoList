using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;

using TodoList.WebUI.Blazor.Authentication;
using TodoList.WebUI.Blazor.Helpers;

//using Syncfusion.Blazor;

using TodoList;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<ProtectedSessionStorage>(); // added
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>(); // added
builder.Services.AddScoped<CustomAuthenticationStateProvider, CustomAuthenticationStateProvider>(); // added
builder.Services.AddTransient<ExceptionPolicy>();
//builder.Services.AddSyncfusionBlazor();
var db = new TodoList.Persistence.Service();
//builder.Services.AddSingleton<TodoList.Persistence.Service>(db); // added
var userService = new TodoList.Business.Authentification.Service(db);
builder.Services.AddSingleton<TodoList.Business.Authentification.Service>(userService); // added
var todoListService = new TodoList.Business.BusyList.Service(db, userService);
builder.Services.AddSingleton<TodoList.Business.BusyList.Service>(todoListService); // added

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");



// Create admin through temporary service (let's consider it works)
var admin = userService.NewUser("admin");
admin.Email = "admin@test.com";
admin.FirstName = "Mickael";
admin.Surname = "Labau";
var password = "Azerty123!";
userService.Insert(admin, password, password);

// Then go to db to get the state and initialize role manually
var adminState = db.Users.Get().Single();
adminState.Role = TodoList.Business.Authentification.eUserRole.Administrator;
db.Users.Upsert(adminState);

app.Run();