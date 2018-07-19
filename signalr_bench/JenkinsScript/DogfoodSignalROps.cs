namespace JenkinsScript
{
    class DogfoodSignalROps
    {
        public static void RegisterDogfoodCloud(string extensionScriptsDir)
        {
            var cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; register_signalr_service_dogfood; cd -";
            ShellHelper.Bash(cmd, handleRes : true);
        }

        public static void UnregisterDogfoodCloud(string extensionScriptsDir)
        {
            var cmd = $"cd {extensionScriptsDir}; . ./az_signalr_service.sh; unregister_signalr_service_dogfood; cd -";
            ShellHelper.Bash(cmd, handleRes: true);
        }
    }
}
