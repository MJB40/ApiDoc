

using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiDoc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHostBuilder hostBilder = CreateHostBuilder(args);
            IHost host = hostBilder.Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBilder = Host.CreateDefaultBuilder(args); 
            hostBilder
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
            return hostBilder;

            //return Host.CreateDefaultBuilder(args)
                //.ConfigureLogging((context, loggingBuilder)=> {
                //    loggingBuilder.AddFilter("System", LogLevel.Warning); //��д��System�����͵���־
                //    loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
                //    loggingBuilder.AddLog4Net();//ʹ��log4Net
                //})
                
        }
            
    }
}
