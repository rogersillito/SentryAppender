using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;

namespace SharpRaven.Log4Net.Tests.Console {
	class Program {
		static void Main(string[] args)
		{
			XmlConfigurator.Configure();
			var log = LogManager.GetLogger(typeof (Program));

			try
			{
				throw new ApplicationException("Testing logging from console app");
			}
			catch (ApplicationException aEx)
			{
				log.Error("Custom message", aEx);
			}
			

		}
	}
}
