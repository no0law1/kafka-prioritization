# Variables
COMPOSE_FILE=docker-compose.yml
TOPIC_NAME=communications
PARTITIONS=18
REPLICATION_FACTOR=1
BROKER=localhost:9092

# Commands
.PHONY: start stop create-topic delete-topic logs

# Start the Docker Compose environment
start:
	@echo "Starting Kafka and Zookeeper environment..."
	docker-compose -f $(COMPOSE_FILE) up -d
	@echo "Waiting for Kafka to start..."
	sleep 10
	@$(MAKE) create-topic

# Stop and remove the Docker Compose environment
stop:
	@echo "Stopping Kafka and Zookeeper environment..."
	docker-compose -f $(COMPOSE_FILE) down

# Create the Kafka topic
create-topic:
	@echo "Creating topic $(TOPIC_NAME) with $(PARTITIONS) partitions..."
	docker-compose -f $(COMPOSE_FILE) exec kafka \
	kafka-topics.sh --create --topic $(TOPIC_NAME) --bootstrap-server $(BROKER) \
	--partitions $(PARTITIONS) --replication-factor $(REPLICATION_FACTOR) || \
	echo "Topic $(TOPIC_NAME) already exists."

# Delete the Kafka topic
delete-topic:
	@echo "Deleting topic $(TOPIC_NAME)..."
	docker-compose -f $(COMPOSE_FILE) exec kafka \
	kafka-topics.sh --delete --topic $(TOPIC_NAME) --bootstrap-server $(BROKER) || \
	echo "Topic $(TOPIC_NAME) does not exist."

# View logs for Kafka and Zookeeper
logs:
	@echo "Showing logs for Kafka and Zookeeper..."
	docker-compose -f $(COMPOSE_FILE) logs -f

# Show the status of Docker containers
status:
	@echo "Showing Docker container status..."
	docker-compose -f $(COMPOSE_FILE) ps

# Default target: Display help information
help: 
	@echo "Available commands:"
	@echo "  make start            Start Kafka and Zookeeper environment, create topic if needed"
	@echo "  make stop             Stop Kafka and Zookeeper environment"
	@echo "  make create-topic     Create the Kafka topic (if not already created)"
	@echo "  make delete-topic     Delete the Kafka topic"
	@echo "  make logs             Show logs for Kafka and Zookeeper"
	@echo "  make status           Show the status of Docker containers"