using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YouTubeApiProject.Models;
using System.Xml;

namespace YouTubeApiProject.Services
{
    public class YouTubeApiService
    {
        private readonly string _apiKey;

        public YouTubeApiService(IConfiguration configuration)
        {
            _apiKey = configuration["YouTubeApiKey"]; // Fetch API key from appsettings.json
        }

        public async Task<(List<YouTubeVideoModel>, string)> SearchVideosAsync(string query, string duration, string uploadDate, string pageToken = null)
        {
            try
            {
                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    ApiKey = _apiKey,
                    ApplicationName = "YoutubeApiProject"
                });

                var searchRequest = youtubeService.Search.List("snippet");
                searchRequest.Q = query;
                searchRequest.MaxResults = 12;
                searchRequest.PageToken = pageToken;
                searchRequest.Type = "video";


                // Convert duration filter
                if (!string.IsNullOrEmpty(duration))
                {
                    searchRequest.VideoDuration = duration.ToLower() switch
                    {
                        "short" => SearchResource.ListRequest.VideoDurationEnum.Short__,
                        "medium" => SearchResource.ListRequest.VideoDurationEnum.Medium,
                        "long" => SearchResource.ListRequest.VideoDurationEnum.Long__,
                        _ => null
                    };
                }

                // Apply upload date filter
                if (!string.IsNullOrEmpty(uploadDate))
                {
                    switch (uploadDate.ToLower())
                    {
                        case "today":
                            searchRequest.PublishedAfter = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
                            break;
                        case "week":
                            searchRequest.PublishedAfter = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ");
                            break;
                        case "month":
                            searchRequest.PublishedAfter = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
                            break;
                    }
                }

                var searchResponse = await searchRequest.ExecuteAsync();
                var nextPageToken = searchResponse.NextPageToken;

                // Extract video IDs from search response
                var videoIds = searchResponse.Items
                    .Where(item => item.Id.Kind == "youtube#video")
                    .Select(item => item.Id.VideoId)
                    .ToList();

                // Fetch video details (duration) using Videos API
                var videoDetailsRequest = youtubeService.Videos.List("contentDetails,snippet");
                videoDetailsRequest.Id = string.Join(",", videoIds);
                var videoDetailsResponse = await videoDetailsRequest.ExecuteAsync();

                // Map response data to video model
                var videos = videoDetailsResponse.Items.Select(item => new YouTubeVideoModel
                {
                    Title = item.Snippet.Title,
                    Description = item.Snippet.Description,
                    ThumbnailUrl = item.Snippet.Thumbnails?.Medium?.Url ?? "",
                    VideoUrl = $"https://www.youtube.com/watch?v={item.Id}",
                    ChannelName = item.Snippet.ChannelTitle,
                    PublishedDate = item.Snippet.PublishedAt?.ToString("yyyy-MM-dd") ?? "N/A",
                    Duration = ConvertDuration(item.ContentDetails.Duration) // Convert ISO duration
                }).ToList();

                return (videos, nextPageToken);
            }
            catch (Google.GoogleApiException ex)
            {
                Console.WriteLine($"YouTube API Error: {ex.Message}");
                return (new List<YouTubeVideoModel>(), null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error: {ex.Message}");
                return (new List<YouTubeVideoModel>(), null);
            }
        }

        // Helper function to convert ISO 8601 duration to readable format
        private string ConvertDuration(string isoDuration)
        {
            try
            {
                XmlConvert.ToTimeSpan(isoDuration);
                TimeSpan duration = XmlConvert.ToTimeSpan(isoDuration);
                return $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}";
            }
            catch
            {
                return "N/A";
            }
        }
    }
}



