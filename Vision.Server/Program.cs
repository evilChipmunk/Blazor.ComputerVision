 
using Imaging2;
using Microsoft.AspNetCore.Components.Server.Circuits; 
using MudBlazor.Services;
using Numpy;
using Python.Runtime;
using Vision.Server.Services;

var builder = WebApplication.CreateBuilder(args);
 
// Add services to the container.
builder.Services.AddRazorPages();
// Add services to the container. 

var applicationSettingsSection = builder.Configuration.GetSection("ApplicationSettings");
var config = applicationSettingsSection.Get<Configuration>();
config.OutputDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
builder.Services.AddSingleton(config);


//builder.Services.AddServerSideBlazor();

//these settings probably aren't necessary
builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    o.ClientTimeoutInterval = TimeSpan.FromMinutes(10); // set timeout to 10 minutes
    o.MaximumReceiveMessageSize = 102400;
});
builder.Services.AddSignalR(e => e.MaximumReceiveMessageSize = 2 * 1024 * 1024); // For example, setting a 2 MB limit
//end potentially unnecessary settings

builder.Services.AddMudServices();
builder.Services.AddSingleton<CircuitHandler, AppCircuitHandler>(); // Register your custom circuit handler
 

var app = builder.Build();
 


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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");


// Initialize the Python engine
// Runtime.PythonDLL = @"C:\Users\mwest\AppData\Local\python-3.11.0-embed-amd64\python311.dll";
Runtime.PythonDLL = config.PythonDLL;
PythonEngine.Initialize();
PythonEngine.BeginAllowThreads();

//setting the seed value to 42 (or any number) for repeatable testing. 
//this makes sure that random data is the same random data everytime
using (Py.GIL())
{
    np.random.seed(42);
}
 
SetPackageImports(config);


app.Run();

void SetPackageImports(Configuration config)
{
    using (Py.GIL())
    {
        
        PythonEngine.PythonPath = $"{config.OutputDirectory};{config.SitePackages}";
        dynamic os = Py.Import("os");
        dynamic sys = Py.Import("sys"); 

        string path = sys.path.ToString();

        if (path.Contains(config.SitePackages) == false)
        {
            sys.path.append(config.SitePackages);
        }

        //add python script files
        var entryModule = Path.Join(config.OutputDirectory, "tracker.py");
        string entryModulePath = os.path.dirname(entryModule).ToString();
        if (path.Contains(entryModulePath) == false)
        {
            sys.path.append(entryModulePath);
        }
    }
}