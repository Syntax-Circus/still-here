using Shouldly;
using StillHere.Application.Features.Notifications;
using Xunit;

namespace StillHere.Application.Tests.Features.Notifications;

public sealed class NotificationTemplateSubstitutorTests
{
    [Fact]
    public void Substitute_AllPlaceholdersPresent_ReplacesEachWithContextValue()
    {
        var context = new NotificationEventContext("example.com", "1.2.3.4", "5.6.7.8", "Success", "IP changed");

        var result = NotificationTemplateSubstitutor.Substitute(
            "{domain}|{oldIp}|{newIp}|{status}|{message}", context);

        result.ShouldBe("example.com|1.2.3.4|5.6.7.8|Success|IP changed");
    }

    [Fact]
    public void Substitute_NullOldIpAndNewIp_SubstitutesToEmptyStringNotLiteralNull()
    {
        var context = new NotificationEventContext("example.com", null, null, "Success", "First check");

        var result = NotificationTemplateSubstitutor.Substitute("old=[{oldIp}] new=[{newIp}]", context);

        result.ShouldBe("old=[] new=[]");
    }

    [Fact]
    public void Substitute_NoPlaceholders_ReturnsTemplateUnchanged()
    {
        var context = new NotificationEventContext("example.com", "1.2.3.4", "5.6.7.8", "Success", "IP changed");

        var result = NotificationTemplateSubstitutor.Substitute("no placeholders here", context);

        result.ShouldBe("no placeholders here");
    }

    [Fact]
    public void Substitute_RepeatedPlaceholderOccurrences_ReplacesAllOccurrences()
    {
        var context = new NotificationEventContext("example.com", "1.2.3.4", "5.6.7.8", "Success", "IP changed");

        var result = NotificationTemplateSubstitutor.Substitute("{domain} - {domain} - {domain}", context);

        result.ShouldBe("example.com - example.com - example.com");
    }
}
