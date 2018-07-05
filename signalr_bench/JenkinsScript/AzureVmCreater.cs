using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Compute.Fluent.VirtualMachine.Definition;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core.ResourceActions;
using System.Diagnostics;

namespace JenkinsScript
{
    public class BenchmarkVmBuilder
    {
        private AgentConfig _agentConfig;
        private IAzure _azure;
        private string _rndNum;

        public BenchmarkVmBuilder(AgentConfig agentConfig)
        {
            LoginAzure();

            _agentConfig = agentConfig;

            var rnd = new Random();
            _rndNum = Convert.ToString(rnd.Next(0, 100000) * rnd.Next(0, 100000));

        }

        public Task CreateAppServerVm()
        {
            return Task.Run(() => CreateAppServerVmCore());
        }

        public void CreateAppServerVmCore()
        {
            var sw = new Stopwatch();
            sw.Start();
            var resourceGroup = CreateResourceGroup();
            var vnet = CreateVirtualNetwork(AppSvrVnet, Location, GroupName, AppSvrSubNet);
            var publicIp = CreatePublicIpAsync(AppSvrPublicIpBase, Location, GroupName, AppSvrPublicDnsBase).GetAwaiter().GetResult();
            var nsg = CreateNetworkSecurityGroupAsync(AppSvrNsgBase, Location, GroupName, _agentConfig.SshPort).GetAwaiter().GetResult();
            var nic = CreateNetworkInterfaceAsync(AppSvrNicBase, Location, GroupName, AppSvrSubNet, vnet, publicIp, nsg).GetAwaiter().GetResult();
            var vmTemp = GenerateVmTemplateAsync(AppSvrVmNameBase, Location, GroupName, _agentConfig.AppSvrVmName, _agentConfig.AppSvrVmPassWord, _agentConfig.Ssh, AppSvrVmSize, nic).GetAwaiter().GetResult();
            vmTemp.Create();
            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");
            ModifyLimit(AppSvrDomainName(), _agentConfig.AppSvrVmName, _agentConfig.AppSvrVmPassWord).Wait();
            InstallDotnetAsync(AppSvrDomainName(), _agentConfig.AppSvrVmName, _agentConfig.AppSvrVmPassWord).Wait();
            ModifySshdAndRestart(AppSvrDomainName(), _agentConfig.AppSvrVmName, _agentConfig.AppSvrVmPassWord).Wait();
        }
        public Task CreateAgentVms()
        {
            return Task.Run(() => CreateAgentVmsCore());
        }

        public void CreateAgentVmsCore()
        {
            var sw = new Stopwatch();
            sw.Start();

            var resourceGroup = CreateResourceGroup();
            var avSet = CreateAvailabilitySet(AVSet, Location, GroupName);
            var vNet = CreateVirtualNetwork(VNet, Location, GroupName, SubNet);

            List<ICreatable<IVirtualMachine>> creatableVirtualMachines = new List<ICreatable<IVirtualMachine>>();

            var publicIpTasks = new List<Task<IPublicIPAddress>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                publicIpTasks.Add(CreatePublicIpAsync(PublicIpBase, Location, GroupName, PublicDnsBase, i));
            }
            var publicIps = Task.WhenAll(publicIpTasks).GetAwaiter().GetResult();

            var nsgTasks = new List<Task<INetworkSecurityGroup>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                nsgTasks.Add(CreateNetworkSecurityGroupAsync(NsgBase, Location, GroupName, _agentConfig.SshPort, i));
            }
            var nsgs = Task.WhenAll(nsgTasks).GetAwaiter().GetResult();

