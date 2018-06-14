#!/bin/bash
let randomNum=$RANDOM*$RANDOM

login_with_service_principal() {
  echo "Logging in with service principal..."
  az login --service-principal --username $appId --password $password --tenant $tenantId
}

create_signalr_service() {
  echo "rg $resourceGroupName$randomNum loc $location"
  # Create resource group 
  echo "Creating resource group..."
  az group create --name $resourceGroupName$randomNum --location $location

  # Create the Azure SignalR Service resource and query the hostName
  echo "Creating Azure SignalR service $signalRSvcName$randomNum..."
  signalRhostname=$(az signalr create \
    --name $signalRSvcName$randomNum \
    --resource-group $resourceGroupName$randomNum \
    --sku $sku \
    --unit-count $unitCount \
    --query hostName \
    -o tsv)

  # Get the SignalR primary key 
  echo "Get SignalR connection string..."
  signalRprimarykey=$(az signalr key list --name $signalRSvcName$randomNum \
    --resource-group $resourceGroupName$randomNum --query primaryKey -o tsv)

  # Form the connection string for use in your application
  connstring="Endpoint=https://$signalRhostname;AccessKey=$signalRprimarykey;"
  echo "Connection String: $connstring" 

}

delete_signalr_service() {
  echo "Deleting SignalR service in resource group $resourceGroupName$randomNum..."
  az group delete --name $resourceGroupName$randomNum
}



while :
do
  case "$1" in
    -a | --appid)
      if [ $# -ne 0 ]; then
        appId="$2"
      fi
      shift 2
      ;;
    -p | --password)
      if [ $# -ne 0 ]; then
        password="$2"
      fi
      shift 2
      ;;
    -t | --tenantid)
      if [ $# -ne 0 ]; then
        tenantId="$2"
      fi
      shift 2
      ;;
    -g | --resourcegroup)
      if [ $# -ne 0 ]; then
        resourceGroupName="$2"
      fi
      shift 2
      ;;
    -n | --servicename)
      if [ $# -ne 0 ]; then
        signalRSvcName="$2"
      fi
      shift 2
      ;;
    -s | --sku)
      if [ $# -ne 0 ]; then
        sku="$2"
      fi
      shift 2
      ;;
    -u | --unitcount)
      if [ $# -ne 0 ]; then
        unitCount="$2"
      fi
      shift 2
      ;;
    -l | --location)
      if [ $# -ne 0 ]; then
        location="$2"
      fi
      shift 2
      ;;
    --) # End of all options
      shift
      break
      ;;
    -*)
      echo "Error: Unknown option: $1" >&2
      ## or call function display_help
      exit 1 
      ;;
    *)  # No more options
      break
      ;;
  esac
done

case "$1" in
  login)
    login_with_service_principal
    ;;
  create)
    create_signalr_service
    ;;
  delete)
    delete_signalr_service
    ;;
esac

exit 1