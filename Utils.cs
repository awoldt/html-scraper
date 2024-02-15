
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
        [JsonPropertyName("title")]
        public string? Title { get; set; }
    }

    public class ImgTag : BaseTag
    {
        [JsonPropertyName("src")]
        public string? Src { get; set; }
        [JsonPropertyName("alt")]
        public string? Alt { get; set; }
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
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("onsubmit")]
        public string? OnSubmit { get; set; }
    }

    public class MetaTag : BaseTag
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        [JsonPropertyName("property")]
        public string? Property { get; set; }
    }

    public class InputTag : BaseTag
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
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
                var className = x.GetAttributeValue("class", null);
                var idName = x.GetAttributeValue("id", null);
                switch (tagName)
                {
                    case "img":
                        var src = GetSource(x.GetAttributeValue("src", null), host);
                        if (src != null)
                        {
                            tagDetails.Add(new ImgTag
                            {
                                ClassName = className,
                                IdName = idName,
                                Src = src,
                                Alt = x.GetAttributeValue("alt", null),
                            });
                        }

                        break;

                    case "a":
                        var href = GetSource(x.GetAttributeValue("href", null), host);
                        var title = GetSource(x.GetAttributeValue("title", null), host);
                        if (href != null)
                        {
                            tagDetails.Add(new ATag
                            {
                                ClassName = className,
                                IdName = idName,
                                Href = href,
                                Title = title
                            });
                        }

                        break;

                    case "script":
                        tagDetails.Add(new ScriptTag
                        {
                            ClassName = className,
                            IdName = idName,
                            Src = GetSource(x.GetAttributeValue("src", null), host),
                            InlineScript = x.GetAttributeValue("src", null) == null ? true : false,
                            InlineScriptText = x.GetAttributeValue("src", null) == null ? x.InnerText.Replace("\n", "").Replace("\r", "").Replace("\t", "") : null
                        });

                        break;

                    case "form":
                        var method = x.GetAttributeValue("method", null);
                        var action = GetSource(x.GetAttributeValue("action", null), host);
                        var formName = x.GetAttributeValue("name", null);
                        var onsubmit = x.GetAttributeValue("onsubmit", null);
                        tagDetails.Add(new FormTag
                        {
                            Method = method,
                            Action = action,
                            Name = formName,
                            OnSubmit = onsubmit
                        });
                        break;

                    case "meta":
                        var metaName = x.GetAttributeValue("name", null);
                        var property = x.GetAttributeValue("property", null);
                        var content = x.GetAttributeValue("content", null);
                        if (metaName != null || property != null || content != null)
                        {
                            tagDetails.Add(new MetaTag
                            {
                                Name = metaName,
                                Property = property,
                                Content = content
                            });
                        }
                        break;

                    case "input":
                        var type = x.GetAttributeValue("type", null);
                        var name = x.GetAttributeValue("name", null);
                        if (type != null || name != null)
                        {
                            tagDetails.Add(new InputTag
                            {
                                Type = type,
                                Name = name,
                            });
                        }
                        break;

                    // other text based tags such as p, h, span, etc.....
                    default:
                        var innerText = Regex.Replace(x.InnerText.Replace("\n", "").Replace("\r", "").Replace("\t", "").Trim(), "\\s+", " ");
                        if (innerText != "")
                        {
                            tagDetails.Add(new TextTag
                            {
                                ClassName = className,
                                IdName = idName,
                                InnerText = innerText
                            });
                        }
                        break;
                }
            }

            return new TagInfo
            {
                Tag = tagName,
                Number = tagDetails.Count,
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

        private static string? GetSource(string? path, string host)
        {
            /* 
                Gets the full path url for any url type attribute
                (href, src)

                ex: if href of a tag was "/about" and the site was x.com,
                the string would return "https://x.com/about"
            
             */
            if (path == null) return null;

            Uri? url;
            if (!Uri.TryCreate(path, UriKind.Absolute, out url)) return null;

            // make sure an HTTPS scheme is returned
            if (url.Scheme != "http" && url.Scheme != "https")
            {
                return $"https://{host}{path}";
            }

            return url.AbsoluteUri;
        }
    }
}

