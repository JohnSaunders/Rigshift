using Rigshift.Execution;

namespace Rigshift.Tests;

public class AdminPayloadTests
{
    [Fact]
    public void FormatsAdminStepsAsFlagFollowedByOrderedTokens()
    {
        var steps = new Step[]
        {
            new PortProxyRemove(11435),
            new PortProxyAdd(11435),
            new KillWslService(),
            new FlushDns()
        };

        var formatted = AdminPayload.Format(steps);

        Assert.Equal(
            new[] { "--admin", "proxy-remove:11435", "proxy-add:11435", "kill-wslservice", "flush-dns" },
            formatted);
    }

    [Fact]
    public void RoundTripsFormattedAdminSteps()
    {
        var steps = new Step[]
        {
            new PortProxyRemove(11435),
            new PortProxyAdd(11435),
            new KillWslService(),
            new FlushDns()
        };

        var ok = AdminPayload.TryParse(AdminPayload.Format(steps), out var parsed);

        Assert.True(ok);
        Assert.Equal(steps, parsed);
    }

    [Fact]
    public void FormatThrowsArgumentNullExceptionForNullList()
    {
        Assert.Throws<ArgumentNullException>(() => AdminPayload.Format(null!));
    }

    [Fact]
    public void FormatThrowsArgumentExceptionForEmptyList()
    {
        Assert.Throws<ArgumentException>(() => AdminPayload.Format(Array.Empty<Step>()));
    }

    [Fact]
    public void FormatThrowsArgumentExceptionForSeventeenSteps()
    {
        var tooMany = new List<Step>(Enumerable.Repeat<Step>(new FlushDns(), 17));

        Assert.Throws<ArgumentException>(() => AdminPayload.Format(tooMany));
    }

    [Fact]
    public void FormatThrowsArgumentExceptionForUserSteps()
    {
        Assert.Throws<ArgumentException>(() => AdminPayload.Format([new StartProcess("x")]));
        Assert.Throws<ArgumentException>(() => AdminPayload.Format([new StartWslService("T", "c")]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void FormatThrowsArgumentOutOfRangeExceptionForInvalidPorts(int port)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AdminPayload.Format([new PortProxyAdd(port)]));
    }

    [Fact]
    public void RejectsNullArguments()
    {
        Assert.False(AdminPayload.TryParse(null!, out var steps));
        Assert.Empty(steps);
    }

    [Fact]
    public void RejectsArgumentsWithNullElements()
    {
        Assert.False(AdminPayload.TryParse(new[] { "--admin", null! }, out var steps));
        Assert.Empty(steps);
    }

    // MemberData, not InlineData: InlineData takes `params object[]`, so a
    // string[] argument binds to the params array itself via covariance and the
    // method receives loose strings rather than one array.
    public static IEnumerable<object[]> RejectedArgumentSets() =>
    [
        [Array.Empty<string>()],
        [new[] { "--admin" }],
        [new[] { "--run", "AI", "enable", "" }],
        [new[] { "--admin", "flush-dns", "--admin" }],
        [new[] { "--admin", "Flush-dns" }],
        [new[] { "--admin", " flush-dns" }],
        [new[] { "--admin", "proxy-add:" }],
        [new[] { "--admin", "proxy-add:0" }],
        [new[] { "--admin", "proxy-add:011435" }],
        [new[] { "--admin", "proxy-add:080" }],
        [new[] { "--admin", "proxy-add:+80" }],
        [new[] { "--admin", "proxy-add:65536" }],
        [new[] { "--admin", "proxy-add:\u0663" }],
        [new[] { "--admin", "start:calc.exe" }],
        [new[] { "--admin" }.Concat(Enumerable.Repeat("flush-dns", 17)).ToArray()]
    ];

    [Theory]
    [MemberData(nameof(RejectedArgumentSets))]
    public void RejectsMalformedPayloadsWithEmptySteps(string[] args)
    {
        Assert.False(AdminPayload.TryParse(args, out var steps));
        Assert.Empty(steps);
    }

    [Fact]
    public void ParsesPortOne()
    {
        Assert.True(AdminPayload.TryParse(new[] { "--admin", "proxy-add:1" }, out var steps));
        Assert.Equal(new[] { new PortProxyAdd(1) }, steps);
    }

    [Fact]
    public void ParsesPortSixtyFiveThousandFiveHundredThirtyFive()
    {
        Assert.True(AdminPayload.TryParse(new[] { "--admin", "proxy-add:65535" }, out var steps));
        Assert.Equal(new[] { new PortProxyAdd(65535) }, steps);
    }

    [Fact]
    public void ParsesExactlySixteenSteps()
    {
        var args = new[] { "--admin" }.Concat(Enumerable.Repeat("flush-dns", 16)).ToArray();

        Assert.True(AdminPayload.TryParse(args, out var steps));
        Assert.Equal(16, steps.Count);
        Assert.All(steps, s => Assert.IsType<FlushDns>(s));
    }
}
