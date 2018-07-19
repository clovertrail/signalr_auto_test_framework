namespace JenkinsScript
{
    class DogfoodSignalROps
    {
        public static void RegisterDogfoodCloud()
        {
            var cmd = ". ./az_signalr_service.sh; register_signalr_service_dogfood";
            ShellHelper.Bash(cmd, handleRes : true);
        }

        public static void UnregisterDogfoodCloud()
        {
            var cmd = ". ./az_signalr_service.sh; unregister_signalr_service_dogfood";
            ShellHelper.Bash(cmd, handleRes: true);
        }
    }
}
