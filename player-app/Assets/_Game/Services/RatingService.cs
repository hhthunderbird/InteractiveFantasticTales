using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace InteractiveFantasticTales.Services
{
    [Serializable]
    public class RatingSubmitRequest
    {
        public string storyId;
        public int score;
        public string review;
    }

    [Serializable]
    public class RatingSubmitResponse
    {
        public bool success;
        public RatingEntryData rating;
        public RatingStatsData stats;
        public string error;
    }

    [Serializable]
    public class RatingEntryData
    {
        public string storyId;
        public string userId;
        public int score;
        public string review;
    }

    [Serializable]
    public class RatingStatsData
    {
        public float averageRating;
        public int ratingCount;
    }

    [Serializable]
    public class MyRatingResponse
    {
        public bool success;
        public bool hasRated;
        public RatingEntryData rating;
        public string error;
    }

    public class RatingService
    {
        private static RatingService _instance;
        public static RatingService Instance => _instance ??= new RatingService();

        private const string CloudRunBaseUrl = "https://ift-api-201008727431.us-central1.run.app";
        private const int MaxRetries = 2;

        public IEnumerator SubmitRating(string storyId, int score, string review, Action<RatingSubmitResponse> callback)
        {
            if (string.IsNullOrEmpty(storyId)) throw new ArgumentNullException(nameof(storyId));
            if (score < 1 || score > 5) throw new ArgumentOutOfRangeException(nameof(score));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var requestBody = new RatingSubmitRequest
            {
                storyId = storyId,
                score = score,
                review = review ?? ""
            };

            var jsonBody = JsonUtility.ToJson(requestBody);

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                using (var request = new UnityWebRequest($"{CloudRunBaseUrl}/api/ratings", "POST"))
                {
                    request.SetRequestHeader("Content-Type", "application/json");

                    var auth = CloudRunAuthService.Instance;
                    if (auth != null && auth.IsAuthenticated)
                        request.SetRequestHeader("Authorization", $"Bearer {auth.GetAuthTokenSync()}");

                    var bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<RatingSubmitResponse>(request.downloadHandler.text);
                        if (response != null && response.success)
                        {
                            var catalog = StoryCatalogService.Instance;
                            if (catalog != null)
                            {
                                var story = catalog.AllStories.Find(s => s.id == storyId);
                                if (story != null && response.stats != null)
                                {
                                    story.rating = response.stats.averageRating;
                                    story.ratingCount = response.stats.ratingCount;
                                }
                            }
                            callback(response);
                            yield break;
                        }

                        callback(response ?? new RatingSubmitResponse { success = false, error = "Empty response" });
                        yield break;
                    }

                    if (attempt == MaxRetries)
                    {
                        callback(new RatingSubmitResponse { success = false, error = request.error });
                    }
                }
            }
        }

        public IEnumerator GetMyRating(string storyId, Action<MyRatingResponse> callback)
        {
            if (string.IsNullOrEmpty(storyId)) throw new ArgumentNullException(nameof(storyId));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            using (var request = UnityWebRequest.Get($"{CloudRunBaseUrl}/api/ratings/{storyId}/mine"))
            {
                var auth = CloudRunAuthService.Instance;
                if (auth != null && auth.IsAuthenticated)
                    request.SetRequestHeader("Authorization", $"Bearer {auth.GetAuthTokenSync()}");

                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<MyRatingResponse>(request.downloadHandler.text);
                    callback(response ?? new MyRatingResponse { success = false, error = "Empty response" });
                }
                else
                {
                    callback(new MyRatingResponse { success = false, error = request.error });
                }
            }
        }
    }
}
