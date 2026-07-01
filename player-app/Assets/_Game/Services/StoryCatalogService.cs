using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public List<StoryCatalogEntry> OwnedStories
        {
            get { return AllStories.Where(s => s.isOwned).ToList(); }
        }

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

        /// <summary>
        /// Carrega o catalogo completo de historias do Firestore (ou mock no editor/dev).
        /// </summary>
        public async Task LoadCatalog()
        {
            IsLoaded = false;
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(500);
                AllStories = CreateMockCatalog();
#else
                AllStories = await FetchFromFirestore();
#endif
                IsLoaded = true;
                OnCatalogLoaded?.Invoke(AllStories);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        /// <summary>
        /// Carrega os detalhes de uma historia especifica pelo ID.
        /// </summary>
        public async Task<StoryCatalogEntry> LoadStoryDetail(string storyId)
        {
            try
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                await Task.Delay(500);
                var story = AllStories.FirstOrDefault(s => s.id == storyId);
#else
                var story = await FetchStoryDetailFromFirestore(storyId);
#endif
                if (story != null)
                {
                    OnStoryDetailLoaded?.Invoke(story);
                }
                else
                {
                    OnError?.Invoke($"Historia nao encontrada: {storyId}");
                }
                return story;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Filtra o catalogo por genero.
        /// </summary>
        public List<StoryCatalogEntry> FilterByGenre(string genre)
        {
            return AllStories
                .Where(s => s.genre != null && s.genre.Contains(genre, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Filtra o catalogo por tag.
        /// </summary>
        public List<StoryCatalogEntry> FilterByTag(string tag)
        {
            return AllStories
                .Where(s => s.tags != null && s.tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Busca historias por texto no titulo, autor ou descricao.
        /// </summary>
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

        /// <summary>
        /// Ordena o catalogo. Opcoes: "popular", "newest", "rating", "price".
        /// </summary>
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

        /// <summary>
        /// Verifica se uma historia ja foi comprada pelo jogador.
        /// </summary>
        public bool IsStoryOwned(string storyId)
        {
            var story = AllStories.FirstOrDefault(s => s.id == storyId);
            return story != null && story.isOwned;
        }

        /// <summary>
        /// Verifica se o jogador tem progresso salvo em uma historia.
        /// </summary>
        public bool HasProgress(string storyId)
        {
            var story = AllStories.FirstOrDefault(s => s.id == storyId);
            return story != null && story.hasProgress;
        }

        /// <summary>
        /// Retorna o preco formatado para um tier.
        /// </summary>
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

        /// <summary>
        /// Retorna um emoji representativo do genero.
        /// </summary>
        public static string GetGenreIcon(string genre)
        {
            switch (genre.ToLowerInvariant())
            {
                case "fantasy": return "\U0001F3F0";
                case "fantasia": return "\U0001F3F0";
                case "horror": return "\U0001F987";
                case "steampunk": return "\u2699\uFE0F";
                case "scifi": return "\U0001F680";
                case "ficcao-cientifica": return "\U0001F680";
                case "mystery": return "\U0001F50D";
                case "misterio": return "\U0001F50D";
                case "adventure": return "\U0001F5FA\uFE0F";
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private List<StoryCatalogEntry> CreateMockCatalog()
        {
            return new List<StoryCatalogEntry>
            {
                new StoryCatalogEntry
                {
                    id = "demo",
                    title = "O Labirinto do Arquimago",
                    authorName = "Helena Verne",
                    description = "Um jovem aprendiz de mago deve atravessar o labirinto vivo de um arquimago recluso. Cada corredor muda de forma, e cada sala esconde um desafio arcano diferente. Ideal para iniciantes no mundo dos gamebooks.",
                    language = "pt-BR",
                    genre = new[] { "fantasia", "aventura" },
                    tags = new[] { "fantasia", "masmorra", "mago" },
                    difficulty = "beginner",
                    estimatedMinutes = 45,
                    wordCount = 12000,
                    sectionCount = 13,
                    endingCount = 3,
                    thumbnailUrl = "https://storage.example.com/covers/demo_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/demo_512.webp",
                    rating = 4.2f,
                    ratingCount = 156,
                    priceTier = "free",
                    priceFormatted = GetPriceFormatted("free"),
                    isOwned = true,
                    hasProgress = true,
                    lastSectionId = 7,
                    completionPercent = 53.8f,
                    version = "1.0.0"
                },
                new StoryCatalogEntry
                {
                    id = "mountain-of-fire",
                    title = "A Montanha do Mago de Fogo",
                    authorName = "Rafael Torres",
                    description = "Nas profundezas da Montanha de Cinzas, um mago de fogo forja um exercito de dragoes para conquistar os reinos livres. Voce e o unico que pode escalar a montanha e enfrentar o Mago de Fogo antes que seja tarde.",
                    language = "pt-BR",
                    genre = new[] { "fantasia", "aventura" },
                    tags = new[] { "fantasia", "aventura", "dragao" },
                    difficulty = "intermediate",
                    estimatedMinutes = 210,
                    wordCount = 48000,
                    sectionCount = 142,
                    endingCount = 8,
                    thumbnailUrl = "https://storage.example.com/covers/mountain_of_fire_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/mountain_of_fire_512.webp",
                    rating = 4.7f,
                    ratingCount = 342,
                    priceTier = "tier_2",
                    priceFormatted = GetPriceFormatted("tier_2"),
                    isOwned = false,
                    hasProgress = false,
                    lastSectionId = 0,
                    completionPercent = 0f,
                    version = "2.1.0"
                },
                new StoryCatalogEntry
                {
                    id = "forest-of-doom",
                    title = "Floresta da Perdicao",
                    authorName = "Camila Soares",
                    description = "Uma floresta amaldicoada esconde o segredo de uma civilizacao perdida. Criaturas sombrias, arvores que sussurram e rios que encantam. Sera que voce consegue encontrar a saida antes que a floresta o consuma?",
                    language = "pt-BR",
                    genre = new[] { "fantasia", "aventura" },
                    tags = new[] { "fantasia", "floresta", "aventura" },
                    difficulty = "beginner",
                    estimatedMinutes = 130,
                    wordCount = 28000,
                    sectionCount = 85,
                    endingCount = 5,
                    thumbnailUrl = "https://storage.example.com/covers/forest_of_doom_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/forest_of_doom_512.webp",
                    rating = 4.1f,
                    ratingCount = 98,
                    priceTier = "tier_1",
                    priceFormatted = GetPriceFormatted("tier_1"),
                    isOwned = false,
                    hasProgress = false,
                    lastSectionId = 0,
                    completionPercent = 0f,
                    version = "1.5.2"
                },
                new StoryCatalogEntry
                {
                    id = "city-of-thieves",
                    title = "Cidade dos Ladroes",
                    authorName = "Vitor Marques",
                    description = "Nas ruas nebulosas de Ferropolis, guildas de ladroes disputam o controle da cidade a vapor. Voce e um detetive infiltrado que precisa desvendar uma conspiracao que ameaca destruir a cidade inteira.",
                    language = "pt-BR",
                    genre = new[] { "steampunk", "misterio" },
                    tags = new[] { "steampunk", "crime", "misterio" },
                    difficulty = "advanced",
                    estimatedMinutes = 300,
                    wordCount = 72000,
                    sectionCount = 200,
                    endingCount = 12,
                    thumbnailUrl = "https://storage.example.com/covers/city_of_thieves_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/city_of_thieves_512.webp",
                    rating = 4.9f,
                    ratingCount = 521,
                    priceTier = "tier_3",
                    priceFormatted = GetPriceFormatted("tier_3"),
                    isOwned = false,
                    hasProgress = false,
                    lastSectionId = 0,
                    completionPercent = 0f,
                    version = "1.0.1"
                },
                new StoryCatalogEntry
                {
                    id = "vampire-crypts",
                    title = "As Criptas do Vampiro",
                    authorName = "Isabela Dantas",
                    description = "Um castelo gotico esconde criptas ancestrais onde um vampiro milenar dorme. Voce e um cassador de reliquias que deve explorar as catacumbas e sobreviver aos horrores que despertam com a lua cheia.",
                    language = "pt-BR",
                    genre = new[] { "horror" },
                    tags = new[] { "horror", "vampiro", "gotico" },
                    difficulty = "intermediate",
                    estimatedMinutes = 170,
                    wordCount = 38000,
                    sectionCount = 110,
                    endingCount = 7,
                    thumbnailUrl = "https://storage.example.com/covers/vampire_crypts_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/vampire_crypts_512.webp",
                    rating = 4.5f,
                    ratingCount = 203,
                    priceTier = "tier_2",
                    priceFormatted = GetPriceFormatted("tier_2"),
                    isOwned = false,
                    hasProgress = false,
                    lastSectionId = 0,
                    completionPercent = 0f,
                    version = "2.0.3"
                },
                new StoryCatalogEntry
                {
                    id = "star-portal",
                    title = "O Portal das Estrelas",
                    authorName = "Lucas Nogueira",
                    description = "Um portal interestelar e descoberto nas ruinas de uma civilizacao antiga. Voce e um explorador que deve atravessar mundos desconhecidos, fazer aliancas com especies alienigenas e desvendar o misterio do Portal das Estrelas.",
                    language = "pt-BR",
                    genre = new[] { "ficcao-cientifica", "aventura" },
                    tags = new[] { "ficcao-cientifica", "portal", "exploracao" },
                    difficulty = "beginner",
                    estimatedMinutes = 80,
                    wordCount = 16000,
                    sectionCount = 50,
                    endingCount = 4,
                    thumbnailUrl = "https://storage.example.com/covers/star_portal_128.webp",
                    thumbnailLargeUrl = "https://storage.example.com/covers/star_portal_512.webp",
                    rating = 4.0f,
                    ratingCount = 67,
                    priceTier = "free",
                    priceFormatted = GetPriceFormatted("free"),
                    isOwned = true,
                    hasProgress = false,
                    lastSectionId = 0,
                    completionPercent = 0f,
                    version = "1.0.0"
                }
            };
        }
#endif

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        private async Task<List<StoryCatalogEntry>> FetchFromFirestore()
        {
            throw new NotImplementedException("Firestore integration not yet implemented.");
        }

        private async Task<StoryCatalogEntry> FetchStoryDetailFromFirestore(string storyId)
        {
            throw new NotImplementedException("Firestore integration not yet implemented.");
        }
#endif
    }
}
