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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// “分组项页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234231 上提供

namespace MarkRSSReader
{
    /// <summary>
    /// 显示分组的项集合的页。
    /// </summary>
    public sealed partial class GroupedFeedsPage : MarkRSSReader.Common.LayoutAwarePage
    {
        public GroupedFeedsPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var feedDataGroups = FeedDataSource.getInstance().AllGroups;
            this.DefaultViewModel["Groups"] = feedDataGroups;
            initAll();
        }

        /// <summary>
        /// 初始化所有数据
        /// </summary>
        void initAll() {
            _preloadFeedGrid.Clear();
            FeedDataSource.getInstance().initFeedGroup();
        }

        ///// <summary>
        ///// 在单击组标题时进行调用。
        ///// </summary>
        ///// <param name="sender">用作选定组的组标题的 Button。</param>
        ///// <param name="e">描述如何启动单击的事件数据。</param>
        //void Header_Click(object sender, RoutedEventArgs e)
        //{
        //    // 确定 Button 实例表示的组
        //    var group = (sender as FrameworkElement).DataContext;

        //    // 导航至相应的目标页，并
        //    // 通过将所需信息作为导航参数传入来配置新页
        //    this.Frame.Navigate(typeof(GroupPage), (FeedGroup)group);
        //}

        /// <summary>
        /// 在单击组内的项时进行调用。
        /// </summary>
        /// <param name="sender">显示所单击项的 GridView (在应用程序处于对齐状态时
        /// 为 ListView)。</param>
        /// <param name="e">描述所单击项的事件数据。</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // 导航至相应的目标页，并
            // 通过将所需信息作为导航参数传入来配置新页
            var feed = (Feed)e.ClickedItem;
            this.Frame.Navigate(typeof(FeedPage), feed);
        }

        /// <summary>
        /// 底部工具栏关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void BottomAppBar_Closed(object sender, Object args) {
            pageBar.Visibility = Visibility.Visible;
            if (saveFeedPagePop != null) {
                saveFeedPagePop.IsOpen = false;
            }
            gridViewBar.Visibility = Visibility.Collapsed;
            gridEditFeedBtn.Visibility = Visibility.Collapsed;
            itemGridView.SelectedItems.Clear();
        }

        /// <summary>
        /// 添加Feed，显示添加界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AddFeedBtn_Click(object sender, RoutedEventArgs e) {
            initSaveFeedView();
        }
        
        /// <summary>
        /// 编辑Feed，显示编辑界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void EditFeedBtn_Click(object sender, RoutedEventArgs e) {
            initSaveFeedView(true);
        }

        private Popup saveFeedPagePop = null;

        private void initSaveFeedView(bool isEdit=false) {
            if (saveFeedPagePop == null) {
                // 初始化弹出界面
                SaveFeedPage saveFeedPage = new SaveFeedPage();
                saveFeedPage.SizeChanged += new SizeChangedEventHandler(SaveFeedPage_SizeChanged);
                saveFeedPage.Width = Window.Current.Content.RenderSize.Width;
                saveFeedPagePop = new Popup();
                saveFeedPagePop.Child = saveFeedPage;
                saveFeedPagePop.Opened += new EventHandler<object>(saveFeedPage.ParentPopup_Opened);
            }

            Feed editingFeed = null;
            if (isEdit) {
                // 获取要编辑的Feed的数据
                editingFeed = (Feed)itemGridView.SelectedItem;
            }

            ((SaveFeedPage)saveFeedPagePop.Child).editingFeed = editingFeed;
            saveFeedPagePop.IsOpen = !saveFeedPagePop.IsOpen;
        }

        private void SaveFeedPage_SizeChanged(object sender, SizeChangedEventArgs e) {
            SaveFeedPage saveFeedPage = (sender as SaveFeedPage);
            Popup pop = (saveFeedPage.Parent as Popup);

            // 将弹出界面的位置属性与底部操作栏绑定
            Binding popBinding = new Binding();
            popBinding.Mode = BindingMode.OneWay;
            popBinding.Source = Window.Current.Content.RenderSize.Height -
                (AppBar1.IsOpen ? AppBar1.RenderSize.Height : 0) - saveFeedPage.RenderSize.Height;
            pop.SetBinding(Popup.VerticalOffsetProperty, popBinding);
        }

        /// <summary>
        /// 删除Feed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void DelFeedBtn_Click(object sender, RoutedEventArgs e) {
            while (itemGridView.SelectedItems.Count > 0) {
                Feed f = (Feed) itemGridView.SelectedItems.First();
                var matches = FeedDataSource.getInstance().AllGroups.SelectMany((group) => group.Feeds).
                        Where((feed) => feed.UniqueId.Equals(f.UniqueId));
                if (matches.Count() == 1) {
                    var feed = matches.First();
                    var group = feed.Group;
                    group.Feeds.Remove(feed);
                    await FeedDataSource.getInstance().delFeed(feed);
                }
            }
        }

        /// <summary>
        /// 点击刷新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RefreshBtn_Click(object sender, RoutedEventArgs e) {
            initAll();
        }

        /// <summary>
        /// 刷新单个或多个源中的内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void RefreshFeedBtn_Click(object sender, RoutedEventArgs e) {
            foreach (Feed feed in itemGridView.SelectedItems) {
                var matches = PreloadFeedGrid.Where((feedGrid) => ((Feed)feedGrid.DataContext).UniqueId.Equals(feed.UniqueId));
                if (matches.Count() > 0) {
                    Grid feedGrid = (Grid)matches.First();
                    await loadFeedGrid(feedGrid);
                }
            }
        }

        /// <summary>
        /// 选择了GRID中的项后的处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ItemView_ItemSelect(object sender, SelectionChangedEventArgs e) {
            GridView gridView = (GridView) sender;
            if (gridView.SelectedItems.Count > 0) {
                if (gridView.SelectedItems.Count == 1) {
                    gridEditFeedBtn.Visibility = Visibility.Visible;
                } else {
                    gridEditFeedBtn.Visibility = Visibility.Collapsed;
                }
                pageBar.Visibility = Visibility.Collapsed;
                gridViewBar.Visibility = Visibility.Visible;
            } else {
                gridEditFeedBtn.Visibility = Visibility.Collapsed;
                pageBar.Visibility = Visibility.Visible;
                gridViewBar.Visibility = Visibility.Collapsed;
            }
        }

        async void ItemView_Loaded(object sender, RoutedEventArgs e) {
            var grid = (Grid)sender;
            PreloadFeedGrid.Add(grid);
            await loadFeedGrid(grid);
        }

        private ObservableCollection<Grid> _preloadFeedGrid = new ObservableCollection<Grid>();
        public ObservableCollection<Grid> PreloadFeedGrid {
            get { return this._preloadFeedGrid; }
        }

        /// <summary>
        /// 加载Grid中的Feed源
        /// </summary>
        /// <returns></returns>
        private async Task loadFeedGrid(Grid feedGrid) {
            var matches = feedGrid.Children.Where((el) => el.GetType().Equals(typeof(ProgressRing)));
            ProgressRing progRing = null;
            if (matches.Count() == 1) {
                progRing = (ProgressRing)matches.First();
            }

            matches = feedGrid.Children.Where((el) => el.GetType().Equals(typeof(TextBlock)));
            TextBlock topNews = null;
            if (matches.Count() == 1) {
                topNews = (TextBlock)matches.First();
            }

            if (progRing != null && topNews != null) {
                progRing.Visibility = Visibility.Visible;
                topNews.Visibility = Visibility.Collapsed;
            }

            var feed = (Feed)feedGrid.DataContext;
            await FeedDataSource.getInstance().initFeedAsync(feed);

            if (progRing != null && topNews != null) {
                progRing.Visibility = Visibility.Collapsed;
                topNews.Visibility = Visibility.Visible;
            }
        }
    }
}
