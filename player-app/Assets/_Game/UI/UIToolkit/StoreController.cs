using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using InteractiveFantasticTales.Core;
using InteractiveFantasticTales.Services;

namespace InteractiveFantasticTales.UI.UIToolkit
{
    public class StoreController : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private VisualTreeAsset _storeUxml;
        [SerializeField] private StyleSheet _storeStyleSheet;
        [SerializeField] private MainUIController _mainUIController;

        private VisualElement _root;
        private VisualElement _storeScreen;
        private VisualElement _storyDetail;
        private VisualElement _libraryScreen;
        private VisualElement _libraryGrid;
        private VisualElement _libraryEmpty;
        private VisualElement _statsScreen;
        private VisualElement _confirmDialog;
        private StoryCatalogService _catalog;

        private string _activeTab = "store";
        private string _selectedGenre = null;
        private string _selectedSort = "popular";
        private string _selectedStoryId = null;
        private string _libraryFilter = "all";
        private string _confirmAction = null;
        private List<StoryCatalogEntry> _currentResults;

        private TextField _searchField;
        private VisualElement _storyGrid;
        private VisualElement _featuredContainer;
        private VisualElement _loadingState;
        private VisualElement _emptyState;
        private VisualElement _errorState;
        private DropdownField _sortDropdown;
        private VisualElement _storeScroll;

        private float _debounceTimer;
        private string _pendingSearch;
        private const float DEBOUNCE_DELAY = 0.2f;

