using HtmlAgilityPack;
using System.Globalization;
using System.Net.Mail;
using System.Net.Mime;
using System.Net;
using Mjml.Net;

namespace PointlessWaymarks.MemoriesEmail
{
    public class EmailService
    {
        public async Task Run(EmailMemorySettings emailSettings)
        {
            var httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(emailSettings.BasicAuthUserName) && !string.IsNullOrWhiteSpace(emailSettings.BasicAuthPassword))
            {
                httpClient.DefaultRequestHeaders.Authorization = new BasicAuthenticationHeaderValue(emailSettings.BasicAuthUserName, emailSettings.BasicAuthPassword);
            }

            var targetDate = emailSettings.ReferenceDate.AddYears(-Math.Abs(emailSettings.YearsBack));
            var targetDateString = targetDate.ToString("yyyy-MM-dd");

            var credentials = new NetworkCredential(emailSettings.BasicAuthUserName, emailSettings.BasicAuthPassword);

            HtmlWeb allContent = new HtmlWeb();
            var indexDoc = await allContent.LoadFromWebAsync($"{emailSettings.SiteUrl}", credentials);

            var siteTitleNode = indexDoc.DocumentNode.SelectSingleNode("//title");
            var siteTitle = siteTitleNode == null ? string.Empty : HtmlEntity.DeEntitize(siteTitleNode.InnerText);

            var siteDescriptionNode = indexDoc.DocumentNode.SelectSingleNode("//meta[contains(@name,'description')]");
            var siteDescription = siteDescriptionNode.GetAttributeValue("content", string.Empty);

            //Get the AllContentList from the site as a basis for finding items
            var allContentDoc = await allContent.LoadFromWebAsync($"{emailSettings.SiteUrl}/AllContentList.html", credentials);

            //This should match the list nodes - FRAGILE, class changes will break this...
            var items = allContentDoc.DocumentNode.SelectNodes("//div[contains(@class,'content-list-item-container')]");

            var targetDateMatchItems = new List<(string itemUrl, string imageUrl, string title, string itemType)>();

            foreach (var loopNode in items)
            {
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

                    if (string.IsNullOrWhiteSpace(imgSrcSet)) return;

                    var rawImageList = imgSrcSet.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(n => n.Trim());

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

            if (!targetDateMatchItems.Any()) return;

            var textInfo = new CultureInfo("en-US", false).TextInfo;

            var mjmlRenderer = new MjmlRenderer();

            var fromAddress = new MailAddress(emailSettings.FromEmailAddress, emailSettings.FromDisplayName);
            var toAddress = emailSettings.ToAddressList.Split(";", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => new MailAddress(x.Trim())).ToList();

            var subject = $"[{(string.IsNullOrWhiteSpace(siteTitle) ? emailSettings.SiteUrl : siteTitle)}] {emailSettings.YearsBack} Year{(emailSettings.YearsBack > 1 ? "s" : "")} Ago...";


            var message = new MailMessage()
            {
                Subject = subject,
                IsBodyHtml = true
            };

            message.From = fromAddress;
            toAddress.ForEach(x => message.To.Add(x));

            var groupedItems = targetDateMatchItems.GroupBy(x => x.itemType).ToList();

            var imageCarouselMjmlLines = new List<string>();

            var contentSections = new List<string>();

            long attachmentLimit = 24117248;
            long attachmentTotal = 0;

            foreach (var loopItems in groupedItems.Where(x => x.Key.Equals("image", StringComparison.OrdinalIgnoreCase)).SelectMany(x => x))
            {
                if (attachmentTotal > attachmentLimit) break;

                var imageBytes = await httpClient.GetByteArrayAsync(loopItems.imageUrl);

                attachmentTotal += imageBytes.Length;

                var contentId = Guid.NewGuid().ToString();

                var imageEmbed = new Attachment(new MemoryStream(imageBytes), new System.Net.Mime.ContentType("image/jpeg"));
                imageEmbed.ContentId = contentId;
                imageEmbed.ContentDisposition.Inline = true;
                imageEmbed.ContentDisposition.DispositionType = DispositionTypeNames.Inline;

                message.Attachments.Add(imageEmbed);

                imageCarouselMjmlLines.Add($"""
	<mj-carousel-image src="cid:{contentId}" />
""");
            }

            if (imageCarouselMjmlLines.Any())
            {
                contentSections.Add($"""
<mj-section>
      <mj-column>
        <mj-carousel>
          {string.Join(Environment.NewLine, imageCarouselMjmlLines)}
        </mj-carousel>
      </mj-column>
    </mj-section>
""");
            }

            foreach (var loopItems in groupedItems)
            {
                var itemTexts = new List<string>();
                foreach (var loopItem in loopItems)
                {
                    itemTexts.Add($"""
		<mj-text padding-left="14px" padding-top="2px"><a href="{loopItem.itemUrl}">{loopItem.title}</a></mj-text>
		""");
                }

                contentSections.Add($"""
<mj-section>
	<mj-column>
		<mj-text font-size="14px" padding-left="0px">{textInfo.ToTitleCase(loopItems.Key)}{(loopItems.Count() > 1 ? "s" : string.Empty)}:</mj-text>
			{string.Join(Environment.NewLine, itemTexts)}
	</mj-column>
</mj-section>
""");
            }

            string text = $"""
<mjml>
    <mj-head>
        <mj-title>{(string.IsNullOrWhiteSpace(siteTitle) ? emailSettings.SiteUrl : siteTitle)} Content from {targetDateString}</mj-title>
    </mj-head>
    <mj-body>
	    <mj-section>
      		<mj-column>
        		<mj-text align="center" font-size="30px"><a href="{emailSettings.SiteUrl}">{(string.IsNullOrWhiteSpace(siteTitle) ? emailSettings.SiteUrl : siteTitle)}</a></mj-text>
	      	</mj-column>
	    </mj-section>
	    <mj-section>
            <mj-column>
				<mj-text align="center" font-size="14px">
                {(string.IsNullOrWhiteSpace(siteTitle) ? "C" : $"{siteTitle} c")}ontent created {emailSettings.YearsBack} year{(emailSettings.YearsBack > 1 ? "s" : "")} ago ({targetDateString}).
				</mj-text>
            </mj-column>
        </mj-section>
        {string.Join(Environment.NewLine, contentSections)}
    </mj-body>
</mjml>
""";

            var html = mjmlRenderer.Render(text).Html;
            message.Body = html;

            var smtp = new SmtpClient
            {
                Host = emailSettings.SmtpHost,
                Port = emailSettings.SmtpPort,
                EnableSsl = emailSettings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(emailSettings.FromEmailAddress, emailSettings.FromEmailPassword),
                Timeout = 60000
            };

            smtp.Send(message);

            message.Dispose();

        }
    }
}
