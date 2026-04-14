using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace woliver13.DiplomacyAdjudicator.Api.Tests;

public class AdjudicationApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    // -------------------------------------------------------------------------
    // POST /adjudicate/movement
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Movement_HoldOrder_Returns200WithOrderResult()
    {
        var body = new
        {
            units = new[]
            {
                new { power = "austria", type = "army", province = "vie" }
            },
            orders = new[]
            {
                new { power = "austria", unitType = "army", province = "vie", orderText = "hold" }
            },
            supplyCenters = new Dictionary<string, string[]>
            {
                ["austria"] = ["vie"]
            }
        };

        var response = await _client.PostAsJsonAsync("/adjudicate/movement", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MovementResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.OrderResults);
        Assert.Equal("vie", result.OrderResults[0].Province);
        Assert.Equal("Success", result.OrderResults[0].Outcome);
    }

    [Fact]
    public async Task Movement_SuccessfulAttack_ReturnsDislodgedUnit()
    {
        // TYR→VIE (supported by BOH, which is army-adjacent to VIE)
        var body = new
        {
            units = new[]
            {
                new { power = "austria", type = "army", province = "vie" },
                new { power = "germany", type = "army", province = "tyr" },
                new { power = "germany", type = "army", province = "boh" },
            },
            orders = new[]
            {
                new { power = "austria", unitType = "army", province = "vie", orderText = "hold" },
                new { power = "germany", unitType = "army", province = "tyr", orderText = "move vie" },
                new { power = "germany", unitType = "army", province = "boh", orderText = "support army tyr move vie" },
            },
            supplyCenters = new Dictionary<string, string[]>
            {
                ["austria"] = ["vie"],
                ["germany"] = ["mun"]
            }
        };

        var response = await _client.PostAsJsonAsync("/adjudicate/movement", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MovementResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.DislodgedUnits);
        Assert.Equal("vie", result.DislodgedUnits[0].Unit.Province);
        Assert.Equal("tyr", result.DislodgedUnits[0].AttackedFrom);
        Assert.NotEmpty(result.DislodgedUnits[0].RetreatOptions);
    }

    // -------------------------------------------------------------------------
    // POST /adjudicate/retreat
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Retreat_ValidRetreatOrder_Returns200WithSurvivedUnit()
    {
        var body = new
        {
            dislodgedUnits = new[]
            {
                new
                {
                    unit = new { power = "austria", type = "army", province = "vie" },
                    attackedFrom = "tyr",
                    retreatOptions = new[] { "bud", "gal" }
                }
            },
            retreatOrders = new[]
            {
                new { power = "austria", unitType = "army", province = "vie", orderText = "retreat bud" }
            }
        };

        var response = await _client.PostAsJsonAsync("/adjudicate/retreat", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RetreatResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.SurvivedUnits);
        Assert.Equal("bud", result.SurvivedUnits[0].Province);
    }

    // -------------------------------------------------------------------------
    // Validation: null / empty collections
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Movement_UnknownUnitType_Returns400()
    {
        var body = new
        {
            units = new[]
            {
                new { power = "austria", type = "zeppelin", province = "vie" }
            },
            orders = new[]
            {
                new { power = "austria", unitType = "zeppelin", province = "vie", orderText = "hold" }
            },
            supplyCenters = new Dictionary<string, string[]> { ["austria"] = ["vie"] }
        };
        var response = await _client.PostAsJsonAsync("/adjudicate/movement", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Retreat_UnknownUnitType_Returns400()
    {
        var body = new
        {
            dislodgedUnits = new[]
            {
                new
                {
                    unit = new { power = "austria", type = "zeppelin", province = "vie" },
                    attackedFrom = "tyr",
                    retreatOptions = new[] { "bud" }
                }
            },
            retreatOrders = new[]
            {
                new { power = "austria", unitType = "zeppelin", province = "vie", orderText = "retreat bud" }
            }
        };
        var response = await _client.PostAsJsonAsync("/adjudicate/retreat", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Build_UnknownUnitType_Returns400()
    {
        var body = new
        {
            units = new[] { new { power = "austria", type = "zeppelin", province = "bud" } },
            supplyCenters = new Dictionary<string, string[]> { ["austria"] = ["vie", "bud"] },
            buildOrders = new[]
            {
                new { power = "austria", unitType = "zeppelin", province = "vie", orderText = "build zeppelin vie" }
            }
        };
        var response = await _client.PostAsJsonAsync("/adjudicate/build", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Retreat_UnknownProvinceCode_Returns400()
    {
        var body = new
        {
            dislodgedUnits = new[]
            {
                new
                {
                    unit = new { power = "austria", type = "army", province = "zzz" },
                    attackedFrom = "tyr",
                    retreatOptions = new[] { "bud" }
                }
            },
            retreatOrders = new[]
            {
                new { power = "austria", unitType = "army", province = "zzz", orderText = "retreat bud" }
            }
        };
        var response = await _client.PostAsJsonAsync("/adjudicate/retreat", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Build_UnknownProvinceCode_Returns400()
    {
        var body = new
        {
            units = new[] { new { power = "austria", type = "army", province = "zzz" } },
            supplyCenters = new Dictionary<string, string[]> { ["austria"] = ["vie", "bud"] },
            buildOrders = new[]
            {
                new { power = "austria", unitType = "army", province = "zzz", orderText = "build army zzz" }
            }
        };
        var response = await _client.PostAsJsonAsync("/adjudicate/build", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Movement_UnknownProvinceCode_Returns400()
    {
        var body = new
        {
            units = new[]
            {
                new { power = "austria", type = "army", province = "zzz" }
            },
            orders = new[]
            {
                new { power = "austria", unitType = "army", province = "zzz", orderText = "hold" }
            },
            supplyCenters = new Dictionary<string, string[]> { ["austria"] = ["vie"] }
        };
        var response = await _client.PostAsJsonAsync("/adjudicate/movement", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Movement_NullBody_Returns400()
    {
        var response = await _client.PostAsJsonAsync<object?>("/adjudicate/movement", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Movement_UnknownRuleset_Returns400()
    {
        var body = new
        {
            ruleset = "nonexistent_ruleset",
            units = new[] { new { power = "austria", type = "army", province = "vie" } },
            orders = new[] { new { power = "austria", unitType = "army", province = "vie", orderText = "hold" } },
            supplyCenters = new Dictionary<string, string[]> { ["austria"] = ["vie"] }
        };
        var response = await _client.PostAsJsonAsync("/adjudicate/movement", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Retreat_EmptyDislodgedUnits_Returns200()
    {
        var body = new
        {
            dislodgedUnits = Array.Empty<object>(),
            retreatOrders  = Array.Empty<object>()
        };
        var response = await _client.PostAsJsonAsync("/adjudicate/retreat", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // End-to-end: Movement → Retreat → Build
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FullGameCycle_MovementThenRetreatThenBuild_AllPhasesProduce200()
    {
        // Phase 1 — Movement: Germany (TYR, BOH) dislodges Austria (VIE)
        var movementBody = new
        {
            units = new[]
            {
                new { power = "austria", type = "army", province = "vie" },
                new { power = "germany", type = "army", province = "tyr" },
                new { power = "germany", type = "army", province = "boh" },
            },
            orders = new[]
            {
                new { power = "austria",  unitType = "army", province = "vie", orderText = "hold" },
                new { power = "germany",  unitType = "army", province = "tyr", orderText = "move vie" },
                new { power = "germany",  unitType = "army", province = "boh", orderText = "support army tyr move vie" },
            },
            supplyCenters = new Dictionary<string, string[]>
            {
                ["austria"] = ["vie"],
                ["germany"] = ["mun"]
            }
        };

        var movResp = await _client.PostAsJsonAsync("/adjudicate/movement", movementBody);
        Assert.Equal(HttpStatusCode.OK, movResp.StatusCode);
        var movResult = await movResp.Content.ReadFromJsonAsync<MovementResponseDto>();
        Assert.NotNull(movResult);
        Assert.Single(movResult.DislodgedUnits);
        var dislodged = movResult.DislodgedUnits[0];
        Assert.Equal("vie", dislodged.Unit.Province);
        Assert.NotEmpty(dislodged.RetreatOptions);

        // Phase 2 — Retreat: Austria retreats from VIE to BUD
        var retreatBody = new
        {
            dislodgedUnits = new[]
            {
                new
                {
                    unit         = new { power = dislodged.Unit.Power, type = dislodged.Unit.Type, province = dislodged.Unit.Province },
                    attackedFrom = dislodged.AttackedFrom,
                    retreatOptions = dislodged.RetreatOptions
                }
            },
            retreatOrders = new[]
            {
                new { power = "austria", unitType = "army", province = "vie", orderText = $"retreat {dislodged.RetreatOptions[0]}" }
            }
        };

        var retResp = await _client.PostAsJsonAsync("/adjudicate/retreat", retreatBody);
        Assert.Equal(HttpStatusCode.OK, retResp.StatusCode);
        var retResult = await retResp.Content.ReadFromJsonAsync<RetreatResponseDto>();
        Assert.NotNull(retResult);
        Assert.Single(retResult.SurvivedUnits);

        // Phase 3 — Build: Austria has VIE (now held by Germany), so no build rights;
        //   use a minimal valid build request (Austria waives, no units built).
        var buildBody = new
        {
            units = new[] { new { power = "austria", type = "army", province = retResult.SurvivedUnits[0].Province } },
            supplyCenters = new Dictionary<string, string[]>
            {
                ["austria"] = ["bud"],
                ["germany"] = ["vie", "mun"]
            },
            buildOrders = Array.Empty<object>()
        };

        var buildResp = await _client.PostAsJsonAsync("/adjudicate/build", buildBody);
        Assert.Equal(HttpStatusCode.OK, buildResp.StatusCode);
    }

    // -------------------------------------------------------------------------
    // POST /adjudicate/build
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Build_ValidBuildOrder_Returns200WithNewUnit()
    {
        var body = new
        {
            units = new[]
            {
                new { power = "austria", type = "army", province = "bud" }
            },
            supplyCenters = new Dictionary<string, string[]>
            {
                ["austria"] = ["vie", "bud"]
            },
            buildOrders = new[]
            {
                new { power = "austria", unitType = "army", province = "vie", orderText = "build army vie" }
            }
        };

        var response = await _client.PostAsJsonAsync("/adjudicate/build", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BuildResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(2, result.ResultingUnits.Count);
        Assert.Contains(result.ResultingUnits, u => u.Province == "vie");
    }
}

// Minimal response DTOs for test deserialization
// Minimal response DTOs — must be internal (not file) so System.Text.Json can deserialize nested types
internal record UnitDto(string Power, string Type, string Province);
internal record OrderResultDto(string Province, string Outcome, string? Reason);
internal record DislodgedUnitDto(UnitDto Unit, string AttackedFrom, IReadOnlyList<string> RetreatOptions);
internal record MovementResponseDto(
    IReadOnlyList<OrderResultDto> OrderResults,
    IReadOnlyList<DislodgedUnitDto> DislodgedUnits,
    string NextPhase);
internal record RetreatResponseDto(
    IReadOnlyList<OrderResultDto> OrderResults,
    IReadOnlyList<UnitDto> SurvivedUnits);
internal record BuildResponseDto(
    IReadOnlyList<OrderResultDto> OrderResults,
    IReadOnlyList<UnitDto> ResultingUnits);

