using System;
using System.Globalization;
using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;

namespace WorkerService5;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RMQ? _rabbitMq;
    private readonly MongoDbRepository _mongoDb;
    
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
        try
        {
            _rabbitMq = new RMQ(logger);
            _rabbitMq.OnMessageReceived += ProcessRabbitMqMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker failed to initialize RMQ. Stopping service startup.");
            throw;
        }
        
        try
        {
            _mongoDb = new MongoDbRepository(logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker failed to initialize MongoDB. Stopping service startup.");
            throw; 
        }

        
    }

    private async Task ProcessRabbitMqMessage(byte[] body, IModel channel)
    {
        try
        {
            var message = Encoding.UTF8.GetString(body);
            //_logger.LogInformation("RAW MESSAGE FROM RMQ: {msg}", message);

            var data = JObject.Parse(message);
            
             BsonDocument bsonData = BsonDocument.Parse(data.ToString());

            // 5. Simpan ke MongoDB
            await _mongoDb.InsertDocumentAsync(bsonData); 
            
            //_logger.LogInformation("Data RFID {id} berhasil disimpan ke MongoDB.", rfid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message and saving to MongoDB.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting consumption...");
        
        // Memulai konsumsi pesan
        _rabbitMq?.StartConsuming(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Worker stopping...");
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _rabbitMq?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
