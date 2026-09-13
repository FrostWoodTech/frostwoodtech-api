using System.Net;

using Microsoft.AspNetCore.Http;

using FrostWoodTech.API.Common;

namespace FrostWoodTech.Tests;

public class ClientAddressTests
{
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("203.0.113.7:52000", "203.0.113.7")]
    [InlineData("203.0.113.7:52000, 10.0.0.1", "203.0.113.7")]
    [InlineData("2001:db8::1", "2001:db8::1")]
    [InlineData("[2001:db8::1]:52000", "2001:db8::1")]
    [InlineData("[2001:db8:0:0:0:0:0:1]:443", "2001:db8:0:0:0:0:0:1")]
    public void The_first_forwarded_address_is_used_without_its_port(string forwardedFor, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;

        Assert.Equal(expected, ClientAddress.Read(context.Request));
    }

    [Fact]
    public void Without_a_forwarded_header_the_connection_address_is_used()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.4");

        Assert.Equal("198.51.100.4", ClientAddress.Read(context.Request));
    }
}
