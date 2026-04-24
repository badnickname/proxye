using System.Text.Json;
using System.Text.Json.Nodes;
using Proxye;
using Host = Proxye.Core.Models.Host;

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
    });

var app = builder.Build();
app.UseStaticFiles(); 
app.UseRouting();

app.UseEndpoints(e => e.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}"));

UpdateOptions(app.Services.GetRequiredService<ProxyeRules>());

app.Run();
return 0;

static void UpdateOptions(ProxyeRules rules)
{
    var raw = File.ReadAllText("rules.json");
    var json = JsonSerializer.Deserialize<JsonNode>(raw);

    rules.UpdateHost(new Host(json["Host"].GetValue<string>(), (ushort) json["Port"].GetValue<int>()));
    rules.UpdateRegex(json["Regex"].GetValue<string>());
}
