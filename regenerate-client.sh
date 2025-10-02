#!/bin/bash
# Script to cleanly regenerate the NSwag client from AutoHost swagger

# USE UNIQUE PORT 7777 for swagger generation to avoid conflicts
SWAGGER_PORT=7777
SWAGGER_URL="http://localhost:${SWAGGER_PORT}"

echo "Regenerating AutoHost client..."
echo "Using dedicated port ${SWAGGER_PORT} for swagger generation"

# 1. Kill any existing processes on our swagger port
lsof -ti :${SWAGGER_PORT} | xargs -r kill -9 2>/dev/null

# 2. Build AutoHost to ensure latest changes
echo "Building AutoHost..."
dotnet build /home/jeremy/auto/AutoHost/AutoHost.csproj || exit 1

# 3. Start AutoHost temporarily on unique port to serve swagger
echo "Starting AutoHost on port ${SWAGGER_PORT} to generate swagger..."
dotnet run --project /home/jeremy/auto/AutoHost/AutoHost.csproj --urls ${SWAGGER_URL} > /tmp/autohost-swagger.log 2>&1 &
AUTOHOST_PID=$!

# 4. Wait for AutoHost to be ready
echo "Waiting for AutoHost to start on port ${SWAGGER_PORT}..."
for i in {1..10}; do
    if curl -s ${SWAGGER_URL}/swagger/v1/swagger.json > /dev/null 2>&1; then
        echo "AutoHost is ready on port ${SWAGGER_PORT}"
        break
    fi
    if [ $i -eq 10 ]; then
        echo "ERROR: AutoHost failed to start. Check /tmp/autohost-swagger.log"
        kill $AUTOHOST_PID 2>/dev/null
        exit 1
    fi
    sleep 1
done

# 5. Update NSwag config to use our unique port
NSWAG_CONFIG="/home/jeremy/auto/AutoWeb/AutoWeb.csproj"
# Temporarily update the swagger URL in the project file
sed -i.bak "s|http://localhost:5050/swagger/v1/swagger.json|${SWAGGER_URL}/swagger/v1/swagger.json|g" $NSWAG_CONFIG

# 6. Remove old client file to force regeneration
echo "Removing old client..."
rm -f /home/jeremy/auto/AutoWeb/Client/AutoHostClient.cs

# 7. Build AutoWeb which will regenerate client via NSwag
echo "Regenerating client via NSwag from ${SWAGGER_URL}..."
REGENERATE_CLIENT=true dotnet build /home/jeremy/auto/AutoWeb/AutoWeb.csproj

# 8. Restore original NSwag config
mv ${NSWAG_CONFIG}.bak ${NSWAG_CONFIG}

# 9. Kill the temporary AutoHost
kill $AUTOHOST_PID 2>/dev/null

echo "Client regeneration complete!"