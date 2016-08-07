using System;
using System.Collections.Generic;

using log4net.Layout;
using log4net.Util;

using SharpRaven.Data;
using SharpRaven.Log4Net.Extra;

using log4net.Appender;
using log4net.Core;

namespace SharpRaven.Log4Net
{
    public class SentryTag
    {
        public string Name { get; set; }
        public IRawLayout Layout { get; set; }
    }

    public class SentryAppender : AppenderSkeleton
    {
        protected IRavenClient RavenClient;
        public string DSN { get; set; }
        public string Logger { get; set; }
        private readonly List<SentryTag> tagLayouts = new List<SentryTag>();

        public void AddTag(SentryTag tag)
        {
            tagLayouts.Add(tag);
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (RavenClient == null)
            {
                RavenClient = new RavenClient(DSN)
                {
                    Logger = Logger,

                    // If something goes wrong when sending the event to Sentry, make sure this is written to log4net's internal
                    // log. See <add key="log4net.Internal.Debug" value="true"/>
                    ErrorOnCapture = ex => LogLog.Error(typeof (SentryAppender), "[" + Name + "] " + ex.Message, ex)
                };
            }

            SentryEvent sentryEvent = null;

            if (loggingEvent.ExceptionObject != null)
            {
                // We should capture both the exception and the message passed
                sentryEvent = new SentryEvent(loggingEvent.ExceptionObject);
                sentryEvent.Message = loggingEvent.RenderedMessage;
            }
            else if (loggingEvent.MessageObject is Exception)
            {
                // We should capture the exception with no custom message
                sentryEvent = new SentryEvent(loggingEvent.MessageObject as Exception);
            }
            else
            {
                // Just capture message
                sentryEvent = new SentryEvent(loggingEvent.RenderedMessage);
            }

            // Assign error level
            sentryEvent.Level = Translate(loggingEvent.Level);

            // Format and add tags
            tagLayouts.ForEach(tl => sentryEvent.Tags.Add(tl.Name, (tl.Layout.Format(loggingEvent) ?? String.Empty).ToString()));

            // Add extra data with or without HTTP-related fields
            var httpExtra = HttpExtra.GetHttpExtra();

            if (httpExtra != null)
            {
                sentryEvent.Extra = new
                {
                    Environment = new EnvironmentExtra(),
                    Http = httpExtra
                };
            }
            else
            {
                sentryEvent.Extra = new
                {
                    Environment = new EnvironmentExtra()
                };
            }

            RavenClient.Capture(sentryEvent);
        }

        public static ErrorLevel Translate(Level level)
        {
            switch (level.DisplayName)
            {
                case "WARN":
                    return ErrorLevel.Warning;

                case "NOTICE":
                    return ErrorLevel.Info;
            }

            ErrorLevel errorLevel;

            return !Enum.TryParse(level.DisplayName, true, out errorLevel)
                       ? ErrorLevel.Error
                       : errorLevel;
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
            {
                Append(loggingEvent);
            }
        }
    }
}