            var nicTasks = new List<Task<INetworkInterface>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                nicTasks.Add(CreateNetworkInterfaceAsync(NicBase, Location, GroupName, SubNet, vNet, publicIps[i], nsgs[i], i));
            }
            var nics = Task.WhenAll(nicTasks).GetAwaiter().GetResult();

            var vmTasks = new List<Task<IWithCreate>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                vmTasks.Add(GenerateVmTemplateAsync(VmNameBase, Location, GroupName, _agentConfig.SlaveVmName, _agentConfig.SlaveVmPassWord, _agentConfig.Ssh, SlaveVmSize, nics[i], avSet, i));
            }

            var vms = Task.WhenAll(vmTasks).GetAwaiter().GetResult();
            creatableVirtualMachines.AddRange(vms);

            Console.WriteLine($"creating vms");
            var virtualMachines = _azure.VirtualMachines.Create(creatableVirtualMachines.ToArray());

            sw.Stop();
            Util.Log($"create vm time: {sw.Elapsed.TotalMinutes} min");

            Console.WriteLine($"Setuping vms");
            var modifyLimitTasks = new List<Task>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                modifyLimitTasks.Add(ModifyLimit(SlaveDomainName(i), _agentConfig.SlaveVmName, _agentConfig.SlaveVmPassWord, i));
            }
            Task.WhenAll(modifyLimitTasks).Wait();

            var installDotnetTasks = new List<Task>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                installDotnetTasks.Add(InstallDotnetAsync(SlaveDomainName(i), _agentConfig.SlaveVmName, _agentConfig.SlaveVmPassWord, i));
            }
            Task.WhenAll(installDotnetTasks).Wait();

            var sshdTasks = new List<Task>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                sshdTasks.Add(ModifySshdAndRestart(SlaveDomainName(i), _agentConfig.SlaveVmName, _agentConfig.SlaveVmPassWord, i));
            }
            Task.WhenAll(sshdTasks).Wait();
        }

        public void LoginAzure()
        {
            var content = AzureBlobReader.ReadBlob("ServicePrincipalFileName");
            var sp = AzureBlobReader.ParseYaml<ServicePrincipalConfig>(content);

            // auth
            var credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(sp.ClientId, sp.ClientSecret, sp.TenantId, AzureEnvironment.AzureGlobalCloud);

            _azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();
        }

        public IResourceGroup CreateResourceGroup()
        {
            Console.WriteLine("Creating resource group...");
            if (_azure.ResourceGroups.Contain(GroupName))
            {
                Console.WriteLine($"Resource group {GroupName} existed");
                return null;
            }

            return _azure.ResourceGroups.Define(GroupName)
                .WithRegion(Location)
                .Create();
        }

        public IAvailabilitySet CreateAvailabilitySet(string name, Region location, string groupName)
        {
            return _azure.AvailabilitySets.Define(name)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithSku(AvailabilitySetSkuTypes.Managed)
                .Create();
        }

        public INetwork CreateVirtualNetwork(string name, Region location, string groupName, string subNetName)
        {
            return _azure.Networks.Define(name)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithAddressSpace("10.0.0.0/16")
                .WithSubnet(subNetName, "10.0.0.0/24")
                .Create();
        }

        public Task<IPublicIPAddress> CreatePublicIpAsync(string publicIpBase, Region location, string groupName, string publicDnsBase, int i=0)
        {
            return _azure.PublicIPAddresses.Define(publicIpBase + Convert.ToString(i))
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithLeafDomainLabel(publicDnsBase + Convert.ToString(i))
                    .WithDynamicIP()
                    .CreateAsync();
        }

        public Task<INetworkSecurityGroup> CreateNetworkSecurityGroupAsync(string nsgBase, Region location, string groupName, int sshPort, int i=0)
        {
            Console.WriteLine($"Creating network security group...");
            return _azure.NetworkSecurityGroups.Define(nsgBase + Convert.ToString(i))
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .DefineRule("SSH-PORT")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToPort(22)
                    .WithAnyProtocol()
                    .WithPriority(100)
                    .Attach()
                .DefineRule("NEW-SSH-PORT")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToPort(sshPort)
                    .WithAnyProtocol()
                    .WithPriority(101)
                    .Attach()
                .DefineRule("BENCHMARK-PORT")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToPort(7000)
                    .WithAnyProtocol()
                    .WithPriority(102)
                    .Attach()
                .DefineRule("RPC")
                    .AllowInbound()
                    .FromAnyAddress()
                    .FromAnyPort()
                    .ToAnyAddress()
                    .ToPort(5555)
                    .WithAnyProtocol()
                    .WithPriority(103)
                    .Attach()
                .CreateAsync();
        }

        public Task<INetworkInterface> CreateNetworkInterfaceAsync(string nicBase, Region location, string groupName, string subNet,  INetwork network, IPublicIPAddress publicIPAddress, INetworkSecurityGroup nsg, int i = 0)
        {
            Console.WriteLine("Creating network interface...");
            return _azure.NetworkInterfaces.Define(nicBase + Convert.ToString(i))
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithExistingPrimaryNetwork(network)
                .WithSubnet(subNet)
                .WithPrimaryPrivateIPAddressDynamic()
                .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                .WithExistingNetworkSecurityGroup(nsg)
                .CreateAsync();
        }

        public Task<IWithCreate> GenerateVmTemplateAsync(string vmNameBase, Region location, string groupName, string user, string password, string ssh, VirtualMachineSizeTypes vmSize, INetworkInterface networkInterface, IAvailabilitySet availabilitySet = null, int i = 0)
        {
            Console.WriteLine($"Create Vm Teamlate");
            var option = _azure.VirtualMachines.Define(vmNameBase + Convert.ToString(i))
                    .WithRegion(location)
                    .WithExistingResourceGroup(groupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithPopularLinuxImage(KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                    .WithRootUsername(user)
                    .WithRootPassword(password)
                    .WithSsh(ssh)
                    .WithComputerName(vmNameBase + Convert.ToString(i));

            IWithCreate creator = null;
            if (availabilitySet != null)
                creator = option.WithExistingAvailabilitySet(availabilitySet);
            else
                creator = option;
            var vm = creator.WithSize(vmSize);
            return Task.FromResult(vm);
        }

        public Task ModifyLimit(string domain, string user, string password, int i = 0)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"modify limits: {domain}");

                var errCode = 0;
                var res = "";
                var cmd = "";

                //var domain = SlaveDomainName(i);

                cmd = $"echo '{password}' | sudo -S cp /etc/security/limits.conf /etc/security/limits.conf.bak";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes: true);

                cmd = $"cp /etc/security/limits.conf ~/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes: true);

                cmd = $"echo 'wanl    soft    nofile  655350\n' >> ~/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes: true);

                cmd = $"echo '{password}' | sudo -S mv ~/limits.conf /etc/security/limits.conf";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes: true);

            });
            
        }

        public Task InstallDotnetAsync(string domain, string user, string password, int i = 0)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"install dotnet: {domain}");
                var errCode = 0;
                var res = "";
                var cmd = "";
                var port = 22;

                cmd = $"wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes: true);

                cmd = $"sudo dpkg -i packages-microsoft-prod.deb";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes: true);

                cmd = $"sudo apt-get -y install apt-transport-https";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes: true);

                cmd = $"sudo apt-get update";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes: true);

                cmd = $"sudo apt-get -y install dotnet-sdk-2.1";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, port, password, cmd, handleRes: true);
            });
            

        }

        public Task ModifySshdAndRestart(string domain, string user, string password, int i = 0)
        {
            return Task.Run(() =>
            {
                Console.WriteLine($"modify sshd_config: {domain}");

                var errCode = 0;
                var res = "";
                var cmd = "";

                cmd = $"echo '{password}' | sudo -S cp   /etc/ssh/sshd_config  /etc/ssh/sshd_config.bak";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes: true);

                cmd = $"echo '{password}' | sudo -S sed -i 's/22/22222/g' /etc/ssh/sshd_config";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes: true);

                cmd = $"echo '{password}' | sudo -S service sshd restart";
                (errCode, res) = ShellHelper.RemoteBash(user, domain, 22, password, cmd, handleRes: true);

            });
        }


        public string SlaveDomainName(int i)
        {
            return PublicDnsBase + Convert.ToString(i) + "." + _agentConfig.Location.ToLower() + ".cloudapp.azure.com";
        }

        public string AppSvrDomainName(int i=0)
        {
            return AppSvrPublicDnsBase + Convert.ToString(i) + "." + _agentConfig.Location.ToLower() + ".cloudapp.azure.com";

        }
        public string AppSvrVnet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrVNet";
            }
        }

        public string VNet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "VNet";
            }
        }

        public string AppSvrVmNameBase
        {
            get
            {
                return _agentConfig.Prefix.ToLower() + _rndNum + "appsvrvm";
            }

        }
        public string VmNameBase
        {
            get
            {
                return _agentConfig.Prefix.ToLower() + _rndNum + "vm";
            }
        }

        public string AppSvrSubNet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrSubnet";
            }
        }

        public string SubNet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "Subnet";
            }
        }

        public string AppSvrNicBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrNIC";
            }
        }

        public string NicBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "NIC";
            }
        }

        public string AppSvrNsgBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrNSG";
            }
        }


        public string NsgBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "NSG";
            }
        }


        public string PublicIpBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "PublicIP";
            }
        }

        public string AppSvrPublicIpBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrPublicIP";
            }
        }

        public string PublicDnsBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "DNS";
            }
        }

        public string AppSvrPublicDnsBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "AppSvrDNS";
            }
        }

        public string AVSet 
        {
            get
            {
                return _agentConfig.Prefix + "AVSet";
            }
        }

        public Region Location
        {
            get
            {
                Region location = null;
                switch (_agentConfig.Location.ToLower())
                {
                    case "useast":
                        location = Region.USEast;
                        break;
                    case "westus":
                        location = Region.USWest;
                        break;
                    default:
                        location = Region.USEast;
                        break;
                }

                return location;
            }
            
        }

        public VirtualMachineSizeTypes SlaveVmSize
        {
            get
            {
                return GetVmSize(_agentConfig.SlaveVmSize);
            }
            
        }

        public VirtualMachineSizeTypes AppSvrVmSize
        {
            get
            {
                return GetVmSize(_agentConfig.SlaveVmSize);
            }

        }

        private VirtualMachineSizeTypes GetVmSize(string vmSizeName)
        {
            switch (vmSizeName.ToLower())
            {
                case "standardds1":
                    return VirtualMachineSizeTypes.StandardDS1;
                case "d2v2":
                    return VirtualMachineSizeTypes.StandardD2V2;
                default:
                    return VirtualMachineSizeTypes.StandardDS1;
            }
        }


        public string GroupName
        {
            get
            {
                return _agentConfig.Prefix + "ResourceGroup";

            }
        }

    }
}
