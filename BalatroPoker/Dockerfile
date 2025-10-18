# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy all source files
COPY . ./

# Build the application
RUN dotnet publish -c Release -o out

# Use nginx to serve the static files
FROM nginx:alpine

# Copy the published files to nginx web directory
COPY --from=build /app/out/wwwroot /usr/share/nginx/html

# Copy custom nginx configuration if needed
COPY nginx.conf /etc/nginx/nginx.conf

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]