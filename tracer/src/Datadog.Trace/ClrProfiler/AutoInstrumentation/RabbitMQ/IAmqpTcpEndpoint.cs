// <copyright file="IAmqpTcpEndpoint.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.DuckTyping;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.RabbitMQ;

/// <summary>
/// AmqpTcpEndpoint interface for duck typing
/// </summary>
[DuckCopy]
internal struct IAmqpTcpEndpoint
{
    /// <summary>
    /// Gets the hostname of this AmqpTcpEndpoint.
    /// </summary>
    [Duck]
    public string HostName;
}
