using Topshelf;

namespace Normet.Cloud.Relay
{
    class Program
    {

        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<RelayServiceHost>(s =>
                {
                    s.ConstructUsing(name => new RelayServiceHost());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });

                x.StartAutomatically();
                x.RunAsLocalSystem();
                x.SetDescription("Normet cloud relay service to enable access to Sovelia and other on-premise applications' data from cloud based services.");
                x.SetDisplayName("Normet Cloud-Relay Service");
                x.SetServiceName("Normet-Cloud-Relay");
            });
        }
    }
}
