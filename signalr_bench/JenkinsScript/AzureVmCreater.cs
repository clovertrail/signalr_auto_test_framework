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


        public void CreateAppServerVm()
        {
            var resourceGroup = CreateResourceGroup();

        }

        public void CreateAgentVms()
        {
            var resourceGroup = CreateResourceGroup();
            var avSet = CreateAvailabilitySet();
            var vNet = CreateVirtualNetwork();

            List<ICreatable<IVirtualMachine>> creatableVirtualMachines = new List<ICreatable<IVirtualMachine>>();

            var publicIpTasks = new List<Task<IPublicIPAddress>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                publicIpTasks.Add(CreatePublicIpAsync(i));
            }
            var publicIps = Task.WhenAll(publicIpTasks).GetAwaiter().GetResult();

            var nsgTasks = new List<Task<INetworkSecurityGroup>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                nsgTasks.Add(CreateNetworkSecurityGroup(i));
            }
            var nsgs = Task.WhenAll(nsgTasks).GetAwaiter().GetResult();

            var nicTasks = new List<Task<INetworkInterface>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                nicTasks.Add(CreateNetworkInterface(i, vNet, publicIps[i], nsgs[i]));
            }
            var nics = Task.WhenAll(nicTasks).GetAwaiter().GetResult();

            var vmTasks = new List<Task<IWithCreate>>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                vmTasks.Add(GenerateVmTempplate(i, nics[i], avSet));
            }

            var vms = Task.WhenAll(vmTasks).GetAwaiter().GetResult();
            creatableVirtualMachines.AddRange(vms);

            Console.WriteLine($"creating vms");
            var virtualMachines = _azure.VirtualMachines.Create(creatableVirtualMachines.ToArray());

            Console.WriteLine($"Setuping vms");
            var modifyLimitTasks = new List<Task>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                modifyLimitTasks.Add(ModifyLimit(i));
            }
            Task.WhenAll(modifyLimitTasks).Wait();

            var installDotnetTasks = new List<Task>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                installDotnetTasks.Add(InstallDotnet(i));
            }
            Task.WhenAll(installDotnetTasks).Wait();

            var sshdTasks = new List<Task>();
            for (var i = 0; i < _agentConfig.SlaveVmCount; i++)
            {
                sshdTasks.Add(ModifySshdAndRestart(i));
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
            var rg = _azure.ResourceGroups.GetByName(GroupName);
            if (rg != null)
            {
                Console.WriteLine($"Resource group {GroupName} existed");
                return rg;
            }

            return _azure.ResourceGroups.Define(GroupName)
                .WithRegion(Location)
                .Create();
        }

        public IAvailabilitySet CreateAvailabilitySet()
        {
            return _azure.AvailabilitySets.Define(AVSet)
                .WithRegion(Location)
                .WithExistingResourceGroup(GroupName)
                .WithSku(AvailabilitySetSkuTypes.Managed)
                .Create();
        }

        public INetwork CreateVirtualNetwork()
        {
            return _azure.Networks.Define(VNet)
                .WithRegion(Location)
                .WithExistingResourceGroup(GroupName)
                .WithAddressSpace("10.0.0.0/16")
                .WithSubnet(SubNet, "10.0.0.0/24")
                .Create();
        }

        public Task<IPublicIPAddress> CreatePublicIpAsync(int i)
        {
            return _azure.PublicIPAddresses.Define(PublicIpBase + Convert.ToString(i))
                    .WithRegion(Location)
                    .WithExistingResourceGroup(GroupName)
                    .WithLeafDomainLabel(PublicDnsBase + Convert.ToString(i))
                    .WithDynamicIP()
                    .CreateAsync();
        }

        public Task<INetworkSecurityGroup> CreateNetworkSecurityGroup(int i)
        {
            Console.WriteLine($"Creating network security group...");
            return _azure.NetworkSecurityGroups.Define(NsgBase + Convert.ToString(i))
                .WithRegion(Location)
                .WithExistingResourceGroup(GroupName)
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
                    .ToPort(_agentConfig.SshPort)
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

        public Task<INetworkInterface> CreateNetworkInterface(int i,  INetwork network, IPublicIPAddress publicIPAddress, INetworkSecurityGroup nsg)
        {
            Console.WriteLine("Creating network interface...");
            return _azure.NetworkInterfaces.Define(NicBase + Convert.ToString(i))
                .WithRegion(Location)
                .WithExistingResourceGroup(GroupName)
                .WithExistingPrimaryNetwork(network)
                .WithSubnet(SubNet)
                .WithPrimaryPrivateIPAddressDynamic()
                .WithExistingPrimaryPublicIPAddress(publicIPAddress)
                .WithExistingNetworkSecurityGroup(nsg)
                .CreateAsync();
        }

        public Task<IWithCreate> GenerateVmTempplate(int i, INetworkInterface networkInterface, IAvailabilitySet availabilitySet)
        {
            var vm = _azure.VirtualMachines.Define(VmNameBase + Convert.ToString(i))
                    .WithRegion(Location)
                    .WithExistingResourceGroup(GroupName)
                    .WithExistingPrimaryNetworkInterface(networkInterface)
                    .WithPopularLinuxImage(KnownLinuxVirtualMachineImage.UbuntuServer16_04_Lts)
                    .WithRootUsername(_agentConfig.SlaveVmName)
                    .WithRootPassword(_agentConfig.SlaveVmPassWord)
                    .WithSsh(_agentConfig.Ssh)
                    .WithComputerName(VmNameBase + Convert.ToString(i))
                    .WithExistingAvailabilitySet(availabilitySet)
                    .WithSize(VmSize);

            return Task.FromResult(vm);
        }

        public Task ModifyLimit(int i)
        {
            Console.WriteLine($"modify limits: {i}th");

            var errCode = 0;
            var res = "";
            var cmd = "";

            var domain = DomainName(i);

            cmd = $"echo '{_agentConfig.SlaveVmPassWord}' | sudo -S cp /etc/security/limits.conf /etc/security/limits.conf.bak";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, 22, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"cp /etc/security/limits.conf ~/limits.conf";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, 22, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"echo 'wanl    soft    nofile  655350\n' >> ~/limits.conf";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, 22, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"echo '{_agentConfig.SlaveVmPassWord}' | sudo -S mv ~/limits.conf /etc/security/limits.conf";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, 22, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            return Task.CompletedTask;
        }

        public Task InstallDotnet(int i)
        {
            Console.WriteLine($"install dotnet: {i}th");
            var errCode = 0;
            var res = "";
            var cmd = "";
            var port = 22;
            var domain = DomainName(i);

            cmd = $"wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, port, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"sudo dpkg -i packages-microsoft-prod.deb";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, port, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"sudo apt-get -y install apt-transport-https";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, port, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"sudo apt-get update";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, port, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"sudo apt-get -y install dotnet-sdk-2.1";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, port, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            return Task.CompletedTask;
        }

        public Task ModifySshdAndRestart(int i)
        {
            Console.WriteLine($"modify sshd_config: {i}th");

            var errCode = 0;
            var res = "";
            var cmd = "";
            var domain = DomainName(i);

            cmd = $"echo '{_agentConfig.SlaveVmPassWord}' | sudo -S cp   /etc/ssh/sshd_config  /etc/ssh/sshd_config.bak";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, 22, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"echo '{_agentConfig.SlaveVmPassWord}' | sudo -S sed -i 's/22/22222/g' /etc/ssh/sshd_config";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, 22, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            cmd = $"echo '{_agentConfig.SlaveVmPassWord}' | sudo -S service sshd restart";
            (errCode, res) = ShellHelper.RemoteBash(_agentConfig.SlaveVmName, domain, 22, _agentConfig.SlaveVmPassWord, cmd, handleRes: true);

            return Task.CompletedTask;
        }


        public string DomainName(int i)
        {
            return _agentConfig.Prefix + _rndNum + "DNS" + Convert.ToString(i) + "." + _agentConfig.Location.ToLower() + ".cloudapp.azure.com";
        }

        public string VNet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "VNet";
            }
        }

        public string VmNameBase
        {
            get
            {
                return _agentConfig.Prefix.ToLower() + _rndNum + "vm";
            }
        }

        public string SubNet
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "Subnet";
            }
        }

        public string NicBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "NIC";
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

        public string PublicDnsBase
        {
            get
            {
                return _agentConfig.Prefix + _rndNum + "DNS";
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

        public VirtualMachineSizeTypes VmSize
        {
            get
            {
                switch (_agentConfig.SlaveVmSize.ToLower())
                {
                    case "standardds1":
                        return VirtualMachineSizeTypes.StandardDS1;
                    case "d2v2":
                        return VirtualMachineSizeTypes.StandardD2V2;
                    default:
                        return VirtualMachineSizeTypes.StandardDS1;
                }
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
