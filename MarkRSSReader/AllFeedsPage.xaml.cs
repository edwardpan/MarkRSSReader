using MarkRSSReader.Data;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// “分组项页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234231 上提供

namespace MarkRSSReader {
    /// <summary>
    /// 显示分组的项集合的页。
    /// </summary>
    public sealed partial class AllFeedsPage : MarkRSSReader.Common.LayoutAwarePage {
        public AllFeedsPage() {
            this.InitializeComponent();

            InputPane input = InputPane.GetForCurrentView();
            input.Showing += input_Showing;
            input.Hiding += input_Hiding;
        }

        private double inputPaneHeight = 0;

        void input_Showing(InputPane sender, InputPaneVisibilityEventArgs e) {
            inputPaneHeight = Window.Current.Content.RenderSize.Height -
                (AppBar1.IsOpen ? AppBar1.ActualHeight : 0) - saveFeedPage.ActualHeight - e.OccludedRect.Height;
            //Storyboard sb = new Storyboard();
            //DoubleAnimation an = new DoubleAnimation();
            //an.Duration = TimeSpan.FromMilliseconds(733);
            //an.From = saveFeedPagePop.VerticalOffset;
            //an.To = inputPaneHeight;
            //sb.Children.Add(an);
            //Storyboard.SetTarget(an, this.saveFeedPagePop);
            //Storyboard.SetTargetProperty(an, "VerticalOffset");
            //sb.Begin();
            saveFeedPagePop.VerticalOffset = inputPaneHeight;
        }

        void input_Hiding(InputPane sender, InputPaneVisibilityEventArgs e) {
            inputPaneHeight = Window.Current.Content.RenderSize.Height -
                (AppBar1.IsOpen ? AppBar1.ActualHeight : 0) - saveFeedPage.ActualHeight;
            saveFeedPagePop.VerticalOffset = inputPaneHeight;
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
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState) {
            // TODO: Create an appropriate data model for your problem domain to replace the sample data
            var feedDatas = FeedDataSource.getInstance().AllFeeds;
            this.DefaultViewModel["Feeds"] = feedDatas;
            initAll();
        }

        /// <summary>
        /// 初始化所有数据
        /// </summary>
        void initAll() {
            _preloadFeedGrid.Clear();
            FeedDataSource.getInstance().initFeedInfo();
        }

        /// <summary>
        /// 在单击组内的项时进行调用。
        /// </summary>
        /// <param name="sender">显示所单击项的 GridView (在应用程序处于对齐状态时
        /// 为 ListView)。</param>
        /// <param name="e">描述所单击项的事件数据。</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e) {
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
            itemListView.SelectedItems.Clear();
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
        private SaveFeedPage saveFeedPage = null;

