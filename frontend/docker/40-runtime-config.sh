#!/bin/sh
# Runs when the nginx container starts: writes config.js so the backend URL can be changed with API_URL, without rebuilding.
cat > /usr/share/nginx/html/config.js <<EOT
window.__APP_CONFIG__ = { API_URL: "${API_URL:-http://localhost:8080}" };
EOT
