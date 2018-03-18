﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DragonMarkdown;
using WordPressSharp.Models;

namespace MarkdownToRWCore
{
    public static class InteractiveConsole
    {
        public static void StartInteractive()
        {
            WordPressConnector.InitializeWordPress("ekerckhove", "AjRSs8HZ");
            var user = WordPressConnector.GetUserProfile();

            if (user!=null)
            {
                Console.WriteLine(user.FirstName);
            }

            string markdownPath = null;
            string htmlPath = null;

            WriteIntro();

            while (markdownPath == null)
            {
                Console.WriteLine("Please provide the path to the markdown file:");
                markdownPath = GetExistingFilePath();
            }

            string sameFolder = null;

            while (sameFolder != "y" && sameFolder != "n")
            {
                Console.WriteLine(
                    "Do you want to output the HTML file containing the WordPress ready code to the same folder as the markdown file? (y/n)");
                sameFolder = Console.ReadLine();
            }

            if (sameFolder == "y")
            {
                htmlPath = Helper.GetFullPathWithoutExtension(markdownPath) + ".html";
            }
            else
            {
                while (htmlPath == null || htmlPath == markdownPath)
                {
                    if (htmlPath == markdownPath)
                    {
                        Console.WriteLine(
                            "Output file can't be the same as the input file! This WILL lead to data loss.");
                    }

                    Console.WriteLine("Please provide the location and name for the output html file:");
                    Console.WriteLine("(e.g. C:\\MyNewFile.html)");
                    htmlPath = GetNewFilePath();
                }
            }

            string uploadImages = null;

            while (uploadImages != "y" && uploadImages != "n" && uploadImages != "only html")
            {
                Console.WriteLine(
                    "Do you want to upload all images and update the links in both the markdown file and the generated html? (y/only html/n)");
                uploadImages = Console.ReadLine();
            }

            string ready = null;

            while (ready != "y" && ready != "n")
            {
                Console.WriteLine("Ready to start the conversion!");
                Console.WriteLine("");
                Console.WriteLine("Parameters:");
                Console.WriteLine("Markdown file path (input): " + markdownPath);
                Console.WriteLine("Generated HTML file path (output): " + htmlPath);
                Console.WriteLine("Upload images and update markdown and generated HTML file: " + uploadImages);
                Console.WriteLine("");
                Console.WriteLine("Are these parameters correct? (y/n)");
                ready = Console.ReadLine();
            }

            if (ready == "n")
            {
                Console.WriteLine("Conversion aborted.");
                return;
            }

            switch (uploadImages)
            {
                case "y":
                    DoConversion(markdownPath, htmlPath, true, false);
                    break;
                case "only html":
                    DoConversion(markdownPath, htmlPath, true, true);
                    break;
                case "n":
                    DoConversion(markdownPath, htmlPath, false, false);
                    break;
            }
        }

        private static void DoConversion(string markdownPath, string htmlPath, bool uploadImages, bool onlyUpdateHtml)
        {
            Console.WriteLine("------------");
            Console.WriteLine("Converting...");
            Converter.ConvertMarkdownFileToHtmlFile(markdownPath, htmlPath);
            Console.WriteLine("Markdown converted!");

            if (uploadImages)
            {
                Console.WriteLine("------------");
                User user = null;
                Console.WriteLine("Starting image upload. Please enter your RW WordPress credentials:");

                while (user == null)
                {
                    Console.WriteLine("Username:");
                    string username = Console.ReadLine();

                    Console.WriteLine("Pasword:");
                    string password = Console.ReadLine();

                    WordPressConnector.InitializeWordPress(username, password);
                    user = WordPressConnector.GetUserProfile();

                    if (user == null)
                    {
                        Console.WriteLine("Incorrect credentials / can't connect to RW WordPress.");
                        Console.WriteLine("Please try again.");
                    }
                }

                Console.WriteLine("");
                Console.WriteLine("Login succesful! Thanks for using the app, " + user.FirstName + "!");
                Console.WriteLine("Gathering files...");
                // Get image paths
                List<string> fullImagePaths = new List<string>();
                List<string> localImagePaths = new List<string>();
                List<string> imageUrls = new List<string>();
                List<string> imageIDs = new List<string>();

                string markdownText = "";
                string htmlText = "";

                using (StreamReader sr = new StreamReader(markdownPath))
                {
                    markdownText = sr.ReadToEnd();
                }

                using (StreamReader sr = new StreamReader(htmlPath))
                {
                    htmlText = sr.ReadToEnd();
                }

                var links = Converter.FindAllImageLinksInHtml(htmlText, Path.GetDirectoryName(htmlPath));

                if (links.Count == 0)
                {
                    Console.WriteLine("No images found. Skipping upload.");
                    ConversionFinished(htmlPath);
                    return;
                }

                foreach (ImageLinkData link in links)
                {
                    fullImagePaths.Add(link.FullImagePath);
                    localImagePaths.Add(link.LocalImagePath);
                }

                Console.WriteLine("");
                Console.WriteLine(fullImagePaths.Count + " image paths found:");

                foreach (string path in fullImagePaths)
                {
                    Console.WriteLine(path + " (" + new FileInfo(path).Length / 1024 + " kb)");
                }

                // Upload images
                for (var i = 0; i < fullImagePaths.Count; i++)
                {
                    string path = fullImagePaths[i];

                    Console.WriteLine("Uploading: " + " (" + i + 1 + "/" + fullImagePaths.Count + ")" + path + "...");

                    var result = WordPressConnector.UploadFile(path);

                    if (result != null)
                    {
                        imageUrls.Add(result.Url);
                        imageIDs.Add(result.Id);
                    }
                    else
                    {
                        Console.WriteLine("Image upload failed! Aborting upload and going into file cleanup mode...");
                        StartFileDeletion(imageIDs);
                        return;
                    }
                }

                // Update markdown & html
                Console.WriteLine("Starting link replacer...");
                markdownText = Converter.BatchReplaceText(markdownText, localImagePaths, imageUrls);
                htmlText = Converter.ConvertMarkdownStringToHtml(markdownText);

                Converter.QuickWriteFile(htmlPath, htmlText);
                Console.WriteLine("Replaced HTML!");

                if (!onlyUpdateHtml)
                {
                    Converter.QuickWriteFile(markdownPath, markdownText);
                    Console.WriteLine("Replaced Markdown!");
                }
            }

            ConversionFinished(htmlPath);
        }

