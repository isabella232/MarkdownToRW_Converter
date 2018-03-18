﻿using System.Collections.Generic;
using System.IO;
using System.Net;
using CommonMark;
using HtmlAgilityPack;

namespace DragonMarkdown
{
    public class Converter
    {
        public static string ConvertMarkdownStringToHtml(string markdown)
        {
            string output = CommonMarkConverter.Convert(markdown);
            output = WebUtility.HtmlDecode(output);

            // HTML readability improvements & RW specific changes

            // Code
            output = output.Replace("<pre><code class=", "\r\n<pre lang=");
            output = output.Replace("lang-", "");
            output = output.Replace("language-", "");
            output = output.Replace("</code></pre>", "</pre>\r\n");

            // Add attributes
            AddClassToImages(ref output);
            AddExtraAttributesToLinks(ref output);

            // Text
            output = output.Replace("<p>", "\r\n");
            output = output.Replace("<br>", "\r\n");
            output = output.Replace("</p>", "");
            output = output.Replace("<h1", "\r\n<h1");
            output = output.Replace("<h2", "\r\n<h2");
            output = output.Replace("<h3", "\r\n<h3");
            output = output.Replace("<h4", "\r\n<h4");
            output = output.Replace("<strong>", "<em>");
            output = output.Replace("</strong>", "</em" +
                                                 ">");

            // List
            output = output.Replace("<ul>", "\r\n<ul>");
            output = output.Replace("<ol>", "\r\n<ol>");

            //// Note
            output = output.Replace("</blockquote>", "</div>");
            output = output.Replace("<blockquote>\r\n", "\r\n<blockquote>");
            output = output.Replace("<blockquote>\r\n<em>Note", "<div class=\"note\">\r\n<em>Note");
            output = output.Replace("<blockquote>", "<div>");

            // Spoiler
            output = output.Replace("<blockquote>\r\n<em>Spoiler", "<div class=\"spoiler\">\r\n<em>Spoiler");
            output = output.Replace("<div class=\"spoiler\">", "[spoiler title=\"Solution\"]");
            // TODO: replace first </div> found after "<div class=\"spoiler\">" with </spoiler> somehow for all spoilers

            // Final cleanup
            output = output.Replace("<div></div>", "");

            output = output.Trim();
            return output;
        }

        public static void ConvertMarkdownFileToHtmlFile(string markdownFilePath, string htmlFilePath)
        {
            using (StreamReader sr = new StreamReader(markdownFilePath))
            {
                string html = ConvertMarkdownStringToHtml(sr.ReadToEnd());
                using (StreamWriter sw = new StreamWriter(htmlFilePath))
                {
                    sw.Write(html);
                    sw.Flush();
                    sw.Close();
                }
            }
        }

        public static void AddClassToImages(ref string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img");

            if (imgNodes == null)
            {
                return;
            }

            for (var i = 0; i < imgNodes.Count; i++)
            {
                HtmlNode node = imgNodes[i];

                if (i == 0) // First image should be right aligned, it's the 250x250 image
                {
                    HtmlAttribute classAttribute = doc.CreateAttribute("class", "alignright size-full");
                    node.Attributes.Add(classAttribute);
                }
                else
                {
                    HtmlAttribute classAttribute = doc.CreateAttribute("class", "aligncenter size-full");
                    node.Attributes.Add(classAttribute);
                }
            }

            html = doc.DocumentNode.OuterHtml;
        }

        private static void AddExtraAttributesToLinks(ref string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection linkNodes = doc.DocumentNode.SelectNodes("//a");

            if (linkNodes == null)
            {
                return;
            }

            for (var i = 0; i < linkNodes.Count; i++)
            {
                HtmlNode node = linkNodes[i];

                HtmlAttribute relAttribute = doc.CreateAttribute("rel", "noopener");
                node.Attributes.Add(relAttribute);

                HtmlAttribute targetAttribute = doc.CreateAttribute("target", "_blank");
                node.Attributes.Add(targetAttribute);
            }

            html = doc.DocumentNode.OuterHtml;
        }

        public static List<ImageLinkData> FindAllImageLinksInHtml(string html, string htmlFolder)
        {
            List<ImageLinkData> links = new List<ImageLinkData>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img");

            if (imgNodes == null)
            {
                return links;
            }

            foreach (HtmlNode node in imgNodes)
            {
                // Skip if web link
                if (node.GetAttributeValue("src", "").StartsWith("http") ||
                    node.GetAttributeValue("src", "").StartsWith("www"))
                {
                    continue;
                }

                string localPath = node.GetAttributeValue("src", null);
                string fullPath = htmlFolder + "/" + localPath;

                // Check if file exists
                if (File.Exists(fullPath))
                {
                    ImageLinkData imageData = new ImageLinkData
                    {
                        LocalImagePath = localPath,
                        FullImagePath = fullPath
                    };

                    links.Add(imageData);
                }
            }

            return links;
        }

        public static string BatchReplaceText(string text, List<string> originals, List<string> replacements)
        {
            string newText = text;

            for (var i = 0; i < originals.Count; i++)
            {
                string original = originals[i];
                string replacement = replacements[i];
                newText = newText.Replace(original, replacement);
            }

            return newText;
        }

        public static void QuickWriteFile(string path, string content)
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(content);
                sw.Flush();
                sw.Close();
            }
        }
    }

    public struct ImageLinkData
    {
        public string FullImagePath;
        public string LocalImagePath;
    }
}