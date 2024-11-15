using System;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using WinUIGallery.Helper;
using System.Threading;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;
using Windows.System;
using DispatcherQueueHandler = Microsoft.UI.Dispatching.DispatcherQueueHandler;
using System.Linq;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;

namespace WinUIGallery.TabViewPages
{
    public class TabItemData : DependencyObject
    {
        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        public string Content
        {
            get { return (string)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public static DependencyProperty HeaderProperty { get; } = DependencyProperty.Register("Header", typeof(string), typeof(TabItemData), new PropertyMetadata(""));
        public static DependencyProperty ContentProperty { get; } = DependencyProperty.Register("Content", typeof(string), typeof(TabItemData), new PropertyMetadata(""));
    }

    public sealed partial class TabViewWindowingSamplePage : Page
    {
        private static readonly List<Window> windowList = new();

        private Win32WindowHelper win32WindowHelper;
        private Window tabTearOutWindow = null;

        private readonly ObservableCollection<TabItemData> tabItemDataList = [];
        public ObservableCollection<TabItemData> TabItemDataList => tabItemDataList;

        public TabViewWindowingSamplePage()
        {
            this.InitializeComponent();

            Loaded += TabViewWindowingSamplePage_Loaded;
        }

        public void SetupWindowMinSize(Window window)
        {
            win32WindowHelper = new Win32WindowHelper(window);
            win32WindowHelper.SetWindowMinMaxSize(new Win32WindowHelper.POINT() { x = 500, y = 300 });
        }

        private void TabViewWindowingSamplePage_Loaded(object sender, RoutedEventArgs e)
        {
            var currentWindow = WindowHelper.GetWindowForElement(this);
            currentWindow.ExtendsContentIntoTitleBar = true;
            currentWindow.SetTitleBar(CustomDragRegion);
            CustomDragRegion.MinWidth = 188;

            if (!windowList.Contains(currentWindow))
            {
                windowList.Add(currentWindow);
                currentWindow.Closed += (s, args) =>
                {
                    windowList.Remove(currentWindow);
                };
            }
        }

        public void LoadDemoData()
        {
            // Main Window -- add some default items
            for (int i = 0; i < 3; i++)
            {
                TabItemDataList.Add(new TabItemData() { Header = $"Item {i}", Content = $"Page {i}" });
            }

            Tabs.SelectedIndex = 0;
        }

        private void Tabs_TabTearOutWindowRequested(TabView sender, TabViewTabTearOutWindowRequestedEventArgs args)
        {
            tabTearOutWindow = CreateNewWindow();
            args.NewWindowId = tabTearOutWindow.AppWindow.Id;
        }

        private void Tabs_TabTearOutRequested(TabView sender, TabViewTabTearOutRequestedEventArgs args)
        {
            if (tabTearOutWindow == null)
            {
                return;
            }

            MoveDataItems(TabItemDataList, GetTabItemDataList(tabTearOutWindow), args.Items, 0);
        }

        private void Tabs_ExternalTornOutTabsDropping(TabView sender, TabViewExternalTornOutTabsDroppingEventArgs args)
        {
            args.AllowDrop = true;
        }

        private void Tabs_ExternalTornOutTabsDropped(TabView sender, TabViewExternalTornOutTabsDroppedEventArgs args)
        {
            MoveDataItems(TabItemDataList, GetTabItemDataList(WindowHelper.GetWindowForElement(sender)), args.Items, args.DropIndex);
        }

        private static Window CreateNewWindow()
        {
            var newPage = new TabViewWindowingSamplePage();

            var window = WindowHelper.CreateWindow();
            window.ExtendsContentIntoTitleBar = true;
            window.Content = newPage;
            window.AppWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
            newPage.SetupWindowMinSize(window);

            return window;
        }

        private static void MoveDataItems(ObservableCollection<TabItemData> source, ObservableCollection<TabItemData> destination, object[] dataItems, int index)
        {
            foreach (TabItemData tabItemData in dataItems.Cast<TabItemData>())
            {
                source.Remove(tabItemData);
                destination.Insert(index, tabItemData);

                index++;
            }
        }

        private static T GetParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject current = child;

            while (current != null)
            {
                if (current is T parent)
                {
                    return parent;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private static ObservableCollection<TabItemData> GetTabItemDataList(Window window)
        {
            var tabViewPage = (TabViewWindowingSamplePage)window.Content;
            return tabViewPage.TabItemDataList;
        }

        private void Tabs_AddTabButtonClick(TabView sender, object args)
        {
            TabItemDataList.Add(new TabItemData() { Header = "New Item", Content = "New Item" });
        }

        private void Tabs_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            TabItemDataList.Remove((TabItemData)args.Item);

            if (TabItemDataList.Count == 0)
            {
                WindowHelper.GetWindowForElement(this).Close();
            }
        }

        private void TabViewContextMenu_Opening(object sender, object e)
        {
            MenuFlyout contextMenu = (MenuFlyout)sender;
            contextMenu.Items.Clear();

            var tabViewItem = (TabViewItem)contextMenu.Target;
            ListView tabViewListView = GetParent<ListView>(tabViewItem);
            TabView tabView = GetParent<TabView>(tabViewListView);
            var window = WindowHelper.GetWindowForElement(tabView);

            if (tabViewListView == null || tabView == null)
            {
                return;
            }

            var tabItemDataList = (ObservableCollection<TabItemData>)tabView.TabItemsSource;
            int index = tabViewListView.IndexFromContainer(tabViewItem);
            var tabDataItem = tabViewListView.ItemFromContainer(tabViewItem);

            if (index > 0)
            {
                MenuFlyoutItem moveLeftItem = new() { Text = "Move tab left" };
                moveLeftItem.Click += (s, args) =>
                {
                    var item = tabItemDataList[index];
                    tabItemDataList.RemoveAt(index);
                    tabItemDataList.Insert(index - 1, item);
                };
                contextMenu.Items.Add(moveLeftItem);
            }

            if (index < tabItemDataList.Count - 1)
            {
                MenuFlyoutItem moveRightItem = new() { Text = "Move tab right" };
                moveRightItem.Click += (s, args) =>
                {
                    var item = tabItemDataList[index];
                    tabItemDataList.RemoveAt(index);
                    tabItemDataList.Insert(index + 1, item);
                };
                contextMenu.Items.Add(moveRightItem);
            }

            MenuFlyoutSubItem moveSubItem = new() { Text = "Move tab to" };

            if (tabItemDataList.Count > 1)
            {
                MenuFlyoutItem newWindowItem = new() { Text = "New window", Icon = new SymbolIcon(Symbol.NewWindow) };

                newWindowItem.Click += (s, args) =>
                {
                    var newWindow = CreateNewWindow();
                    MoveDataItems(tabItemDataList, GetTabItemDataList(newWindow), [tabDataItem], 0);
                    newWindow.Activate();
                };

                moveSubItem.Items.Add(newWindowItem);
            }

            List<MenuFlyoutItem> moveToWindowItems = [];

            foreach (Window otherWindow in windowList)
            {
                if (window == otherWindow)
                {
                    continue;
                }

                var windowTabItemDataList = GetTabItemDataList(otherWindow);

                if (windowTabItemDataList.Count > 0)
                {
                    MenuFlyoutItem moveToWindowItem = new() { Text = $"\"{windowTabItemDataList[0].Header}\" and {windowTabItemDataList.Count - 1} other tabs", Icon = new SymbolIcon(Symbol.BackToWindow) };
                    moveToWindowItem.Click += (s, args) =>
                    {
                        MoveDataItems(tabItemDataList, windowTabItemDataList, [tabDataItem], windowTabItemDataList.Count);

                        if (tabItemDataList.Count == 0)
                        {
                            window.Close();
                        }

                        otherWindow.Activate();
                    };
                    moveToWindowItems.Add(moveToWindowItem);
                }
            }

            if (moveToWindowItems.Count > 0)
            {
                contextMenu.Items.Add(new MenuFlyoutSeparator());
            }

            foreach (MenuFlyoutItem moveToWindowItem in moveToWindowItems)
            {
                moveSubItem.Items.Add(moveToWindowItem);
            }

            if (moveSubItem.Items.Count > 0)
            {
                contextMenu.Items.Add(moveSubItem);
            }

            if (contextMenu.Items.Count == 0)
            {
                contextMenu.Hide();
            }
        }
    }
}
