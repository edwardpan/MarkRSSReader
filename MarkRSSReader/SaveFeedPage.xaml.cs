using MarkRSSReader.Data;

using System;
using System.Collections.Generic;
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

// The SaveFeedPage item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MarkRSSReader {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SaveFeedPage : MarkRSSReader.Common.LayoutAwarePage {
        public SaveFeedPage() {
            this.InitializeComponent();
        }

        public void ParentPopup_Opened(object sender, object e) {
            var feedDataGroups = FeedDataSource.getInstance().AllGroups;
            this.DefaultViewModel["Groups"] = feedDataGroups;
            saveFeedGroupView.Visibility = Visibility.Collapsed;
            if (editingFeed == null) {
                saveFeedTitle.Text = "";
                saveFeedUri.Text = "";
                saveFeedGroup.SelectedItem = null;
            } else {
                saveFeedTitle.Text = editingFeed.Title;
                saveFeedUri.Text = editingFeed.Source.ToString();
                saveFeedGroup.SelectedItem = editingFeed.Group;
            }
        }

        private Feed _editingFeed = null;
        public Feed editingFeed {
            get {
                return _editingFeed;
            }
            set{
                _editingFeed = value;
            }
        }
        private FeedGroup editingGroup = null;

        void AddFeedGroupBtn_Click(object sender, RoutedEventArgs e) {
            saveFeedGroupView.Visibility = Visibility.Visible;
            addFeedGroupPopInSb.Begin();
        }

        /// <summary>
        /// 执行分组保存操作
        /// </summary>
        /// <param name="send"></param>
        /// <param name="e"></param>
        async void SaveFeedGroupOKBtn_Click(object send, RoutedEventArgs e) {
            string title = saveFeedGroupTitle.Text;

            if (title == null || title.Trim(null).Length == 0) {
                return;
            }

            FeedGroup group = new FeedGroup();
            if (editingGroup != null) {// 为编辑FeedGroup操作
                group = editingGroup;
            }
            group.Title = title;
            group.Description = "";
            await FeedDataSource.getInstance().saveFeedGroup(group);

            // 刷新界面
            addFeedGroupPopOutSb.Begin();

            // 选择新添加的
            saveFeedGroup.SelectedItem = group;
        }

        void SaveFeedGroupNoBtn_Click(object send, RoutedEventArgs e) {
            addFeedGroupPopOutSb.Begin();
        }

        void AddFeedGroupViewPopOut_Completed(object sender, object args) {
            saveFeedGroupView.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 执行Feed保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void SaveFeedOkBtn_Click(object sender, RoutedEventArgs e) {
            string title = saveFeedTitle.Text;
            string uri = saveFeedUri.Text;

            FeedGroup group = (FeedGroup)saveFeedGroup.SelectedItem;
            if (title == null || title.Trim(null).Length == 0) {
                return;
            }
            if (uri == null || uri.Trim(null).Length == 0) {
                return;
            }

            Feed feed = new Feed();
            if (editingFeed != null) {// 为编辑Feed操作
                feed = editingFeed;
            }
            feed.Title = title;
            feed.Source = new Uri(uri);
            feed.Background = Backgrounds.Instance.Color;
            feed.Group = group;
            await FeedDataSource.getInstance().saveFeed(feed);

            // 关闭编辑界面
            closePopup();
        }

        /// <summary>
        /// 取消保存操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void SaveFeedNoBtn_Click(object sender, RoutedEventArgs e) {
            saveFeedTitle.Text = "";
            saveFeedUri.Text = "";
            saveFeedGroup.SelectedItem = null;
            closePopup();
        }

        private void closePopup() {
            ((Popup)this.Parent).IsOpen = false;
        }
    }
}
