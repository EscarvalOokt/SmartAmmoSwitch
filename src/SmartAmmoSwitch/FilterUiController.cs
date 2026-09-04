using System;
using System.Collections.Generic;
using System.Linq;
using TerrariaModder.Core;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace SmartAmmoSwitch
{
    internal sealed class FilterUiController
    {
        private const int PanelWidth = 860;
        private const int PanelHeight = 580;

        private const int RowHeight = 28;
        private const int SectionSpacing = 6;
        private const int ControlHeight = 28;
        private const int MinListHeight = 80;

        private readonly ScrollView _catalogScroll = new ScrollView();
        private readonly ScrollView _selectedScroll = new ScrollView();

        private readonly List<AmmoCatalogCategoryFilter> _visibleCategoryTabs = new List<AmmoCatalogCategoryFilter>();

        private AmmoCatalogCategoryFilter _activeCategory = AmmoCatalogCategoryFilter.All;

        private int _activeTab;

        private List<SupportedAmmoInfo> _catalog = new List<SupportedAmmoInfo>();

        private SmartAmmoSwitchConfig _config;
        private List<SupportedAmmoInfo> _filteredCatalog = new List<SupportedAmmoInfo>();
        private AmmoFilterMode _mode = AmmoFilterMode.Disabled;
        private DraggablePanel _panel;
        private TextInput _searchInput;
        private string _searchText = string.Empty;
        private List<SupportedAmmoInfo> _selectedItems = new List<SupportedAmmoInfo>();

        public void Initialize(ModContext context)
        {
            _config = context.GetConfig<SmartAmmoSwitchConfig>();

            if (_config == null)
            {
                throw new InvalidOperationException("SmartAmmoSwitchConfig was not registered by TerrariaModder.");
            }

            _panel = new DraggablePanel(
                "smart-ammo-switch-filter",
                "Smart Ammo Switch - Filter Editor",
                PanelWidth,
                PanelHeight)
            {
                ClipContent = false,
                OnClose = OnPanelClose
            };

            _panel.RegisterDrawCallback(DrawUI);

            _searchInput = new TextInput("Search ammo by name or item ID...", maxLength: 100);

            RefreshFromConfig();
        }

        public void Unload()
        {
            _panel?.UnregisterDrawCallback();

            _config = null;
            _panel = null;
            _searchInput = null;

            _catalog.Clear();
            _filteredCatalog.Clear();
            _selectedItems.Clear();
            _visibleCategoryTabs.Clear();
        }

        public void Toggle()
        {
            EnsureCatalogBuilt();
            RefreshFromConfig();
            _panel?.Toggle();
        }

        public void RefreshFromConfig()
        {
            _mode = AmmoFilterSettings.Mode;
            RebuildLists(resetScroll: false);
        }

        private void EnsureCatalogBuilt()
        {
            _catalog = SupportedAmmoCatalog.GetSortedItems();
            RebuildLists(resetScroll: true);
        }

        private void OnPanelClose()
        {
            _searchInput?.Unfocus();
        }

        private void RebuildLists(bool resetScroll)
        {
            IEnumerable<SupportedAmmoInfo> catalogQuery = _catalog;
            catalogQuery = ApplyCategoryFilter(catalogQuery, _activeCategory);
            catalogQuery = ApplySearchFilter(catalogQuery, _searchText);

            _filteredCatalog = catalogQuery.OrderBy(x => x.Category).ThenBy(x => x.ItemName).ThenBy(x => x.ItemId)
                .ToList();

            IEnumerable<SupportedAmmoInfo> selectedQuery = _catalog.Where(x => AmmoFilterSettings.Contains(x.ItemId));

            selectedQuery = ApplyCategoryFilter(selectedQuery, _activeCategory);
            selectedQuery = ApplySearchFilter(selectedQuery, _searchText);

            _selectedItems = selectedQuery.OrderBy(x => x.Category).ThenBy(x => x.ItemName).ThenBy(x => x.ItemId)
                .ToList();

            RebuildVisibleCategoryTabs();
            EnsureValidActiveCategory();

            if (resetScroll)
            {
                _catalogScroll.ResetScroll();
                _selectedScroll.ResetScroll();
            }
        }

        private void RebuildVisibleCategoryTabs()
        {
            _visibleCategoryTabs.Clear();
            _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.All);

            var categories = new HashSet<SupportedAmmoCategory>(GetCurrentCategoryTabSource().Select(x => x.Category));

            if (categories.Contains(SupportedAmmoCategory.Arrows))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Arrows);

            if (categories.Contains(SupportedAmmoCategory.Bullets))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Bullets);

            if (categories.Contains(SupportedAmmoCategory.Rockets))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Rockets);

            if (categories.Contains(SupportedAmmoCategory.Darts))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Darts);

            if (categories.Contains(SupportedAmmoCategory.Coins))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Coins);

            if (categories.Contains(SupportedAmmoCategory.Flares))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Flares);

            if (categories.Contains(SupportedAmmoCategory.Sand))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Sand);

            if (categories.Contains(SupportedAmmoCategory.Solution))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Solution);

            if (categories.Contains(SupportedAmmoCategory.Other))
                _visibleCategoryTabs.Add(AmmoCatalogCategoryFilter.Other);
        }

        private IEnumerable<SupportedAmmoInfo> GetCurrentCategoryTabSource()
        {
            if (_activeTab == 0)
                return ApplySearchFilter(_catalog, _searchText);

            return ApplySearchFilter(_catalog.Where(x => AmmoFilterSettings.Contains(x.ItemId)), _searchText);
        }

        private void EnsureValidActiveCategory()
        {
            if (!_visibleCategoryTabs.Contains(_activeCategory))
                _activeCategory = AmmoCatalogCategoryFilter.All;
        }

        private void SaveCurrentSettings()
        {
            try
            {
                AmmoFilterSettings.Update(_mode, AmmoFilterSettings.GetSortedIds());

                _config.FilterMode = _mode.ToString();
                _config.FilteredAmmoItemIds = AmmoFilterSettings.SerializeToConfigString();
                _config.Save();
            }
            catch (Exception ex)
            {
                SmartAmmoSwitchMod.Context?.Logger?.Error("[Smart Ammo Switch] Failed to save filter settings: " + ex);
            }
        }

        // ReSharper disable once InconsistentNaming
        private void DrawUI()
        {
            if (_panel == null)
                return;

            _searchInput?.Update();

            if (!_panel.BeginDraw())
                return;

            try
            {
                int contentX = _panel.ContentX;
                int contentY = _panel.ContentY;
                int contentWidth = _panel.ContentWidth;
                int contentHeight = _panel.ContentHeight;

                int currentY = contentY;

                currentY = DrawHeaderSection(contentX, currentY, contentWidth);
                currentY += SectionSpacing;

                currentY = DrawMainTabs(contentX, currentY, contentWidth);
                currentY += SectionSpacing;

                currentY = DrawCategoryTabs(contentX, currentY, contentWidth);
                currentY += SectionSpacing;

                DrawListSection(contentX, currentY, contentWidth, contentHeight - (currentY - contentY));
            }
            finally
            {
                _panel.EndDraw();
            }
        }

        private int DrawHeaderSection(int x, int y, int width)
        {
            var layout = new StackLayout(x, y, width, spacing: SectionSpacing);

            layout.SectionHeader("Filter Mode");

            int buttonsRowY = layout.Advance(ControlHeight);
            int thirdWidth = (width - 12) / 3;

            DrawFilterModeButton(
                layout.X,
                buttonsRowY,
                thirdWidth,
                AmmoFilterMode.Disabled,
                "Disabled",
                "Filter is ignored. All ammo types may be used.",
                () =>
                {
                    _mode = AmmoFilterMode.Disabled;
                    SaveCurrentSettings();
                });

            DrawFilterModeButton(
                layout.X + thirdWidth + 6,
                buttonsRowY,
                thirdWidth,
                AmmoFilterMode.Blacklist,
                "Blacklist",
                "Selected ammo items are excluded.",
                () =>
                {
                    _mode = AmmoFilterMode.Blacklist;
                    SaveCurrentSettings();
                });

            int lastButtonX = layout.X + (thirdWidth + 6) * 2;
            int lastButtonWidth = width - (thirdWidth * 2) - 12;

            DrawFilterModeButton(
                lastButtonX,
                buttonsRowY,
                lastButtonWidth,
                AmmoFilterMode.Whitelist,
                "Whitelist",
                "Only selected ammo items are allowed.",
                () =>
                {
                    _mode = AmmoFilterMode.Whitelist;
                    SaveCurrentSettings();
                });

            layout.Label(GetSelectedItemsLabel());

            string newSearch = _searchInput.Draw(layout.X, layout.CurrentY, layout.Width);

            layout.Advance(ControlHeight);

            if (!string.Equals(_searchText, newSearch, StringComparison.Ordinal))
            {
                _searchText = newSearch ?? string.Empty;
                RebuildLists(resetScroll: true);
            }

            int actionButtonsY = layout.Advance(ControlHeight);
            int halfWidth = (width - 6) / 2;

            if (_activeTab == 0)
            {
                if (Button.Draw(layout.X, actionButtonsY, halfWidth, ControlHeight, "Select All Visible"))
                {
                    var changed = false;

                    foreach (SupportedAmmoInfo item in _filteredCatalog)
                    {
                        if (AmmoFilterSettings.Add(item.ItemId))
                            changed = true;
                    }

                    if (changed)
                    {
                        SaveCurrentSettings();
                        RebuildLists(resetScroll: false);
                    }
                }
            }
            else
            {
                if (Button.Draw(layout.X, actionButtonsY, halfWidth, ControlHeight, "Clear Visible"))
                {
                    var changed = false;

                    foreach (SupportedAmmoInfo item in _selectedItems)
                    {
                        if (AmmoFilterSettings.Remove(item.ItemId))
                            changed = true;
                    }

                    if (changed)
                    {
                        SaveCurrentSettings();
                        RebuildLists(resetScroll: false);
                    }
                }
            }

            if (Button.Draw(layout.X + halfWidth + 6, actionButtonsY, halfWidth, ControlHeight, "Clear Selected"))
            {
                AmmoFilterSettings.Clear();
                SaveCurrentSettings();
                RebuildLists(resetScroll: false);
            }

            return layout.CurrentY;
        }

        private void DrawFilterModeButton(
            int x,
            int y,
            int width,
            AmmoFilterMode mode,
            string label,
            string tooltip,
            Action onClick)
        {
            string buttonText = _mode == mode ? $"{label} *" : label;

            if (Button.Draw(x, y, width, ControlHeight, buttonText))
                onClick?.Invoke();

            if (WidgetInput.IsMouseOver(x, y, width, ControlHeight))
                Tooltip.Set(tooltip);
        }

        private int DrawMainTabs(int x, int y, int width)
        {
            string[] tabs = ["Catalog", "Selected"];

            // ReSharper disable once RedundantArgumentDefaultValue
            int newTab = TabBar.Draw(x, y, width, tabs, _activeTab, height: ControlHeight);

            if (newTab != _activeTab)
            {
                _activeTab = newTab;
                RebuildLists(resetScroll: true);
            }

            return y + ControlHeight;
        }

        private int DrawCategoryTabs(int x, int y, int width)
        {
            string[] labels = _visibleCategoryTabs.Select(GetCategoryLabel).ToArray();

            int selectedIndex = _visibleCategoryTabs.IndexOf(_activeCategory);
            if (selectedIndex < 0)
                selectedIndex = 0;

            // ReSharper disable once RedundantArgumentDefaultValue
            int newIndex = TabBar.Draw(x, y, width, labels, selectedIndex, height: ControlHeight);

            AmmoCatalogCategoryFilter newCategory = _visibleCategoryTabs[newIndex];
            if (newCategory != _activeCategory)
            {
                _activeCategory = newCategory;
                RebuildLists(resetScroll: true);
            }

            return y + ControlHeight;
        }

        private static string GetCategoryLabel(AmmoCatalogCategoryFilter category)
        {
            switch (category)
            {
                case AmmoCatalogCategoryFilter.Arrows:
                    return "Arrows";

                case AmmoCatalogCategoryFilter.Bullets:
                    return "Bullets";

                case AmmoCatalogCategoryFilter.Rockets:
                    return "Rockets";

                case AmmoCatalogCategoryFilter.Darts:
                    return "Darts";

                case AmmoCatalogCategoryFilter.Coins:
                    return "Coins";

                case AmmoCatalogCategoryFilter.Flares:
                    return "Flares";

                case AmmoCatalogCategoryFilter.Sand:
                    return "Sand";

                case AmmoCatalogCategoryFilter.Solution:
                    return "Solution";

                case AmmoCatalogCategoryFilter.Other:
                    return "Other";

                default:
                    return "All";
            }
        }

        private string GetSelectedItemsLabel()
        {
            int count = AmmoFilterSettings.FilteredItemIds.Count;

            switch (_mode)
            {
                case AmmoFilterMode.Blacklist:
                    return $"Blacklisted items: {count}";

                case AmmoFilterMode.Whitelist:
                    return $"Whitelisted items: {count}";

                default:
                    return $"Selected items: {count}";
            }
        }

        private void DrawListSection(int x, int y, int width, int height)
        {
            int listHeight = Math.Max(MinListHeight, height);

            if (_activeTab == 0)
                DrawCatalogList(x, y, width, listHeight);
            else
                DrawSelectedList(x, y, width, listHeight);
        }

        private void DrawCatalogList(int x, int y, int width, int height)
        {
            int totalHeight = _filteredCatalog.Count * RowHeight;
            _catalogScroll.Begin(x, y, width, height, totalHeight);

            try
            {
                int contentX = _catalogScroll.ContentX;
                int contentY = _catalogScroll.ContentY;
                int contentWidth = _catalogScroll.ContentWidth;

                for (var i = 0; i < _filteredCatalog.Count; i++)
                {
                    int itemOffsetY = i * RowHeight;

                    if (!_catalogScroll.IsVisible(itemOffsetY, RowHeight))
                        continue;

                    int rowY = contentY + itemOffsetY;
                    DrawItemRow(_filteredCatalog[i], contentX, rowY, contentWidth);
                }
            }
            finally
            {
                _catalogScroll.End();
            }
        }

        private void DrawSelectedList(int x, int y, int width, int height)
        {
            int totalHeight = _selectedItems.Count * RowHeight;
            _selectedScroll.Begin(x, y, width, height, totalHeight);

            try
            {
                int contentX = _selectedScroll.ContentX;
                int contentY = _selectedScroll.ContentY;
                int contentWidth = _selectedScroll.ContentWidth;

                for (var i = 0; i < _selectedItems.Count; i++)
                {
                    int itemOffsetY = i * RowHeight;

                    if (!_selectedScroll.IsVisible(itemOffsetY, RowHeight))
                        continue;

                    int rowY = contentY + itemOffsetY;
                    DrawItemRow(_selectedItems[i], contentX, rowY, contentWidth);
                }
            }
            finally
            {
                _selectedScroll.End();
            }
        }

        private void DrawItemRow(SupportedAmmoInfo info, int x, int y, int width)
        {
            bool isSelected = AmmoFilterSettings.Contains(info.ItemId);
            bool hover = WidgetInput.IsMouseOver(x, y, width, RowHeight);

            if (hover)
                UIRenderer.DrawRect(x, y, width, RowHeight, UIColors.ItemHoverBg);

            UIRenderer.DrawItem(info.ItemId, x + 4, y + 3, 22, 22);

            var checkboxSize = 16;
            var checkboxMarginRight = 8;

            int checkboxX = x + width - checkboxMarginRight - checkboxSize;
            int checkboxY = y + 6;

            var label = $"[{info.ItemId}] {info.ItemName} ({info.Category})";
            UIRenderer.DrawText(label, x + 32, y + 6, UIColors.Text);

            bool clicked = Checkbox.Draw(checkboxX, checkboxY, checkboxSize, isSelected);

            bool rowClick = !clicked && hover && WidgetInput.MouseLeftClick;

            if (rowClick)
            {
                clicked = true;
                WidgetInput.ConsumeClick();
            }

            if (clicked)
            {
                AmmoFilterSettings.Toggle(info.ItemId);
                SaveCurrentSettings();
                RebuildLists(resetScroll: false);
            }
        }

        private static IEnumerable<SupportedAmmoInfo> ApplySearchFilter(
            IEnumerable<SupportedAmmoInfo> source,
            string searchText)
        {
            string search = (searchText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(search))
                return source;

            string lower = search.ToLowerInvariant();

            return source.Where(x =>
                (!string.IsNullOrWhiteSpace(x.ItemName) && x.ItemName.ToLowerInvariant().Contains(lower)) ||
                x.ItemId.ToString().Contains(lower) || x.Category.ToString().ToLowerInvariant().Contains(lower));
        }

        private static IEnumerable<SupportedAmmoInfo> ApplyCategoryFilter(
            IEnumerable<SupportedAmmoInfo> source,
            AmmoCatalogCategoryFilter category)
        {
            switch (category)
            {
                case AmmoCatalogCategoryFilter.Arrows:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Arrows);

                case AmmoCatalogCategoryFilter.Bullets:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Bullets);

                case AmmoCatalogCategoryFilter.Rockets:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Rockets);

                case AmmoCatalogCategoryFilter.Darts:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Darts);

                case AmmoCatalogCategoryFilter.Coins:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Coins);

                case AmmoCatalogCategoryFilter.Flares:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Flares);

                case AmmoCatalogCategoryFilter.Sand:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Sand);

                case AmmoCatalogCategoryFilter.Solution:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Solution);

                case AmmoCatalogCategoryFilter.Other:
                    return source.Where(x => x.Category == SupportedAmmoCategory.Other);

                default:
                    return source;
            }
        }
    }
}