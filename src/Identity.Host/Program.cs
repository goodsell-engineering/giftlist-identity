using BuildingBlocks.HealthChecks;
using BuildingBlocks.Logging;
using BuildingBlocks.Messaging;
using BuildingBlocks.Persistence;
using Identity.Infrastructure.Platform;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// GL-45: scope rendering for the console provider WebApplication.CreateBuilder already
// registers — without this, CorrelationIdIncomingStep's own logger scope pushes correctly but
// silently, since Microsoft.Extensions.Logging's simple console formatter defaults
// IncludeScopes to false.
builder.Services.AddBuildingBlocksLogging();

// Identity's own database/queue (ARCHITECTURE.md "Data model", "Tech stack") — wiring these here, ahead of any
// use case, is what makes "healthy Rebus connection + Mongo connection" (Phase 0 exit
// criterion) an observable fact rather than something scraped out of logs.
builder.Services.AddBuildingBlocksMongo(builder.Configuration, "identity");
builder.Services.AddBuildingBlocksRebus(builder.Configuration, "identity");
builder.Services.AddBuildingBlocksHealthChecks(builder.Configuration);

// GL-16: User aggregate, SignUp/Login use cases, and their Rebus handlers. Everything below
// this line is Identity's own composition root, in Identity.Infrastructure — Host itself
// contains no business wiring beyond this one call (CONVENTIONS.md "Project reference graph").
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var app = builder.Build();

// The unique email index (ARCHITECTURE.md "Data model") is a correctness requirement, not an
// optimisation — applied once at startup rather than left to be inferred from application code.
await IdentityInfrastructureServiceCollectionExtensions.EnsureIndexesAsync(app.Services, CancellationToken.None);

// Liveness: only "is the process up and answering HTTP". Deliberately checks nothing
// external — a RabbitMQ/Mongo blip must not make Docker kill an otherwise-healthy container
// (ARCHITECTURE.md "Why workers still need a little HTTP").
app.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = _ => false });

// Readiness: the Mongo + RabbitMQ checks BuildingBlocks registered above, tagged "ready".
// This is the endpoint compose's healthcheck targets, because it's the one that should gate
// `depends_on: condition: service_healthy` for anything waiting on Identity.
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();
