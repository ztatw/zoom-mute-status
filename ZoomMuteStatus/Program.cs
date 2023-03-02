using Topshelf;

namespace ZoomMuteStatus
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var rc = HostFactory.Run(x =>
            {
                x.Service<StateMonitor>(s =>
                {
                    s.ConstructUsing(name => new StateMonitor());
                    s.WhenStarted(sm => sm.Start());
                    s.WhenStopped(sm => sm.Stop());
                });
                x.RunAsLocalService();
                x.SetDisplayName("ZoomMuteStateMonitor");
                x.SetDescription("Monitor zoom mute state, turn on/off blink1");
                x.SetServiceName("ZoomMuteStateMonitor");
            });
        }
    }
}