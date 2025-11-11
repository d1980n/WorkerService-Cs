using System;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json.Linq;

namespace WorkerService2
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ConnectionFactory _factory;
        //private readonly string _mongoUri = "mongodb://localhost:27017/";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            // konfigurasi koneksi RabbitMQ
            _factory = new ConnectionFactory()
            {
                HostName = "192.168.4.228",
                Port = 5672,
                VirtualHost = "/dan_vhost",
                UserName = "dan_user",
                Password = "123"
            };
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
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Received data from RMQ: {msg}", message);

                    // parse JSON
                    var data = JObject.Parse(message);

                    var nilai = data["nilai"]?.ToString();
                    if (!string.IsNullOrEmpty(nilai))
                    {
                        _logger.LogInformation("Extracted nilai: {val}", nilai);

                        //var document = new BsonDocument
                        //{
                        //    { "data", nilai }
                        //};
            
                        _logger.LogInformation("Data inserted to MongoDB: {val}", nilai);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid JSON format or missing 'nilai' field.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing message");
                }
            };

            channel.BasicConsume(
                queue: queueName,
                autoAck: true,
                consumer: consumer
            );

            _logger.LogInformation("Started consuming...");

            // loop agar service tetap jalan
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
