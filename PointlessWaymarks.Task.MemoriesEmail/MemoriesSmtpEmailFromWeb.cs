using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.Json;
using HtmlAgilityPack;
using Mjml.Net;
using PointlessWaymarks.CommonTools;
using Serilog;
using ContentType = System.Net.Mime.ContentType;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

namespace PointlessWaymarks.Task.MemoriesEmail;

public class MemoriesSmtpEmailFromWeb : IMemoriesSmtpEmailFromWeb
{
    public async System.Threading.Tasks.Task GenerateEmail(string settingsFile)
    {
        var notifier = (await WindowsNotificationBuilders.NewNotifier(MemoriesSmtpEmailFromWebSettings.ProgramShortName))
            .SetErrorReportAdditionalInformationMarkdown(FileAndFolderTools.ReadAllText(Path.Combine(
                AppContext.BaseDirectory, "README.md"))).SetAutomationLogoNotificationIconUrl();

        if (string.IsNullOrWhiteSpace(settingsFile))
        {
            Log.Error("Blank settings file is not valid...");
            await notifier.Error("Blank Settings File Name.", "The program should be run with the Settings File as the argument.");
            return;
        }

        var settingsFileInfo = new FileInfo(settingsFile.Trim());

        if (!settingsFileInfo.Exists)
        {
            Log.Error("Could not find settings file: {settingsFile}", settingsFile);
            await notifier.Error($"Could not find settings file: {settingsFile}");
            return;
        }

        MemoriesSmtpEmailFromWebSettings? settings;
        try
        {
            var settingsFileJsonString = await File.ReadAllTextAsync(settingsFileInfo.FullName);
            var tryReadSettings =
                JsonSerializer.Deserialize<MemoriesSmtpEmailFromWebSettings>(settingsFileJsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tryReadSettings == null)
            {
                Log.Error("Settings file {settingsFile} deserialized into a null object - is the format correct?",
                    settingsFile);
                await notifier.Error($"Error: Settings file {settingsFile} deserialized into a null object.", "The program found and was able to read the Settings File - {settingsFile} - but nothing was returned when converting the file into program settings - this probably indicates a format problem with the settings file.");
                return;
            }

            settings = tryReadSettings;
        }
        catch (Exception e)
        {
            Log.Error(e, "Exception reading settings file {settingsFile}", settingsFile);
            await notifier.Error(e);
            return;
        }

        var validationContext = new ValidationContext(settings, null, null);
        var simpleValidationResults = new List<ValidationResult>();
        var simpleValidationPassed = Validator.TryValidateObject(
            settings, validationContext, simpleValidationResults,
            true
        );

        if (!simpleValidationPassed)
        {
            Log.ForContext("SimpleValidationErrors", simpleValidationResults.SafeObjectDump())
                .Error("Validating data from {settingsFile} failed.", settingsFile);
            simpleValidationResults.ForEach(Console.WriteLine);
            await notifier.Error($"Validating data from {settingsFile} failed.",
                simpleValidationResults.SafeObjectDump());

            return;
        }

        Log.ForContext("settings",
                settings.Dump(new DumpOptions
                    { ExcludeProperties = new List<string> { nameof(settings.FromEmailPassword) } }))
            .Information("Settings Passed Basic Validation - Settings File {settingsFile}", settingsFile);

        string fromEmailAddress;
        string fromEmailPassword;

        if (string.IsNullOrEmpty(settings.LoginCode))
        {
            fromEmailAddress = settings.FromEmailAddress;
            fromEmailPassword = settings.FromEmailPassword;
        }
        else
        {
            var emailCredentials =
                PasswordVaultTools.GetCredentials(
                    MemoriesSmtpEmailFromWebSettings.PasswordVaultResourceIdentifier(settings.LoginCode));
            fromEmailAddress = emailCredentials.username;
            fromEmailPassword = emailCredentials.password;
        }

        var httpClient = new HttpClient();
        if (!string.IsNullOrWhiteSpace(settings.BasicAuthUserName) &&
            !string.IsNullOrWhiteSpace(settings.BasicAuthPassword))
        {
            Log.Information("Setting Up HttpClient with Basic Auth Headers");
            httpClient.DefaultRequestHeaders.Authorization =
                new BasicAuthenticationHeaderValue(settings.BasicAuthUserName, settings.BasicAuthPassword);
        }

        var credentials = new NetworkCredential(settings.BasicAuthUserName, settings.BasicAuthPassword);

        var allContent = new HtmlWeb();
        var indexDoc = await allContent.LoadFromWebAsync($"{settings.SiteUrl}", credentials);

        Log.Verbose("Loaded {siteUrl} - Inner Length {indexInnerLength}", settings.SiteUrl,
            indexDoc.DocumentNode.InnerLength);

        var siteTitleNode = indexDoc.DocumentNode.SelectSingleNode("//title");
        var siteTitle = siteTitleNode == null ? string.Empty : HtmlEntity.DeEntitize(siteTitleNode.InnerText);

        //Get the AllContentList from the site as a basis for finding items
        var allContentUrl = $"{settings.SiteUrl}/AllContentList.html";
        var allContentDoc = await allContent.LoadFromWebAsync(allContentUrl, credentials);

        Log.Verbose("Loaded {allContentUrl} - Inner Length {allContentInnerLength}", allContentUrl,
            allContentDoc.DocumentNode.InnerLength);

        //This should match the list nodes - FRAGILE, class changes will break this...
        var items = allContentDoc.DocumentNode.SelectNodes("//div[contains(@class,'content-list-item-container')]");

        if (items == null)
        {
            Log.Error("No Content List Items Found from {allContentUrl}?", allContentUrl);
            await notifier.Error($"Error: No Content List Items Found from {allContentUrl}",
                $"Normally the All Content URL - {allContentUrl} - should have some content. In this case no content - either from the dates relevant to this email or for any other dates were found. This could indicate that there is a problem with the Url in the Settings File - {settingsFile} - or a problem with the site.");
            return;
        }

        Log.Verbose("{allContentUrl} - Content List Items {contentItemCount}", allContentUrl, items.Count);

        Console.WriteLine($"Content Processing - Starting - {items.Count} Items");

        var textInfo = new CultureInfo("en-US", false).TextInfo;

        var fromAddress = new MailAddress(fromEmailAddress, settings.FromDisplayName);
        var toAddress = settings.ToAddressList.Split(";", StringSplitOptions.RemoveEmptyEntries)
            .Select(x => new MailAddress(x.Trim())).ToList();

        var message = new MailMessage
        {
            IsBodyHtml = true
        };

        message.From = fromAddress;
        toAddress.ForEach(x => message.To.Add(x));

        var contentSections = new List<string>();
        var yearsWithContent = new List<int>();

        long attachmentLimit = 24117248;
        long attachmentTotal = 0;

        foreach (var loopYearsBack in settings.YearsBack)
        {
            var currentItem = 0;

            var targetDateMatchItems = new List<(string itemUrl, string imageUrl, string title, string itemType)>();

            var targetDate = settings.ReferenceDate.AddYears(-Math.Abs(loopYearsBack));
            var targetDateString = targetDate.ToString("yyyy-MM-dd");

            Log.Information("Checking for Target Date {targetDate}", targetDateString);

            foreach (var loopNode in items)
            {
                currentItem++;

                if (currentItem % (items.Count / 10) == 0)
                    Console.WriteLine($"  Content Processing - {currentItem} of {items.Count}");

                //Get the url for this item - if it isn't present continue (nothing to do, can't link to it...)
                var linkToUrl = loopNode.GetAttributeValue("data-target-url", string.Empty);
                if (string.IsNullOrWhiteSpace(linkToUrl)) continue;

                //Get the Title for this item - if it isn't present continue (not sure what that would mean...)
                var linkToTitle = loopNode.GetAttributeValue("data-title", string.Empty);
                if (string.IsNullOrWhiteSpace(linkToTitle)) continue;

                //Get the date and check for a match
                var dateCreated = loopNode.GetAttributeValue("data-created", string.Empty);
                var type = loopNode.GetAttributeValue("data-content-type", string.Empty);
                if (string.IsNullOrWhiteSpace(dateCreated) || string.IsNullOrWhiteSpace(type)) continue;
                if (!dateCreated.StartsWith(targetDateString)) continue;

                //Try to find an image and extract the url of an appropriate 
                var imageUrl = string.Empty;

                var imgNode = loopNode.SelectSingleNode(".//img");
                if (imgNode != null)
                {
                    var imgSrcSet = imgNode.GetAttributeValue("srcset", string.Empty);

                    if (string.IsNullOrWhiteSpace(imgSrcSet)) continue;

                    var rawImageList = imgSrcSet.Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim());

                    var imageList = new List<(int size, string url)>();

                    foreach (var loopImages in rawImageList)
                    {
                        var urlAndSize = loopImages.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (urlAndSize.Length != 2 || urlAndSize.Any(string.IsNullOrWhiteSpace)) continue;
                        var sizeParsed = int.TryParse(urlAndSize[1][..^1], out var parsedSize);
                        if (!sizeParsed) continue;
                        imageList.Add((parsedSize, urlAndSize[0]));
                    }

                    imageUrl = imageList.Where(x => x.size <= 700).MaxBy(x => x.size).url;
                }

                targetDateMatchItems.Add((linkToUrl, imageUrl, linkToTitle, type));
            }

            if (!targetDateMatchItems.Any())
            {
                Log.Information("No Items Found on Target Date {targetDate} - Ending...", targetDateString);
                continue;
            }

            yearsWithContent.Add(loopYearsBack);

            // ReSharper disable once StringLiteralTypo
            contentSections.Add($"""               
                            <mj-section>
                                <mj-column>
                    				<mj-text align="center" font-size="14px">
                                    {(string.IsNullOrWhiteSpace(siteTitle) ? "C" : $"{siteTitle} c")}ontent created {loopYearsBack}
                                     year{(loopYearsBack > 1 ? "s" : "")} ago ({targetDateString}).
                    				</mj-text>
                                </mj-column>
                            </mj-section>
                    """);

            var groupedItems = targetDateMatchItems.GroupBy(x => x.itemType).ToList();

            var imageCarouselMjmlLines = new List<string>();

            var imageItems = groupedItems.Where(x => x.Key.Equals("image", StringComparison.OrdinalIgnoreCase))
                .SelectMany(x => x).ToList();

            Log.Information("Found {imageCount} Images to Add to the Email", imageItems);

            var imagesProcessed = 0;

            Console.WriteLine($"Image Processing - Starting - {imageItems.Count} Images");

            foreach (var loopItems in imageItems)
            {
                imagesProcessed++;

                if (attachmentTotal > attachmentLimit)
                {
                    Log.Information(
                        "Adding Images ended because the Current Size of the Attachments ({attachmentTotal}) exceeds the Limit set by the Program ({attachmentLimit}) - Image Number {imagesProcessed} of {totalImages} Total Images",
                        attachmentTotal, attachmentLimit, imagesProcessed, imageItems.Count);
                    break;
                }

                Console.WriteLine($"  Downloading {loopItems.imageUrl}");
                var imageBytes = await httpClient.GetByteArrayAsync(loopItems.imageUrl);

                attachmentTotal += imageBytes.Length;

                var contentId = Guid.NewGuid().ToString();

                var imageEmbed = new Attachment(new MemoryStream(imageBytes),
                    new ContentType("image/jpeg"));
                imageEmbed.ContentId = contentId;
                imageEmbed.ContentDisposition!.Inline = true;
                imageEmbed.ContentDisposition.DispositionType = DispositionTypeNames.Inline;

                message.Attachments.Add(imageEmbed);

                imageCarouselMjmlLines.Add($"""
	<mj-carousel-image src="cid:{contentId}" />
""");
            }

            if (imageCarouselMjmlLines.Any())
                contentSections.Add($"""
<mj-section>
      <mj-column>
        <mj-carousel>
          {string.Join(Environment.NewLine, imageCarouselMjmlLines)}
        </mj-carousel>
      </mj-column>
    </mj-section>
""");

            foreach (var loopItems in groupedItems)
            {
                Log.Information("Found {contentItemCount} {contentType} Items to Process", loopItems.Count(),
                    loopItems.Key);

                var itemTexts = new List<string>();
                foreach (var loopItem in loopItems)
                    itemTexts.Add($"""
		<mj-text padding-left="14px" padding-top="2px"><a href="{loopItem.itemUrl}">{loopItem.title}</a></mj-text>
		""");

                contentSections.Add($"""
<mj-section>
	<mj-column>
		<mj-text font-size="14px" padding-left="0px">{textInfo.ToTitleCase(loopItems.Key)}{(loopItems.Count() > 1
            ? "s"
            : string.Empty)}:</mj-text>
			{string.Join(Environment.NewLine, itemTexts)}
	</mj-column>
</mj-section>
""");
            }
        }

