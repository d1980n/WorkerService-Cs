using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WorkerService4;

namespace WorkerService4;

// Delegate untuk menangani pesan yang diterima
// Parameter: byte[] body (data pesan), IModel channel (untuk potensi Acknowledge manual)
public delegate Task MessageReceivedHandler(byte[] body, IModel channel);

public class RMQ : IDisposable
{
    private readonly ILogger _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ConnectionFactory _factory;
    private readonly string _queueName;

    // Event untuk dipicu ketika pesan diterima
    public event MessageReceivedHandler? OnMessageReceived;

    public RMQ(ILogger logger) // Hapus semua parameter konfigurasi
    {
        _logger = logger;
        // PENGGUNAAN CONFIG BARU
        _factory = new ConnectionFactory()
        {
            HostName = AppConfig.RmqHostName,
            Port = AppConfig.RmqPort,
            VirtualHost = AppConfig.RmqVirtualHost,
            UserName = AppConfig.RmqUserName,
            Password = AppConfig.RmqPassword
        };

        try
        {
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            _queueName = _channel.QueueDeclare().QueueName;

            _channel.QueueBind(
                queue: _queueName,
                exchange: AppConfig.RmqExchange, // PENGGUNAAN CONFIG BARU
                routingKey: AppConfig.RmqRoutingKey // PENGGUNAAN CONFIG BARU
            );

            _logger.LogInformation("RabbitMQ connected, queue '{queue}' bound to exchange '{exchange}' with routing key '{routingKey}'.", _queueName, AppConfig.RmqExchange, AppConfig.RmqRoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect or setup RabbitMQ.");
            throw; 
        }
    }

    public void StartConsuming(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            // Panggil event OnMessageReceived dalam Task.Run untuk menjalankan async operation 
            // dan tidak memblokir thread RabbitMQ consumer.
            Task.Run(async () =>
            {
                if (OnMessageReceived != null)
                {
                    // Di sini kita meneruskan body dan channel-nya
                    await OnMessageReceived.Invoke(ea.Body.ToArray(), _channel);
                }
            }, stoppingToken).ConfigureAwait(false); 

            // Catatan: BasicConsume di bawah menggunakan autoAck: true, 
            // jadi tidak perlu BasicAck di sini. Jika autoAck: false, 
            // BasicAck perlu dipanggil setelah pemrosesan selesai.
        };

        // Mulai konsumsi pesan. autoAck: true menandakan pesan otomatis di-acknowledge
        _channel.BasicConsume(
            queue: _queueName,
            autoAck: true, // Auto-acknowledge, pesan akan hilang dari queue setelah dikirim ke consumer
            consumer: consumer
        );

        _logger.LogInformation("Started consuming messages on queue '{queueName}'.", _queueName);
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }
}