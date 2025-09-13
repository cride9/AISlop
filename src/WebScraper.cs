namespace AISlop;

using System.Collections.Immutable;
using System.Text;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;

public record SearchResult(string Title, string Link, string Snippet);
public static class WebScraper
{
    public static async Task<string> Search(string query)
    {
        try
        {
            var results = await PerformSearch(query);
            if (!results.Any())
                return "No search results found.";

            StringBuilder sb = new();

            int count = 1;
            foreach (var result in results)
            {
                sb.AppendLine($"{count++}. {result.Title}");
                sb.AppendLine($"\tLink: {result.Link}");
                sb.AppendLine($"\tSnippet: {result.Snippet}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Performs a web search by scraping DuckDuckGo.
    /// This method is static and belongs to the static WebSearcher class.
    /// </summary>
    /// <param name="query">The search term.</param>
    /// <returns>A list of search results.</returns>
    public static async Task<List<SearchResult>> PerformSearch(string query)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        string encodedQuery = HttpUtility.UrlEncode(query);
        string searchUrl = $"https://html.duckduckgo.com/html/?q={encodedQuery}";

        string htmlContent = await client.GetStringAsync(searchUrl);

        var context = BrowsingContext.New(Configuration.Default);
        IDocument document = await context.OpenAsync(req => req.Content(htmlContent));

        var resultNodes = document.QuerySelectorAll("div.result");

        var searchResults = new List<SearchResult>();

        var baseUri = new Uri("https://html.duckduckgo.com");

        foreach (var node in resultNodes)
        {
            var titleNode = node.QuerySelector("h2.result__title a");
            var linkNode = node.QuerySelector("a.result__url");
            var snippetNode = node.QuerySelector("a.result__snippet");

            if (titleNode != null && linkNode != null && snippetNode != null)
            {
                string title = titleNode.TextContent.Trim();
                string relativeLink = linkNode.GetAttribute("href")?.Trim() ?? string.Empty;
                string snippet = snippetNode.TextContent.Trim();

                if (string.IsNullOrEmpty(relativeLink)) continue;

                // Create a full, absolute URI from the base and the relative link
                var absoluteUri = new Uri(baseUri, relativeLink);

                // Now extract the clean link from the query parameters
                var queryParams = HttpUtility.ParseQueryString(absoluteUri.Query);
                string cleanLink = queryParams["uddg"] ?? absoluteUri.ToString();

                searchResults.Add(new SearchResult(title, cleanLink, snippet));
            }
        }

        return searchResults;
    }

    /// <summary>
    /// Downloads a webpage and extracts its meaningful text content.
    /// </summary>
    /// <param name="url">The URL of the webpage to scrape.</param>
    /// <returns>A string containing the cleaned text content of the page.</returns>
    public static async Task<string> ScrapeTextFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            return "Invalid url provided.";

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

            string htmlContent = await client.GetStringAsync(url);
            IDocument document = await context.OpenAsync(req => req.Content(htmlContent));

            var elementsToRemove = document.QuerySelectorAll("script, style, nav, header, footer, aside");
            foreach (var element in elementsToRemove)
            {
                element.Remove();
            }
            string rawText = document.Body?.TextContent ?? string.Empty;

            var lines = rawText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    builder.AppendLine(trimmedLine);
                }
            }

            return builder.ToString();
        }
        catch (HttpRequestException e)
        {
            return $"Failed to download content from {url}. Status: {e.StatusCode}";
        }
        catch (Exception e)
        {
            return $"An unexpected error occurred while scraping {url}.";
        }
    }
}
