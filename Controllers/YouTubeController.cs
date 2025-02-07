using Microsoft.AspNetCore.Mvc;
using YouTubeApiProject.Services;
using YouTubeApiProject.Models;

namespace YouTubeApiProject.Controllers
{
    public class YouTubeController : Controller
    {
        private readonly YouTubeApiService _youtubeService;

        public YouTubeController(YouTubeApiService youtubeService)
        {
            _youtubeService = youtubeService;
        }

        // Display Search Page
        public IActionResult Index()
        {
            return View(new List<YouTubeVideoModel>()); // Pass an empty list initially
        }

        // Handle the search query
        [HttpPost]
        public async Task<IActionResult> Search(string query, string duration, string uploadDate, string pageToken = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                ViewBag.Query = null;
                return View("Index", new List<YouTubeVideoModel>()); // No search performed yet
            }

            var (videos, nextPageToken) = await _youtubeService.SearchVideosAsync(query, duration, uploadDate, pageToken);

            ViewBag.NextPageToken = nextPageToken;
            ViewBag.Query = query;
            ViewBag.Duration = duration;
            ViewBag.UploadDate = uploadDate;

            return View("Index", videos);
        }

    }
}