        private static void StartFileDeletion(List<string> imageIDs)
        {
            Console.WriteLine("------------------------------------------------");

            Console.WriteLine("               _                            __ ");
            Console.WriteLine(" ____         | |_      _____         _    |  |");
            Console.WriteLine("|    \\ ___ ___|_| |_   |  _  |___ ___|_|___|  |");
            Console.WriteLine("|  |  | . |   | |  _|  |   __| .'|   | |  _|__|");
            Console.WriteLine("|____/|___|_|_| |_|    |__|  |__,|_|_|_|___|__|");
            Console.WriteLine("");
            Console.WriteLine("------------------------------------------------");

            Console.WriteLine("Open source Hacked Core Restorative Automatic Program (OH CRAP) v2");
            Console.WriteLine("------------");
            Console.WriteLine("Deleting uploaded images...");

            foreach (string iD in imageIDs)
            {
                var result = WordPressConnector.Delete(Convert.ToInt32(iD));
                if (result)
                {
                    Console.WriteLine("Deleted file with id " + iD);
                }
                else
                {
                    Console.WriteLine("Failed to delete file with id " + iD);
                }
            }

            Console.WriteLine("Cleanup complete! Press any key to exit.");
            Console.ReadKey();
        }

        private static void ConversionFinished(string generatedHtmlPath)
        {
            Console.WriteLine("------------");
            Console.WriteLine("All tasks completed! Thanks for using this app! :]");

            string openHtml = null;

            while (openHtml != "y" && openHtml != "n")
            {
                Console.WriteLine(
                    "Do you want to open the generated HTML file so you can copy its contents to the WordPress post now? (y/n)");
                openHtml = Console.ReadLine();
            }

            if (openHtml == "y")
            {
                Process.Start(generatedHtmlPath);
            }

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void WriteIntro()
        {
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine(" _____         _     _                  _____        _____ _ _ _ ");
            Console.WriteLine("|     |___ ___| |_ _| |___ _ _ _ ___   |_   _|___   | __  | | | |");
            Console.WriteLine("| | | | .'|  _| '_| . | . | | | |   |    | | | . |  |    -| | | |");
            Console.WriteLine("|_|_|_|__,|_| |_,_|___|___|_____|_|_|    |_| |___|  |__|__|_____|");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Markdown To RW Wordpress HTML Converter [.NET Core Version]");
            Console.WriteLine("Made by Eric Van de Kerckhove (BlackDragonBE)");
            Console.WriteLine("");
        }

        private static string GetExistingFilePath()
        {
            string path = Console.ReadLine().Replace("\"", "");

            if (File.Exists(path))
            {
                return path;
            }

            Console.WriteLine("File " + path + " doesn't exist!");
            Console.WriteLine("");
            return null;
        }

        private static string GetNewFilePath()
        {
            string path = Console.ReadLine().Replace("\"", "");

            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                return path;
            }

            Console.WriteLine("Invalid folder!");
            Console.WriteLine("");
            return null;
        }
    }
}