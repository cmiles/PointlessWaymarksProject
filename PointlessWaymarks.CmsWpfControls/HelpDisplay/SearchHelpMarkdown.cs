namespace PointlessWaymarks.CmsWpfControls.HelpDisplay;

public static class SearchHelpMarkdown
{
    public static string HelpBlock =>
        @"
### Search Help

In general you can simply type in a word, or words, into the search box and there is a reasonable chance that you can find the content you want! (Remember that you may have to hit 'Load All' if there are hundreds of items in the list...). Searching this way will search Title, Tags, Summary, Folder, Created By, Updated By and Content Id for you search string.

There are also additional search options. First some examples:

Tags: flower  
Created On: >= 1/1/2019 < 1/1/2021  
Focal Length: == 90  

The search above will return photos from 2019 or 2020 shot with a 90mm lens.

This search attempts to find night photos by eliminating anything taken between 7am and 7pm and only including higher ISO photos where any common text field contains 'star'.

!Photo Created On: > 7am < 7pm  
Iso: >= 1200  
star  

To use the additional search options put each of your filters on its own line - a search filter can start with ! if you want to indicate Not but otherwise each line must start with one of the following field names or be a general text field search:

General
  - Created On: (date, time)
  - Created By: (string)
  - Last Updated On: (date, time)
  - Last Updated By: (string)
  - Folder: (string)
  - Summary: (string)
  - Tags: (string)
  - Title: (string)
  - Type: (string) - this is the type of content - for example 'Photo' or 'Post'

Photo Specific
  - Lens: (string)
  - License: (string)
  - Aperture: (with or without a 'f', careful of comparisons as '> f9' means larger (more open) aperture which will be small numbers)
  - Focal Length: (number with or without mm)
  - ISO: (number)
  - Camera: (searches make and model)
  - Shutter Speed: (indicate in standard notation such as 1/40)
  - Photo Created On: (string)

For string fields the search is a 'contains' search. With dates and number (or number like values) you can use ==, >, >=, < and <=.

All lines are combined with And/&&.
";
}