        private void initSaveFeedView(bool isEdit = false) {
            if (saveFeedPagePop == null || saveFeedPage == null) {
                // 初始化弹出界面
                saveFeedPage = new SaveFeedPage();
                saveFeedPage.SizeChanged += new SizeChangedEventHandler(SaveFeedPage_SizeChanged);
                saveFeedPagePop = new Popup();
                saveFeedPagePop.Child = saveFeedPage;
                saveFeedPagePop.Opened += new EventHandler<object>(saveFeedPage.ParentPopup_Opened);
            }
            if (ApplicationView.Value == ApplicationViewState.Snapped) {
                saveFeedPage.Height = 150;
            }
            saveFeedPage.Width = Window.Current.Content.RenderSize.Width;

            Feed editingFeed = null;
            if (isEdit) {
                // 获取要编辑的Feed的数据
                if (itemGridView.Visibility == Visibility.Visible) {
                    editingFeed = (Feed)itemGridView.SelectedItem;
                } else {
                    editingFeed = (Feed)itemListView.SelectedItem;
                }
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
                (AppBar1.IsOpen ? AppBar1.ActualHeight : 0) - saveFeedPage.ActualHeight;
            pop.SetBinding(Popup.VerticalOffsetProperty, popBinding);
        }

        /// <summary>
        /// 删除Feed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void DelFeedBtn_Click(object sender, RoutedEventArgs e) {
            if (itemGridView.Visibility == Visibility.Visible) {
                while (itemGridView.SelectedItems.Count > 0) {
                    Feed f = (Feed)itemGridView.SelectedItems.First();
                    await FeedDataSource.getInstance().delFeed(f);
                }
            } else {
                while (itemListView.SelectedItems.Count > 0) {
                    Feed f = (Feed)itemListView.SelectedItems.First();
                    await FeedDataSource.getInstance().delFeed(f);
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
            IList<object> feedList = null;
            if (itemGridView.Visibility == Visibility.Visible) {
                feedList = itemGridView.SelectedItems;
            } else {
                feedList = itemListView.SelectedItems;
            }
            if (feedList != null) {
                foreach (Feed feed in feedList) {
                    var matches = PreloadFeedGrid.Where((feedGrid) => ((Feed)feedGrid.DataContext).UniqueId.Equals(feed.UniqueId));
                    if (matches.Count() > 0) {
                        Grid feedGrid = (Grid)matches.First();
                        await loadFeedGrid(feedGrid);
                    }
                }
            }
        }

        /// <summary>
        /// 将选中的Feed中的所有文章都标记为已读
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void ReadFeedBtn_Click(object sender, RoutedEventArgs e) {
            IList<object> feedList = null;
            if (itemGridView.Visibility == Visibility.Visible) {
                feedList = itemGridView.SelectedItems;
            } else {
                feedList = itemListView.SelectedItems;
            }
            if (feedList != null) {
                foreach (Feed feed in feedList) {
                    await FeedItemDatabase.getInstance().readedFeed(feed);

                    var matches = PreloadFeedGrid.Where((feedGrid) => ((Feed)feedGrid.DataContext).UniqueId.Equals(feed.UniqueId));
                    if (matches.Count() > 0) {
                        Grid feedGrid = (Grid)matches.First();
                        await loadFeedGrid(feedGrid);
                    }
                }
            }
        }

        /// <summary>
        /// 将选中的Feed中的所有文章都标记为未读
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void NoReadFeedBtn_Click(object sender, RoutedEventArgs e) {
            IList<object> feedList = null;
            if (itemGridView.Visibility == Visibility.Visible) {
                feedList = itemGridView.SelectedItems;
            } else {
                feedList = itemListView.SelectedItems;
            }
            if (feedList != null) {
                foreach (Feed feed in feedList) {
                    await FeedItemDatabase.getInstance().noReadFeed(feed);

                    var matches = PreloadFeedGrid.Where((feedGrid) => ((Feed)feedGrid.DataContext).UniqueId.Equals(feed.UniqueId));
                    if (matches.Count() > 0) {
                        Grid feedGrid = (Grid)matches.First();
                        await loadFeedGrid(feedGrid);
                    }
                }
            }
        }

        /// <summary>
        /// 选择了GRID中的项后的处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ItemView_ItemSelect(object sender, SelectionChangedEventArgs e) {
            ListViewBase itemView = (ListViewBase)sender;
            if (itemView.SelectedItems.Count > 0) {
                if (itemView.SelectedItems.Count == 1) {
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

        void itemView_DragItemsStarting(object sender, DragItemsStartingEventArgs e) {
            // TODO 读取正在拖动的数据对象
        }

        void itemView_Drop(object sender, DragEventArgs e) {
            // TODO 更改XML中的排序，drop时未触发该事件
            //e.Data.Properties["a"] = "a";
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
            ProgressRing progRing = (ProgressRing)feedGrid.FindName("prog");
            TextBlock subTitle = (TextBlock)feedGrid.FindName("subTitle");
            TextBlock description = (TextBlock)feedGrid.FindName("description");
            TextBlock newNumText = (TextBlock)feedGrid.FindName("newNum");

            progRing.Visibility = Visibility.Visible;
            subTitle.Visibility = Visibility.Collapsed;
            description.Visibility = Visibility.Collapsed;

            var feed = (Feed)feedGrid.DataContext;
            await FeedDataSource.getInstance().loadFeedAsync(feed);

            Binding b = new Binding();
            b.Mode = BindingMode.TwoWay;
            b.Source = feed.Items.Count((i) => !i.IsRead);
            newNumText.SetBinding(TextBlock.TextProperty, b);

            progRing.Visibility = Visibility.Collapsed;
            subTitle.Visibility = Visibility.Visible;
            if (ApplicationView.Value != ApplicationViewState.Snapped) {
                description.Visibility = Visibility.Visible;
            }
        }
    }
}
