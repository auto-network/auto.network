#!/bin/bash

# Source NVM to get npm/npx in PATH
export NVM_DIR="$HOME/.nvm"
[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"

# Kill everything
pkill -f dotnet || true
pkill -f tailwindcss || true
sleep 2

# Start AutoHost
cd /home/jeremy/auto/AutoHost
dotnet run --urls http://localhost:5050 &

# Start TailwindCSS
cd /home/jeremy/auto/AutoWeb
npx @tailwindcss/cli -i ./wwwroot/css/input.css -o ./wwwroot/css/app.css --watch &

# Keep script running
wait