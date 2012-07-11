using MarkRSSReader.Data;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// “项详细信息页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234232 上提供

namespace MarkRSSReader {
    /// <summary>
    /// 显示组内单个项的详细信息同时允许使用手势
    /// 浏览同一组的其他项的页。
    /// </summary>
    public sealed partial class FeedPage : MarkRSSReader.Common.LayoutAwarePage {
        public FeedPage() {
            this.InitializeComponent();
        }

        private Feed _currFeed;

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState) {
            // Allow saved page state to override the initial item to display
            if (pageState != null && pageState.ContainsKey("DisplayItem")) {
                navigationParameter = pageState["DisplayItem"];
            }

            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            _currFeed = (Feed)navigationParameter;
            this.DefaultViewModel["Feed"] = _currFeed;
            this.DefaultViewModel["Items"] = _currFeed.Items;
        }

        /// <summary>
        /// 初始化所有数据
        /// </summary>
        async Task initAll() {
            await FeedDataSource.getInstance().loadFeedAsync(_currFeed);
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState) {
            pageState["DisplayItem"] = _currFeed;
        }

        /// <summary>
        /// 底部工具栏打开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void BottomAppBar_Opened(object sender, Object args) {
            int selectedItemCount = 0;
            FeedItem item = null;
            if (itemGridView.Visibility == Visibility.Visible) {
                selectedItemCount = itemGridView.SelectedItems.Count;
                item = (FeedItem) itemGridView.SelectedItem;
            } else {
                selectedItemCount = itemListView.SelectedItems.Count;
                item = (FeedItem)itemListView.SelectedItem;
            }
            if (selectedItemCount > 0) {
                pageBar.Visibility = Visibility.Collapsed;
                gridViewBar.Visibility = Visibility.Visible;
                gridReadFeedBtn.Visibility = Visibility.Visible;
                gridNoReadFeedBtn.Visibility = Visibility.Visible;
            } else {
                pageBar.Visibility = Visibility.Visible;
                gridViewBar.Visibility = Visibility.Collapsed;
            }

            if (selectedItemCount == 1 && item != null) {
                if (item.IsRead) {// 已读状态，隐藏标记已读按钮
                    gridReadFeedBtn.Visibility = Visibility.Collapsed;
                } else {// 未读状态，隐藏标记未读按钮
                    gridNoReadFeedBtn.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 在单击某个项时进行调用。
        /// </summary>
        /// <param name="sender">显示所单击项的 GridView (在应用程序处于对齐状态时
        /// 为 ListView)。</param>
        /// <param name="e">描述所单击项的事件数据。</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e) {
            // 导航至相应的目标页，并
            // 通过将所需信息作为导航参数传入来配置新页
            var item = (FeedItem)e.ClickedItem;
            this.Frame.Navigate(typeof(FeedItemPage), item);
        }

        void ItemView_Loaded(object sender, RoutedEventArgs e) {
            var grid = (Grid)sender;
            FeedItem item = (FeedItem)grid.DataContext;
            PreloadFeedGrid.Add(grid);

            Binding b = new Binding();
            b.Mode = BindingMode.TwoWay;
            b.Source = item.IsRead ?
                Application.Current.Resources["FeedItemReadedThemeColor"] :
                Application.Current.Resources["FeedItemNoReadThemeColor"];
            grid.SetBinding(Grid.BackgroundProperty, b);

            WebView htmlContent = (WebView) grid.FindName("htmlContent");
            string content = "<html><body>" + item.Content + "</body></html>";
            htmlContent.NavigateToString(content);
        }

        private ObservableCollection<Grid> _preloadFeedGrid = new ObservableCollection<Grid>();
        public ObservableCollection<Grid> PreloadFeedGrid {
            get { return this._preloadFeedGrid; }
        }

        /// <summary>
        /// 选择了GRID中的项后的处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ItemView_ItemSelect(object sender, SelectionChangedEventArgs e) {
            ListViewBase itemView = (ListViewBase)sender;
            if (itemView.SelectedItems.Count > 0) {
                gridReadFeedBtn.Visibility = Visibility.Visible;
                gridNoReadFeedBtn.Visibility = Visibility.Visible;
                if (itemView.SelectedItems.Count == 1) {
                    FeedItem item = (FeedItem)itemView.SelectedItem;
                    if (item.IsRead) {// 已读状态，隐藏标记已读按钮
                        gridReadFeedBtn.Visibility = Visibility.Collapsed;
                    } else {// 未读状态，隐藏标记未读按钮
                        gridNoReadFeedBtn.Visibility = Visibility.Collapsed;
                    }
                }
                pageBar.Visibility = Visibility.Collapsed;
                gridViewBar.Visibility = Visibility.Visible;
            } else {
                pageBar.Visibility = Visibility.Visible;
                gridViewBar.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 点击刷新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void RefreshBtn_Click(object sender, RoutedEventArgs e) {
            await initAll();
        }

        /// <summary>
        /// 将Feed中的所有文章都标记为已读
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void ReadFeedBtn_Click(object sender, RoutedEventArgs e) {
            await FeedItemDatabase.getInstance().readedFeed(_currFeed);
            await initAll();
        }

        /// <summary>
        /// 将Feed中的所有文章都标记为未读
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void NoReadFeedBtn_Click(object sender, RoutedEventArgs e) {
            await FeedItemDatabase.getInstance().noReadFeed(_currFeed);
            await initAll();
        }

        /// <summary>
        /// 将选中的文章都标记为已读
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void ReadFeedItemBtn_Click(object sender, RoutedEventArgs e) {
            IList<object> feedList = null;
            if (itemGridView.Visibility == Visibility.Visible) {
                feedList = itemGridView.SelectedItems;
            } else {
                feedList = itemListView.SelectedItems;
            }
            if (feedList != null) {
                foreach (FeedItem item in feedList) {
                    item.IsRead = true;
                    await FeedItemDatabase.getInstance().readedFeedItem(item);

                    var matches = PreloadFeedGrid.Where((itemGrid) => ((FeedItem)itemGrid.DataContext).Link.Equals(item.Link));
                    if (matches.Count() > 0) {
                        Grid itemGrid = (Grid)matches.First();
                        itemGrid.Background = (Brush)Application.Current.Resources["FeedItemReadedThemeColor"];
                    }
                }
            }
        }

        /// <summary>
        /// 将选中的文章都标记为未读
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void NoReadFeedItemBtn_Click(object sender, RoutedEventArgs e) {
            IList<object> feedList = null;
            if (itemGridView.Visibility == Visibility.Visible) {
                feedList = itemGridView.SelectedItems;
            } else {
                feedList = itemListView.SelectedItems;
            }
            if (feedList != null) {
                foreach (FeedItem item in feedList) {
                    item.IsRead = false;
                    await FeedItemDatabase.getInstance().noReadFeedItem(item);

                    var matches = PreloadFeedGrid.Where((itemGrid) => ((FeedItem)itemGrid.DataContext).Link.Equals(item.Link));
                    if (matches.Count() > 0) {
                        Grid itemGrid = (Grid)matches.First();
                        itemGrid.Background = (Brush)Application.Current.Resources["FeedItemNoReadThemeColor"];
                    }
                }
            }
        }

        /// <summary>
        /// 清除所有选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ClearSelectBtn_Click(object sender, RoutedEventArgs e) {
            if (itemGridView.Visibility == Visibility.Visible) {
                itemGridView.SelectedItems.Clear();
            } else {
                itemListView.SelectedItems.Clear();
            }
        }
    }
}
