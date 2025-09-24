# Use official Node.js LTS image
FROM node:18-slim

# Create and set working directory
WORKDIR /app

# Copy package.json and package-lock.json first (for caching)
COPY package*.json ./

# Install dependencies
RUN npm install --production

# Copy the rest of the code
COPY . .

# Expose the port Cloud Run expects
EXPOSE 8080

# Start the app
CMD ["node", "index.js"]