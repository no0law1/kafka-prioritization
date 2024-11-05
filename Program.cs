namespace KafkaPrioritization;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

public class Program
{
    private const string TopicName = "communications";

    private const int TotalPartitions = 18;
    
    private const double HighPriorityPercentage = 0.5;
    private const double MediumPriorityPercentage = 0.3;
    private const double LowPriorityPercentage = 0.2;
    
    static async Task Main(string[] args)
    {
        // Create Consumers
        var highPriorityConsumer = CreateConsumer(Priority.High);
        var mediumPriorityConsumer = CreateConsumer(Priority.Medium);
        var lowPriorityConsumer = CreateConsumer(Priority.Low);

        var cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ConsumeMessages(highPriorityConsumer, cancellationTokenSource.Token));
        Task.Run(() => ConsumeMessages(mediumPriorityConsumer, cancellationTokenSource.Token));
        Task.Run(() => ConsumeMessages(lowPriorityConsumer, cancellationTokenSource.Token));

        // Create Producer
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = "localhost:9092"
        };

        var producer = new ProducerBuilder<string, string>(producerConfig)
            .SetPartitioner(TopicName, CustomPartitioner)
            .Build();

        
        // Console Application
        Console.WriteLine("Enter your message followed by priority (high, medium, low). Type 'exit' to quit.");
        while (true)
        {
            Console.Write("Priority (high, medium, low): ");
            var priorityInput = Console.ReadLine();
            if (!Enum.TryParse(priorityInput, true, out Priority priority))
            {
                Console.WriteLine("Invalid priority. Setting priority to 'low'.");
                priority = Priority.Low;
            }

            // Use priority as the key and the actual message as the value
            var kafkaMessage = new Message<string, string>
            {
                Key = priority.ToString(),
                Value = priority.ToString()
            };

            // Produce the message
            await producer.ProduceAsync(TopicName, kafkaMessage);

            Console.WriteLine($"Produced {priority} priority message: {priority.ToString()}");
        }
    }

    #region Consumer
    
    private static IConsumer<string, string> CreateConsumer(Priority priority)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
        };

        var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Assign(AssignPartitions(priority));

        return consumer;
    }

    private static List<TopicPartition> AssignPartitions(Priority priority)
    {
        List<TopicPartition> partitions = [];
        int highPartitionCount = (int)(TotalPartitions * HighPriorityPercentage);
        int mediumPartitionCount = (int)(TotalPartitions * MediumPriorityPercentage);
        int lowPartitionCount = (int)(TotalPartitions * LowPriorityPercentage);

        // Assign partitions based on priority
        switch (priority)
        {
            case Priority.High:
                for (int i = 0; i < highPartitionCount; i++)
                {
                    partitions.Add(new TopicPartition(TopicName, i));
                }
                break;

            case Priority.Medium:
                for (int i = highPartitionCount; i < highPartitionCount + mediumPartitionCount; i++)
                {
                    partitions.Add(new TopicPartition(TopicName, i));
                }
                break;

            case Priority.Low:
                for (int i = highPartitionCount + mediumPartitionCount; i < TotalPartitions; i++)
                {
                    partitions.Add(new TopicPartition(TopicName, i));
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(priority), "Invalid priority level.");
        }

        return partitions;
    }

    private static void ConsumeMessages(IConsumer<string, string> consumer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(cancellationToken);
                if (cr.IsPartitionEOF)
                    continue;

                // Process the message
                Console.WriteLine($"[{consumer.Name}] Consumed message: {cr.Message.Value} on partition {cr.Partition.Value}");
            }
            catch (ConsumeException e)
            {
                Console.WriteLine($"Error occurred: {e.Error.Reason}");
            }
        }

        consumer.Close();
        Console.WriteLine($"[{consumer.Name}] Closed");
    }

    #endregion

    #region Producer

    // Custom partitioner delegate function
    public static Partition CustomPartitioner(string topic, int partitionCount, ReadOnlySpan<byte> key, bool keyIsNull)
    {
        // Convert the key to a string to determine priority
        var keyString = Encoding.UTF8.GetString(key);
        if (!Enum.TryParse(keyString, true, out Priority priority))
            throw new ArgumentException("Invalid priority level in message key");

        var rand = new Random();
        int selectedPartition;

        // Partition selection based on percentage ranges
        switch (priority)
        {
            case Priority.High:
                selectedPartition = rand.Next(0, (int)(partitionCount * 0.5)); // First 50% of partitions
                break;
            case Priority.Medium:
                selectedPartition = rand.Next((int)(partitionCount * 0.5), (int)(partitionCount * 0.8)); // Next 30%
                break;
            case Priority.Low:
                selectedPartition = rand.Next((int)(partitionCount * 0.8), partitionCount); // Last 20%
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(priority), "Invalid priority level.");
        }

        return new Partition(selectedPartition);
    }

    #endregion
}
