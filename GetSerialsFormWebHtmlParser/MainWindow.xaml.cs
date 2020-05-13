using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GetSerialsFormWebHtmlParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public List<string> URLs = new List<string>();
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Client setup
        public static void SetSilent(WebBrowser browser, bool silent)
        {
            if (browser == null)
            {
                throw new ArgumentNullException("browser");
            }

            // get an IWebBrowser2 from the document
            IOleServiceProvider sp = browser.Document as IOleServiceProvider;
            if (sp != null)
            {
                Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

                object webBrowser;
                sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
                if (webBrowser != null)
                {
                    webBrowser.GetType().InvokeMember("Silent",
                        BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.PutDispProperty, null, webBrowser,
                        new object[] { silent });
                }
            }
        }
        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"),
            InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IOleServiceProvider
        {
            [PreserveSig]
            int QueryService([In] ref Guid guidService, [In] ref Guid riid,
                [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }
#endregion

        private async void SearchClick(object sender, RoutedEventArgs e)
        {
            URLs = new List<string>();
            if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                return;
            }
            var queryString = WebUtility.UrlEncode(TxtSearch.Text);
            var htmlWeb = new HtmlWeb();
            var query = $"http://fanserial.net/search/?query={TxtSearch.Text}";
            var doc = await htmlWeb.LoadFromWebAsync(query);
            var results = doc.DocumentNode.SelectNodes("//div[@class='search-dark-list']");
            if (results == null)
            {
                LbxResults.ItemsSource = null;
                return;
            }
            var items = results[0].SelectNodes("//div[@class='item-search-serial']");
            if (items == null)
            {
                LbxResults.ItemsSource = null;
                return;
            }
            var searchResults = new List<SearchResult>();
            int i = 0;
            foreach (var singleItem in items)
            {
                string aInnerHtml = singleItem.SelectNodes("//div[@class='field-img']")[i].
                    Element("a").InnerHtml;
                string imageUrl = GetUrlFromTag(aInnerHtml);
                var itemNameTag = singleItem.SelectNodes("//div[@class='serial-info']")[i].Element("div").
                    SelectNodes("//div[@class='item-search-header']")[i].Element("h2").Element("a");
                string itemName = itemNameTag.InnerText;
                string itemNameInEnglis = singleItem.SelectNodes("//div[@class='serial-info']")[i].Element("div").
                    SelectNodes("//div[@class='item-search-header']")[i].SelectNodes("//div[@class='name-origin-search']")[i].InnerText;
                string itemUrl = $"http://fanserial.net/{itemNameInEnglis.ToLower().Replace(" ", "")}/";
                string itemDescription = singleItem.SelectNodes("//div[@class='serial-info']")[i].SelectNodes("//div[@class='text_ssb']")[i].
                    Element("p").InnerText;
                URLs.Add(itemUrl);
                searchResults.Add(new SearchResult() { Source = imageUrl, Name = $"{itemName}({itemNameInEnglis})", NameOfRow = $"_{i}", Description = itemDescription });
                i++;
            }
            LbxResults.ItemsSource = searchResults;
        }
        private string GetUrlFromTag(string aInnerHtml)
        {
            int count = 0;
            var indexes = new List<int>(aInnerHtml.Length);
            for (int i = 0; i < aInnerHtml.Length; i++)
            {
                if (aInnerHtml[i] == '\"')
                {
                    indexes.Add(i);
                    count++;
                }
                if (count == 2)
                {
                    break;
                }
            }
            string src = aInnerHtml.Substring(indexes[0] + 1, indexes[1] - indexes[0] - 1);
            return src;
        }
        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Grid grid = (Grid)sender;
            var item = grid.Children.Cast<Label>()
                .Where(i => i.Visibility == Visibility.Hidden);
            Label label = item.First();
            int index = Convert.ToInt32(
                Convert.ToString(
                    label.Content).Substring(1)
                    );
            System.Diagnostics.Process.Start(URLs[index]);
        }
    }
}

