#!/bin/bash

configNginx() {
    . ./cfg.sh
    sshpass -p "${vmPassword}" ssh -p 22  -o StrictHostKeyChecking=no  ${vmAdminName}@${bsDns} "

echo '${bsPassword}' | sudo -S cat > signalr <<EOF
server {
        listen 8000 default_server;
        listen [::]:8000 default_server;


        root /home/${vmAdminName}/NginxRoot/;

        # Add index.php to the list if you are using PHP
        index index.html index.htm index.nginx-debian.html;

        server_name _;

        location / {
                # First attempt to serve request as file, then
                # as directory, then fall back to displaying a 404.
                try_files $uri $uri/ =404;
        }
}
EOF
echo '${bsPassword}' | sudo -S mv signalr /etc/nginx/sites-enabled/signalr
echo '${bsPassword}' | sudo -S nginx -s reload"
}

changeBsSshdPort() {
    sshpass -p $bsPassword ssh -p 22  -o StrictHostKeyChecking=no  ${bsAdminName}@${bsDns} "
        echo '${bsPassword}' | sudo -S cp   /etc/ssh/sshd_config  /etc/ssh/sshd_config.bak
        echo '${bsPassword}' | sudo -S sed -i 's/22/22222/g' /etc/ssh/sshd_config
        echo '${bsPassword}' | sudo -S service sshd restart
    "
}

createBsResourceGroup() {
    . ./cfg.sh
    isGroupExist=$(az group exists --name $bsResourceGroup 2>&1)
    if [ "$isGroupExist" == "true" ]; then 
        echo "Group $bsResourceGroup exists."
    else
        echo "Creating resource group $bsResourceGroup"
        az group create --name $bsResourceGroup --location $bsLocation
        isGroupExist=$(az group exists --name $bsResourceGroup 2>&1)
        if [ "$isGroupExist" == "true" ]; then 
            echo "Group $bsResourceGroup Successfully created."
        else
            echo "Group $bsResourceGroup fail to create."
        fi
    fi
}

createBsVM() {
    . ./cfg.sh
    isGroupExist=$(az group exists --name $bsResourceGroup 2>&1)
    if [ "$isGroupExist" == "true" ]; then 
        echo "Creating VM..."
        az vm create \
            --resource-group $bsResourceGroup \
            --name $bsName \
            --image $bsImage \
            --size $bsSize \
            --location $bsLocation \
            --admin-username $bsAdminName \
            --admin-password $bsPassword \
            --generate-ssh-keys \
            --public-ip-address-dns-name ${bsResourceGroup,,}$i
        isVmExist=$(az group exists --name $bsName 2>&1)

    else
        echo "Group $bsResourceGroup doesn't exist."
    fi
}
