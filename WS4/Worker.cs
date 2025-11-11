using System;
using System.Globalization;
using System.Text;
using RabbitMQ.Client;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;

namespace WorkerService4;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RMQ? _rabbitMq;
    private readonly MongoDbRepository _mongoDb;
    
    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;

        // --- Konfigurasi dan inisialisasi RabbitMQ menggunakan class RMQ ---
        try
        {
            _rabbitMq = new RMQ(logger);
            // Langganan (subscribe) ke event OnMessageReceived dari class RMQ
            _rabbitMq.OnMessageReceived += ProcessRabbitMqMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker failed to initialize RMQ. Stopping service startup.");
            // PENTING: Dalam aplikasi yang sebenarnya, Anda mungkin ingin menghentikan startup 
            // atau mencoba menghubungkan kembali secara berkala di sini.
        }
        
        // --- Konfigurasi dan inisialisasi MongoDB ---
        try
        {
            _mongoDb = new MongoDbRepository(logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker failed to initialize MongoDB. Stopping service startup.");
            throw; // Gagal di konstruktor, service gagal start.
        }

        
    }

    // Metode yang akan menangani pesan dari RabbitMQ
    private async Task ProcessRabbitMqMessage(byte[] body, IModel channel)
    {
        try
        {
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("RAW MESSAGE FROM RMQ: {msg}", message);

            var data = JObject.Parse(message);
            
            // 1. Ekstraksi Data
            var rfid = data["rfid"]?.ToString();
            var weightStr = data["weight"]?.ToString();
            var heightStr = data["height"]?.ToString();
            var pict1 = data["pict1"]?.ToString();
            var pict2 = data["pict2"]?.ToString();
            var pict3 = data["pict3"]?.ToString();
            
            // 2. Validasi Kunci Utama (rfid tidak boleh kosong)
            if (string.IsNullOrEmpty(rfid))
            {
                _logger.LogWarning("Required field 'rfid' is missing or empty. Received message: {msg}", message);
                return; // Berhenti memproses jika rfid kosong
            }

            // Validasi Weight (juga tidak boleh kosong)
            if (string.IsNullOrEmpty(weightStr))
            {
                _logger.LogWarning("Required field 'weight' is missing or empty for RFID: {id}", rfid);
                return; // Berhenti memproses jika weight kosong
            }
            
            // 3. Konversi Data Numerik dengan InvariantCulture
            
            // Menggunakan int.TryParse. Untuk integer, kita tidak perlu InvariantCulture
            // selama format yang dikirim adalah angka bulat.
            // Konversi Weight: WAJIB menggunakan InvariantCulture agar titik/koma desimal dikenali
            if (!double.TryParse(weightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double weightValue))
            //if (!int.TryParse(weightStr, out int weightValue))
            {
                // Catatan: Jika data yang dikirim adalah "12.2", ini akan gagal dan menghasilkan error.
                _logger.LogError("Failed to parse weight value as double. Weight must be a whole number. Value: {weightStr} for RFID: {id}", weightStr, rfid);
                return; 
            }
            
            double heightValue = 0.0;
            if (!string.IsNullOrEmpty(heightStr))
            {
                if (!double.TryParse(heightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out heightValue))
                {
                    _logger.LogWarning("Failed to parse height value as double, setting to 0.0. Height must be a whole number. Value: {heightStr} for RFID: {id}", heightStr, rfid);
                    // Lanjut, tapi heightValue adalah 0
                }
            }
            
            _logger.LogInformation("Processing RFID: {id}, Weight: {w} (Correctly Parsed!), Height: {h}", rfid, weightValue, heightValue);
            // Pembulatan opsional ke 1 digit desimal
            weightValue = Math.Round(weightValue, 1);
            heightValue = Math.Round(heightValue, 1);
        
            // 4. Buat BsonDocument
            var document = new BsonDocument
            {
                { "rfid", rfid },
                { "weight", weightValue }, // Tersimpan sebagai 12.2
                { "height", heightValue }, 
                { "pict1_url", pict1 ?? string.Empty },
                { "pict2_url", pict2 ?? string.Empty },
                { "pict3_url", pict3 ?? string.Empty },
                { "ingestion_timestamp", DateTime.UtcNow }
            };

            // 5. Simpan ke MongoDB
            await _mongoDb.InsertDocumentAsync(document); 
            
            _logger.LogInformation("Data RFID {id} berhasil disimpan ke MongoDB.", rfid);
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
            // Worker thread tetap berjalan, menunggu sinyal pembatalan
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("Worker stopping...");
        // _rabbitMq akan di-dispose secara otomatis oleh Dependency Injection (DI) jika didaftarkan sebagai Scoped/Singleton, 
        // atau kita bisa memanggil Dispose() secara eksplisit jika perlu (tetapi BackgroundService biasanya diurus oleh DI).
    }

    // Penting: Pastikan untuk mengimplementasikan IDisposable atau memanfaatkan 
    // mekanisme DI untuk memanggil Dispose() pada _rabbitMq agar koneksi tertutup. 
    // Dalam BackgroundService, ini sering diurus oleh container DI jika RMQ didaftarkan dengan benar.
    // Jika tidak menggunakan DI untuk lifetime management:
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _rabbitMq?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}