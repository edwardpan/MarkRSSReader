using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.Storage;
using Windows.Web.Syndication;
using Windows.Data.Xml.Dom;
using System.Runtime.InteropServices;

// The data model defined by this file serves as a representative example of a strongly-typed
// model that supports notification when members are added, removed, or modified.  The property
// names chosen coincide with data bindings in the standard item templates.
//
// Applications may use this model as a starting point and build on it, or discard it entirely and
// replace it with something appropriate to their needs.

namespace MarkRSSReader.Data {
    /// <summary>
    /// 程序数据源，RSS源数据获取对象
    /// </summary>
    public sealed class FeedDataSource {
        private static FeedDataSource _feedDataSource = new FeedDataSource();

        public static FeedDataSource getInstance() {
            if (_feedDataSource == null) {
                _feedDataSource = new FeedDataSource();
            }
            return _feedDataSource;
        }

        private ObservableCollection<Feed> _allFeeds = new ObservableCollection<Feed>();
        public ObservableCollection<Feed> AllFeeds {
            get { return this._allFeeds; }
        }
        private StorageFile feedsFile = null;
        private XmlDocument feedsDoc = null;

        private FeedDataSource() { }
        public async Task init() {
            var loadSettings = new Windows.Data.Xml.Dom.XmlLoadSettings();
            loadSettings.ProhibitDtd = true;
            loadSettings.ResolveExternals = false;

            if (feedsFile == null) {
                // 获取当前应用的配置文件夹（支持可同步的）
                StorageFolder folder = Windows.Storage.ApplicationData.Current.RoamingFolder;
                // 打开RSS源XML配置文件，如果不存在则创建
                feedsFile = await folder.CreateFileAsync("feeds.xml", CreationCollisionOption.OpenIfExists);

                // 如果应用为第一次运行，则复制基础XML配置文件
                ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                object notFirstRun = roamingSettings.Values["notFirstRun"];
                if (notFirstRun == null) {
                    StorageFolder templateFolder = await
                        Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("data");
                    StorageFile templateFile = await templateFolder.CreateFileAsync(
                        "feeds.xml", CreationCollisionOption.OpenIfExists);
                    await templateFile.CopyAndReplaceAsync(feedsFile);
                    roamingSettings.Values["notFirstRun"] = true;
                }
            }
            if (feedsDoc == null) {
                // 读取XML配置文件
                feedsDoc = await XmlDocument.LoadFromFileAsync(feedsFile, loadSettings);
            }
        }

        public async Task saveCache() {
            await feedsDoc.SaveToFileAsync(feedsFile);
        }

        public void initFeedInfo() {
            AllFeeds.Clear();

            IXmlNode root = feedsDoc.SelectSingleNode("feeds");
            XmlNodeList feedList = root.SelectNodes("feed");
            if (feedList != null) {
                IOrderedEnumerable<IXmlNode> feedOrderList = feedList.OrderBy(f =>
                    int.Parse(f.SelectSingleNode("order").InnerText));
                foreach (IXmlNode feedXml in feedOrderList) {
                    Feed feed = new Feed();
                    // 编号
                    feed.UniqueId = feedXml.SelectSingleNode("uniqueId").InnerText;
                    // 标题
                    feed.Title = feedXml.SelectSingleNode("title").InnerText;
                    // URI
                    string uri = feedXml.SelectSingleNode("uri").InnerText;
                    feed.Source = new Uri(uri);
                    // 颜色
                    IXmlNode bgcolorXml = feedXml.SelectSingleNode("bgcolor");
                    if (bgcolorXml == null) {
                        bgcolorXml = feedsDoc.CreateElement("bgcolor");
                        bgcolorXml.InnerText = Backgrounds.Instance.Color;
                        //feedXml.AppendChild(bgcolorXml);
                    }
                    feed.Background = bgcolorXml.InnerText;
                    feed.Order = int.Parse(feedXml.SelectSingleNode("order").InnerText);

                    this.AllFeeds.Add(feed);
                }
            }
        }

