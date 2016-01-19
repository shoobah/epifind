using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Mvc;
using EPiServer.Find;
using EPiServer.Find.ClientConventions;
using EPiServer.Find.UnifiedSearch;
using FindApi.Common;

namespace FindApi.Controllers
{
    [EnableCors("*", "*", "*")]
    [System.Web.Http.RoutePrefix("api")]
    public class HomeController : Controller
    {
        [System.Web.Http.Route("find")]
        [System.Web.Http.HttpPost]
        public FindResponse Find([FromBody] QueryObject query)
        {
            var client = Client.CreateFromConfig();
            ITypeSearch<ISearchContent> search = client.UnifiedSearchFor(query.Text, Language.Swedish).UsingSynonyms().StaticallyCacheFor(TimeSpan.FromMinutes(5)).Skip(query.Skip).Take(query.Take).ApplyBestBets();
            var result1 = search.GetResult(new HitSpecification { EncodeExcerpt = true, EncodeTitle = false, HighlightExcerpt = true, HighlightTitle = false }).Where(r => r.IsBestBet()).ToList();

            client.Conventions.ForInstancesOf<object>().FieldsOfType<string>().StripHtml();

            //Filter the search
            search.TermsFacetFor(x => x.SearchTypeName);
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                search = search.Filter(f => f.SearchTypeName.MatchCaseInsensitive(query.Type));
            }
            if (query.Extensions != null && query.Extensions.Count > 0)
            {
                search = search.Filter(f => f.SearchFileExtension.In(query.Extensions));
            }
            if (!string.IsNullOrWhiteSpace(query.Section))
            {
                search = search.Filter(f => f.SearchSection.MatchCaseInsensitive(query.Section));
            }
            if (!string.IsNullOrWhiteSpace(query.Subsection))
            {
                search = search.Filter(f => f.SearchSubsection.MatchCaseInsensitive(query.Subsection));
            }
            //Filtrerar så endast sidor med del av sökväg kommer med
            if (!string.IsNullOrWhiteSpace(query.UrlFilter))
            {
                search = search.Filter(f => f.SearchHitUrl.MatchRegex(".*" + query.UrlFilter + ".*"));
            }
            //Filtrerar bort sökvägar som ligger i en lista
            if (query.NotUrlFilter != null && query.NotUrlFilter.Count > 0)
            {
                search = query.NotUrlFilter.Aggregate(search, (current, qf) => current.Filter(f => f.SearchHitUrl.MatchNotRegex(".*" + qf + ".*")));
            }

            //Exempel från forumet, låt stå!
            //searchResults = client.Search<WithComplexCollection>()
            //    .Filter(x => x.EnumerableOfComplexType.MatchContainedCaseInsensitive(c => c.StringProperty, "Banana"))
            //    .GetResult();

            //Order
            if (!string.IsNullOrEmpty(query.Order))
            {
                if (query.Order == "date")
                {
                    search = search.OrderByDescending(e => e.SearchUpdateDate) as IQueriedSearch<ISearchContent>;
                }
            }
            //MinDate
            search = search.Filter(f => f.SearchPublishDate.After(query.MinDate));


            var result2 = search.GetResult(new HitSpecification { EncodeExcerpt = true, EncodeTitle = false, HighlightExcerpt = true, HighlightTitle = false });
            var searchResults = new List<UnifiedSearchHit>();
            searchResults.AddRange(result1);
            searchResults.AddRange(result2.ToList());
            var myHits = searchResults.Select(GetHit).ToList();
            var response = new FindResponse
            {
                Created = DateTime.Now,
                Name = query.Text,
                Total = result2.TotalMatching + result1.Count,
                Hits = myHits,
                Taken = query.Take,
                Skipped = query.Skip
            };
            return response;
        }

        private static MyHit GetHit(UnifiedSearchHit hit)
        {
            var sHit = new MyHit
            {
                Id = GetId(hit.OriginalObjectGetter),
                UpdateDate = hit.UpdateDate,
                Title = FixHtml(hit.Title),
                Excerpt = FixHtml(hit.Excerpt),
                Section = hit.Section,
                Subsection = hit.Subsection,
                MetaData = hit.MetaData,
                Url = hit.Url,
                Authors = hit.Authors,
                FileExtension = hit.FileExtension,
                Filename = hit.Filename,
                GeoLocation = hit.GeoLocation,
                HitTypeName = hit.HitTypeName,
                ImageUri = hit.ImageUri,
                OriginalObjectGetter = hit.OriginalObjectGetter,
                OriginalObjectType = hit.OriginalObjectType,
                PublishDate = hit.PublishDate,
                TypeName = hit.TypeName,
                Breadcrumb = GetBreadcrumb(hit.OriginalObjectGetter),
                IsBestBet = hit.IsBestBet()
            };
            return sHit;
        }

        private static string FixHtml(string excerpt)
        {
            if (excerpt == null) return "";
            excerpt = excerpt.Replace("<em>", "[em]");
            excerpt = excerpt.Replace("</em>", "[/em]");
            var txt = Regex.Replace(excerpt, "<.*?>", string.Empty);
            var newText = txt.Take(1000).ToString();
            newText = newText.Replace("[em]", "<em>");
            newText = newText.Replace("[/em]", "</em>");
            return newText;
        }

        private static string CleanExcerpt(string excerpt)
        {
            var e = Regex.Replace(excerpt, "&lt;[^&]*&gt;", "", RegexOptions.IgnoreCase);
            e = Regex.Replace(e, @"\\r\\n", " ", RegexOptions.IgnoreCase);
            e = Regex.Replace(e, "src=&quot;[^&]*&quot;", "", RegexOptions.IgnoreCase);
            e = Regex.Replace(e, "src=&quot;[^&]*", "", RegexOptions.IgnoreCase);
            e = Regex.Replace(e, "alt=&quot;[^&]*&quot;", "", RegexOptions.IgnoreCase);
            e = Regex.Replace(e, "&lt;img ", " ", RegexOptions.IgnoreCase);
            return e;
        }

        private static string GetId(Func<object> originalObjectGetter)
        {
            return "no id";
        }
        private static string GetBreadcrumb(Func<object> originalObjectGetter)
        {
            return "no breadcrumb";
        }
    }

    public class QueryObject
    {
        public string Text { get; set; }
        public int Take { get; set; }
        public int Skip { get; set; }
        public string Order { get; set; }
        public DateTime MinDate { get; set; }
        public string Type { get; set; }
        public List<string> Extensions { get; set; }
        public string Subsection { get; set; }
        public string Section { get; set; }
        public string UrlFilter { get; set; }
        public List<string> NotUrlFilter { get; set; }
    }

    public class FindResponse
    {
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public int Total { get; set; }
        public int Taken { get; set; }
        public int Skipped { get; set; }
        public List<MyHit> Hits { get; set; }
    }

    public class MyHit : UnifiedSearchHit
    {
        public string Id { get; set; }
        public string Breadcrumb { get; set; }
        public bool IsBestBet { get; set; }
        public IDictionary<string, IndexValue> Metadata { get; set; }
    }
}
