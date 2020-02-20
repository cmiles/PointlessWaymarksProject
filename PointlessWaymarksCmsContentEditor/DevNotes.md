## Todos
 - RSS - Check Functionality (seemed to work but double check all)
 - JSON Link Backup file in list directory
 - Change Created and Updated style when creator and updater are the same (do via tests!)
 - Better startup experience when settings file and/or db are missing
 - RSS Feed main images when appropriate
 - Revisit og metadata - used correctly? Other tags that could be included?
 - Look at adding Dublin Core metadata to pages
 - Top of the page search box - maybe pass a query parameter to the all content list page?
 - Should there be a small menu at the top? Ugh.
 - Need to be able to select/switch settings files (and so associated db) - this seems much easier and happier in SQLite? That does have implications for elevation (Z) in Spatialite - but with that functionality minimal so far worth working around. Actually like the current MS Sql setup but painful compared to SQLite in terms of setup...

## Ideas
 - What if you piped/setup your google alerts to RSS and there was integrated support for pulling them in and working with them. Obvious con is not sure even if RSS is still currently an option whether it will always be an option.
 - Backup the master media directory and database
 - Restore from JSON
 - Some sort of Master JSON Backup
 - Check in on the Spatialite Z bug in EF Core and/or investigate moving to SQLite (what about an elevation lookup table vs Z values?)

2/19/2020
Updated the Link List generation to alter the structures that are generated and to change some classes where I wanted the structure to be different than the compact content.
Changed the compact content generation so that the classes indicate when you have an 'optional' summary (has image - not imagining ever hiding the picture over the summary?) or a non-optional summary. This allowed a quick CSS change to get the desired effect which is good - but it did remind me that it would be interesting to look more at current best practices for css class strategies, I wonder if this should have a been an additional functional class (hide-for-mobile? or can-hide of the optional? or...) instead...

2/17/2020
Added Pinboard integration via a nice wrapper I found in Nuget/GitHub - notes:
 - In addition to 'liking' Pinboard integration is included because Pinboard has a plan where a copy of the of website at the time of the link is stashed - this is important for a long lived project because it is hard to predict what links and sites are going to disappear off the web...
 - A nice part of the Pinboard API is that 'add' has a flag for replace - for this integration to keep it simple I just call Add with the replace and don't worry about whether it existed before or not
 - Rather than separate actions for saving to Pinboard or UI choices I just decided to check for a Pinboard API Token - this could cause confusion if you setup a new config and didn't realize items weren't saving but I don't think that is a major concern at this point and I just put a progress line in.

2/16/2020
AngleSharp Notes:
 - Getting web pages thru the AngleSharp library was pleasantly easy - only hitch was that with the simple sample defaults one of the first pages I hit seemed to detect this style as 'noscript' and some of the metadata wasn't included (I am not even sure this was the intended behaviour, but regardless that was what was happening) but this was quickly fixed by including the AngleSharp.Js package and including .WithJs() - it could be this eventually causes an issue but pleasantly it didn't yet cause any issues or a long enough delay that I cared.
 - I have dealt with Open Graph metadata, and looked at other metadata, for HikeLemmon but hadn't before looked across sites for 'standard metadata' like author and publish date - at least with the sample of sites I looked at, which included many smaller news sites, it was a mess - og: data was often included (I assume in part for Facebook), some sites used https://www.dublincore.org/, some had twitter info, and some didn't have great info. I suppose I understand in terms of business, average consumer and lack of incentives - but this (plus the insane number of script tags included in basically all of these sites) makes me want to do a better job on this with this project - I feel like this data 'should' be there and consistent
