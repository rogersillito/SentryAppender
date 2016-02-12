using System;

namespace SharpRaven.Log4Net.Extra
{
    public class EnvironmentExtra
    {
        public EnvironmentExtra()
        {
            MachineName = Environment.MachineName;
            Version = Environment.Version.ToString();
            OSVersion = Environment.OSVersion.ToString();
			try {
		        CommandLineArgs = Environment.GetCommandLineArgs();
			} catch (NotSupportedException) { } // The system does not support command-line arguments.
        }


        public string MachineName { get; private set; }

        public string Version { get; private set; }

        public string OSVersion { get; private set; }

		public string[] CommandLineArgs { get; private set; }
    }
}