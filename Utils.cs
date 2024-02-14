using System.Text.Json.Serialization;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Utils
{
    public class TagInfo
    {
        [JsonPropertyName("tag")]
        public string Tag { get; set; }
        [JsonPropertyName("num_of_tags")]
        public int Number { get; set; }
        [JsonPropertyName("results")]
        public object[] Values { get; set; }
    }

    public class ParsedFileInfo
    {
        [JsonPropertyName("parsed_in_ms")]
        public long TimeToParse { get; set; }
        [JsonPropertyName("url")]
        public string PageParsed { get; set; }
        [JsonPropertyName("data")]
        public TagInfo[]? Data { get; set; }
    }

    public class BaseTag
    {
        /* 
            properties that every tag should have
         */

        [JsonPropertyName("class")]
        public string? ClassName { get; set; }
        [JsonPropertyName("id")]
        public string? IdName { get; set; }
    }

    public class TextTag : BaseTag
    {
        [JsonPropertyName("inner_text")]
        public string? InnerText { get; set; }
    }

    public class ATag : BaseTag
    {
        [JsonPropertyName("href")]
        public string? Href { get; set; }
    }

    public class ImgTag : BaseTag
    {
        [JsonPropertyName("src")]
        public string? Src { get; set; }
    }

    public class ScriptTag : BaseTag
    {
        [JsonPropertyName("src")]
        public string? Src { get; set; }
        [JsonPropertyName("inline_script")]
        public bool InlineScript { get; set; }
        [JsonPropertyName("inline_text")]
        public string? InlineScriptText { get; set; }
    }

    public class FormTag : BaseTag
    {
        [JsonPropertyName("method")]
        public string? Method { get; set; }
        [JsonPropertyName("action")]
        public string? Action { get; set; }

    }

    public class Functions
    {
        public static TagInfo GenerateTagInfo(HtmlNodeCollection tags, string tagName, string host)
        {
            /* 
                generates data on each tag returned from html page
                each tag will have tag name, num of results, and results
             */

            List<BaseTag> tagDetails = new List<BaseTag>();
            foreach (var x in tags)
            {
                Uri? validUrl;
                switch (tagName)
                {
                    case "img":

                        // img src is from another site
                        if (x.GetAttributeValue("src", null) != null && Uri.TryCreate(x.GetAttributeValue("src", null), UriKind.Absolute, out validUrl))
                        {
                            tagDetails.Add(new ImgTag
                            {
                                ClassName = x.GetAttributeValue("class", null),
                                IdName = x.GetAttributeValue("id", null),
                                Src = validUrl.AbsoluteUri
                            });
                        }
                        // img src is from current site
                        else
                        {
                            tagDetails.Add(new ImgTag
                            {
                                ClassName = x.GetAttributeValue("class", null),
                                IdName = x.GetAttributeValue("id", null),
                                Src = $"https://{host}{x.GetAttributeValue("src", null)}"
                            });
                        }

                        break;

                    case "a":
                        tagDetails.Add(new ATag
                        {
                            ClassName = x.GetAttributeValue("class", null),
                            IdName = x.GetAttributeValue("id", null),
                            Href = x.GetAttributeValue("href", null),
                        });
                        break;

                    case "script":
                        // script src is from another site
                        if (x.GetAttributeValue("src", null) != null && Uri.TryCreate(x.GetAttributeValue("src", null), UriKind.Absolute, out validUrl))
                        {
                            tagDetails.Add(new ScriptTag
                            {
                                ClassName = x.GetAttributeValue("class", null),
                                IdName = x.GetAttributeValue("id", null),
                                Src = x.GetAttributeValue("src", null) == null ? null : validUrl.AbsoluteUri,
                                InlineScript = x.GetAttributeValue("src", null) == null ? true : false,
                                InlineScriptText = x.GetAttributeValue("src", null) == null ? x.InnerText.Replace("\n", "").Replace("\r", "").Replace("\t", "") : null
                            });
                        }
                        // script src is from current site
                        else
                        {
                            tagDetails.Add(new ScriptTag
                            {
                                ClassName = x.GetAttributeValue("class", null),
                                IdName = x.GetAttributeValue("id", null),
                                Src = x.GetAttributeValue("src", null) == null ? null : $"https://{host}{x.GetAttributeValue("src", null)}",
                                InlineScript = x.GetAttributeValue("src", null) == null ? true : false,
                                InlineScriptText = x.GetAttributeValue("src", null) == null ? x.InnerText.Replace("\n", "").Replace("\r", "").Replace("\t", "") : null
                            });
                        }

                        break;

                    case "form":
                        tagDetails.Add(new FormTag
                        {
                            Method = x.GetAttributeValue("method", null),
                            Action = x.GetAttributeValue("action", null)
                        });
                        break;

                    // other text based tags such as p, h, span, etc.....
                    default:
                        if (Regex.Replace(x.InnerText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim(), "\\s+", " ") != "")
                        {
                            tagDetails.Add(new TextTag
                            {
                                ClassName = x.GetAttributeValue("class", null),
                                IdName = x.GetAttributeValue("id", null),
                                InnerText = Regex.Replace(x.InnerText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim(), "\\s+", " ")
                            });
                        }
                        break;
                }
            }

            return new TagInfo
            {
                Tag = tagName,
                Number = tags.Count,
                Values = tagDetails.ToArray()
            };
        }

        public static string RemoveSubdomains(Uri url)
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
}

