# Manage SignalR Service in Dogfood environment
commands examples:

Register Dogfood cloud
`dotnet run --ExtensionScriptsDir "/home/hongjiang/hz_signalr_auto_test_framework/signalr_bench/JenkinsScript/ExternalScripts" -S "RegisterDogfoodCloud"`

Azure lgoin and create SignalR service
`dotnet run --ExtensionScriptsDir "/home/hongjiang/hz_signalr_auto_test_framework/signalr_bench/JenkinsScript/ExternalScripts" -S "CreateDogfoodSignalr"`

Delete SignalR service
`dotnet run --ExtensionScriptsDir "/home/hongjiang/hz_signalr_auto_test_framework/signalr_bench/JenkinsScript/ExternalScripts" -S "DeleteDogfoodSignalr" --ResourceGroup group26825820 --SignalRService sr26825820`

Unregister Dogfood cloud
`dotnet run --ExtensionScriptsDir "/home/hongjiang/hz_signalr_auto_test_framework/signalr_bench/JenkinsScript/ExternalScripts" -S "UnregisterDogfoodCloud"`

