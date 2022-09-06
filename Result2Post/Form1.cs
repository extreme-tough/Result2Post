using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Web;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;    
using CefSharp;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Management;
using HAP = HtmlAgilityPack ;

namespace WindowsFormsApp1
{
    // <div class="nrn-react-div" style="background-color:#fff;border:1px solid #292c2f47;box-shadow: 2px 2px 12px #aaaaaa69;border-right-color: #1da1f2;border-right-width: medium;margin-bottom: 13px;padding-left: 10px;padding-bottom: 10px;">
    public partial class Form1 : Form
    {
        MySqlCommand command; MySqlConnection sqlConn;
        string ConnectionString;
        string wp_site,article_url,article_title;
        public Form1()
        {
            InitializeComponent();
        }
        bool pageLoaded = false, resultFetched = false;
       
        public string GetSystemMACID()
        {
            string systemName = System.Windows.Forms.SystemInformation.ComputerName;
            try
            {
                ManagementScope theScope = new ManagementScope("\\\\" + Environment.MachineName + "\\root\\cimv2");
                ObjectQuery theQuery = new ObjectQuery("SELECT * FROM Win32_NetworkAdapter");
                ManagementObjectSearcher theSearcher = new ManagementObjectSearcher(theScope, theQuery);
                ManagementObjectCollection theCollectionOfResults = theSearcher.Get();

                foreach (ManagementObject theCurrentObject in theCollectionOfResults)
                {
                    if (theCurrentObject["MACAddress"] != null)
                    {
                        string macAdd = theCurrentObject["MACAddress"].ToString();
                        return macAdd.Replace(':', '-');
                    }
                }
            }
            catch (ManagementException e)
            {
            }
            catch (System.UnauthorizedAccessException e)
            {

            }
            return string.Empty;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string postHeader;

            int maxPosts = int.Parse(txtPostCount.Text);
            int postCategory = getDefaultCategory();

            int postCount=1;
            File.WriteAllText("output.html", "");
            if (keywords.Text.Trim() == "")
            {
                MessageBox.Show("Please enter the keyword to search", "Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            
            
            

            /*
            var script = @"
            document.getElementsByName('q')[0].value = 'CefSharp C# Example';
            document.getElementById('search_button').click();
               ";
            script = @"
            document.getElementById('rld-1').children[0].click();
               ";
            chromiumWebBrowser1.ExecuteScriptAsync(script);
            */
            string[] phrases = keywords.Text.Split(new string[] { "," }, StringSplitOptions.None);
            string article="";
            StringBuilder postContent=new StringBuilder("");
            StringBuilder postFooter = new StringBuilder();
            foreach (string phrase in phrases)
            {
                
                postHeader = txtHeader.Text.Replace("<KEYWORD>", phrase);
                postFooter = new StringBuilder("<h2 id=\"excerpts\">Excerpt Links</h2><p>");

                toolStripStatusLabel1.Text = "Searhing for " + phrase + "...";
                statusStrip.Refresh();
                postContent = new StringBuilder(postHeader + "</br>");
                string search_phrase = HttpUtility.UrlEncode(phrase); 
                search_phrase = search_phrase.Replace(" ", "+");
                postCount = 1;
                pageLoaded = false;
                chromiumWebBrowser1.LoadUrlAsync("https://duckduckgo.com/?q=" + search_phrase + "&ia=web").ContinueWith(x =>
                {
                    if (x.IsCompleted)
                    {
                        pageLoaded = true;
                    }
                });
                while (!pageLoaded)
                {
                    Application.DoEvents();
                }
                
                for (int i = 0; i <= 9; i++)
                {
                    // string script = @"document.getElementById('r1-0').innerHTML;";
                    //chromiumWebBrowser1.ExecuteScriptAsync(script);
                    resultFetched = false;
                    article = "";
                    chromiumWebBrowser1.EvaluateScriptAsync("document.getElementById('r1-" + i.ToString().Trim() + "').parentElement.outerHTML").ContinueWith(x =>
                    {
                        var response = x.Result;
                        if (response.Success && response.Result != null)
                        {
                            //Saving the data to a database.
                            resultFetched = true;
                            article = response.Result.ToString();
                        }
                    },TaskScheduler.FromCurrentSynchronizationContext());
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (!resultFetched)
                    {
                        if (stopwatch.Elapsed.Seconds > 3 && !resultFetched)
                            resultFetched = true;
                        Application.DoEvents();
                    }
                    stopwatch.Stop();
                    resultFetched = false;
                    article_url    = getArticleLink(article);
                    if (article_url == null) break;
                    article_title = getArticleTitle(article);

                    article = FormatArticle(article, postCount);
                    postFooter.Append("("+ postCount.ToString() + "). <a id=\"excerpt" + postCount.ToString() + "\" href=\"" + article_url + "\" target=\"_blank\" rel=\"nofollow noopener\">"+ article_title + "</a><br>");

                    File.AppendAllText("output.html", postCount.ToString() + ")   " + article + "\n\n\n\n\n\n");
                    postContent.Append(article);
                    postCount++;
                    if (postCount > maxPosts)
                        break;
                } //Loop for first page result

                var script = @"document.getElementById('rld-1').children[0].click();";
                chromiumWebBrowser1.ExecuteScriptAsync(script);
                if (postCount < maxPosts)
                { 
                    for (int i = 10; i <= 26; i++)
                    {
                        // string script = @"document.getElementById('r1-0').innerHTML;";
                        //chromiumWebBrowser1.ExecuteScriptAsync(script);
                        resultFetched = false;
                        article = "";
                        chromiumWebBrowser1.EvaluateScriptAsync("document.getElementById('r1-" + i.ToString().Trim() + "').parentElement.outerHTML").ContinueWith(x =>
                        {
                            var response = x.Result;
                            if (response.Success && response.Result != null)
                            {
                                //Saving the data to a database.
                                resultFetched = true;
                                article = response.Result.ToString();
                            }
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        while (!resultFetched)
                        {
                            if (stopwatch.Elapsed.Seconds > 3 && !resultFetched)
                                resultFetched = true;
                            Application.DoEvents();
                        }
                        stopwatch.Stop();
                        article_url = getArticleLink(article);
                        if (article_url == null) break;
                        article_title = getArticleTitle(article);

                        article = FormatArticle(article, postCount);
                        postFooter.Append("(" + postCount.ToString() + "). <a id=\"excerpt" + postCount.ToString() + "\" href=\"" + article_url + "\" target=\"_blank\" rel=\"nofollow noopener\">" + article_title + "</a><br>");

                        File.AppendAllText("output.html", postCount.ToString() + ")   " + article + "\n\n\n\n\n\n");
                        postContent.Append(article);
                        postCount++;
                        if (postCount > maxPosts)
                            break;
                    } //Loop for second page result
                }

                postFooter.Append("</p>"); 
                postContent.Append(postFooter.ToString());

                
                if (true)
                {
                    string post_name = GenerateSlug(phrase);
                    string query = "INSERT INTO wpuf_posts SET post_author=1,post_date=NOW(),post_date_gmt=UTC_TIMESTAMP(),post_content='" + postContent.ToString().Replace("'", "''").Replace(@"\'",@"\''") +
                            "',post_excerpt ='',to_ping='',pinged ='',post_content_filtered ='',post_title='" + phrase.Replace("'","''").Replace(@"\'", @"\''") + "', " +
                            "post_status='publish',comment_status='open',ping_status='open',post_name='" + post_name.Replace("'", "''").Replace(@"\'", @"\''") + "',post_modified=NOW(),post_modified_gmt=UTC_TIMESTAMP(), " +
                            "post_parent=0,guid='',menu_order=0,post_type='post',comment_count=0";
                    command = new MySqlCommand(query, sqlConn);
                    command.ExecuteNonQuery();

                    

                    command = new MySqlCommand(query, sqlConn);
                    command.CommandText = "SELECT last_insert_id()";
                    object oResult = command.ExecuteScalar();
                    int lastID = Convert.ToInt32(oResult);

                    query = "INSERT INTO wpuf_term_relationships SET object_id="+ lastID.ToString()+ ",term_taxonomy_id="+ postCategory.ToString() + ",term_order=0";
                    command = new MySqlCommand(query, sqlConn);
                    command.ExecuteNonQuery();

                    query = "UPDATE wpuf_posts SET guid='"+ wp_site + "?p=" + lastID.ToString() + "' WHERE ID=" + lastID.ToString();
                    command = new MySqlCommand(query, sqlConn);
                    command.ExecuteNonQuery();
                }
            } //Loop for each keyword
            MessageBox.Show("Import has been finished","Import",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        public int getDefaultCategory()
        {
            string query = "SELECT option_value FROM wpuf_options WHERE option_name = 'default_category'";
            command = new MySqlCommand(query, sqlConn);
            object oResult = command.ExecuteScalar();
            int lastID = Convert.ToInt32(oResult);
            return lastID;
        }
        public string getArticleLink(string article)
        {
            HAP.HtmlDocument html = new HAP.HtmlDocument();
            html.LoadHtml(article);
            HAP.HtmlNodeCollection nodes = html.DocumentNode.SelectNodes("//a[@data-testid='result-extras-url-link']");
            if (nodes == null) return null;
            return nodes[0].GetAttributeValue("href", "");
        }

        public string getArticleTitle(string article)
        {
            HAP.HtmlDocument html = new HAP.HtmlDocument();
            html.LoadHtml(article);
            return html.DocumentNode.SelectNodes("//h2")[0].InnerText;
        }

        public string FormatArticle(string article, int count)
        {
            HAP.HtmlDocument html = new HAP.HtmlDocument();
            html.LoadHtml(article);
            HAP.HtmlNodeCollection nodes = html.DocumentNode.SelectNodes ("//div/article/div");
            nodes[0].Remove();
            string title = html.DocumentNode.SelectNodes("//h2")[0].InnerText;
            html.DocumentNode.SelectNodes("//h2")[0].InnerHtml = title;
            html.DocumentNode.SelectNodes("//article")[0].SetAttributeValue("class", "ddg_article");
            HAP.HtmlNode aNode = HAP.HtmlNode.CreateNode("<a href=\"#excerpt"+count.ToString() + "\" rel=\"noopener\"><sup>("+count.ToString()+")</sup></a>");
            int spans = html.DocumentNode.SelectNodes("//span").Count;
            html.DocumentNode.SelectNodes("//span")[spans-1].AppendChild(aNode);
            return html.DocumentNode.InnerHtml;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void chromiumWebBrowser1_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                pageLoaded = true;
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void keyword_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtHeader.SelectedText = "<KEYWORD>";
        }

        public string RemoveAccent( string txt)
        {
            byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConnectionString = ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
            sqlConn = new MySqlConnection(ConnectionString);
            sqlConn.Open();
            wp_site = ConfigurationManager.AppSettings["site"];
            toolStripStatusLabel1.Text = GetSystemMACID();
            statusStrip.Refresh();
        }


        public string GenerateSlug(string value)
        {

            //First to lower case
            value = value.ToLowerInvariant();

            //Remove all accents
            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(value);
            value = Encoding.ASCII.GetString(bytes);

            //Replace spaces
            value = Regex.Replace(value, @"\s", "-", RegexOptions.Compiled);

            //Remove invalid chars
            value = Regex.Replace(value, @"[^a-z0-9\s-_]", "", RegexOptions.Compiled);

            //Trim dashes from end
            value = value.Trim('-', '_');

            //Replace double occurences of - or _
            value = Regex.Replace(value, @"([-_]){2,}", "$1", RegexOptions.Compiled);

            return value;
        }

        public string GenerateSlug_old( string phrase)
        {
            string str = RemoveAccent(phrase).ToLower();
            // invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // convert multiple spaces into one space   
            str = Regex.Replace(str, @"\s+", " ").Trim();
            // cut and trim 
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
            str = Regex.Replace(str, @"\s", "-"); // hyphens   
            return str;
        }

       
    }


}