        if (!contentSections.Any())
        {
            Log.Information("No Content Found for Any Referenced Year/Date");
            return;
        }

        var yearTextList = yearsWithContent.Count > 1
            ? string.Join(", ", yearsWithContent.Take(yearsWithContent.Count - 1)) + " and " + yearsWithContent.Last()
            : yearsWithContent.First().ToString();

        var subject =
            $"[{(string.IsNullOrWhiteSpace(siteTitle) ? settings.SiteUrl : siteTitle)}] {yearTextList} Year{(yearTextList.Equals("1") ? "" : "s")} Ago...";
        message.Subject = subject;

        var text = $"""

            <mjml>
    <mj-head>
        <mj-title>
            {(string.IsNullOrWhiteSpace(siteTitle) ? settings.SiteUrl : siteTitle)} Content from {yearTextList} Year{(yearTextList.Equals("1") ? "" : "s")} Ago...</mj-title>
    </mj-head>
    <mj-body>
	    <mj-section>
      		<mj-column>
        		<mj-text align="center" font-size="30px"><a href="{settings.SiteUrl}">{
                    (string.IsNullOrWhiteSpace(siteTitle) ? settings.SiteUrl : siteTitle)}</a></mj-text>
	      	</mj-column>
	    </mj-section>
        {string.Join(Environment.NewLine, contentSections)}
    </mj-body>
</mjml>
""";

        Console.WriteLine("Rendering Email");
        var mjmlRenderer = new MjmlRenderer();
        var html = mjmlRenderer.Render(text).Html;
        message.Body = html;

        Console.WriteLine("Sending Email");
        var smtp = new SmtpClient
        {
            Host = settings.SmtpHost,
            Port = settings.SmtpPort,
            EnableSsl = settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential(fromEmailAddress, fromEmailPassword),
            Timeout = 60000
        };

        smtp.Send(message);

        message.Dispose();

        Log.Information("Sent Email!");
    }
}