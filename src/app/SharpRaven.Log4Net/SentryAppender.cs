using System;
using System.Collections.Generic;
using System.Linq;

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
        private readonly IList<SentryTag> tagLayouts = new List<SentryTag>();

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

            var httpExtra = HttpExtra.GetHttpExtra();
            object extra;

            if (httpExtra != null)
            {
                extra = new
                {
                    Environment = new EnvironmentExtra(),
                    Http = httpExtra
                };
            }
            else
            {
                extra = new
                {
                    Environment = new EnvironmentExtra()
                };
            }

            var tags = tagLayouts.ToDictionary(t => t.Name, t => (t.Layout.Format(loggingEvent) ?? "").ToString());

            var exception = loggingEvent.ExceptionObject ?? loggingEvent.MessageObject as Exception;
            var level = Translate(loggingEvent.Level);

	        if (loggingEvent.ExceptionObject != null)
	        {
				// We should capture buth the exception and the message passed
		        RavenClient.CaptureException(exception,
			        new SentryMessage(loggingEvent.RenderedMessage),
			        level,
			        tags: tags,
			        extra: extra);
	        }
	        else if (loggingEvent.MessageObject is Exception)
	        {
				// We should capture the exception with no custom message
				RavenClient.CaptureException(loggingEvent.MessageObject as Exception,
					null,
					level,
					tags: tags,
					extra: extra);
	        }
	        else
	        {
				// Just capture message
		        var message = loggingEvent.RenderedMessage;

		        if (message != null)
		        {
			        RavenClient.CaptureMessage(message, level, tags, extra);
		        }
	        }
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