using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webscraper.Models;
using static Utils;

namespace webscraper.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _httpClient;
    private readonly AmazonS3Client _s3Client;
    private readonly IConfiguration _config;
    private readonly HtmlWeb _webParser;

    private readonly db _db;

    string s3Url = "https://491292639630-us-east-1-urlparser.s3.amazonaws.com";
    string bucketName = "491292639630-us-east-1-urlparser";

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory http, IConfiguration config, db database)
    {
        _config = config;
        _logger = logger;
        _httpClient = http.CreateClient();
        _s3Client = new AmazonS3Client(new BasicAWSCredentials(_config["aws_access_key"], _config["aws_secret_key"]), Amazon.RegionEndpoint.USEast1);
        _webParser = new HtmlWeb();
        _db = database;
    }

    public IActionResult Index()
    {
        var numberOfSitesParsed = _db.ScrapeDetails.Count();
        ViewData["num_of_sites_parsed"] = numberOfSitesParsed;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string url_value, List<string> tags_to_parse)
    {
        if (!ModelState.IsValid)
        {
            HttpContext.Response.StatusCode = 500;
            ErrorViewModel viewModel = new ErrorViewModel("There was an error while processing form data");
            return View("Error", viewModel);
        }

        Uri? validUrl;
        Uri.TryCreate(url_value, UriKind.Absolute, out validUrl);
        if (validUrl == null)
        {
            HttpContext.Response.StatusCode = 400;
            ErrorViewModel viewModel = new ErrorViewModel("Not a vaild URL");
            return View("Error", viewModel);
        }
        if (validUrl.Host == "localhost")
        {
            HttpContext.Response.StatusCode = 400;
            ErrorViewModel viewModel = new ErrorViewModel("Cannot use localhost as URL to scrape");
            return View("Error", viewModel);
        }

        // see if url response gives a valid response
        var response = await _httpClient.GetAsync(validUrl);
        var urlStatusCode = response.StatusCode;
        if (urlStatusCode != System.Net.HttpStatusCode.OK && urlStatusCode != System.Net.HttpStatusCode.Accepted)
        {
            HttpContext.Response.StatusCode = 400;
            ErrorViewModel viewModel = new ErrorViewModel($"URL responses must have a 200 status code, {validUrl} returned a {urlStatusCode} status");
            return View("Error", viewModel);
        }

        // make sure page content type is html 
        if (response.Content.Headers.ContentType != null && response.Content.Headers.ContentType.MediaType != "text/html")
        {
            HttpContext.Response.StatusCode = 400;
            ErrorViewModel viewModel = new ErrorViewModel($"URL must return a content type of text/html");
            return View("Error", viewModel);
        }

        string[] tags = tags_to_parse.ToArray();

        Stopwatch timer = new Stopwatch();
        timer.Start();
        var HTMLDoc = await _webParser.LoadFromWebAsync(validUrl.ToString());
        List<TagInfo> tagsInfo = new List<TagInfo>();
        foreach (var x in tags)
        {
            var t = HTMLDoc.DocumentNode.SelectNodes($"//{x}");
            if (t != null)
            {
                var info = GenerateTagInfo(t, x, validUrl.Host);
                tagsInfo.Add(info);
            }
        }
        timer.Stop();

        var final = new ParsedFileInfo
        {
            TimeToParse = timer.ElapsedMilliseconds,
            Data = tagsInfo.Count == 0 ? null : tagsInfo.ToArray(),
            PageParsed = validUrl.ToString()
        };
        var jsonFile = JsonSerializer.Serialize(final);

        // upload json file to s3 (BROWSER VIEWABLE FILE)
        string viewableFilename = $"{new Random().Next(1000000)}_{DateTimeOffset.Now.ToUnixTimeSeconds()}.json";
        PutObjectRequest viewableFilenameRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = viewableFilename,
            ContentBody = jsonFile
        };
        await _s3Client.PutObjectAsync(viewableFilenameRequest);


        // upload json file to s3 (DOWNLOADABLE FILE)
        string downloadableFilename = $"{new Random().Next(1000000)}_{DateTimeOffset.Now.ToUnixTimeSeconds()}.json";
        PutObjectRequest downloadableFilenameRequest = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = downloadableFilename,
            ContentBody = jsonFile,
            Headers = {
                ContentDisposition = "attachment"
            }
        };
        await _s3Client.PutObjectAsync(downloadableFilenameRequest);
        var viewData = new ScrapeDetails(validUrl, timer.ElapsedMilliseconds, $"{s3Url}/{viewableFilename}", $"{s3Url}/{downloadableFilename}", jsonFile);

        // save url parse info to db
        await _db.ScrapeDetails.AddAsync(viewData);
        await _db.SaveChangesAsync();

        ViewData["tags_parsed"] = tags_to_parse.ToArray();

        return View(viewData);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("error");
    }
}