
using Npgsql;
using Utils;

namespace db;

public class Database
{
    private readonly string connectionString;

    public Database(IConfiguration config)
    {
        connectionString = config.GetConnectionString("db_connection");
    }

    public async Task<NpgsqlConnection> ConnectToDatabase()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var dataSource = dataSourceBuilder.Build();

        return await dataSource.OpenConnectionAsync();
    }

    public async Task InsertNewRecord(string id, Uri host, string url, DateTime scrapedOn, long timeToComplete, string s3FileUrl, string s3DownloadableFileUrl)
    {
        /* 
            When a new url is parsed, insert the details 
            into the database

            Since ID is primary key, don't need to specifiy here
         */

        using var conn = await ConnectToDatabase();
        var query = "INSERT INTO public.scrape_data (id, host, url, scraped_on, time_to_complete, s3_file_url, s3_file_url_downloadable) VALUES (@id, @host, @url, @scrapedOn, @timeToComplete, @s3FileUrl, @s3DownloadableFileUrl)";
        using var cmd = new NpgsqlCommand(query, conn);
        // Add parameters to command to prevent SQL injection
        cmd.Parameters.AddWithValue("@host", Functions.RemoveSubdomains(host));
        cmd.Parameters.AddWithValue("@url", url);
        cmd.Parameters.AddWithValue("@scrapedOn", scrapedOn);
        cmd.Parameters.AddWithValue("@timeToComplete", timeToComplete);
        cmd.Parameters.AddWithValue("@s3FileUrl", s3FileUrl);
        cmd.Parameters.AddWithValue("@s3DownloadableFileUrl", s3DownloadableFileUrl);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }


    public async Task<long> GetAllRows()
    {
        /* 
            Returns number of records in database
         */

        using var conn = await ConnectToDatabase();


        await using (var query = new NpgsqlCommand("SELECT COUNT(*) from public.scrape_data", conn))
        {
            var rowCount = (long)await query.ExecuteScalarAsync();
            return rowCount;
        }
    }

    public async Task<ScrapeDetails?> GetReport(string id)
    {
        try
        {
            await using var conn = await ConnectToDatabase();

            var query = new NpgsqlCommand("SELECT * FROM scrape_data WHERE id=@id", conn);
            query.Parameters.AddWithValue("@id", id); // Assuming id is of type int or long

            var record = await query.ExecuteReaderAsync();
            if (!record.HasRows)
            {
                return null;
            }

            var recordData = new ScrapeDetails();

            while (await record.ReadAsync())
            {
                recordData.Id = record.GetString(0);
                recordData.Host = record.GetString(1);
                recordData.Url = record.GetString(2);
                recordData.ScrapedOn = record.GetDateTime(3);
                recordData.TimeToComplete = record.GetInt64(4);
                recordData.S3FileUrlViewable = record.GetString(5);
                recordData.S3FileUrlDownloadable = record.GetString(6);
            }

            return recordData;
        }
        catch (Exception err)
        {
            Console.WriteLine(err.ToString());
            return null;
        }
    }
}