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

namespace MarkRSSReader.Data {
    public sealed class FeedItemDatabase {
        private static FeedItemDatabase _feedItemDatabase = new FeedItemDatabase();

        public static FeedItemDatabase getInstance() {
            if (_feedItemDatabase == null) {
                _feedItemDatabase = new FeedItemDatabase();
            }
            return _feedItemDatabase;
        }

        private StorageFile dbFile = null;
        private XmlDocument dbDoc = null;

        private FeedItemDatabase() { }

        public async Task init() {
            var loadSettings = new Windows.Data.Xml.Dom.XmlLoadSettings();
            loadSettings.ProhibitDtd = true;
            loadSettings.ResolveExternals = false;

            if (dbFile == null) {
                // 保存取得的文章数据
                // 获取当前应用的配置文件夹（支持可同步的）
                StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                // 打开RSS源XML配置文件，如果不存在则创建
                dbFile = await localFolder.CreateFileAsync("feeditems_database.xml", CreationCollisionOption.OpenIfExists);

                // 如果应用为第一次运行，则复制基础database文件
                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                object noDatabase = localSettings.Values["noDatabase"];
                if (noDatabase == null) {
                    StorageFolder templateFolder = await
                        Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync("data");
                    StorageFile templateFile = await templateFolder.CreateFileAsync(
                        "feeditems_database.xml", CreationCollisionOption.OpenIfExists);
                    await templateFile.CopyAndReplaceAsync(dbFile);
                    localSettings.Values["noDatabase"] = true;
                }
            }

            if (dbDoc == null) {
                // 读取XML配置文件
                dbDoc = await XmlDocument.LoadFromFileAsync(dbFile, loadSettings);
            }
        }

        public async Task saveCache() {
            await dbDoc.SaveToFileAsync(dbFile);
        }

        /// <summary>
        /// 读取数据文件中的文章数据
        /// </summary>
        /// <param name="loadFeed"></param>
        /// <returns>读取到的文章数量</returns>
        public int loadFeedItems(Feed loadFeed) {
            loadFeed.Items.Clear();
            IXmlNode database = dbDoc.SelectSingleNode("database");
            IXmlNode feedXml = database.SelectSingleNode("feed[uniqueId='" + loadFeed.UniqueId + "']");
            if (feedXml == null) return 0;
            XmlNodeList feeditemXmlList = feedXml.SelectNodes("feeditem");
            foreach (IXmlNode feeditemXml in feeditemXmlList) {
                FeedItem feedItem = new FeedItem();
                feedItem.Title = feeditemXml.SelectSingleNode("title").InnerText;
                feedItem.PubDate = DateTime.Parse(feeditemXml.SelectSingleNode("pubdate").InnerText);
                feedItem.Author = feeditemXml.SelectSingleNode("author").InnerText;
                feedItem.Content = feeditemXml.SelectSingleNode("content").InnerText;
                feedItem.Link = new Uri(feeditemXml.SelectSingleNode("link").InnerText);
                feedItem.IsRead = Boolean.Parse(feeditemXml.SelectSingleNode("isread").InnerText);
                feedItem.Feed = loadFeed;
                loadFeed.Items.Add(feedItem);
            }
            return feeditemXmlList.Count;
        }

        /// <summary>
        /// 保存FeedItem文章数据到数据文件中
        /// </summary>
        /// <param name="toFeed">文章所属源</param>
        /// <param name="feedItemList">文章集</param>
        /// <returns></returns>
        public void saveFeedItems(Feed toFeed, List<FeedItem> feedItemList) {
            IXmlNode database = dbDoc.SelectSingleNode("database");
            IXmlNode feedXml = database.SelectSingleNode("feed[uniqueId='" + toFeed.UniqueId + "']");
            if (feedXml == null) {
                feedXml = dbDoc.CreateElement("feed");
                IXmlNode id = dbDoc.CreateElement("uniqueId");
                id.InnerText = toFeed.UniqueId;
                feedXml.AppendChild(id);
                database.AppendChild(feedXml);
            }
            // TODO 检查是否超过100条，超过就删除，可由用户设置
            IXmlNode oldFirstItem = feedXml.FirstChild;
            foreach (FeedItem item in feedItemList) {
                IXmlNode itemXml = feedXml.SelectSingleNode("feeditem[link='" + item.Link + "']");
                if (itemXml != null) {
                    continue;
                }
                itemXml = dbDoc.CreateElement("feeditem");

                IXmlNode title = dbDoc.CreateElement("title");
                title.InnerText = item.Title;
                itemXml.AppendChild(title);
                IXmlNode pubdate = dbDoc.CreateElement("pubdate");
                pubdate.InnerText = item.PubDate.ToString();
                itemXml.AppendChild(pubdate);
                IXmlNode author = dbDoc.CreateElement("author");
                author.InnerText = item.Author;
                itemXml.AppendChild(author);
                IXmlNode content = dbDoc.CreateElement("content");
                content.InnerText = item.Content;
                itemXml.AppendChild(content);
                IXmlNode link = dbDoc.CreateElement("link");
                link.InnerText = item.Link.ToString();
                itemXml.AppendChild(link);
                IXmlNode isread = dbDoc.CreateElement("isread");
                isread.InnerText = "false";
                itemXml.AppendChild(isread);

                feedXml.InsertBefore(itemXml, oldFirstItem);
            }
        }

