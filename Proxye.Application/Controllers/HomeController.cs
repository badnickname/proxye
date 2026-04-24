using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Host = Proxye.Core.Models.Host;

namespace Proxye.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("rules")]
    public IActionResult GetRules()
    {
        var stream = System.IO.File.OpenRead("rules.json");
        return File(stream, "application/json");
    }

    [HttpPost("rules")]
    public async Task<IActionResult> SendRules([FromServices] ProxyeRules rules)
    {
        var raw = await new StreamReader(Request.Body).ReadToEndAsync();
        var json = JsonSerializer.Deserialize<JsonNode>(raw);

        rules.UpdateHost(new Host(json["Host"].GetValue<string>(), (ushort) json["Port"].GetValue<int>()));
        rules.UpdateRegex(json["Regex"].GetValue<string>());

        await System.IO.File.WriteAllTextAsync("rules.json", raw);

        return Ok();
    }
}
