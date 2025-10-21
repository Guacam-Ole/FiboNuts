#!/bin/bash

# Start FibuNuts with Docker Compose (rebuild and deploy)

echo "🐳 Building and starting FibuNuts with Docker Compose..."
echo ""

docker-compose up -d --build

echo ""
echo "✅ Containers started!"
echo ""
echo "To view logs: docker-compose logs -f"
echo "To stop:      docker-compose down"
