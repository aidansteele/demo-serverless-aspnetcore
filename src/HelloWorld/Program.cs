using Amazon.Lambda.AspNetCoreServer;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace HelloWorld
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        // Microsoft.AspNetCore.Mvc.Testing wants the method below to have this exact name
        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.
                CreateDefaultBuilder(args).
                // maybe you'll want to add your own logging configuration here (e.g. Serilog, etc)
                UseStartup<Startup>();
        }
    }

    // On Lambda, Program.Main is **not** executed. Instead, Lambda loads this DLL
    // into its own app and uses the following class to translate from the Lambda
    // protocol to the standard ASP.Net Core web host and middleware pipeline.
    public class LambdaHandler : APIGatewayHttpApiV2ProxyFunction<Startup>
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return Program.CreateWebHostBuilder(null);
        }
    }
}