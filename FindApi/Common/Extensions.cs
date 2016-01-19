using EPiServer.Find;
using FindApi.Common.RegexFilter;

namespace FindApi.Common
{
    public static class Extensions
    {
        public static DelegateFilterBuilder MatchRegex(this string value, string regex)
        {
            return new DelegateFilterBuilder(field => new RegexFilter.RegexFilter(field, regex));
        }
        public static DelegateFilterBuilder MatchNotRegex(this string value, string regex)
        {
            return new DelegateFilterBuilder(field => new NotRegexFilter(field, regex));
        }
    }
}