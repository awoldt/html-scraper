using Utils;

public class ScrapeDetails
{
    // Parameterless constructor for json deserialization
    public ScrapeDetails() { }

    public ScrapeDetails(string id, Uri url, long timeToParseUrl, string s3ViewableUrl, string s3DownloadbleUrl)
    {
        Url = url.ToString().Trim();
        Host = Functions.RemoveSubdomains(url);
        ScrapedOn = DateTime.UtcNow;
        TimeToComplete = timeToParseUrl;
        S3FileUrlViewable = s3ViewableUrl;
        S3FileUrlDownloadable = s3DownloadbleUrl;
        Id = id;
    }

    public string Id { get; set; }

    public string Host { get; set; }

    public string Url { get; set; }

    public DateTime ScrapedOn { get; set; }

    public long TimeToComplete { get; set; } // how long it took to parse the webpage 

    public string S3FileUrlViewable { get; set; } // this endpoint will show json file in browser

    public string S3FileUrlDownloadable { get; set; } // hitting this endpoint will download the json file
}

