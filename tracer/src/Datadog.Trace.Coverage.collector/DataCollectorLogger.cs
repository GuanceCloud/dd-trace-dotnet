// <copyright file="DataCollectorLogger.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using Datadog.Trace.Configuration;
using Datadog.Trace.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Datadog.Trace.Coverage.Collector
{
    internal class DataCollectorLogger : ICollectorLogger
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<DataCollectorLogger>();

        private readonly DataCollectionLogger _logger;
        private readonly bool _isDebugEnabled;
        private DataCollectionContext _collectionContext;

        public DataCollectorLogger(DataCollectionLogger logger, DataCollectionContext collectionContext)
        {
            _logger = logger;
            _collectionContext = collectionContext;

            var settings = GlobalSettings.FromDefaultSources();
            _isDebugEnabled = settings.DebugEnabled;
        }

        public void Error(string? text)
        {
            _logger.LogError(_collectionContext, text ?? string.Empty);
            Log.Error(text);
        }

        public void Error(Exception exception)
        {
            _logger.LogError(_collectionContext, exception);
            Log.Error(exception, exception.Message);
        }

        public void Error(Exception exception, string? text)
        {
            _logger.LogError(_collectionContext, text ?? string.Empty, exception);
            Log.Error(exception, text);
        }

        public void Warning(string? text)
        {
            _logger.LogWarning(_collectionContext, text ?? string.Empty);
            Log.Warning(text);
        }

        public void Debug(string? text)
        {
            if (_isDebugEnabled)
            {
                _logger.LogWarning(_collectionContext, text ?? string.Empty);
                Log.Debug(text);
            }
        }

        public void SetContext(DataCollectionContext collectionContext)
        {
            _collectionContext = collectionContext;
        }
    }
}
