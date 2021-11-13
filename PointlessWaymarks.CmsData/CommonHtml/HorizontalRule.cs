using HtmlTags;

namespace PointlessWaymarks.CmsData.CommonHtml
{
    public static class HorizontalRule
    {
        public static HtmlTag StandardRule()
        {
            return new HtmlTag("hr").AddClass("standard-rule");
        }

        public static HtmlTag StandardRuleIfAnyNotEmptyTags(params HtmlTag[] values)
        {
            if (!values.Any()) return HtmlTag.Empty();
            return values.All(x => string.IsNullOrWhiteSpace(x.ToString()))
                ? HtmlTag.Empty()
                : new HtmlTag("hr").AddClass("standard-rule");
        }

        public static HtmlTag StandardRuleIfNotEmptyTag(HtmlTag tag)
        {
            return string.IsNullOrWhiteSpace(tag.ToString())
                ? HtmlTag.Empty()
                : new HtmlTag("hr").AddClass("standard-rule");
        }
    }
}