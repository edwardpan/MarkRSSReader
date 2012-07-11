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
            if (editingFeed == null) {
                saveFeedTitle.Text = "";
                saveFeedUri.Text = "";
            } else {
                saveFeedTitle.Text = editingFeed.Title;
                saveFeedUri.Text = editingFeed.Source.ToString();
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

        /// <summary>
        /// 执行Feed保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void SaveFeedOkBtn_Click(object sender, RoutedEventArgs e) {
            string title = saveFeedTitle.Text;
            string uri = saveFeedUri.Text;

            if (title == null || title.Trim(null).Length == 0) {
                title = "";
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
            closePopup();
        }

        private void closePopup() {
            ((Popup)this.Parent).IsOpen = false;
        }
    }
}
