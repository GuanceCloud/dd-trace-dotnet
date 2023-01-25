// <copyright file="AspNetCore5AsmData.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#if NETCOREAPP3_0_OR_GREATER
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Datadog.Trace.AppSec;
using Datadog.Trace.AppSec.RcmModels.AsmData;
using Datadog.Trace.Configuration;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Security.IntegrationTests.Rcm
{
    public class AspNetCore5AsmDataSecurityDisabledBlockingRequestIp : AspNetCore5AsmDataBlockingRequestIp
    {
        public AspNetCore5AsmDataSecurityDisabledBlockingRequestIp(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
            : base(fixture, outputHelper, enableSecurity: false, testName: "AspNetCore5AsmDataSecurityDisabled")
        {
        }
    }

    public class AspNetCore5AsmDataSecurityEnabledBlockingRequestIp : AspNetCore5AsmDataBlockingRequestIp
    {
        public AspNetCore5AsmDataSecurityEnabledBlockingRequestIp(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
            : base(fixture, outputHelper, enableSecurity: true, testName: "AspNetCore5AsmDataSecurityEnabled")
        {
        }
    }

    public class AspNetCore5AsmDataSecurityDisabledBlockingUser : AspNetCore5AsmDataBlockingUser
    {
        public AspNetCore5AsmDataSecurityDisabledBlockingUser(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
            : base(fixture, outputHelper, enableSecurity: false, testName: "AspNetCore5AsmDataSecurityDisabled")
        {
        }
    }

    public class AspNetCore5AsmDataSecurityEnabledBlockingUser : AspNetCore5AsmDataBlockingUser
    {
        public AspNetCore5AsmDataSecurityEnabledBlockingUser(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
            : base(fixture, outputHelper, enableSecurity: true, testName: "AspNetCore5AsmDataSecurityEnabled")
        {
        }
    }

    public abstract class AspNetCore5AsmDataBlockingRequestIp : RcmBase
    {
        public AspNetCore5AsmDataBlockingRequestIp(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper, bool enableSecurity, string testName)
            : base(fixture, outputHelper, enableSecurity, testName: testName)
        {
            SetEnvironmentVariable(ConfigurationKeys.DebugEnabled, "0");
        }

        [SkippableTheory]
        [InlineData("blocking-ips", "/")]
        [Trait("RunOnWindows", "True")]
        public async Task RunTest(string test, string url)
        {
            await TryStartApp();
            var agent = Fixture.Agent;
            using var logEntryWatcher = new LogEntryWatcher($"{LogFileNamePrefix}{Fixture.Process.ProcessName}*", LogDirectory);
            var sanitisedUrl = VerifyHelper.SanitisePathsForVerify(url);
            // we want to see the ip here
            var scrubbers = VerifyHelper.SpanScrubbers.Where(s => s.RegexPattern.ToString() != @"http.client_ip: (.)*(?=,)");
            var settings = VerifyHelper.GetSpanVerifierSettings(scrubbers: scrubbers, parameters: new object[] { test, sanitisedUrl });
            var spanBeforeAsmData = await SendRequestsAsync(agent, url);

            var product = new AsmDataProduct();
            agent.SetupRcm(
                Output,
                new[]
                {
                    (
                        (object)new Payload { RulesData = new[] { new RuleData { Id = "blocked_ips", Type = "ip_with_expiration", Data = new[] { new Data { Expiration = 5545453532, Value = MainIp } } } } }, "asm_data"),
                    (new Payload { RulesData = new[] { new RuleData { Id = "blocked_ips", Type = "ip_with_expiration", Data = new[] { new Data { Expiration = 1545453532, Value = MainIp } } } } }, "asm_data_servicea"),
                },
                product.Name);

            var request1 = await agent.WaitRcmRequestAndReturnLast();
            if (EnableSecurity == true)
            {
                await logEntryWatcher.WaitForLogEntry($"1 {RulesUpdatedMessage()}", LogEntryWatcherTimeout);
            }
            else
            {
                await Task.Delay(1500);
            }

            var spanAfterAsmData = await SendRequestsAsync(agent, url);
            var spans = new List<MockSpan>();
            spans.AddRange(spanBeforeAsmData);
            spans.AddRange(spanAfterAsmData);
            await VerifySpans(spans.ToImmutableList(), settings);
        }
    }

    public class AspNetCore5AsmDataSecurityEnabledBlockingRequestIpOneClick : RcmBase
    {
        public AspNetCore5AsmDataSecurityEnabledBlockingRequestIpOneClick(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper)
            : base(fixture, outputHelper, enableSecurity: true, testName: "AspNetCore5AsmDataSecurityEnabled")
        {
        }

        [SkippableTheory]
        [InlineData("blocking-ips-oneclick", "/")]
        [Trait("RunOnWindows", "True")]
        public async Task RunTest(string test, string url)
        {
            await TryStartApp();
            var agent = Fixture.Agent;
            var sanitisedUrl = VerifyHelper.SanitisePathsForVerify(url);
            // we want to see the ip here
            var scrubbers = VerifyHelper.SpanScrubbers.Where(s => s.RegexPattern.ToString() != @"http.client_ip: (.)*(?=,)");
            var settings = VerifyHelper.GetSpanVerifierSettings(scrubbers: scrubbers, parameters: new object[] { test, sanitisedUrl });
            using var logEntryWatcher = new LogEntryWatcher($"{LogFileNamePrefix}{Fixture.Process.ProcessName}*", LogDirectory);
            var spanBeforeAsmData = await SendRequestsAsync(agent, url);

            var product = new AsmDataProduct();
            agent.SetupRcm(
                Output,
                new[]
                {
                    (
                        (object)new Payload { RulesData = new[] { new RuleData { Id = "blocked_ips", Type = "ip_with_expiration", Data = new[] { new Data { Expiration = 5545453532, Value = MainIp } } } } }, "asm_data"),
                    (new Payload { RulesData = new[] { new RuleData { Id = "blocked_ips", Type = "ip_with_expiration", Data = new[] { new Data { Expiration = 1545453532, Value = MainIp } } } } }, "asm_data_servicea"),
                },
                product.Name);

            var request1 = await agent.WaitRcmRequestAndReturnLast();
            var rulesUpdatedMessage = RulesUpdatedMessage();
            await logEntryWatcher.WaitForLogEntry($"1 {rulesUpdatedMessage}", LogEntryWatcherTimeout);

            var spanAfterAsmData = await SendRequestsAsync(agent, url);
            spanAfterAsmData.First().GetTag(Tags.AppSecEvent).Should().NotBeNull();
            agent.SetupRcm(Output, new[] { ((object)new AsmFeatures { Asm = new AsmFeature { Enabled = false } }, "1") }, "ASM_FEATURES");
            var requestAfterDeactivation = await agent.WaitRcmRequestAndReturnLast();
            await logEntryWatcher.WaitForLogEntry(AppSecDisabledMessage(), LogEntryWatcherTimeout);

            var spanAfterAsmDeactivated = await SendRequestsAsync(agent, url);

            agent.SetupRcm(Output, new[] { ((object)new AsmFeatures { Asm = new AsmFeature { Enabled = true } }, "1") }, "ASM_FEATURES");
            var requestAfterReactivation = await agent.WaitRcmRequestAndReturnLast();
            await logEntryWatcher.WaitForLogEntries(new[] { $"1 {rulesUpdatedMessage}", AppSecEnabledMessage() }, LogEntryWatcherTimeout);

            var spanAfterAsmDataReactivated = await SendRequestsAsync(agent, url);

            var spans = new List<MockSpan>();
            spans.AddRange(spanBeforeAsmData);
            spans.AddRange(spanAfterAsmData);
            spans.AddRange(spanAfterAsmDeactivated);
            spans.AddRange(spanAfterAsmDataReactivated);

            await VerifySpans(spans.ToImmutableList(), settings);
        }
    }

    public abstract class AspNetCore5AsmDataBlockingUser : RcmBase
    {
        public AspNetCore5AsmDataBlockingUser(AspNetCoreTestFixture fixture, ITestOutputHelper outputHelper, bool enableSecurity, string testName)
            : base(fixture, outputHelper, enableSecurity, testName: testName)
        {
            this.EnableDebugMode();
            SetEnvironmentVariable(ConfigurationKeys.DebugEnabled, "1");
        }

        [SkippableTheory]
        [InlineData("blocking-user", "/user")]
        [Trait("RunOnWindows", "True")]
        public async Task RunTest(string test, string url)
        {
            await TryStartApp();
            var agent = Fixture.Agent;
            using var logEntryWatcher = new LogEntryWatcher($"{LogFileNamePrefix}{Fixture.Process.ProcessName}*", LogDirectory);
            var sanitisedUrl = VerifyHelper.SanitisePathsForVerify(url);
            var settings = VerifyHelper.GetSpanVerifierSettings(parameters: new object[] { test, sanitisedUrl });
            var spanBeforeAsmData = await SendRequestsAsync(agent, url);

            var product = new AsmDataProduct();
            agent.SetupRcm(
                Output,
                new[]
                {
                    ((object)new Payload { RulesData = new[] { new RuleData { Id = "blocked_users", Type = "data_with_expiration", Data = new[] { new Data { Expiration = 5545453532, Value = "user3" } } } } }, "asm_data")
                },
                product.Name);

            var request1 = await agent.WaitRcmRequestAndReturnLast();
            if (EnableSecurity == true)
            {
                await logEntryWatcher.WaitForLogEntry($"1 {RulesUpdatedMessage()}", LogEntryWatcherTimeout);
            }
            else
            {
                await Task.Delay(1500);
            }

            var spanAfterAsmData = await SendRequestsAsync(agent, url);
            var spans = new List<MockSpan>();
            spans.AddRange(spanBeforeAsmData);
            spans.AddRange(spanAfterAsmData);
            await VerifySpans(spans.ToImmutableList(), settings, true);
        }
    }
}
#endif
