using System.Reflection;
using ITFPulse.Domain.Posts;
using ITFPulse.Application.Feeds;
using ITFPulse.Contracts.Messaging;

namespace ITFPulse.Architecture.Tests;

public sealed class DependencyTests
{
    [Theory]
    [InlineData(typeof(Post))]
    [InlineData(typeof(PlanPostFanoutHandler))]
    [InlineData(typeof(PostCreatedV1))]
    public void InnerLayersHaveNoInfrastructureDependencies(Type representative)
    {
        var names = representative.Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();
        Assert.DoesNotContain(names, n => n.StartsWith("MassTransit") || n.StartsWith("RabbitMQ")
            || n.StartsWith("Microsoft.EntityFrameworkCore") || n.StartsWith("Npgsql")
            || n is "ITFPulse.Infrastructure" or "ITFPulse.Api" or "ITFPulse.Worker");
    }

    [Fact]
    public void DomainDoesNotReferenceApplicationOrContracts()
    {
        Assert.DoesNotContain(typeof(Post).Assembly.GetReferencedAssemblies(),
            a => a.Name is "ITFPulse.Application" or "ITFPulse.Contracts");
    }
}
