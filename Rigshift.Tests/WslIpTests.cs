using Rigshift.Execution;

namespace Rigshift.Tests;

public class WslIpTests
{
    [Theory]
    [InlineData("172.20.1.5 \n")]
    [InlineData("172.20.1.5\r\n")]
    [InlineData("172.20.1.5 fd00::1")]
    public void ParsesStrictIpv4FirstToken(string output)
    {
        Assert.True(WslIp.TryParseFirst(output, out var ip));
        Assert.Equal("172.20.1.5", ip);
    }

    [Fact]
    public void RejectsNullOutput()
    {
        Assert.False(WslIp.TryParseFirst(null!, out var ip));
        Assert.Equal("", ip);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("fd00::1 172.20.1.5")]
    [InlineData("1")]
    [InlineData("10.1")]
    [InlineData("010.0.0.1")]
    [InlineData("256.1.1.1")]
    [InlineData("172.20.1.5;calc")]
    [InlineData("172.20.1.5&calc")]
    public void RejectsOutputWithNoStrictIpv4FirstToken(string output)
    {
        Assert.False(WslIp.TryParseFirst(output, out var ip));
        Assert.Equal("", ip);
    }
}
