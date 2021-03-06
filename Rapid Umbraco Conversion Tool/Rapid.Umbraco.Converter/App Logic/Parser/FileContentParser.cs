﻿using Codetreehouse.RapidUmbracoConverter.Tools.Entities;
using Codetreehouse.RapidUmbracoConverter.Tools.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;

namespace Codetreehouse.RapidUmbracoConverter.Tools
{
    public class FileContentParser
    {
        /// <summary>
        /// Gets the files from the defined template file path, and converts them to UmbracoConversionObjects
        /// </summary>
        /// <param name="templateDirectory">The directory that contains the template files</param>
        /// <param name="allowedExtensions">The extensions that will be used within the directory</param>
        /// <returns></returns>
        public IEnumerable<RapidUmbracoConversionObject> Convert(string templateDirectory, string[] allowedExtensions)
        {
            if (String.IsNullOrWhiteSpace(templateDirectory))
                throw new ArgumentNullException("templateDirectory", "Cannot find the files if a file location is not supplied");

            if (allowedExtensions.Count() <= 0)
                throw new ArgumentException("Accepted extensions were not provided");

            //Extract the files into the basic Rapid Umbraco Conversion Object
            List<RapidUmbracoConversionObject> umbracoConversionObjects = ConvertWithoutProperties(templateDirectory, allowedExtensions);

            foreach (var conversionObject in umbracoConversionObjects)
            {
                string fileContents = conversionObject.FileContent;

                int startOfTagIndex = 0,
                    endOfTagIndex = 0;

                //Get all of the indexes for the position
                while (startOfTagIndex >= 0 && endOfTagIndex >= 0)
                {
                    startOfTagIndex = fileContents.IndexOf(RapidUmbracoSettings.TagStart, startOfTagIndex + 1);
                    endOfTagIndex = fileContents.IndexOf(RapidUmbracoSettings.TagEnd, endOfTagIndex + 1);

                    if (startOfTagIndex >= 0 && endOfTagIndex >= 0)
                    {
                        Debug.WriteLine($"Found tag at position: {startOfTagIndex} to {endOfTagIndex}");

                        string tag = fileContents.Substring(startOfTagIndex, (endOfTagIndex - startOfTagIndex + RapidUmbracoSettings.TagEnd.Length));
                        Debug.WriteLine($"Tag: {tag}");

                        UmbracoConversionProperty convertedProperty = ConvertMarkupTagToUmbracoConversionProperty(tag);
                        conversionObject.PropertyCollection.Add(convertedProperty);
                    }
                }
            }

            Debug.WriteLine("Umbraco Conversion Objects: " + umbracoConversionObjects.Count());
            return umbracoConversionObjects;
        }




        /// <summary>
        /// Itterates the defined path looking for files that match the allowed extensions and converts them to basic Umbraco Conversion Objects without the properties having been extracted
        /// </summary>
        /// <param name="templateDirectory"></param>
        /// <param name="allowedExtensions"></param>
        /// <returns></returns>
        public List<RapidUmbracoConversionObject> ConvertWithoutProperties(string templateDirectory, string[] allowedExtensions)
        {

            if (templateDirectory.StartsWith("~/"))
                templateDirectory = HttpContext.Current.Server.MapPath(templateDirectory);

            if (!Directory.Exists(templateDirectory))
                throw new ArgumentException("The defined file location does not exist. Location: " + templateDirectory);

            List<RapidUmbracoConversionObject> convertList = new List<RapidUmbracoConversionObject>();

            foreach (string extension in allowedExtensions)
            {
                //Insert the period at the start if it doesn't already have one
                if (!extension.StartsWith(".")) extension.Insert(0, ".");

                //Get all of the files in the given directory with the current extension
                foreach (FileInfo file in new DirectoryInfo(templateDirectory).GetFiles("*" + extension, SearchOption.AllDirectories))
                {
                    using (StreamReader streamReader = file.OpenText())
                    {
                        //Create the new Umbraco Conversion Object
                        convertList.Add(new RapidUmbracoConversionObject()
                        {
                            FileName = file.Name,
                            Name = file.Name.Replace(file.Extension, ""),
                            FileContent = streamReader.ReadToEnd(),
                            FilePath = file.FullName
                        });
                    }
                }
            }
            return convertList;
        }


        /// <summary>
        /// Converts a well formed Umbraco Conversion Tag into an Umbraco Conversion Property that can then be applied to a ContentType
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public UmbracoConversionProperty ConvertMarkupTagToUmbracoConversionProperty(string tag)
        {
            //Split the values from the tag into a dictionary that can then be used to set the properties
            Dictionary<string, string> tagDictionary = tag.Split(' ')
                .Select(x => x.Split('='))
                .Where(x => x.Length == 2)
                .ToDictionary(x => x.First().RemoveSpecialCharacters(), x => x.Last().RemoveSpecialCharacters());

            var conversionProperty = new UmbracoConversionProperty(tag)
            {
                Alias = GetPropertyValue(tagDictionary, "alias", isRequired: true),
                Label = GetPropertyValue(tagDictionary, "label", isRequired: true, defaultValue: GetPropertyValue(tagDictionary, "alias", isRequired: true).FromCamelToWord()),
                Description = GetPropertyValue(tagDictionary, "description", isRequired: false),
                Editor = GetPropertyValue(tagDictionary, "editorAlias", isRequired: true, defaultValue: "Umbraco.Textbox"),
                Tab = GetPropertyValue(tagDictionary, "tab")
            };

            return conversionProperty;
        }


        /// <summary>
        /// Extracts the property value from the tag dictionary if it exists. Throws an exception if the key is requires, but not available
        /// </summary>
        /// <param name="tagDictionary"></param>
        /// <param name="key"></param>
        /// <param name="isRequired"></param>
        /// <returns></returns>
        private string GetPropertyValue(Dictionary<string, string> tagDictionary, string key, bool isRequired = false, string defaultValue = "")
        {
            if (tagDictionary.ContainsKey(key))
            {
                return tagDictionary[key];
            }
            else if (!String.IsNullOrWhiteSpace(defaultValue))
            {
                return defaultValue;
            }
            else if (isRequired)
            {
                throw new Exception($"Umbraco Conversion Tag is missing the '{key}' argument'");
            }
            else
            {
                return string.Empty;
            }


        }
    }
}
