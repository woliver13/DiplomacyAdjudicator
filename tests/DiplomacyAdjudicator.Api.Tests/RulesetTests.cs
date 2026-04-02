using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DiplomacyAdjudicator.Api.Tests;

public class RulesetTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    // Cycle 2 — unknown ruleset returns 400 with list of supported values
    [Fact]
    public async Task Movement_WithUnknownRuleset_Returns400WithSupportedList()
    {
        var body = new
        {
            ruleset = "house_rules_1847",
            units  = new[] { new { power = "austria", type = "army", province = "vie" } },
            orders = new[] { new { power = "austria", unitType = "army", province = "vie", orderText = "hold" } },
            supplyCenters = new Dictionary<string, string[]> { ["austria"] = ["vie"] }
        };

        var response = await _client.PostAsJsonAsync("/adjudicate/movement", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body2 = await response.Content.ReadAsStringAsync();
        Assert.Contains("standard_2000", body2);
    }

    // Cycle 1 — explicit ruleset "standard_2000" is accepted and adjudicated normally
    [Fact]
    public async Task Movement_WithExplicitStandard2000Ruleset_Returns200()
    {
        var body = new
        {
            ruleset = "standard_2000",
            units  = new[] { new { power = "austria", type = "army", province = "vie" } },
            orders = new[] { new { power = "austria", unitType = "army", province = "vie", orderText = "hold" } },
            supplyCenters = new Dictionary<string, string[]> { ["austria"] = ["vie"] }
        };

        var response = await _client.PostAsJsonAsync("/adjudicate/movement", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Cycle 3 — standard_1971: A TUN can move directly to NAP (army-adjacent in 1971 map)
    //           standard_2000: same move fails (TUN and NAP are not army-adjacent)
    [Fact]
    public async Task Movement_TunToNap_SucceedsIn1971_FailsIn2000()
    {
        var body1971 = new
        {
            ruleset = "standard_1971",
            units  = new[] { new { power = "italy", type = "army", province = "tun" } },
            orders = new[] { new { power = "italy", unitType = "army", province = "tun", orderText = "move nap" } },
            supplyCenters = new Dictionary<string, string[]> { ["italy"] = ["tun"] }
        };

        var body2000 = new
        {
            ruleset = "standard_2000",
            units  = new[] { new { power = "italy", type = "army", province = "tun" } },
            orders = new[] { new { power = "italy", unitType = "army", province = "tun", orderText = "move nap" } },
            supplyCenters = new Dictionary<string, string[]> { ["italy"] = ["tun"] }
        };

        var resp1971 = await _client.PostAsJsonAsync("/adjudicate/movement", body1971);
        var resp2000 = await _client.PostAsJsonAsync("/adjudicate/movement", body2000);

        Assert.Equal(HttpStatusCode.OK, resp1971.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resp2000.StatusCode);

        var result1971 = await resp1971.Content.ReadAsStringAsync();
        var result2000 = await resp2000.Content.ReadAsStringAsync();

        Assert.Contains("\"Success\"", result1971);
        Assert.Contains("\"Failure\"", result2000);
    }
}
