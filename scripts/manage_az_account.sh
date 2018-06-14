#!/bin/bash

login_with_service_principal() {
  echo "Logging in with service principal..."
  az login --service-principal \
      --username $appid \
      --password $password \
      --tenant $tenant
}
