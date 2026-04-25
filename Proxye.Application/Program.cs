using System.Text.Json;
using Proxye;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMvc().Services
    .AddProxye(o =>
    {
        var proxye = builder.Configuration.GetSection("Proxye").Get<ProxyeOptions>();
        o.Dns = proxye?.Dns ?? o.Dns;
        o.Port = proxye?.Port ?? o.Port;
        o.EnableDns = proxye?.EnableDns ?? o.EnableDns;
        o.DnsPort = proxye?.DnsPort ?? o.DnsPort;
        var raw = File.ReadAllText("rules.json");
        o.Rules = JsonSerializer.Deserialize<ProxyeRuleOptions>(raw)!;
    });

var app = builder.Build();
app.UseStaticFiles(); 
app.UseRouting();

app.UseEndpoints(e => e.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}"));

app.Run();
return 0;