        public async Task loadFeedAsync(Feed loadFeed) {
            // using Windows.Web.Syndication;
            SyndicationClient client = new SyndicationClient();

            try {
                SyndicationFeed synFeed = await client.RetrieveFeedAsync(loadFeed.Source);

                // This code is executed after RetrieveFeedAsync returns the SyndicationFeed.
                // Process it and copy the data we want into our FeedData and FeedItem classes.

                if (loadFeed.Title.Trim().Length == 0) {
                    loadFeed.Title = synFeed.Title.Text;
                }
                if (synFeed.Subtitle.Text != null && synFeed.Subtitle.Text.Trim().Length != 0) {
                    loadFeed.Subtitle = synFeed.Subtitle.Text;
                }
                if (synFeed.Items.Count != 0) {
                    loadFeed.Description = synFeed.Items[0].Title.Text;
                }
                // Use the date of the latest post as the last updated date.
                loadFeed.PubDate = synFeed.Items[0].PublishedDate.DateTime;

                // 加载Feed中的文章，并保存到DB中
                List<FeedItem> feedItemList = new List<FeedItem>();
                foreach (SyndicationItem item in synFeed.Items) {
                    FeedItem feedItem = new FeedItem();
                    feedItem.Title = item.Title.Text;
                    feedItem.PubDate = item.PublishedDate.DateTime;
                    feedItem.Author = item.Authors[0].Name.ToString();
                    // Handle the differences between RSS and Atom feeds
                    if (synFeed.SourceFormat == SyndicationFormat.Atom10) {
                        feedItem.Content = item.Content.Text;
                        feedItem.Link = new Uri("http://windowsteamblog.com" + item.Id);
                    } else if (synFeed.SourceFormat == SyndicationFormat.Rss20) {
                        feedItem.Content = item.Summary.Text;
                        feedItem.Link = item.Links[0].Uri;
                    }
                    feedItem.Feed = loadFeed;
                    feedItemList.Add(feedItem);
                }
                // 保存到DB
                FeedItemDatabase.getInstance().saveFeedItems(loadFeed, feedItemList);
                // 读取DB
                FeedItemDatabase.getInstance().loadFeedItems(loadFeed);
                await FeedItemDatabase.getInstance().saveCache();
            } catch (Exception) {
                return;
            }
        }

        public async Task<Feed> saveFeed(Feed feed) {
            IXmlNode feedsXml = feedsDoc.SelectSingleNode("feeds");

            if (feed.UniqueId == null || "".Equals(feed.UniqueId)) {// 新创建的
                feed.UniqueId = Guid.NewGuid().ToString();

                IXmlNode feedXml = createFeedXml(feed);
                feedsXml.AppendChild(feedXml);
                this.AllFeeds.Add(feed);
            } else {// 对已有的编辑
                // 找到旧的Feed
                IXmlNode oldFeedXml = feedsDoc.SelectSingleNode("//feed[uniqueId='" + feed.UniqueId + "']");
                string oldFeedUri = oldFeedXml.SelectSingleNode("uri").InnerText;

                oldFeedXml.SelectSingleNode("title").InnerText = feed.Title;
                oldFeedXml.SelectSingleNode("uri").InnerText = feed.Source.ToString();
                // 当URI变更后，清除原来的数据
                if (!oldFeedUri.Equals(feed.Source.ToString())) {
                    await FeedItemDatabase.getInstance().clearOldFeedItems(feed);
                }

                await saveCache();
                // 重新加Feed数据
                await loadFeedAsync(feed);
            }
            return feed;
        }

        // 创建用于描述Feed信息的XML节点
        private IXmlNode createFeedXml(Feed feed) {
            // 创建源XML节点
            XmlElement feedXml = feedsDoc.CreateElement("feed");
            // 创建源唯一编号节点
            XmlElement uniqueIdXml = feedsDoc.CreateElement("uniqueId");
            uniqueIdXml.InnerText = feed.UniqueId;
            feedXml.AppendChild(uniqueIdXml);
            // 创建标题节点
            XmlElement titleXml = feedsDoc.CreateElement("title");
            titleXml.InnerText = feed.Title;
            feedXml.AppendChild(titleXml);
            // 创建地址节点
            XmlElement uriXml = feedsDoc.CreateElement("uri");
            uriXml.InnerText = feed.Source.ToString();
            feedXml.AppendChild(uriXml);
            // 创建背景色节点
            XmlElement bgcolorXml = feedsDoc.CreateElement("bgcolor");
            bgcolorXml.InnerText = feed.Background;
            feedXml.AppendChild(bgcolorXml);

            // 创建排序节点
            IXmlNode feedsXml = feedsDoc.SelectSingleNode("feeds");
            IOrderedEnumerable<IXmlNode> nodeList = feedsXml.SelectNodes("feed").OrderByDescending(f => 
                int.Parse(f.SelectSingleNode("order").InnerText));
            IXmlNode node = null;
            if (nodeList != null && nodeList.Count() > 0) {
                node = nodeList.First();
            }
            int order = 1;
            if (node != null) {
                order = int.Parse(node.SelectSingleNode("order").InnerText) + 1;
            }
            XmlElement orderXml = feedsDoc.CreateElement("order");
            orderXml.InnerText = order.ToString();
            feedXml.AppendChild(orderXml);
            return feedXml;
        }

        public async Task delFeed(Feed feed) {
            // 从集合中删除
            this.AllFeeds.Remove(feed);

            // 查找到要删除的源
            IXmlNode feedXml = feedsDoc.SelectSingleNode("//feed[uniqueId='" + feed.UniqueId + "']");
            IXmlNode feedsXml = feedXml.ParentNode;
            feedsXml.RemoveChild(feedXml);
            await saveCache();
        }
    }
}
