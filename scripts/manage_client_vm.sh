#!/bin/bash

openPort() {
    echo "Setting VM network for VM $vmNameBase$i..."
    echo "Opening port $vmPort1 with priority 900"
    az vm open-port --port $vmPort1 --resource-group $vmResourceGroup --name $vmNameBase$i --priority 900
    echo "Opening port $vmPort2 with priority 901"
    az vm open-port --port $vmPort2 --resource-group $vmResourceGroup --name $vmNameBase$i --priority 901
}

changeSshdPort() {
    sshpass -p $vmPassword ssh -p 22  -o StrictHostKeyChecking=no  ${vmAdminName}@${vmResourceGroup}${i}.${vmLocation}.cloudapp.azure.com "
        echo '${vmPassword}' | sudo -S cp   /etc/ssh/sshd_config  /etc/ssh/sshd_config.bak
        echo '${vmPassword}' | sudo -S sed -i 's/22/22222/g' /etc/ssh/sshd_config
        echo '${vmPassword}' | sudo -S service sshd restart"
}

createResourceGroup() {
	 . ./cfg.sh
	isGroupExist=$(az group exists --name $vmResourceGroup 2>&1)
	if [ "$isGroupExist" == "true" ]; then 
		echo "Group $vmResourceGroup exists."
	else
		echo "Creating resource group $vmResourceGroup"
		az group create --name $vmResourceGroup --location $vmLocation
		isGroupExist=$(az group exists --name $vmResourceGroup 2>&1)
        echo "isGroupExist = $isGroupExist"
		if [ "$isGroupExist" == "true" ]; then 
			echo "Group $vmResourceGroup Successfully created."
		else
			echo "Group $vmResourceGroup fail to create."
		fi
	fi
}

createVM() {
	isGroupExist=$(az group exists --name $vmResourceGroup 2>&1)
	if [ "$isGroupExist" == "true" ]; then 
        echo "Creating VM..."
      	az vm create \
            --resource-group $vmResourceGroup \
    		--name $vmNameBase$i \
    		--image $vmImage \
            --size $vmSize \
    		--location $vmLocation \
    		--admin-username $vmAdminName \
    		--admin-password $vmPassword \
    		--generate-ssh-keys \
            --public-ip-address-dns-name ${vmResourceGroup,,}$i
        isVmExist=$(az group exists --name $vmNameBase$i 2>&1)

    else
        echo "Group $vmResourceGroup doesn't exist."
    fi
}

createVMs() {
    . ./cfg.sh
    for (( i=0;i<$vmCount;i++)); do
      createVM
    done
}

setVmsNetwork() {
    . ./cfg.sh
    for (( i=0;i<$vmCount;i++)); do
      openPort
    done
}

deleteVM() {
    echo "Deleting VM..."
}

changeVmsSshdPort() {
    . ./cfg.sh
    for (( i=0;i<$vmCount;i++)); do
      changeSshdPort
    done
}