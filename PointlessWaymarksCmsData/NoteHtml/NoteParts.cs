using System;
using HtmlTags;
using PointlessWaymarksCmsData.Models;

namespace PointlessWaymarksCmsData.NoteHtml
{
    public static class NoteParts
    {
        public static string TitleString(NoteContent content)
        {
            return $"Notes - {CommonHtml.Tags.CreatedByAndUpdatedOnString(content).TrimLastCharacter()}";
        }
        
        public static HtmlTag TitleDiv(NoteContent dbEntry)
        {
            var settings = UserSettingsSingleton.CurrentSettings();

            var titleContainer = new HtmlTag("div").AddClass("note-title-link-container");

            var header = new HtmlTag("h2").AddClass("note-title-link-content");
            var linkToFullPost = new LinkTag(TitleString(dbEntry), settings.NotePageUrl(dbEntry));
            header.Children.Add(linkToFullPost);

            titleContainer.Children.Add(header);

            return titleContainer;
        }
        
        public static string TrimLastCharacter(this String str)
        {
            return string.IsNullOrEmpty(str) ? str : str.TrimEnd(str[^1]);
        }
    }
}