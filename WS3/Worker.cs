using System;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using WorkerService3;

namespace WorkerService3;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ConnectionFactory _factory;
    private MongoClient _mongoClient;
    private IMongoCollection<BsonDocument> _collection;
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;

        // konfigurasi koneksi RabbitMQ
        _factory = new ConnectionFactory()
        {
            HostName = "192.168.1.8",
            Port = 5672,
            VirtualHost = "/dan_vhost",
            UserName = "dan_user",
            Password = "123"
        };

        // konfigurasi koneksi MongoDB
        _mongoClient = new MongoClient("mongodb://192.168.1.7:27017");
        var database = _mongoClient.GetDatabase("testdb");
        _collection = database.GetCollection<BsonDocument>("tiga");
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var connection = _factory.CreateConnection();
        using var channel = connection.CreateModel();

        // deklarasi queue temporary (anonymous)
        var queueName = channel.QueueDeclare().QueueName;

        // bind ke exchange amq.topic
        channel.QueueBind(
            queue: queueName,
            exchange: "amq.topic",
            routingKey: "dan_routing_key"
        );

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            Task.Run(() =>
            {
                try
                {
                    //ambil data dulu dari RMQ
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Received data from RMQ: {msg}", message);

                    // parse JSON
                    var data = JObject.Parse(message);

                    var nilai = data["nilai"]?.ToString();

                    if (!string.IsNullOrEmpty(nilai))
                    {
                        _logger.LogInformation("Extracted nilai: {val}", nilai);
                        // 1. Buat BsonDocument
                        var document = new BsonDocument
                        {
                            { "nilai_sensor", nilai }, // Key sesuai data yang diterima
                            { "timestamp", DateTime.UtcNow } // Tambahkan timestamp
                        };

                        // 2. Simpan ke MongoDB (menggunakan await/async di dalam Task.Run)
                        _collection.InsertOneAsync(document).Wait(); // Menggunakan .Wait() karena di dalam Task.Run sync block
                        
                        _logger.LogInformation("Data berhasil disimpan ke MongoDB: {val}", nilai);

                    }
                    else
                    {
                        _logger.LogWarning("Invalid JSON format or missing 'nilai' field.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{time}: Error processing message from RMQ.", DateTimeOffset.Now);
                }
            }, stoppingToken);
        };

        channel.BasicConsume(
                queue: queueName,
                autoAck: true,
                consumer: consumer
        );

        _logger.LogInformation("Started consuming...");

        while (!stoppingToken.IsCancellationRequested)
        {
            //if (_logger.IsEnabled(LogLevel.Information))
            //{
            //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
           // }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
