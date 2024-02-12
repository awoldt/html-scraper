using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

public class ScrapeDetails
{

    public ScrapeDetails() { } // Parameterless constructor for EF

    public ScrapeDetails(Uri url, long timeToParseUrl, string s3ViewableUrl, string s3DownloadbleUrl, string parsedText)
    {
        Url = url.ToString().Trim();
        Host = RemoveSubdomains(url);
        ScrapedOn = DateTime.UtcNow;
        TimeToComplete = timeToParseUrl;
        S3FileUrlViewable = s3ViewableUrl;
        S3FileUrlDownloadable = s3DownloadbleUrl;
        ParsedText = parsedText;
    }

    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("host")]
    public string Host { get; set; }
    [Column("url")]
    public string Url { get; set; }
    [Column("scraped_on")]
    public DateTime ScrapedOn { get; set; }
    [Column("time_to_complete")]
    public long TimeToComplete { get; set; } // how long it took to parse the webpage 
    [Column("s3_file_url")]
    public string S3FileUrlViewable { get; set; } // this endpoint will show json file in browser
    [Column("s3_file_url_downloadable")]
    public string S3FileUrlDownloadable { get; set; } // hitting this endpoint will download the json file
    [NotMapped]
    public string ParsedText { get; set; }

    public string RemoveSubdomains(Uri url)
    {
        /* 
            returns only the website host of a url (no subdomains)
            ex: account.website.com => website.com

         */

        var host = url.Host;
        var splitHost = host.Split(".");
        if (splitHost.Length > 2)
        {
            return splitHost[splitHost.Length - 2] + "." + splitHost[splitHost.Length - 1];
        }
        return host;
    }
}

