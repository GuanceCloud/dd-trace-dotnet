// <copyright file="DatabaseMonitoringPropagatorTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Agent;
using Datadog.Trace.Configuration;
using Datadog.Trace.DatabaseMonitoring;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Sampling;
using FluentAssertions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Tests.DatabaseMonitoring
{
    public class DatabaseMonitoringPropagatorTests
    {
        private readonly Tracer _tracer;
        private readonly Mock<IAgentWriter> _writerMock;

        public DatabaseMonitoringPropagatorTests(ITestOutputHelper output)
        {
            var settings = new TracerSettings();
            _writerMock = new Mock<IAgentWriter>();
            var samplerMock = new Mock<ITraceSampler>();

            _tracer = new Tracer(settings, _writerMock.Object, samplerMock.Object, scopeManager: null, statsd: null);
        }

        [Theory]
        [InlineData("string100", SamplingPriorityValues.UserKeep, "npgsql", "", "")]
        [InlineData("full", SamplingPriorityValues.UserKeep, "sqlite", "", "")]
        [InlineData("disabled", SamplingPriorityValues.UserKeep, "sqlclient", "", "")]
        [InlineData("Service", SamplingPriorityValues.AutoReject, "npgsql", "Test.Service-postgres", "/*dddbs='Test.Service-postgres',ddps='Test.Service'*/")]
        [InlineData("full", SamplingPriorityValues.UserReject, "sqlclient", "Test.Service-sql-server", "/*dddbs='Test.Service-sql-server',ddps='Test.Service'*/")]
        [InlineData("fUlL", SamplingPriorityValues.AutoKeep, "mysql", "Test.Service-mysql", "/*dddbs='Test.Service-mysql',ddps='Test.Service',traceparent='00-00000000000000006172c1c9a829c71c-05a5f7b5320d6e4d-01'*/")]
        public void ExpectedCommentInjected(string propagationMode, int? samplingPriority, string integration, string dbServiceName, string expectedComment)
        {
            DbmPropagationLevel dbmPropagationLevel;
            Enum.TryParse(propagationMode, true, out dbmPropagationLevel);

            IntegrationId integrationId;
            Enum.TryParse(integration, true, out integrationId);

            var context = new SpanContext(traceId: (TraceId)7021887840877922076, spanId: 407003698947780173, samplingPriority: samplingPriority, serviceName: dbServiceName, "origin");
            var returnedComment = DatabaseMonitoringPropagator.PropagateSpanData(dbmPropagationLevel, "Test.Service", context, integrationId);

            returnedComment.Should().Be(expectedComment);
        }

        [Theory]

        [InlineData("/*dddbs='Test.Service-mysql',dde='testing',ddps='Test.Service',ddpv='1.0.0'*/", "testing", "1.0.0")]
        [InlineData("/*dddbs='Test.Service-mysql',ddps='Test.Service',ddpv='1.0.0'*/", null, "1.0.0")]
        [InlineData("/*dddbs='Test.Service-mysql',dde='testing',ddps='Test.Service'*/", "testing", null)]
        [InlineData("/*dddbs='Test.Service-mysql',ddps='Test.Service'*/", null, null)]
        public void ExpectedTagsInjected(string expectedComment, string env = null, string version = null)
        {
            var span = _tracer.StartSpan(operationName: "mysql.query", parent: SpanContext.None, serviceName: "Test.Service-mysql", traceId: (TraceId)7021887840877922076, spanId: 407003698947780173);
            span.Context.TraceContext.Environment = env;
            span.Context.TraceContext.ServiceVersion = version;
            span.SetTraceSamplingPriority(SamplingPriority.AutoKeep);

            var returnedComment = DatabaseMonitoringPropagator.PropagateSpanData(DbmPropagationLevel.Service, "Test.Service", span.Context, IntegrationId.MySql);

            returnedComment.Should().Be(expectedComment);
        }

        [Theory]
        [InlineData("/*dddbs='Test.Service%20%21%23%24%25%26%27%28%29%2A%2B%2C%2F%3A%3B%3D%3F%40%5B%5D-mysql',dde='testing',ddps='Test.Service%20%21%23%24%25%26%27%28%29%2A%2B%2C%2F%3A%3B%3D%3F%40%5B%5D',ddpv='1.0.0'*/", "Test.Service !#$%&'()*+,/:;=?@[]", "testing", "1.0.0")]
        [InlineData("/*dddbs='Test.Service-mysql',dde='te%23%27sti%2F%2Ang',ddps='Test.Service',ddpv='1.0.0'*/", "Test.Service", "te#'sti/*ng", "1.0.0")]
        [InlineData("/*dddbs='Test.Service-mysql',dde='testing',ddps='Test.Service',ddpv='1.%2A0.0'*/", "Test.Service", "testing", "1.*0.0")]
        [InlineData("/*dddbs='Test.Service%20%21%23%24%25%26%27%28%29%2A%2B%2C%2F%3A%3B%3D%3F%40%5B%5D-mysql',dde='te%23%27sti%2F%2Ang',ddps='Test.Service%20%21%23%24%25%26%27%28%29%2A%2B%2C%2F%3A%3B%3D%3F%40%5B%5D',ddpv='1.%2A0.0'*/", "Test.Service !#$%&'()*+,/:;=?@[]", "te#'sti/*ng", "1.*0.0")]
        public void ExpectedTagsEncoded(string expectedComment, string service, string env, string version)
        {
            var span = _tracer.StartSpan(operationName: "mysql.query", parent: SpanContext.None, serviceName: $"{service}-mysql", traceId: (TraceId)7021887840877922076, spanId: 407003698947780173);
            span.Context.TraceContext.Environment = env;
            span.Context.TraceContext.ServiceVersion = version;
            span.SetTraceSamplingPriority(SamplingPriority.AutoKeep);

            var returnedComment = DatabaseMonitoringPropagator.PropagateSpanData(DbmPropagationLevel.Service, service, span.Context, IntegrationId.MySql);

            returnedComment.Should().Be(expectedComment);
        }
    }
}
