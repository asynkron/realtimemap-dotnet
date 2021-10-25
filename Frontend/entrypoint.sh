#!/bin/bash

set -ue

echo "Hello from entrypoint"

CONFIG_FILE=/usr/share/nginx/html/config.js

sed -i 's|VUE_APP_BACKEND_URL|'${VUE_APP_BACKEND_URL}'|g' $CONFIG_FILE
sed -i 's|VUE_APP_MAPBOX_TOKEN|'${VUE_APP_MAPBOX_TOKEN}'|g' $CONFIG_FILE

# call original nginx docker entrypoint 
exec /docker-entrypoint.sh "$@"