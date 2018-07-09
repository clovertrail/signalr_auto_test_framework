## Usage
1. Setup Jenkins
2. Create Jenkins Job
3. Setup Report Server
4. Prepare Configration File

### Setup Jenkins

### Jenkins Job Configuation
String Parameter:
* AzureStorageConnectionString
* ConfigBlobContainerName
* JobConfigFileName
* AgentConfigFileName
* SignalrConfigFileName
* ServicePrincipalFileName

### Configuation Files
* Agent config

```
# RPC master url
master: localhost
# Port for RPC
rpcPort: 5555
# Port for SSH
sshPort: 22222
# Repo that contains benchmarks and other tools 
repo: https://github.com/realwanpengli/signalr_auto_test_framework.git

# User name and password for all VMs
user: xxxxx
password: xxxxx!

# Config for creating VMs
prefix: xxxxx
location: xxxxx

# slaves
slaveVmSize: xxxxx
slaveVmCount: 5

# app server
appSvrVmSize: d2v2

# service 
svcVmPassWord: xxxxx

# public key 
ssh: xxxxx



```

* Job Config



## Todo
* mix scenario
* rewrite html
* make sure signalr is available
* retry when reading azure blob fails
* retry when login azure fails