        private LocalizationManager L => LocalizationManager.Instance;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError("StoreController: No UIDocument found!");
                return;
            }

            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            if (_storeStyleSheet == null)
                _storeStyleSheet = Resources.Load<StyleSheet>("UIToolkit/StoreUI");
            if (_storeStyleSheet != null && !_root.styleSheets.Contains(_storeStyleSheet))
                _root.styleSheets.Add(_storeStyleSheet);

            if (_storeUxml != null)
                _storeUxml.CloneTree(_root);

            BindElements();
            BindEvents();

            _catalog = StoryCatalogService.Instance;
            if (_catalog != null)
            {
                _catalog.OnCatalogLoaded += OnCatalogLoaded;
                _catalog.OnError += OnCatalogError;
            }

            _storeScreen.style.display = DisplayStyle.None;
            if (_storyDetail != null) _storyDetail.style.display = DisplayStyle.None;
            if (_libraryScreen != null) _libraryScreen.style.display = DisplayStyle.None;
            if (_statsScreen != null) _statsScreen.style.display = DisplayStyle.None;
            if (_confirmDialog != null) _confirmDialog.style.display = DisplayStyle.None;

            ShowLoading();
            _ = LoadCatalogAsync();
        }

        private void OnDestroy()
        {
            if (_catalog != null)
            {
                _catalog.OnCatalogLoaded -= OnCatalogLoaded;
                _catalog.OnError -= OnCatalogError;
            }
        }

        private void Update()
        {
            if (_pendingSearch != null)
            {
                _debounceTimer -= Time.deltaTime;
                if (_debounceTimer <= 0)
                {
                    ApplyFiltersInternal(_pendingSearch);
                    _pendingSearch = null;
                }
            }
        }

        private async Task LoadCatalogAsync()
        {
            if (_catalog != null)
                await _catalog.LoadCatalog();
        }

        private void BindElements()
        {
            _storeScreen = _root.Q<VisualElement>("store-screen");
            _storyDetail = _root.Q<VisualElement>("story-detail");
            _libraryScreen = _root.Q<VisualElement>("library-screen");
            _libraryGrid = _root.Q<VisualElement>("library-grid");
            _libraryEmpty = _root.Q<VisualElement>("library-empty");
            _statsScreen = _root.Q<VisualElement>("stats-screen");
            _confirmDialog = _root.Q<VisualElement>("confirm-dialog");
            _searchField = _root.Q<TextField>("search-input");
            _storyGrid = _root.Q<VisualElement>("story-grid");
            _featuredContainer = _root.Q<VisualElement>("featured-container");
            _loadingState = _root.Q<VisualElement>("store-loading");
            _emptyState = _root.Q<VisualElement>("store-empty");
            _errorState = _root.Q<VisualElement>("store-error");
            _sortDropdown = _root.Q<DropdownField>("sort-dropdown");
            _storeScroll = _root.Q<ScrollView>("store-scroll");

            if (_sortDropdown != null)
            {
                _sortDropdown.choices = new List<string>
                {
                    "Popular", "Rating", "Preco", "Novos"
                };
                _sortDropdown.index = 0;
            }
        }

        private void BindEvents()
        {
            var tabStore = _root.Q<Button>("tab-store");
            var tabLibrary = _root.Q<Button>("tab-library");
            var tabProfile = _root.Q<Button>("tab-profile");
            if (tabStore != null) tabStore.clicked += () => SwitchTab("store");
            if (tabLibrary != null) tabLibrary.clicked += () => SwitchTab("library");
            if (tabProfile != null) tabProfile.clicked += () => SwitchTab("profile");

            if (_searchField != null)
            {
                _searchField.RegisterValueChangedCallback(evt => OnSearchChanged(evt.newValue));
            }

            if (_sortDropdown != null)
            {
                _sortDropdown.RegisterValueChangedCallback(evt =>
                {
                    var map = new Dictionary<string, string>
                    {
                        { "Popular", "popular" }, { "Rating", "rating" },
                        { "Preco", "price" }, { "Novos", "newest" }
                    };
                    if (map.TryGetValue(evt.newValue, out var sort))
                        SetSort(sort);
                });
            }

            var chipTodos = _root.Q<Button>("chip-todos");
            var chipFantasia = _root.Q<Button>("chip-fantasia");
            var chipHorror = _root.Q<Button>("chip-horror");
            var chipScifi = _root.Q<Button>("chip-scifi");
            var chipSteampunk = _root.Q<Button>("chip-steampunk");
            var chipMisterio = _root.Q<Button>("chip-misterio");

            if (chipTodos != null) chipTodos.clicked += () => SetGenre(null);
            if (chipFantasia != null) chipFantasia.clicked += () => SetGenre("fantasia");
            if (chipHorror != null) chipHorror.clicked += () => SetGenre("horror");
            if (chipScifi != null) chipScifi.clicked += () => SetGenre("ficcao-cientifica");
            if (chipSteampunk != null) chipSteampunk.clicked += () => SetGenre("steampunk");
            if (chipMisterio != null) chipMisterio.clicked += () => SetGenre("misterio");

            var detailBack = _root.Q<Button>("detail-back-btn");
            if (detailBack != null) detailBack.clicked += HideStoryDetail;

            var previewPlay = _root.Q<Button>("preview-play-btn");
            if (previewPlay != null) previewPlay.clicked += () => PlayPreview(_selectedStoryId);

            var buyBtn = _root.Q<Button>("detail-buy-btn");
            if (buyBtn != null) buyBtn.clicked += () => BuyStory(_selectedStoryId);

            var downloadBtn = _root.Q<Button>("detail-download-btn");
            if (downloadBtn != null) downloadBtn.clicked += () => DownloadStory(_selectedStoryId);

            var continueBtn = _root.Q<Button>("detail-continue-btn");
            if (continueBtn != null) continueBtn.clicked += () => ContinueStory(_selectedStoryId);

            var restartBtn = _root.Q<Button>("detail-restart-btn");
            if (restartBtn != null) restartBtn.clicked += () => RestartStory(_selectedStoryId);

            var retryBtn = _root.Q<Button>("retry-btn");
            if (retryBtn != null) retryBtn.clicked += async () => { ShowLoading(); await LoadCatalogAsync(); };

            var libChipAll = _root.Q<Button>("lib-chip-all");
            var libChipProgress = _root.Q<Button>("lib-chip-inprogress");
            var libChipCompleted = _root.Q<Button>("lib-chip-completed");
            if (libChipAll != null) libChipAll.clicked += () => FilterLibrary("all");
            if (libChipProgress != null) libChipProgress.clicked += () => FilterLibrary("in_progress");
            if (libChipCompleted != null) libChipCompleted.clicked += () => FilterLibrary("completed");

            var statsBack = _root.Q<Button>("stats-back-btn");
            if (statsBack != null) statsBack.clicked += HideStats;

            var exploreBtn = _root.Q<Button>("library-explore-btn");
            if (exploreBtn != null) exploreBtn.clicked += () => SwitchTab("store");

            var statsClearBtn = _root.Q<Button>("stats-clear-btn");
            if (statsClearBtn != null) statsClearBtn.clicked += () => ShowConfirmDialog("clear_stats",
                "Limpar dados", $"Tem certeza que deseja limpar todos os dados de \"{GetSelectedStoryTitle()}\"? Esta acao nao pode ser desfeita.", () =>
                {
                    ClearStoryDataInternal(_selectedStoryId);
                });

            var confirmCancel = _root.Q<Button>("confirm-dialog-cancel");
            var confirmOk = _root.Q<Button>("confirm-dialog-ok");
            if (confirmCancel != null) confirmCancel.clicked += HideConfirmDialog;
            if (confirmOk != null) confirmOk.clicked += ExecuteConfirmAction;

            var detailStatsBtn = _root.Q<Button>("detail-stats-btn");
            if (detailStatsBtn != null) detailStatsBtn.clicked += () => ShowStatsFromProgress(_selectedStoryId);

            if (_confirmDialog?.Q<VisualElement>("confirm-dialog-bg") is VisualElement bg)
                bg.RegisterCallback<ClickEvent>(evt => { if (evt.target == bg) HideConfirmDialog(); });
        }

        public void ShowStoreScreen()
        {
            _storeScreen.style.display = DisplayStyle.Flex;
            if (_storyDetail != null) _storyDetail.style.display = DisplayStyle.None;

            if (_catalog != null && _catalog.IsLoaded)
                OnCatalogLoaded(_catalog.AllStories);
        }

        public void HideStoreScreen()
        {
            _storeScreen.style.display = DisplayStyle.None;
            if (_storyDetail != null) _storyDetail.style.display = DisplayStyle.None;
        }

        public void SwitchTab(string tab)
        {
            if (tab == _activeTab) return;

            if (tab == "profile")
            {
                _mainUIController?.ShowToast("Em breve");
                return;
            }

            _activeTab = tab;
            UpdateTabSelection();

            if (tab == "store")
            {
                _storeScreen.style.display = DisplayStyle.Flex;
                if (_libraryScreen != null) _libraryScreen.style.display = DisplayStyle.None;
                if (_storyDetail != null) _storyDetail.style.display = DisplayStyle.None;
                if (_statsScreen != null) _statsScreen.style.display = DisplayStyle.None;
                if (_catalog != null && _catalog.IsLoaded)
                    OnCatalogLoaded(_catalog.AllStories);
            }
            else if (tab == "library")
            {
                ShowLibrary();
            }
        }

        private void UpdateTabSelection()
        {
            var tabs = new[] { "store", "library", "profile" };
            foreach (var t in tabs)
            {
                var btn = _root.Q<Button>($"tab-{t}");
                if (btn != null)
                {
                    if (t == _activeTab) btn.AddToClassList("selected");
                    else btn.RemoveFromClassList("selected");
                }
            }
        }

        public void SetGenre(string genre)
        {
            _selectedGenre = genre;
            UpdateChipSelection();
            ApplyFilters();
        }

        private void UpdateChipSelection()
        {
            var chipMap = new Dictionary<string, string>
            {
                { null, "chip-todos" },
                { "fantasia", "chip-fantasia" },
                { "horror", "chip-horror" },
                { "ficcao-cientifica", "chip-scifi" },
                { "steampunk", "chip-steampunk" },
                { "misterio", "chip-misterio" }
            };

            foreach (var kv in chipMap)
            {
                var chip = _root.Q<Button>(kv.Value);
                if (chip != null)
                {
                    if (kv.Key == _selectedGenre) chip.AddToClassList("selected");
                    else chip.RemoveFromClassList("selected");
                }
            }
        }

        public void SetSort(string sort)
        {
            _selectedSort = sort;
            ApplyFilters();
        }

        public void OnSearchChanged(string query)
        {
            _pendingSearch = query;
            _debounceTimer = DEBOUNCE_DELAY;
        }

        public void ApplyFilters()
        {
            ApplyFiltersInternal(_searchField?.value ?? "");
        }

        private void ApplyFiltersInternal(string query)
        {
            if (_catalog == null || !_catalog.IsLoaded) return;

            var results = _catalog.AllStories;

            if (!string.IsNullOrWhiteSpace(query))
                results = _catalog.Search(query);

            if (!string.IsNullOrEmpty(_selectedGenre))
                results = results.Where(s => s.genre != null &&
                    s.genre.Contains(_selectedGenre, StringComparer.OrdinalIgnoreCase)).ToList();

            switch (_selectedSort)
            {
                case "popular":
                    results = results.OrderByDescending(s => s.ratingCount).ToList();
                    break;
                case "rating":
                    results = results.OrderByDescending(s => s.rating).ToList();
                    break;
                case "price":
                    results = results.OrderBy(s => GetTierOrder(s.priceTier)).ToList();
                    break;
                case "newest":
                    break;
                default:
                    results = results.OrderByDescending(s => s.ratingCount).ToList();
                    break;
            }

            _currentResults = results;
            RenderStoreGrid(results);
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

        public void ShowStoryDetail(string storyId)
        {
            if (_catalog == null) return;

            var story = _catalog.AllStories.FirstOrDefault(s => s.id == storyId);
            if (story == null) return;

            _selectedStoryId = storyId;
            RenderStoryDetail(story);
            _storeScreen.style.display = DisplayStyle.None;
            if (_storyDetail != null) _storyDetail.style.display = DisplayStyle.Flex;
        }

        public void HideStoryDetail()
        {
            _selectedStoryId = null;
            if (_storyDetail != null) _storyDetail.style.display = DisplayStyle.None;
            _storeScreen.style.display = DisplayStyle.Flex;
        }

        public void PlayPreview(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            if (storyId == "demo")
            {
                _mainUIController?.StartStory(storyId);
                return;
            }

            StartCoroutine(PlayPreviewRoutine(storyId));
        }

        private IEnumerator PlayPreviewRoutine(string storyId)
        {
            _mainUIController?.ShowToast("Carregando previa...");

            var story = _catalog?.AllStories.FirstOrDefault(s => s.id == storyId);
            if (story == null)
            {
                _mainUIController?.ShowToast("Historia nao encontrada", "bad");
                yield break;
            }

            var textAsset = Resources.Load<TextAsset>($"demo-labirinto-do-arquimago");
            if (textAsset != null)
            {
                var engine = GameEngine.Instance;
                if (engine != null)
                {
                    engine.LoadStoryFromJson(textAsset.text);
                    engine.GoToSection(engine.CurrentStory?.metadata.startSection ?? 1);
                    _mainUIController?.HideStoreUI();
                }
            }
            else
            {
                _mainUIController?.ShowToast("Previa indisponivel", "bad");
            }
        }

        public void BuyStory(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            var story = _catalog?.AllStories.FirstOrDefault(s => s.id == storyId);
            if (story == null) return;

            if (!AgeGate.PurchaseAllowed)
            {
                _mainUIController?.ShowToast(L["store_purchase_blocked_age"], "bad");
                return;
            }

            if (story.priceTier == "free")
            {
                DownloadStory(storyId);
                return;
            }

            StartCoroutine(BuyStoryRoutine(storyId, story.priceTier));
        }

        private IEnumerator BuyStoryRoutine(string storyId, string priceTier)
        {
            _mainUIController?.ShowToast(L["store_purchasing"] ?? "Processando compra...");

            var authService = new CloudRunAuthService();
            authService.Initialize();

            var purchaseService = new PurchaseService(authService);
            purchaseService.Initialize();

            var purchaseToken = $"mock_token_{storyId}_{Guid.NewGuid():N}";
            PurchaseVerifyResponse response = null;

            yield return purchaseService.VerifyPurchase(storyId, purchaseToken, "mock_store", resp =>
            {
                response = resp;
            });

            if (response != null && response.success)
            {
                var story = _catalog?.AllStories.FirstOrDefault(s => s.id == storyId);
                if (story != null)
                {
                    story.isOwned = true;
                    RenderStoryDetail(story);
                }
                _mainUIController?.ShowToast(L["store_purchase_success"] ?? "Compra realizada com sucesso!", "good");
                RefreshLibrary();
                ApplyFilters();
            }
            else
            {
                _mainUIController?.ShowToast(
                    L["store_purchase_failed"] ?? $"Falha na compra: {response?.error ?? "Erro desconhecido"}", "bad");
            }
        }

        public void ContinueStory(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            if (storyId == "demo")
            {
                _mainUIController?.ContinueStoryDemo();
                return;
            }

            _mainUIController?.ContinueStory(storyId);
        }

        public void RestartStory(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            var story = _catalog?.AllStories.FirstOrDefault(s => s.id == storyId);
            if (story == null || !story.isOwned) return;

            var confirmMsg = L["store_restart_confirm"] ?? $"Tem certeza que deseja recomecar \"{story.title}\"?";
            var engine = GameEngine.Instance;
            if (engine != null)
            {
                var textAsset = Resources.Load<TextAsset>($"demo-labirinto-do-arquimago");
                if (textAsset != null)
                {
                    engine.LoadStoryFromJson(textAsset.text);
                    _mainUIController?.StartNewStoryFlow(storyId);
                }
            }
        }

        public void DownloadStory(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            var story = _catalog?.AllStories.FirstOrDefault(s => s.id == storyId);
            if (story == null) return;

            if (!story.isOwned && story.priceTier == "free")
            {
                story.isOwned = true;
            }

            StartCoroutine(DownloadStoryRoutine(story));
        }

        private IEnumerator DownloadStoryRoutine(StoryCatalogEntry story)
        {
            _mainUIController?.ShowToast(L["store_downloading"] ?? "Baixando historia...");

            yield return new WaitForSeconds(0.8f);

            story.isOwned = true;
            RenderStoryDetail(story);
            _mainUIController?.ShowToast(L["store_ready"] ?? "Historia pronta para jogar!", "good");
            RefreshLibrary();
            ApplyFilters();
        }

        private void OnCatalogLoaded(List<StoryCatalogEntry> stories)
        {
            ShowContent();
            _currentResults = stories;
            RenderFeatured(stories);
            RenderStoreGrid(stories);
        }

        private void OnCatalogError(string error)
        {
            ShowError(error);
        }

        private void RenderStoreGrid(List<StoryCatalogEntry> stories)
        {
            _storyGrid?.Clear();

            if (stories == null || stories.Count == 0)
            {
                ShowEmpty();
                return;
            }

            ShowContent();
            foreach (var story in stories)
            {
                _storyGrid?.Add(CreateStoryCard(story));
            }
        }

        private void RenderFeatured(List<StoryCatalogEntry> stories)
        {
            _featuredContainer?.Clear();

            var featured = stories
                .Where(s => s.rating >= 3.5f && s.ratingCount > 0)
                .OrderByDescending(s => s.rating)
                .Take(3)
                .ToList();

            foreach (var story in featured)
            {
                _featuredContainer?.Add(CreateStoryCard(story, featured: true));
            }
        }

        private VisualElement CreateStoryCard(StoryCatalogEntry story, bool featured = false)
        {
            var card = new VisualElement();
            card.AddToClassList("story-card");
            if (featured) card.AddToClassList("featured");
            card.tooltip = story.title;
            card.tabIndex = 0;
            card.focusable = true;

            // Thumbnail area
            var thumb = new VisualElement();
            thumb.AddToClassList("story-card-thumb");
            if (featured) thumb.AddToClassList("featured-thumb");

            if (featured)
            {
                var overlay = new VisualElement();
                overlay.AddToClassList("featured-thumb-overlay");
                thumb.Add(overlay);

                var info = new VisualElement();
                info.AddToClassList("featured-card-info");

                var title = new Label(story.title);
                title.AddToClassList("story-card-title");
                title.AddToClassList("featured-title");
                info.Add(title);

                var author = new Label(story.authorName ?? "");
                author.AddToClassList("story-card-author");
                author.AddToClassList("featured-author");
                info.Add(author);

                thumb.Add(info);
            }
            else
            {
                var placeholder = new VisualElement();
                placeholder.AddToClassList("thumb-placeholder");
                thumb.Add(placeholder);
            }

            card.Add(thumb);

            // Tags for featured cards
            if (featured && story.genre != null && story.genre.Length > 0)
            {
                var tagsRow = new VisualElement();
                tagsRow.AddToClassList("featured-card-tags");
                foreach (var g in story.genre.Take(3))
                {
                    var tag = new Label(StoryCatalogService.GetGenreIcon(g) + " " + g);
                    tag.AddToClassList("featured-card-tag");
                    tagsRow.Add(tag);
                }
                card.Add(tagsRow);
            }

            if (!featured)
            {
                var cardInfo = new VisualElement();
                cardInfo.AddToClassList("card-info");

                var title = new Label(story.title);
                title.AddToClassList("story-card-title");
                cardInfo.Add(title);

                var author = new Label(story.authorName ?? "");
                author.AddToClassList("story-card-author");
                cardInfo.Add(author);

                var rating = new VisualElement();
                rating.AddToClassList("story-card-rating");

                var stars = new Label(FormatStars(story.rating));
                stars.AddToClassList("rating-stars");
                rating.Add(stars);

                var count = new Label($"({story.ratingCount})");
                count.AddToClassList("rating-count");
                rating.Add(count);

                cardInfo.Add(rating);

                var bottom = new VisualElement();
                bottom.AddToClassList("card-bottom");

                var badge = new Label();
                badge.AddToClassList("story-card-badge");
                if (story.isOwned)
                {
                    badge.text = L["store_badge_owned"] ?? "Possui";
                    badge.AddToClassList("owned");
                }
                else if (story.priceTier == "free")
                {
                    badge.text = L["store_badge_free"] ?? "Gratis";
                    badge.AddToClassList("free");
                }
                else
                {
                    badge.text = story.priceFormatted ?? StoryCatalogService.GetPriceFormatted(story.priceTier);
                    badge.AddToClassList("paid");
                }
                bottom.Add(badge);

                cardInfo.Add(bottom);
                card.Add(cardInfo);

                // Progress bar for owned stories with progress
                if (story.isOwned && story.hasProgress && story.completionPercent > 0)
                {
                    var progress = new VisualElement();
                    progress.AddToClassList("story-card-progress");
                    progress.style.display = DisplayStyle.Flex;

                    var fill = new VisualElement();
                    fill.AddToClassList("progress-fill");
                    fill.style.width = Length.Percent(story.completionPercent);
                    progress.Add(fill);

                    card.Add(progress);
                }
            }

            // Featured card footer
            if (featured)
            {
                var footer = new VisualElement();
                footer.AddToClassList("featured-card-footer");

                var rating = new VisualElement();
                rating.AddToClassList("story-card-rating");

                var stars = new Label(FormatStars(story.rating));
                stars.AddToClassList("rating-stars");
                rating.Add(stars);

                footer.Add(rating);

                var badge = new Label();
                badge.AddToClassList("story-card-badge");
                if (story.priceTier == "free")
                    badge.text = L["store_badge_free"] ?? "Gratis";
                else
                    badge.text = story.priceFormatted ?? StoryCatalogService.GetPriceFormatted(story.priceTier);
                badge.AddToClassList(story.priceTier == "free" ? "free" : "paid");
                footer.Add(badge);

                card.Add(footer);
            }

            var storyId = story.id;
            card.RegisterCallback<ClickEvent>(evt =>
            {
                ShowStoryDetail(storyId);
            });
            card.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                ShowStoryDetail(storyId);
            });

            return card;
        }

        private string FormatStars(float rating)
        {
            int full = Mathf.FloorToInt(rating);
            bool half = rating - full >= 0.5f;
            var result = new string('\u2605', full);
            if (half) result += "\u00BD";
            int empty = 5 - full - (half ? 1 : 0);
            result += new string('\u2606', empty);
            return result;
        }

        private void RenderStoryDetail(StoryCatalogEntry story)
        {
            if (story == null) return;

            var title = _root.Q<Label>("detail-title");
            var author = _root.Q<Label>("detail-author");
            var stars = _root.Q<Label>("detail-rating-stars");
            var count = _root.Q<Label>("detail-rating-count");
            var difficulty = _root.Q<Label>("detail-difficulty");
            var tags = _root.Q<VisualElement>("detail-tags");
            var description = _root.Q<Label>("detail-description");

            var secValue = _root.Q<Label>("detail-stat-sections-value");
            var endValue = _root.Q<Label>("detail-stat-endings-value");
            var durValue = _root.Q<Label>("detail-stat-duration-value");
            var wordValue = _root.Q<Label>("detail-stat-words-value");

            if (title != null) title.text = story.title;
            if (author != null) author.text = story.authorName ?? "";
            if (stars != null) stars.text = FormatStars(story.rating);
            if (count != null) count.text = $"({story.ratingCount})";
            if (difficulty != null)
            {
                var diffMap = new Dictionary<string, string>
                {
                    { "beginner", "\U0001F7E2 Iniciante" },
                    { "intermediate", "\U0001F7E1 Intermediario" },
                    { "advanced", "\U0001F534 Avancado" }
                };
                difficulty.text = diffMap.ContainsKey(story.difficulty)
                    ? diffMap[story.difficulty] : story.difficulty ?? "";
            }

            if (tags != null)
            {
                tags.Clear();
                if (story.genre != null)
                {
                    foreach (var g in story.genre)
                    {
                        var tag = new Label(StoryCatalogService.GetGenreIcon(g) + " " + g);
                        tag.AddToClassList("detail-tag");
                        tags.Add(tag);
                    }
                }
            }

            if (description != null) description.text = story.description ?? "";

            if (secValue != null) secValue.text = story.sectionCount.ToString();
            if (endValue != null) endValue.text = story.endingCount.ToString();
            if (durValue != null) durValue.text = $"{story.estimatedMinutes} min";
            if (wordValue != null) wordValue.text = story.wordCount.ToString("N0");

            // Action buttons
            var previewCard = _root.Q<VisualElement>("detail-preview-card");
            var buyBtn = _root.Q<Button>("detail-buy-btn");
            var downloadBtn = _root.Q<Button>("detail-download-btn");
            var ownedSection = _root.Q<VisualElement>("detail-owned-section");

            if (previewCard != null) previewCard.style.display = DisplayStyle.None;
            if (buyBtn != null) buyBtn.style.display = DisplayStyle.None;
            if (downloadBtn != null) downloadBtn.style.display = DisplayStyle.None;
            if (ownedSection != null) ownedSection.style.display = DisplayStyle.None;

            if (story.isOwned)
            {
                if (ownedSection != null)
                {
                    ownedSection.style.display = DisplayStyle.Flex;

                    var progressPct = _root.Q<Label>("detail-progress-pct");
                    var progressFill = _root.Q<VisualElement>("detail-progress-fill");
                    var continueBtn = _root.Q<Button>("detail-continue-btn");

                    if (progressPct != null)
                        progressPct.text = story.hasProgress && story.completionPercent > 0
                            ? $"{story.completionPercent:F0}%" : "0%";
                    if (progressFill != null)
                        progressFill.style.width = Length.Percent(story.completionPercent);
                    if (continueBtn != null)
                        continueBtn.style.display = story.hasProgress ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
            else
            {
                if (previewCard != null)
                {
                    previewCard.style.display = DisplayStyle.Flex;
                    var previewText = previewCard.Q<Label>("preview-text");
                    if (previewText != null)
                        previewText.text = story.description?.Length > 120
                            ? story.description.Substring(0, 120) + "..." : story.description ?? "";
                }

                if (story.priceTier == "free")
                {
                    if (downloadBtn != null)
                    {
                        downloadBtn.style.display = DisplayStyle.Flex;
                        downloadBtn.text = L["store_download_play"] ?? "Baixar e Jogar";
                    }
                }
                else
                {
                    if (buyBtn != null)
                    {
                        buyBtn.style.display = DisplayStyle.Flex;
                        buyBtn.text = string.Format(L["store_buy_cta"] ?? "{0} \u2014 Comprar",
                            story.priceFormatted ?? StoryCatalogService.GetPriceFormatted(story.priceTier));
                    }
                }
            }
        }

        private void ShowLoading()
        {
            if (_loadingState != null) _loadingState.style.display = DisplayStyle.Flex;
            if (_emptyState != null) _emptyState.style.display = DisplayStyle.None;
            if (_errorState != null) _errorState.style.display = DisplayStyle.None;
            if (_storyGrid != null) _storyGrid.Clear();
            if (_featuredContainer != null) _featuredContainer.Clear();
        }

        private void ShowEmpty()
        {
            if (_loadingState != null) _loadingState.style.display = DisplayStyle.None;
            if (_emptyState != null) _emptyState.style.display = DisplayStyle.Flex;
            if (_errorState != null) _errorState.style.display = DisplayStyle.None;
        }

        private void ShowError(string message)
        {
            if (_loadingState != null) _loadingState.style.display = DisplayStyle.None;
            if (_emptyState != null) _emptyState.style.display = DisplayStyle.None;
            if (_errorState != null) _errorState.style.display = DisplayStyle.Flex;

            var errorDesc = _root.Q<Label>("error-desc");
            if (errorDesc != null) errorDesc.text = message ?? "Falha ao carregar catalogo.";
        }

        private void ShowContent()
        {
            if (_loadingState != null) _loadingState.style.display = DisplayStyle.None;
            if (_emptyState != null) _emptyState.style.display = DisplayStyle.None;
            if (_errorState != null) _errorState.style.display = DisplayStyle.None;
        }

        private void ShowLibraryContent()
        {
            var libLoading = _root.Q<VisualElement>("library-loading");
            if (libLoading != null) libLoading.style.display = DisplayStyle.None;
        }

        private void ShowLibraryEmpty()
        {
            var libLoading = _root.Q<VisualElement>("library-loading");
            if (libLoading != null) libLoading.style.display = DisplayStyle.None;
            if (_libraryGrid != null) _libraryGrid.Clear();
            if (_libraryEmpty != null) _libraryEmpty.style.display = DisplayStyle.Flex;
        }

        private void HideLibraryEmpty()
        {
            if (_libraryEmpty != null) _libraryEmpty.style.display = DisplayStyle.None;
        }

        // ===== LIBRARY =====

        public void ShowLibrary()
        {
            _storeScreen.style.display = DisplayStyle.None;
            if (_storyDetail != null) _storyDetail.style.display = DisplayStyle.None;
            if (_statsScreen != null) _statsScreen.style.display = DisplayStyle.None;
            if (_libraryScreen != null) _libraryScreen.style.display = DisplayStyle.Flex;

            RefreshLibrary();
        }

        public void RefreshLibrary()
        {
            if (_catalog == null) return;

            var owned = _catalog.OwnedStories;
            if (owned == null || owned.Count == 0)
            {
                ShowLibraryEmpty();
                return;
            }

            HideLibraryEmpty();
            ShowLibraryContent();

            var stories = owned.Select(s => LoadProgressForStory(s)).ToList();

            if (_libraryFilter == "in_progress")
                stories = stories.Where(s => s.completionPercent > 0 && s.completionPercent < 100).ToList();
            else if (_libraryFilter == "completed")
                stories = stories.Where(s => s.completionPercent >= 100).ToList();

            RenderLibraryCards(stories);
        }

        public void FilterLibrary(string filter)
        {
            _libraryFilter = filter;
            UpdateLibraryChipSelection();
            RefreshLibrary();
        }

        private void UpdateLibraryChipSelection()
        {
            var map = new Dictionary<string, string>
            {
                { "all", "lib-chip-all" },
                { "in_progress", "lib-chip-inprogress" },
                { "completed", "lib-chip-completed" }
            };

            foreach (var kv in map)
            {
                var chip = _root.Q<Button>(kv.Value);
                if (chip != null)
                {
                    if (kv.Key == _libraryFilter) chip.AddToClassList("selected");
                    else chip.RemoveFromClassList("selected");
                }
            }
        }

        private void RenderLibraryCards(List<StoryProgressData> stories)
        {
            _libraryGrid?.Clear();

            foreach (var progress in stories)
            {
                _libraryGrid?.Add(CreateLibraryCard(progress));
            }

            if (stories.Count == 0)
            {
                ShowLibraryEmpty();
            }
        }

        private VisualElement CreateLibraryCard(StoryProgressData progress)
        {
            var card = new VisualElement();
            card.AddToClassList("library-card");
            card.tabIndex = 0;
            card.focusable = true;

            var thumb = new VisualElement();
            thumb.AddToClassList("library-card-thumb");
            var placeholder = new VisualElement();
            placeholder.AddToClassList("thumb-placeholder");
            thumb.Add(placeholder);
            card.Add(thumb);

            var info = new VisualElement();
            info.AddToClassList("library-card-info");

            var title = new Label(progress.title ?? "");
            title.AddToClassList("library-card-title");
            info.Add(title);

            var author = new Label(progress.authorName ?? "");
            author.AddToClassList("library-card-author");
            info.Add(author);

            var meta = new VisualElement();
            meta.AddToClassList("library-card-meta");

            if (!string.IsNullOrEmpty(progress.lastPlayedText))
            {
                var lastPlayed = new Label(progress.lastPlayedText);
                lastPlayed.AddToClassList("library-card-last-played");
                meta.Add(lastPlayed);
            }

            if (!string.IsNullOrEmpty(progress.statsMini))
            {
                var statsMini = new Label(progress.statsMini);
                statsMini.AddToClassList("library-card-stats-mini");
                meta.Add(statsMini);
            }

            info.Add(meta);

            if (progress.lastSectionId > 0)
            {
                var sectionBadge = new Label($"Secao {progress.lastSectionId}");
                sectionBadge.AddToClassList("library-card-section-badge");
                meta.Add(sectionBadge);
            }

            card.Add(info);

            var progressBar = new VisualElement();
            progressBar.AddToClassList("library-card-progress");

            var bar = new VisualElement();
            bar.AddToClassList("library-card-progress-bar");

            var fill = new VisualElement();
            fill.AddToClassList("library-card-progress-fill");
            fill.style.width = Length.Percent(Mathf.Clamp(progress.completionPercent, 0, 100));
            bar.Add(fill);

            var pctLabel = new Label($"{(int)progress.completionPercent}%");
            pctLabel.AddToClassList("library-card-progress-pct");

            progressBar.Add(bar);
            progressBar.Add(pctLabel);
            card.Add(progressBar);

            var actions = new VisualElement();
            actions.AddToClassList("library-card-actions");

            var continueBtn = new Button();
            continueBtn.AddToClassList("library-action-btn");
            continueBtn.AddToClassList("primary");
            continueBtn.text = "\u25B6 Continuar";
            continueBtn.tabIndex = 0;
            var storyId = progress.storyId;
            continueBtn.clicked += () => ContinueStoryLib(storyId);
            actions.Add(continueBtn);

            var restartBtn = new Button();
            restartBtn.AddToClassList("library-action-btn");
            restartBtn.text = "\u21BA Recomecar";
            restartBtn.tabIndex = 0;
            var rid = storyId;
            restartBtn.clicked += () => ShowConfirmDialog("restart",
                "Recomecar historia",
                $"Tem certeza que deseja recomecar \"{progress.title}\"? Todo o progresso sera perdido.",
                () => RestartStoryLib(rid));
            actions.Add(restartBtn);

            var statsBtn = new Button();
            statsBtn.AddToClassList("library-action-btn");
            statsBtn.text = "\U0001F4CA Stats";
            statsBtn.tabIndex = 0;
            var sid = storyId;
            statsBtn.clicked += () => ShowStatsFromProgress(sid);
            actions.Add(statsBtn);

            var clearBtn = new Button();
            clearBtn.AddToClassList("library-action-btn");
            clearBtn.text = "\U0001F5D1 Limpar";
            clearBtn.tabIndex = 0;
            var cid = storyId;
            clearBtn.clicked += () => ShowConfirmDialog("clear",
                "Limpar dados",
                $"Tem certeza que deseja limpar todos os dados de \"{progress.title}\"?",
                () => ClearStoryData(cid));
            actions.Add(clearBtn);

            card.Add(actions);

            return card;
        }

        private StoryProgressData LoadProgressForStory(StoryCatalogEntry story)
        {
            var progress = new StoryProgressData
            {
                storyId = story.id,
                title = story.title,
                authorName = story.authorName,
                completionPercent = story.completionPercent,
                lastSectionId = story.lastSectionId
            };

            string key = $"ift_progress_{story.id}";
            string json = PlayerPrefs.GetString(key, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var saved = JsonUtility.FromJson<StoryProgressData>(json);
                    if (saved != null)
                    {
                        progress.timePlayed = saved.timePlayed;
                        progress.sessionsPlayed = saved.sessionsPlayed;
                        progress.sectionsVisited = saved.sectionsVisited;
                        progress.totalSections = saved.totalSections > 0 ? saved.totalSections : story.sectionCount;
                        progress.choicesMade = saved.choicesMade;
                        progress.endingsFound = saved.endingsFound;
                        progress.totalEndings = saved.totalEndings > 0 ? saved.totalEndings : story.endingCount;
                        progress.combatsWon = saved.combatsWon;
                        progress.combatsLost = saved.combatsLost;
                        progress.combatsFled = saved.combatsFled;
                        progress.deaths = saved.deaths;
                        progress.rewindsUsed = saved.rewindsUsed;
                        progress.firstSession = saved.firstSession;
                        progress.lastSession = saved.lastSession;
                    }
                }
                catch { }
            }

            if (progress.totalSections <= 0) progress.totalSections = story.sectionCount;
            if (progress.totalEndings <= 0) progress.totalEndings = story.endingCount;
            if (string.IsNullOrEmpty(progress.firstSession)) progress.firstSession = System.DateTime.Now.ToString("dd/MM/yy");
            if (string.IsNullOrEmpty(progress.lastSession)) progress.lastSession = System.DateTime.Now.ToString("dd/MM/yy");

            if (progress.lastSession != progress.firstSession)
                progress.lastPlayedText = $"ha {(System.DateTime.Now - System.DateTime.Now).Days} dias";
            else
                progress.lastPlayedText = "hoje";

            progress.statsMini = FormatTimePlayed(progress.timePlayed) + " | " +
                $"{progress.endingsFound}/{progress.totalEndings} finais";

            return progress;
        }

        private void SaveProgressForStory(string storyId, StoryProgressData progress)
        {
            if (progress == null || string.IsNullOrEmpty(storyId)) return;
            string key = $"ift_progress_{storyId}";
            string json = JsonUtility.ToJson(progress);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public void ContinueStoryLib(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            if (storyId == "demo")
            {
                _mainUIController?.ContinueStoryDemo();
                return;
            }

            _mainUIController?.ContinueStory(storyId);
        }

        public void RestartStoryLib(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            var progress = new StoryProgressData { storyId = storyId };
            SaveProgressForStory(storyId, progress);

            _mainUIController?.StartNewStoryFlow(storyId);
        }

        public void ClearStoryData(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;
            ShowConfirmDialog("clear", "Limpar dados",
                $"Tem certeza que deseja limpar todos os dados de \"{GetStoryTitle(storyId)}\"?",
                () => ClearStoryDataInternal(storyId));
        }

        private void ClearStoryDataInternal(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            string key = $"ift_progress_{storyId}";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey($"ift_save_{storyId}_last");
            PlayerPrefs.Save();

            var story = _catalog?.AllStories.FirstOrDefault(s => s.id == storyId);
            if (story != null)
            {
                story.hasProgress = false;
                story.completionPercent = 0;
                story.lastSectionId = 0;
            }

            _mainUIController?.ShowToast("Dados limpos com sucesso!", "good");
            HideStats();
            RefreshLibrary();
        }

        // ===== STATS SCREEN =====

        public void ShowStatsFromProgress(string storyId)
        {
            if (string.IsNullOrEmpty(storyId)) return;

            var story = _catalog?.AllStories.FirstOrDefault(s => s.id == storyId);
            if (story == null) return;

            var progress = LoadProgressForStory(story);
            _selectedStoryId = storyId;

            _storeScreen.style.display = DisplayStyle.None;
            if (_storyDetail != null) _storyDetail.style.display = DisplayStyle.None;
            if (_libraryScreen != null) _libraryScreen.style.display = DisplayStyle.None;
            if (_statsScreen != null) _statsScreen.style.display = DisplayStyle.Flex;

            RenderStats(progress);
        }

        private void RenderStats(StoryProgressData progress)
        {
            if (progress == null) return;

            var titleLabel = _root.Q<Label>("stats-story-title");
            if (titleLabel != null) titleLabel.text = progress.title ?? "";

            SetStat("stats-time", FormatTimePlayed(progress.timePlayed));
            SetStat("stats-sessions", progress.sessionsPlayed.ToString());
            SetStat("stats-sections", $"{progress.sectionsVisited} / {progress.totalSections}");
            SetStat("stats-choices", progress.choicesMade.ToString());
            SetStat("stats-endings", $"{progress.endingsFound} / {progress.totalEndings}");
            SetStat("stats-completion", $"{(int)progress.completionPercent}%");
            SetStat("stats-combat", $"{progress.combatsWon} / {progress.combatsLost} / {progress.combatsFled}");
            SetStat("stats-deaths", progress.deaths.ToString());
            SetStat("stats-rewinds", progress.rewindsUsed.ToString());
            SetStat("stats-first", progress.firstSession ?? "--");
            SetStat("stats-last", progress.lastSession ?? "--");
        }

        private void SetStat(string elementName, string value)
        {
            var val = _root.Q<Label>($"stats-{elementName}-val");
            if (val != null) val.text = value ?? "--";
        }

        public void HideStats()
        {
            if (_statsScreen != null) _statsScreen.style.display = DisplayStyle.None;

            if (_activeTab == "library")
                ShowLibrary();
            else
            {
                _storeScreen.style.display = DisplayStyle.Flex;
            }
        }

        private string GetStoryTitle(string storyId)
        {
            var story = _catalog?.AllStories.FirstOrDefault(s => s.id == storyId);
            return story?.title ?? storyId;
        }

        private string GetSelectedStoryTitle()
        {
            return GetStoryTitle(_selectedStoryId);
        }

        // ===== CONFIRM DIALOG =====

        private void ShowConfirmDialog(string action, string title, string message, Action onConfirm)
        {
            _confirmAction = action;

            var titleLabel = _root.Q<Label>("confirm-dialog-title");
            var messageLabel = _root.Q<Label>("confirm-dialog-message");

            if (titleLabel != null) titleLabel.text = title ?? "Confirmar";
            if (messageLabel != null) messageLabel.text = message ?? "";

            _confirmActionCallback = onConfirm;

            if (_confirmDialog != null) _confirmDialog.style.display = DisplayStyle.Flex;
        }

        private Action _confirmActionCallback;

        private void ExecuteConfirmAction()
        {
            HideConfirmDialog();
            _confirmActionCallback?.Invoke();
            _confirmActionCallback = null;
        }

        private void HideConfirmDialog()
        {
            if (_confirmDialog != null) _confirmDialog.style.display = DisplayStyle.None;
            _confirmAction = null;
            _confirmActionCallback = null;
        }

        // ===== HELPERS =====

        private string FormatTimePlayed(float seconds)
        {
            if (seconds <= 0) return "0m";
            if (seconds < 3600)
                return $"{(int)(seconds / 60)}min";
            var hrs = Mathf.FloorToInt(seconds / 3600);
            var mins = Mathf.FloorToInt((seconds % 3600) / 60);
            return $"{hrs}h {mins}min";
        }

        [Serializable]
        private class StoryProgressData
        {
            public string storyId;
            public string title;
            public string authorName;
            public float completionPercent;
            public int lastSectionId;
            public float timePlayed;
            public int sessionsPlayed;
            public int sectionsVisited;
            public int totalSections;
            public int choicesMade;
            public int endingsFound;
            public int totalEndings;
            public int combatsWon;
            public int combatsLost;
            public int combatsFled;
            public int deaths;
            public int rewindsUsed;
            public string firstSession;
            public string lastSession;

            [NonSerialized] public string lastPlayedText;
            [NonSerialized] public string statsMini;
        }
    }
}
