using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using WorkerService4;

namespace WorkerService4;

public class MongoDbRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger _logger;

    public MongoDbRepository(ILogger logger) 
    {
        _logger = logger;
        try
        {
            // PENGGUNAAN CONFIG BARU
            var mongoClient = new MongoClient(AppConfig.MongoConnectionString); 
            var database = mongoClient.GetDatabase(AppConfig.MongoDatabaseName);
            _collection = database.GetCollection<BsonDocument>(AppConfig.MongoCollectionName);
            
            _logger.LogInformation("MongoDB connection established and using collection '{collectionName}'.", AppConfig.MongoCollectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to MongoDB at {conn}.", AppConfig.MongoConnectionString);
            throw;
        }
    }

    public async Task InsertDocumentAsync(BsonDocument document)
    {
        await _collection.InsertOneAsync(document);
    }
}