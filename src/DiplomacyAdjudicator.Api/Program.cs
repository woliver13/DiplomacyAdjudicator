using DiplomacyAdjudicator.Core.Adjudication;
using DiplomacyAdjudicator.Core.Map;
using DiplomacyAdjudicator.Core.Rulesets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IRulesetRegistry, RulesetRegistry>();
builder.Services.AddSingleton<IMovementAdjudicator, MovementAdjudicator>();
builder.Services.AddSingleton<IRetreatAdjudicator, RetreatAdjudicator>();
builder.Services.AddSingleton<IBuildAdjudicator, BuildAdjudicator>();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }
