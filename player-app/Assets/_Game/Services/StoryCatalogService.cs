using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using UnityEngine;

namespace InteractiveFantasticTales.Services
{
    [Serializable]
    public class StoryCatalogEntry
    {
        public string id;
        public string title;
        public string authorName;
        public string description;
        public string language;
        public string[] genre;
        public string[] tags;
        public string difficulty;
        public int estimatedMinutes;
        public int wordCount;
        public int sectionCount;
        public int endingCount;
        public string thumbnailUrl;
        public string thumbnailLargeUrl;
        public float rating;
        public int ratingCount;
        public string priceTier;
        public string priceFormatted;
        public bool isOwned;
        public bool hasProgress;
        public int lastSectionId;
        public float completionPercent;
        public string version;
    }

    public class StoryCatalogService : MonoBehaviour
    {
        public static StoryCatalogService Instance;

        public event Action<List<StoryCatalogEntry>> OnCatalogLoaded;
        public event Action<StoryCatalogEntry> OnStoryDetailLoaded;
        public event Action<string> OnError;

        public bool IsLoaded { get; private set; }
        public List<StoryCatalogEntry> AllStories { get; private set; } = new List<StoryCatalogEntry>();

        public List<StoryCatalogEntry> OwnedStories =>
            AllStories.Where(s => s.isOwned).ToList();

        private FirebaseFirestore _db;
        private bool _firestoreReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (Core.FirebaseBootstrap.Instance != null)
            {
                Core.FirebaseBootstrap.Instance.OnFirebaseReady += OnFirebaseReady;
                if (Core.FirebaseBootstrap.Instance.IsReady)
                    BindFirestore();
            }
        }

        private void OnFirebaseReady(bool ready)
        {
            Core.FirebaseBootstrap.Instance.OnFirebaseReady -= OnFirebaseReady;
            if (ready)
                BindFirestore();
        }

        private void BindFirestore()
        {
            try
            {
                _db = FirebaseFirestore.DefaultInstance;
                _firestoreReady = true;
                Debug.Log("[StoryCatalogService] Bound to Firestore");
            }
            catch (Exception e)
            {
                Debug.LogError($"[StoryCatalogService] Firestore bind failed: {e.Message}");
                _firestoreReady = false;
            }
        }

        public async Task LoadCatalog()
        {
            IsLoaded = false;
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!_firestoreReady)
                {
                    await Task.Delay(500);
                    AllStories = CreateMockCatalog();
                    IsLoaded = true;
                    OnCatalogLoaded?.Invoke(AllStories);
                    return;
                }
#endif
                AllStories = await FetchFromFirestore();
                IsLoaded = true;
                OnCatalogLoaded?.Invoke(AllStories);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StoryCatalogService] LoadCatalog failed: {ex.Message}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                AllStories = CreateMockCatalog();
                IsLoaded = true;
                OnCatalogLoaded?.Invoke(AllStories);
#else
                OnError?.Invoke(ex.Message);
#endif
            }
        }

        public async Task<StoryCatalogEntry> LoadStoryDetail(string storyId)
        {
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!_firestoreReady)
                {
                    await Task.Delay(500);
                    var mockStory = AllStories.FirstOrDefault(s => s.id == storyId);
                    mockStory ??= CreateMockCatalog().FirstOrDefault(s => s.id == storyId);
                    OnStoryDetailLoaded?.Invoke(mockStory);
                    return mockStory;
                }
#endif
                var story = await FetchStoryDetailFromFirestore(storyId);
                if (story != null)
                    OnStoryDetailLoaded?.Invoke(story);
                else
                    OnError?.Invoke($"Historia nao encontrada: {storyId}");
                return story;
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var fallback = CreateMockCatalog().FirstOrDefault(s => s.id == storyId);
                OnStoryDetailLoaded?.Invoke(fallback);
                return fallback;
#else
                OnError?.Invoke(ex.Message);
                return null;
#endif
            }
        }

        public List<StoryCatalogEntry> FilterByGenre(string genre)
        {
            return AllStories
                .Where(s => s.genre != null && s.genre.Contains(genre, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        public List<StoryCatalogEntry> FilterByTag(string tag)
        {
            return AllStories
                .Where(s => s.tags != null && s.tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        public List<StoryCatalogEntry> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return AllStories;

            var lower = query.ToLowerInvariant();
            return AllStories
                .Where(s =>
                    (s.title != null && s.title.ToLowerInvariant().Contains(lower)) ||
                    (s.authorName != null && s.authorName.ToLowerInvariant().Contains(lower)) ||
                    (s.description != null && s.description.ToLowerInvariant().Contains(lower)))
                .ToList();
        }

        public List<StoryCatalogEntry> SortBy(string sort)
        {
            var list = new List<StoryCatalogEntry>(AllStories);
            switch (sort.ToLowerInvariant())
            {
                case "popular":
                    list.Sort((a, b) => b.ratingCount.CompareTo(a.ratingCount));
                    break;
                case "newest":
                    break;
                case "rating":
                    list.Sort((a, b) => b.rating.CompareTo(a.rating));
                    break;
                case "price":
                    list.Sort((a, b) => GetTierOrder(a.priceTier).CompareTo(GetTierOrder(b.priceTier)));
                    break;
            }
            return list;
        }

        public bool IsStoryOwned(string storyId)
        {
            var story = AllStories.FirstOrDefault(s => s.id == storyId);
            return story != null && story.isOwned;
        }

        public bool HasProgress(string storyId)
        {
            var story = AllStories.FirstOrDefault(s => s.id == storyId);
            return story != null && story.hasProgress;
        }

        public static string GetPriceFormatted(string tier)
        {
            switch (tier)
            {
                case "free": return "Gratis";
                case "tier_1": return "R$ 4,99";
                case "tier_2": return "R$ 9,99";
                case "tier_3": return "R$ 14,99";
                case "tier_4": return "R$ 19,99";
                case "tier_5": return "R$ 29,99";
                default: return "R$ --";
            }
        }

        public static string GetGenreIcon(string genre)
        {
            switch (genre.ToLowerInvariant())
            {
                case "fantasy":
                case "fantasia": return "\U0001F3F0";
                case "horror": return "\U0001F987";
                case "steampunk": return "\u2699\uFE0F";
                case "scifi":
                case "ficcao-cientifica": return "\U0001F680";
                case "mystery":
                case "misterio": return "\U0001F50D";
                case "adventure":
                case "aventura": return "\U0001F5FA\uFE0F";
                default: return "\u2753";
            }
        }

        private int GetTierOrder(string tier)
        {
            switch (tier)
            {
                case "free": return 0;
                case "tier_1": return 1;
                case "tier_2": return 2;
                case "tier_3": return 3;
                case "tier_4": return 4;
                case "tier_5": return 5;
                default: return 99;
            }
        }

        // ── Firestore Queries ────────────────────────────────────────

        private async Task<List<StoryCatalogEntry>> FetchFromFirestore()
        {
            if (_db == null) throw new InvalidOperationException("Firestore not bound");

            var storiesRef = _db.Collection("stories");
            var query = storiesRef
                .WhereEqualTo("metadata.isPublished", true)
                .OrderByDescending("stats.purchaseCount")
                .Limit(100);

            var snapshot = await query.GetSnapshotAsync();

            if (snapshot == null || snapshot.Documents == null)
                return new List<StoryCatalogEntry>();

            var userId = CloudRunAuthService.Instance?.GetUserId();
            var ownedSet = userId != null
                ? await FetchOwnedStoryIds(userId)
                : new HashSet<string>();

            var progressMap = userId != null
                ? await FetchProgressMap(userId)
                : new Dictionary<string, (int sectionId, float pct)>();

            var stories = new List<StoryCatalogEntry>();
            foreach (var doc in snapshot.Documents)
            {
                var entry = DocToCatalogEntry(doc);
                if (entry == null) continue;

                entry.isOwned = ownedSet.Contains(entry.id);
                if (progressMap.TryGetValue(entry.id, out var progress))
                {
                    entry.hasProgress = true;
                    entry.lastSectionId = progress.sectionId;
                    entry.completionPercent = progress.pct;
                }

                stories.Add(entry);
            }

            return stories;
        }

        private async Task<StoryCatalogEntry> FetchStoryDetailFromFirestore(string storyId)
        {
            if (_db == null) throw new InvalidOperationException("Firestore not bound");

            var docRef = _db.Collection("stories").Document(storyId);
            var doc = await docRef.GetSnapshotAsync();

            if (!doc.Exists)
                return null;

            var entry = DocToCatalogEntry(doc);
            if (entry == null) return null;

            var userId = CloudRunAuthService.Instance?.GetUserId();
            if (userId != null)
            {
                var ownedSet = await FetchOwnedStoryIds(userId);
                entry.isOwned = ownedSet.Contains(entry.id);

                var progressMap = await FetchProgressMap(userId);
                if (progressMap.TryGetValue(entry.id, out var progress))
                {
                    entry.hasProgress = true;
                    entry.lastSectionId = progress.sectionId;
                    entry.completionPercent = progress.pct;
                }
            }

            return entry;
        }

        private async Task<HashSet<string>> FetchOwnedStoryIds(string userId)
        {
            try
            {
                var userDoc = await _db.Collection("users").Document(userId).GetSnapshotAsync();
                if (!userDoc.Exists) return new HashSet<string>();

                var arr = userDoc.GetValue<List<object>>("ownedStoryIds");
                if (arr == null) return new HashSet<string>();

                return new HashSet<string>(arr
                    .Where(o => o is string)
                    .Select(o => (string)o));
            }
            catch
            {
                return new HashSet<string>();
            }
        }

        private async Task<Dictionary<string, (int sectionId, float pct)>> FetchProgressMap(string userId)
        {
            var map = new Dictionary<string, (int, float)>();
            try
            {
                var progressSnapshot = await _db
                    .Collection("users").Document(userId)
                    .Collection("storyProgress")
                    .GetSnapshotAsync();

                foreach (var doc in progressSnapshot.Documents)
                {
                    if (!doc.Exists) continue;
                    var sectionId = doc.GetValue<int?>("lastSectionId") ?? 0;
                    var pct = doc.GetValue<float?>("completionPercent") ?? 0f;
                    map[doc.Id] = (sectionId, pct);
                }
            }
            catch { }

            return map;
        }

        private static StoryCatalogEntry DocToCatalogEntry(DocumentSnapshot doc)
        {
            if (!doc.Exists) return null;

            try
            {
                var title = doc.GetValue<string>("metadata.title") ?? "";
                var authorName = doc.GetValue<string>("metadata.authorName") ?? "";
                var description = doc.GetValue<string>("metadata.description") ?? "";
                var language = doc.GetValue<string>("metadata.language") ?? "pt-BR";

                var genres = doc.GetValue<List<string>>("metadata.genre");
                var tags = doc.GetValue<List<string>>("metadata.tags");
                var difficulty = doc.GetValue<string>("metadata.difficulty") ?? "beginner";

                var estimatedMinutes = (int)(doc.GetValue<long>("metadata.estimatedDurationMinutes"));
                var wordCount = (int)(doc.GetValue<long>("stats.wordCount"));
                var sectionCount = (int)(doc.GetValue<long>("stats.sectionCount"));
                var endingCount = (int)(doc.GetValue<long>("stats.endingCount"));

                var rating = (float)(doc.GetValue<double>("stats.averageRating"));
                var ratingCount = (int)(doc.GetValue<long>("stats.ratingCount"));

                var storeType = doc.GetValue<string>("pricing.type") ?? "free";
                string priceTier;
                if (storeType == "free")
                    priceTier = "free";
                else
                    priceTier = doc.GetValue<string>("pricing.priceTier") ?? "free";

                var thumbnailUrl = doc.GetValue<string>("storage.thumbnailSmallPath") ?? "";
                var thumbnailLargeUrl = doc.GetValue<string>("storage.thumbnailLargePath") ?? "";

                var version = "1.0.0";

                return new StoryCatalogEntry
                {
                    id = doc.Id,
                    title = title ?? "",
                    authorName = authorName ?? "",
                    description = description ?? "",
                    language = language ?? "pt-BR",
                    genre = genres?.ToArray(),
                    tags = tags?.ToArray(),
                    difficulty = difficulty ?? "beginner",
                    estimatedMinutes = estimatedMinutes,
                    wordCount = wordCount,
                    sectionCount = sectionCount,
                    endingCount = endingCount,
                    thumbnailUrl = thumbnailUrl ?? "",
                    thumbnailLargeUrl = thumbnailLargeUrl ?? "",
                    rating = rating,
                    ratingCount = ratingCount,
                    priceTier = priceTier ?? "free",
                    priceFormatted = GetPriceFormatted(priceTier),
                    version = version
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StoryCatalogService] Failed to parse document {doc.Id}: {e.Message}");
                return null;
            }
        }

        // ── Mock Data (Editor / Dev Build) ────────────────────────────

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private List<StoryCatalogEntry> CreateMockCatalog()
        {
            return new List<StoryCatalogEntry>
            {
                new StoryCatalogEntry
                {
                    id = "demo", title = "O Labirinto do Arquimago",
                    authorName = "Helena Verne",
                    description = "Um jovem aprendiz de mago deve atravessar o labirinto vivo de um arquimago recluso. Cada corredor muda de forma, e cada sala esconde um desafio arcano diferente. Ideal para iniciantes no mundo dos gamebooks.",
                    language = "pt-BR", genre = new[] { "fantasia", "aventura" },
                    tags = new[] { "fantasia", "masmorra", "mago" },
                    difficulty = "beginner", estimatedMinutes = 45, wordCount = 12000,
                    sectionCount = 13, endingCount = 3,
                    thumbnailUrl = "https://storage.example.com/covers/demo_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/demo_512.webp",
                    rating = 4.2f, ratingCount = 156, priceTier = "free",
                    priceFormatted = GetPriceFormatted("free"),
                    isOwned = true, hasProgress = true, lastSectionId = 7,
                    completionPercent = 53.8f, version = "1.0.0"
                },
                new StoryCatalogEntry
                {
                    id = "mountain-of-fire", title = "A Montanha do Mago de Fogo",
                    authorName = "Rafael Torres",
                    description = "Nas profundezas da Montanha de Cinzas, um mago de fogo forja um exercito de dragoes para conquistar os reinos livres. Voce e o unico que pode escalar a montanha e enfrentar o Mago de Fogo antes que seja tarde.",
                    language = "pt-BR", genre = new[] { "fantasia", "aventura" },
                    tags = new[] { "fantasia", "aventura", "dragao" },
                    difficulty = "intermediate", estimatedMinutes = 210, wordCount = 48000,
                    sectionCount = 142, endingCount = 8,
                    thumbnailUrl = "https://storage.example.com/covers/mountain_of_fire_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/mountain_of_fire_512.webp",
                    rating = 4.7f, ratingCount = 342, priceTier = "tier_2",
                    priceFormatted = GetPriceFormatted("tier_2"), version = "2.1.0"
                },
                new StoryCatalogEntry
                {
                    id = "forest-of-doom", title = "Floresta da Perdicao",
                    authorName = "Camila Soares",
                    description = "Uma floresta amaldicoada esconde o segredo de uma civilizacao perdida. Criaturas sombrias, arvores que sussurram e rios que encantam. Sera que voce consegue encontrar a saida antes que a floresta o consuma?",
                    language = "pt-BR", genre = new[] { "fantasia", "aventura" },
                    tags = new[] { "fantasia", "floresta", "aventura" },
                    difficulty = "beginner", estimatedMinutes = 130, wordCount = 28000,
                    sectionCount = 85, endingCount = 5,
                    thumbnailUrl = "https://storage.example.com/covers/forest_of_doom_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/forest_of_doom_512.webp",
                    rating = 4.1f, ratingCount = 98, priceTier = "tier_1",
                    priceFormatted = GetPriceFormatted("tier_1"), version = "1.5.2"
                },
                new StoryCatalogEntry
                {
                    id = "city-of-thieves", title = "Cidade dos Ladroes",
                    authorName = "Vitor Marques",
                    description = "Nas ruas nebulosas de Ferropolis, guildas de ladroes disputam o controle da cidade a vapor. Voce e um detetive infiltrado que precisa desvendar uma conspiracao que ameaca destruir a cidade inteira.",
                    language = "pt-BR", genre = new[] { "steampunk", "misterio" },
                    tags = new[] { "steampunk", "crime", "misterio" },
                    difficulty = "advanced", estimatedMinutes = 300, wordCount = 72000,
                    sectionCount = 200, endingCount = 12,
                    thumbnailUrl = "https://storage.example.com/covers/city_of_thieves_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/city_of_thieves_512.webp",
                    rating = 4.9f, ratingCount = 521, priceTier = "tier_3",
                    priceFormatted = GetPriceFormatted("tier_3"), version = "1.0.1"
                },
                new StoryCatalogEntry
                {
                    id = "vampire-crypts", title = "As Criptas do Vampiro",
                    authorName = "Isabela Dantas",
                    description = "Um castelo gotico esconde criptas ancestrais onde um vampiro milenar dorme. Voce e um cassador de reliquias que deve explorar as catacumbas e sobreviver aos horrores que despertam com a lua cheia.",
                    language = "pt-BR", genre = new[] { "horror" },
                    tags = new[] { "horror", "vampiro", "gotico" },
                    difficulty = "intermediate", estimatedMinutes = 170, wordCount = 38000,
                    sectionCount = 110, endingCount = 7,
                    thumbnailUrl = "https://storage.example.com/covers/vampire_crypts_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/vampire_crypts_512.webp",
                    rating = 4.5f, ratingCount = 203, priceTier = "tier_2",
                    priceFormatted = GetPriceFormatted("tier_2"), version = "2.0.3"
                },
                new StoryCatalogEntry
                {
                    id = "star-portal", title = "O Portal das Estrelas",
                    authorName = "Lucas Nogueira",
                    description = "Um portal interestelar e descoberto nas ruinas de uma civilizacao antiga. Voce e um explorador que deve atravessar mundos desconhecidos, fazer aliancas com especies alienigenas e desvendar o misterio do Portal das Estrelas.",
                    language = "pt-BR", genre = new[] { "ficcao-cientifica", "aventura" },
                    tags = new[] { "ficcao-cientifica", "portal", "exploracao" },
                    difficulty = "beginner", estimatedMinutes = 80, wordCount = 16000,
                    sectionCount = 50, endingCount = 4,
                    thumbnailUrl = "https://storage.example.com/covers/star_portal_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/star_portal_512.webp",
                    rating = 4.0f, ratingCount = 67, priceTier = "free",
                    priceFormatted = GetPriceFormatted("free"),
                    isOwned = true, version = "1.0.0"
                }
            };
        }
#endif
    }
}
