// Config.cs

namespace WorkerService4;

public static class AppConfig
{
    // --- RabbitMQ Configuration ---
    public const string RmqHostName = "rmq2.pptik.id";
    public const int RmqPort = 5672;
    public const string RmqVirtualHost = "/kawalanak";
    public const string RmqUserName = "kawalanak";
    public const string RmqPassword = "kawalanak2025";
    public const string RmqExchange = "amq.topic";
    public const string RmqRoutingKey = "kawalanak";

    // --- MongoDB Configuration ---
    public const string MongoConnectionString = "mongodb://kawal_anak:1hoUMt847hO4pgi@nosql.smartsystem.id:27017/kawal_anak";
    public const string MongoDatabaseName = "kawal_anak";
    public const string MongoCollectionName = "alat";
}
