using Beamable.Server;
using System.Threading.Tasks;

namespace Beamable.TunaTacoGull
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="TunaTacoGull"/> service.
		/// </summary>
		public static async Task Main()
		{
			// inject data from the CLI.
			await MicroserviceBootstrapper.Prepare<TunaTacoGull>();
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<TunaTacoGull>();
		}
	}
}
