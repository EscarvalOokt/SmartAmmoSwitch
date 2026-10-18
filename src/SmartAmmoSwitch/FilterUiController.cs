using System;
using System.Collections.Generic;
using System.Linq;
using Escarval.TerrariaWindowKit;
using Terraria.ID;
using TerrariaModder.Core;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace SmartAmmoSwitch
{
    internal sealed class FilterUiController
    {
        private const int PanelWidth = 520;
        private const int PanelHeight = 420;
        private const int HeaderHeight = 32;
        private const int PanelPadding = 8;
        private const int CloseButtonSize = 24;
        private const int PinButtonSpacing = 6;
        private const string PanelTitle = "Smart Ammo Switch - Filter Editor";

        private const int RowHeight = 26;
        private const int SectionSpacing = 4;
        private const int ControlHeight = 26;
        private const int CategoryButtonHeight = 28;
        private const int CategoryIconButtonWidth = 32;
        private const int CategoryButtonSpacing = 4;

        private static readonly AmmoCatalogCategoryFilter[] _categoryOrder =
        [
            AmmoCatalogCategoryFilter.All,
            AmmoCatalogCategoryFilter.Arrows,
            AmmoCatalogCategoryFilter.Bullets,
            AmmoCatalogCategoryFilter.Rockets,
            AmmoCatalogCategoryFilter.Darts,
            AmmoCatalogCategoryFilter.Coins,
            AmmoCatalogCategoryFilter.Flares,
            AmmoCatalogCategoryFilter.Sand,
            AmmoCatalogCategoryFilter.Solution,
            AmmoCatalogCategoryFilter.Other
        ];

        private readonly HashSet<AmmoCatalogCategoryFilter> _availableCategoryTabs =
            new HashSet<AmmoCatalogCategoryFilter>();

        private readonly ScrollView _catalogScroll = new ScrollView();
        private readonly ScrollView _selectedScroll = new ScrollView();
        private AmmoCatalogCategoryFilter _activeCategory = AmmoCatalogCategoryFilter.All;

        private int _activeTab;

        private CoreWindowBackend _backend;

        private List<SupportedAmmoInfo> _catalog = new List<SupportedAmmoInfo>();
        private SmartAmmoSwitchConfig _config;
        private List<SupportedAmmoInfo> _filteredCatalog = new List<SupportedAmmoInfo>();
        private int _listTopOffset = -1;
        private AmmoFilterMode _mode = AmmoFilterMode.Disabled;
        private TextInput _searchInput;
        private string _searchText = string.Empty;
        private List<SupportedAmmoInfo> _selectedItems = new List<SupportedAmmoInfo>();

        private bool _wasMouseLeftDown;
        private WindowHost _window;

        public void Initialize(ModContext context)
        {
            _config = context.GetConfig<SmartAmmoSwitchConfig>();

            if (_config == null)
            {
                throw new InvalidOperationException("SmartAmmoSwitchConfig was not registered by TerrariaModder.");
            }

            _searchInput = new TextInput("Search ammo by name or item ID...", maxLength: 100);

            RefreshFromConfig();
        }

        public void Unload()
        {
            try
            {
                _window?.Dispose();
            }
            finally
            {
                _window = null;
                try
                {
                    _backend?.Dispose();
                }
                finally
                {
                    _backend = null;
                    _config = null;
                    _searchInput = null;
                    _wasMouseLeftDown = false;
                    _listTopOffset = -1;

                    _catalog.Clear();
                    _filteredCatalog.Clear();
                    _selectedItems.Clear();
                    _availableCategoryTabs.Clear();
                }
            }
        }

        public void Toggle()
        {
            EnsureCatalogBuilt();
            RefreshFromConfig();
            EnsureWindowCreated();
            _window.Toggle();
            _wasMouseLeftDown = UIRenderer.MouseLeft;
        }

        private void EnsureWindowCreated()
        {
            if (_window != null)
                return;

            // Create the backend on the UI thread when the editor is first opened.
            var backend = new CoreWindowBackend(SharedCoordinator.Connect());
            try
            {
                // Keep full mode labels readable even when the selected-item count reaches three digits.
                int modeLabelWidth = Math.Max(
                    UIRenderer.MeasureText("Disabled (999 saved)"),
                    Math.Max(UIRenderer.MeasureText("Blacklist (999)"), UIRenderer.MeasureText("Whitelist (999)")));
                int windowWidth = Math.Max(PanelWidth, (modeLabelWidth + 16) * 3 + 12 + PanelPadding * 2);

                var options = new WindowOptions(
                    new WindowBounds(
                        Math.Max(0, (backend.ViewportWidth - windowWidth) / 2),
                        Math.Max(0, (backend.ViewportHeight - PanelHeight) / 2),
                        windowWidth,
                        PanelHeight))
                {
                    Draggable = true,
                    Resizable = false,
                    Pinnable = true,
                    MinimumWidth = windowWidth,
                    MinimumHeight = PanelHeight
                };
                var callbacks = new WindowCallbacks
                {
                    Draw = DrawUI,
                    HasTextFocus = () => _searchInput?.IsFocused == true,
                    ReleaseTextFocus = OnPanelClose,
                    WheelAt = GetWheelDisposition
                };

                _window = new WindowHost(SmartAmmoSwitchMod.ModId, "filter", options, backend, callbacks);
                _backend = backend;
            }
            catch
            {
                backend.Dispose();
                throw;
            }
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
            RebuildAvailableCategoryTabs();
            bool categoryChanged = !_availableCategoryTabs.Contains(_activeCategory);
            if (categoryChanged)
                _activeCategory = AmmoCatalogCategoryFilter.All;

            IEnumerable<SupportedAmmoInfo> catalogQuery = ApplyCategoryFilter(_catalog, _activeCategory);
            catalogQuery = ApplySearchFilter(catalogQuery, _searchText);

            _filteredCatalog = catalogQuery.OrderBy(x => x.Category)
                .ThenBy(x => x.ItemName)
                .ThenBy(x => x.ItemId)
                .ToList();

            IEnumerable<SupportedAmmoInfo> selectedQuery = _catalog.Where(x => AmmoFilterSettings.Contains(x.ItemId));
            selectedQuery = ApplyCategoryFilter(selectedQuery, _activeCategory);
            selectedQuery = ApplySearchFilter(selectedQuery, _searchText);

            _selectedItems = selectedQuery.OrderBy(x => x.Category)
                .ThenBy(x => x.ItemName)
                .ThenBy(x => x.ItemId)
                .ToList();

            if (resetScroll || categoryChanged)
            {
                _catalogScroll.ResetScroll();
                _selectedScroll.ResetScroll();
            }
        }

        private void RebuildAvailableCategoryTabs()
        {
            _availableCategoryTabs.Clear();
            _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.All);

            var categories = new HashSet<SupportedAmmoCategory>(GetCurrentCategoryTabSource().Select(x => x.Category));

            if (categories.Contains(SupportedAmmoCategory.Arrows))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Arrows);

            if (categories.Contains(SupportedAmmoCategory.Bullets))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Bullets);

            if (categories.Contains(SupportedAmmoCategory.Rockets))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Rockets);

            if (categories.Contains(SupportedAmmoCategory.Darts))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Darts);

            if (categories.Contains(SupportedAmmoCategory.Coins))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Coins);

            if (categories.Contains(SupportedAmmoCategory.Flares))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Flares);

            if (categories.Contains(SupportedAmmoCategory.Sand))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Sand);

            if (categories.Contains(SupportedAmmoCategory.Solution))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Solution);

            if (categories.Contains(SupportedAmmoCategory.Other))
                _availableCategoryTabs.Add(AmmoCatalogCategoryFilter.Other);
        }

        private IEnumerable<SupportedAmmoInfo> GetCurrentCategoryTabSource()
        {
            if (_activeTab == 0)
                return ApplySearchFilter(_catalog, _searchText);

            return ApplySearchFilter(_catalog.Where(x => AmmoFilterSettings.Contains(x.ItemId)), _searchText);
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
            WindowHost window = _window;
            if (window == null || !window.IsOpen)
                return;

            int mouseX = WidgetInput.MouseX;
            int mouseY = WidgetInput.MouseY;
            bool mouseLeftDown = UIRenderer.MouseLeft;
            bool searchClickAway = FilterEditorClickAwayPolicy.IsEligiblePress(
                _searchInput?.IsFocused == true,
                WidgetInput.BlockInput,
                WidgetInput.MouseLeftClick,
                mouseLeftDown,
                _wasMouseLeftDown);
            _wasMouseLeftDown = mouseLeftDown;
            window.UpdatePointer(mouseX, mouseY, mouseLeftDown, WidgetInput.BlockInput);
            WindowBounds bounds = window.Bounds;

            int closeX = bounds.X + bounds.Width - PanelPadding - CloseButtonSize;
            int closeY = bounds.Y + (HeaderHeight - CloseButtonSize) / 2;
            bool hasCloseButton = bounds is { Width: >= CloseButtonSize + PanelPadding * 2, Height: >= HeaderHeight };
            bool closeHovered = hasCloseButton &&
                                WidgetInput.IsMouseOver(closeX, closeY, CloseButtonSize, CloseButtonSize);

            int pinWidth = CorePinButtonRenderer.MeasureWidth("Pin", "Unpin");
            int pinX = closeX - PinButtonSpacing - pinWidth;
            bool hasPinButton = hasCloseButton &&
                                bounds.Width >= (long)PanelPadding * 2 + CloseButtonSize + PinButtonSpacing + pinWidth;
            bool pinHovered = hasPinButton &&
                              !WidgetInput.BlockInput &&
                              WidgetInput.IsMouseOver(pinX, closeY, pinWidth, CloseButtonSize);

            if (!WidgetInput.BlockInput && WidgetInput.MouseLeftClick)
            {
                if (pinHovered)
                {
                    window.SetPinned(!window.IsPinned);
                    WidgetInput.ConsumeClick();
                }
                else if (!closeHovered &&
                         WidgetInput.IsMouseOver(
                             bounds.X,
                             bounds.Y,
                             bounds.Width,
                             Math.Min(HeaderHeight, bounds.Height)))
                {
                    window.BeginDrag(mouseX, mouseY);
                    WidgetInput.ConsumeClick();
                }
            }

            UIRenderer.DrawPanel(bounds.X, bounds.Y, bounds.Width, bounds.Height, UIColors.PanelBg);
            UIRenderer.DrawRect(
                bounds.X + 1,
                bounds.Y + 1,
                Math.Max(0, bounds.Width - 2),
                Math.Max(0, Math.Min(HeaderHeight, bounds.Height - 2)),
                UIColors.HeaderBg);

            int titleWidth = bounds.Width - PanelPadding * 3 - CloseButtonSize;
            if (hasPinButton)
                titleWidth -= pinWidth + PinButtonSpacing;

            if (titleWidth > 0 && bounds.Height >= HeaderHeight)
            {
                UIRenderer.DrawText(
                    TextUtil.Truncate(PanelTitle, titleWidth),
                    bounds.X + PanelPadding + 4,
                    bounds.Y + 7,
                    UIColors.TextTitle);
            }

            if (hasPinButton)
            {
                CorePinButtonRenderer.Draw(
                    pinX,
                    closeY,
                    pinWidth,
                    CloseButtonSize,
                    window.IsPinned,
                    pinHovered,
                    "Pin",
                    "Unpin");

                if (pinHovered)
                    Tooltip.Set(window.IsPinned ? "Unpin window" : "Pin window");
            }

            if (hasCloseButton &&
                Button.Draw(
                    closeX,
                    closeY,
                    CloseButtonSize,
                    CloseButtonSize,
                    "X",
                    UIColors.CloseBtn,
                    UIColors.CloseBtnHover,
                    UIColors.Text))
            {
                window.Close();
                return;
            }

            _searchInput?.Update();

            try
            {
                int contentX = bounds.X + PanelPadding;
                int contentY = bounds.Y + HeaderHeight + PanelPadding;
                int contentWidth = Math.Max(0, bounds.Width - PanelPadding * 2);
                int contentHeight = Math.Max(0, bounds.Height - HeaderHeight - PanelPadding * 2);

                int currentY = contentY;

                currentY = DrawFilterModeAndSearch(contentX, currentY, contentWidth, searchClickAway);
                currentY += SectionSpacing;

                currentY = DrawMainTabs(contentX, currentY, contentWidth);
                currentY += SectionSpacing;

                currentY = DrawCategoryTabs(contentX, currentY, contentWidth);
                currentY += SectionSpacing;

                currentY = DrawBulkActions(contentX, currentY, contentWidth);
                currentY += SectionSpacing;

                _listTopOffset = currentY - bounds.Y;
                DrawListSection(contentX, currentY, contentWidth, contentHeight - (currentY - contentY));
            }
            finally
            {
                Tooltip.DrawDeferred();
            }
        }

        private int GetWheelDisposition(int x, int y)
        {
            WindowHost window = _window;
            if (window == null || !window.IsOpen || _listTopOffset < 0)
                return (int)WheelDisposition.PassThrough;

            WindowBounds bounds = window.Bounds;
            int listX = bounds.X + PanelPadding;
            int listY = bounds.Y + _listTopOffset;
            int listWidth = bounds.Width - PanelPadding * 2;
            int listHeight = bounds.Height - _listTopOffset - PanelPadding;

            return listWidth > 0 &&
                   listHeight > 0 &&
                   x >= listX &&
                   x < (long)listX + listWidth &&
                   y >= listY &&
                   y < (long)listY + listHeight
                ? (int)WheelDisposition.Deliver
                : (int)WheelDisposition.PassThrough;
        }

        private int DrawFilterModeAndSearch(int x, int y, int width, bool searchClickAway)
        {
            var layout = new StackLayout(x, y, width, spacing: SectionSpacing);

            int buttonsRowY = layout.Advance(ControlHeight);
            int thirdWidth = Math.Max(0, (width - 12) / 3);

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
            int lastButtonWidth = Math.Max(0, width - thirdWidth * 2 - 12);

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

            int searchY = layout.CurrentY;
            string newSearch = _searchInput.Draw(layout.X, searchY, width, height: ControlHeight);

            if (width > 0)
            {
                UIRenderer.DrawRectOutline(
                    layout.X,
                    searchY,
                    width,
                    ControlHeight,
                    _searchInput.IsFocused ? UIColors.Accent : UIColors.Border);
            }

            if (searchClickAway &&
                _searchInput.IsFocused &&
                !WidgetInput.IsMouseOver(layout.X, searchY, width, ControlHeight))
                _searchInput.Unfocus();

            layout.Advance(ControlHeight);

            if (!string.Equals(_searchText, newSearch, StringComparison.Ordinal))
            {
                _searchText = newSearch ?? string.Empty;
                RebuildLists(resetScroll: true);
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
            if (width <= 0)
                return;

            bool active = _mode == mode;
            int count = AmmoFilterSettings.FilteredItemIds.Count;
            string buttonText = !active ? label :
                mode == AmmoFilterMode.Disabled ? $"{label} ({count} saved)" : $"{label} ({count})";

            if (Button.Draw(
                    x,
                    y,
                    width,
                    ControlHeight,
                    buttonText,
                    active ? UIColors.ItemActiveBg : UIColors.Button,
                    active ? UIColors.ItemActiveBg : UIColors.ButtonHover,
                    UIColors.Text))
                onClick?.Invoke();

            if (active)
                UIRenderer.DrawRectOutline(x, y, width, ControlHeight, UIColors.Accent);

            if (WidgetInput.IsMouseOver(x, y, width, ControlHeight))
            {
                if (mode == AmmoFilterMode.Whitelist && active && count == 0)
                    Tooltip.Set("Whitelist is empty. Ammo switching by this mod has no allowed ammo types.");
                else
                    Tooltip.Set(tooltip);
            }
        }

        private int DrawBulkActions(int x, int y, int width)
        {
            int clearWidth = Math.Min(120, Math.Max(0, width / 3));
            int actionWidth = Math.Min(190, Math.Max(0, width - clearWidth - SectionSpacing));
            int actionX = x + Math.Max(0, width - actionWidth - clearWidth - SectionSpacing);
            int clearX = actionX + actionWidth + SectionSpacing;

            if (actionWidth > 0 &&
                Button.Draw(
                    actionX,
                    y,
                    actionWidth,
                    ControlHeight,
                    _activeTab == 0 ? "Select results" : "Clear results"))
            {
                var changed = false;

                if (_activeTab == 0)
                {
                    foreach (SupportedAmmoInfo item in _filteredCatalog)
                    {
                        if (AmmoFilterSettings.Add(item.ItemId))
                            changed = true;
                    }
                }
                else
                {
                    foreach (SupportedAmmoInfo item in _selectedItems)
                    {
                        if (AmmoFilterSettings.Remove(item.ItemId))
                            changed = true;
                    }
                }

                if (changed)
                {
                    SaveCurrentSettings();
                    RebuildLists(resetScroll: false);
                }
            }

            if (clearWidth > 0 &&
                Button.Draw(
                    clearX,
                    y,
                    clearWidth,
                    ControlHeight,
                    "Clear all",
                    UIColors.ItemBg,
                    UIColors.ButtonHover,
                    UIColors.TextDim))
            {
                AmmoFilterSettings.Clear();
                SaveCurrentSettings();
                RebuildLists(resetScroll: false);
            }

            return y + ControlHeight;
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
            if (width <= 0)
                return y;

            int groupWidth = CategoryButtonSpacing * (_categoryOrder.Length - 1);
            foreach (AmmoCatalogCategoryFilter category in _categoryOrder)
                groupWidth += GetCategoryButtonWidth(category);

            int startX = x + Math.Max(0, (width - groupWidth) / 2);
            int currentX = startX;
            int rowY = y;

            foreach (AmmoCatalogCategoryFilter category in _categoryOrder)
            {
                int desiredWidth = GetCategoryButtonWidth(category);
                int buttonWidth = Math.Min(width, desiredWidth);

                if (currentX > startX && currentX + buttonWidth > x + width)
                {
                    startX = x;
                    currentX = x;
                    rowY += CategoryButtonHeight + CategoryButtonSpacing;
                }

                bool enabled = _availableCategoryTabs.Contains(category);
                bool active = _activeCategory == category;
                bool hovered = !WidgetInput.BlockInput &&
                               WidgetInput.IsMouseOver(currentX, rowY, buttonWidth, CategoryButtonHeight);

                UIRenderer.DrawRect(
                    currentX,
                    rowY,
                    buttonWidth,
                    CategoryButtonHeight,
                    active ? UIColors.ItemActiveBg : enabled && hovered ? UIColors.ButtonHover : UIColors.SectionBg);
                UIRenderer.DrawRectOutline(
                    currentX,
                    rowY,
                    buttonWidth,
                    CategoryButtonHeight,
                    active ? UIColors.Accent : UIColors.Border);

                int itemId = GetCategoryIconItemId(category);
                if (itemId != 0 && buttonWidth >= 22)
                {
                    int iconX = currentX + (buttonWidth - 22) / 2;
                    UIRenderer.DrawItem(itemId, iconX, rowY + 3, 22, 22);
                    if (!enabled)
                        UIRenderer.DrawRect(iconX, rowY + 3, 22, 22, UIColors.SectionBg.WithAlpha(160));
                }
                else if (itemId == 0)
                {
                    string text = TextUtil.Truncate(GetCategoryLabel(category), Math.Max(0, buttonWidth - 4));
                    int textWidth = UIRenderer.MeasureText(text);
                    if (textWidth <= buttonWidth - 4)
                    {
                        UIRenderer.DrawText(
                            text,
                            currentX + (buttonWidth - textWidth) / 2,
                            rowY + 6,
                            enabled ? UIColors.Text : UIColors.TextDim);
                    }
                }

                if (hovered)
                    Tooltip.Set(enabled ? GetCategoryLabel(category) : GetCategoryLabel(category) + " (no results)");

                if (enabled && hovered && WidgetInput.MouseLeftClick && category != _activeCategory)
                {
                    WidgetInput.ConsumeClick();
                    _activeCategory = category;
                    RebuildLists(resetScroll: true);
                }

                currentX += buttonWidth + CategoryButtonSpacing;
            }

            return rowY + CategoryButtonHeight;
        }

        private static int GetCategoryButtonWidth(AmmoCatalogCategoryFilter category)
        {
            return category switch
            {
                AmmoCatalogCategoryFilter.All => 48,
                AmmoCatalogCategoryFilter.Other => 66,
                _ => CategoryIconButtonWidth
            };
        }

        private static int GetCategoryIconItemId(AmmoCatalogCategoryFilter category)
        {
            return category switch
            {
                AmmoCatalogCategoryFilter.Arrows => ItemID.WoodenArrow,
                AmmoCatalogCategoryFilter.Bullets => ItemID.MusketBall,
                AmmoCatalogCategoryFilter.Rockets => ItemID.RocketI,
                AmmoCatalogCategoryFilter.Darts => ItemID.PoisonDart,
                AmmoCatalogCategoryFilter.Coins => ItemID.CopperCoin,
                AmmoCatalogCategoryFilter.Flares => ItemID.Flare,
                AmmoCatalogCategoryFilter.Sand => ItemID.SandBlock,
                AmmoCatalogCategoryFilter.Solution => ItemID.GreenSolution,
                _ => 0
            };
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

        private void DrawListSection(int x, int y, int width, int height)
        {
            int listHeight = Math.Max(0, height);
            if (width <= 0 || listHeight <= 0)
                return;

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

                if (_filteredCatalog.Count == 0)
                    DrawEmptyListMessage("No matching ammunition", contentX, contentY, contentWidth);

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

                if (_selectedItems.Count == 0)
                {
                    string emptyMessage = AmmoFilterSettings.FilteredItemIds.Count == 0
                        ? "No ammunition selected"
                        : "No matching selected ammunition";
                    DrawEmptyListMessage(emptyMessage, contentX, contentY, contentWidth);
                }

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

        private static void DrawEmptyListMessage(string message, int x, int y, int width)
        {
            string text = TextUtil.Truncate(message, Math.Max(0, width - 16));
            if (UIRenderer.MeasureText(text) <= width - 16)
                UIRenderer.DrawText(text, x + 8, y + 8, UIColors.TextDim);
        }

        private void DrawItemRow(SupportedAmmoInfo info, int x, int y, int width)
        {
            bool isSelected = AmmoFilterSettings.Contains(info.ItemId);
            bool hover = WidgetInput.IsMouseOver(x, y, width, RowHeight);

            if (hover)
            {
                UIRenderer.DrawRect(x, y, width, RowHeight, UIColors.ItemHoverBg);
                Tooltip.Set(info.ItemName, $"Item ID: {info.ItemId} | Category: {info.Category}");
            }

            const int checkboxSize = 16;
            const int iconSize = 22;
            const int horizontalGap = 6;

            int checkboxX = x + 4;
            int checkboxY = y + (RowHeight - checkboxSize) / 2;
            int iconX = checkboxX + checkboxSize + horizontalGap;
            int iconY = y + (RowHeight - iconSize) / 2;
            int labelX = iconX + iconSize + horizontalGap;
            int labelWidth = Math.Max(0, x + width - labelX - 4);

            UIRenderer.DrawItem(info.ItemId, iconX, iconY, iconSize, iconSize);

            if (labelWidth > 0)
            {
                string label = TextUtil.Truncate(info.ItemName, labelWidth);
                if (UIRenderer.MeasureText(label) <= labelWidth)
                    UIRenderer.DrawText(label, labelX, y + 5, UIColors.Text);
            }

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
                x.ItemId.ToString().Contains(lower) ||
                x.Category.ToString().ToLowerInvariant().Contains(lower));
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

    // Keep this input decision independent of Terraria/Core UI so it can be tested without the game runtime.
    internal static class FilterEditorClickAwayPolicy
    {
        internal static bool IsEligiblePress(
            bool hasTextFocus,
            bool inputBlocked,
            bool mouseLeftClick,
            bool mouseLeftDown,
            bool wasMouseLeftDown)
        {
            // The normal click signal can be consumed by another widget before this panel receives it.
            return !inputBlocked && hasTextFocus && (mouseLeftClick || (mouseLeftDown && !wasMouseLeftDown));
        }
    }
}