        /// <summary>
        /// 清除该Feed中保存的所有文章数据，适用于更改URI后
        /// </summary>
        /// <param name="clearFeed"></param>
        /// <returns></returns>
        public async Task clearOldFeedItems(Feed clearFeed) {
            IXmlNode database = dbDoc.SelectSingleNode("database");
            IXmlNode feedXml = database.SelectSingleNode("feed[uniqueId='" + clearFeed.UniqueId + "']");
            if (feedXml == null) return;
            database.RemoveChild(feedXml);
            await saveCache();
        }

        /// <summary>
        /// 将Feed中的所有文章标记为已读
        /// </summary>
        /// <param name="feed"></param>
        public async Task readedFeed(Feed feed) {
            IXmlNode database = dbDoc.SelectSingleNode("database");
            IXmlNode feedXml = database.SelectSingleNode("feed[uniqueId='" + feed.UniqueId + "']");
            // 找到所有未读的文章
            XmlNodeList feeditemList = feedXml.SelectNodes("feeditem[isread='false']");
            foreach (IXmlNode itemXml in feeditemList) {
                readedFeedItemXml(itemXml);
            }
            await saveCache();
        }

        /// <summary>
        /// 将Feed中的所有文章标记为未读
        /// </summary>
        /// <param name="feed"></param>
        public async Task noReadFeed(Feed feed) {
            IXmlNode database = dbDoc.SelectSingleNode("database");
            IXmlNode feedXml = database.SelectSingleNode("feed[uniqueId='" + feed.UniqueId + "']");
            // 找到所有已读的文章
            XmlNodeList feeditemList = feedXml.SelectNodes("feeditem[isread='true']");
            foreach (IXmlNode itemXml in feeditemList) {
                noReadFeedItemXml(itemXml);
            }
            await saveCache();
        }

        /// <summary>
        /// 将Feed中指定的文章标记为已读
        /// </summary>
        /// <param name="item"></param>
        public async Task readedFeedItem(FeedItem item) {
            IXmlNode database = dbDoc.SelectSingleNode("database");
            IXmlNode feedXml = database.SelectSingleNode("feed[uniqueId='" + item.Feed.UniqueId + "']");
            IXmlNode feeditemXml = feedXml.SelectSingleNode("feeditem[link='" + item.Link + "']");
            readedFeedItemXml(feeditemXml);
            await saveCache();
        }

        /// <summary>
        /// 将Feed中指定的文章标记为未读
        /// </summary>
        /// <param name="item"></param>
        public async Task noReadFeedItem(FeedItem item) {
            IXmlNode database = dbDoc.SelectSingleNode("database");
            IXmlNode feedXml = database.SelectSingleNode("feed[uniqueId='" + item.Feed.UniqueId + "']");
            IXmlNode feeditemXml = feedXml.SelectSingleNode("feeditem[link='" + item.Link + "']");
            noReadFeedItemXml(feeditemXml);
            await saveCache();
        }

        private void readedFeedItemXml(IXmlNode itemXml) {
            itemXml.SelectSingleNode("isread").InnerText = "true";
        }

        private void noReadFeedItemXml(IXmlNode itemXml) {
            itemXml.SelectSingleNode("isread").InnerText = "false";
        }
    }
}
