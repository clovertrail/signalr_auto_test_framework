## Usage
1. Setup Jenkins
2. Create Jenkins Job
3. Setup Report Server
4. Prepare Configration Files


## Setup Jenkins


## Create Jenkins Job
### Jenkins Job Configuation
String Parameter:
* AzureStorageConnectionString
* ConfigBlobContainerName
* JobConfigFileName
* AgentConfigFileName
* SignalrConfigFileName
* ServicePrincipalFileName

## Setup Report Server
1. clone repe:

## Prepare Configration Files

All Configuration files should be saved in Azure Blob.
AzureStorageConnectionString, ConfigBlobContainerName, AgentConfigFileName and JobConfigFileName, SignalrConfigFileName and ServicePrincipalFileName should be provided in Jenkins Job Configuration.

### Agent config

```
# RPC master url
master: xxx
# Port for RPC
rpcPort: 5555
# Port for SSH
sshPort: 22222
# Repo that contains benchmarks and other tools 
repo: https://github.com/realwanpengli/signalr_auto_test_framework.git

# User name and password for all VMs
user: xxxxx
password: xxxxx

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


### Job Config

```
# Common configuration
duration: 20
interval: 1
pipeline: [createConn, startConn, scenario, stopConn, disposeConn]
serverUrl: http://wanlsignalrautotestappserver.eastus.cloudapp.azure.com:5000/signalrbench
#serverUrl: http://localhost


# Benchmark Matrix
hubProtocolList: [json]
transportTypeList: [websockets]
# scenarioList: [echo, broadcast, mix, group]
scenarioList: [echo]
signalrUnit: [unit1]
#serviceTypeList: [selfhost]

# SignalR Unit Based Configuration
connectionIncreaseStep: 
  echo: [100]
  broadcast: [5]
connectionBase: 
  echo: [500]
  broadcast: [5]

# Special Configuration for Scenarios
mix: 
 echo:
  connections: 1000
 broadcast:
  connections: 8000
group:
 groupNum: 10

```

### SignalR Configuration
```
```

### Service Principal Configuration
```
```


## Todo
* mix scenario
* make sure signalr is available
* retry when reading azure blob fails
* retry when login azure fails
