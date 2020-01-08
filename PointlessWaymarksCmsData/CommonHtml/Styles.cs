namespace PointlessWaymarksCmsData.CommonHtml
{
    public static class Styles
    {
        public static string BodyStyle()
        {
            return @"
            body {
                font-family: Geneva, ‘Lucida Sans’, ‘Lucida Grande’, ‘Lucida Sans Unicode’, Verdana, sans-serif;
                font-size: 16px;
            }";
        }

        public static string SiteNameFooterStyles()
        {
            return @"
            .site-name-footer-container {
                margin: 2.5rem;
                text-align: center;
            }

            .site-name-footer-content {
                color: black;
            }";
        }

        public static string StandardRuleStyles()
        {
            return @"
            .standard-rule {
                border-radius: 4px;
                color: rgb(220, 220, 220);
                opacity: .2;
                max-width: 60%;
                margin-bottom: 1rem;
            }";
        }

        public static string TagsContainerStyles()
        {
            return @"
            .tags-container {
                display: flex;
                justify-content: center;
                flex-wrap: wrap;
                align-items: center;
                margin-top: 1rem;
            }

            .tag-detail {
                margin: 3px;
                box-shadow: 0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24);
                transition: all 0.3s cubic-bezier(.25,.8,.25,1);
                border-radius: 4px;
            }

            .tag-detail-content {
                margin: 6px;
            }";
        }
    }
}