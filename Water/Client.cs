namespace Clients;

using Microsoft.Extensions.DependencyInjection;

using SimpleRpc.Serialization.Hyperion;
using SimpleRpc.Transports;
using SimpleRpc.Transports.Http.Client;

using NLog;

using Services;

/// <summary>
/// Client example.
/// </summary>
class Client
{
    /// <summary>
	/// Logger for this class.
	/// </summary>
	Logger mLog = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Configures logging subsystem.
    /// </summary>
    private void ConfigureLogging()
    {
        var config = new NLog.Config.LoggingConfiguration();

        var console =
            new NLog.Targets.ConsoleTarget("console")
            {
                Layout = @"${date:format=HH\:mm\:ss}|${level}| ${message} ${exception}"
            };
        config.AddTarget(console);
        config.AddRuleForAllLevels(console);

        LogManager.Configuration = config;
    }

    /// <summary>
    /// Program body.
    /// </summary>
    private void Run()
    {
        //configure logging
        ConfigureLogging();

        //initialize random number generator
        var rnd = new Random();

        //run everythin in a loop to recover from connection errors
        while (true)
        {
            try
            {
                //connect to the server, get service client proxy
                var sc = new ServiceCollection();
                sc
                    .AddSimpleRpcClient(
                        "FarmService", //must be same as on line 86
                        new HttpClientTransportOptions
                        {
                            Url = "http://127.0.0.1:5000/simplerpc",
                            Serializer = "HyperionMessageSerializer"
                        }
                    )
                    .AddSimpleRpcHyperionSerializer();

                sc.AddSimpleRpcProxy<IFarmService>("FarmService"); //must be same as on line 77

                var sp = sc.BuildServiceProvider();

                var api = sp.GetRequiredService<IFarmService>();
                var sum = api.SubmitWater(2);
                Console.WriteLine($"Submitted food {sum.IsAccepted}");
            }
            catch (Exception e)
            {
                //log whatever exception to console
                mLog.Warn(e, "Unhandled exception caught. Will restart main loop.");

                //prevent console spamming
                Thread.Sleep(2000);
            }
        }
    }
    
    /// <summary>
	/// Program entry point.
	/// </summary>
	/// <param name="args">Command line arguments.</param>
	static void Main(string[] args)
	{
		var self = new Client();
		self.Run();
	}
}