using System;
using EPiServer.Find.Api.Querying;
using EPiServer.Find.Helpers;
using Newtonsoft.Json;

namespace FindApi.Common.RegexFilter
{
    [JsonConverter(typeof (NotRegexFilterConverter))]
    public class NotRegexFilter : Filter
    {
        public NotRegexFilter(string field, string regex)
        {
            field.ValidateNotNullArgument("field");
            regex.ValidateNotNullArgument("regex");
            Field = field;
            Regex = regex;
        }

        [JsonIgnore]
        public string Field { get; set; }

        [JsonProperty("value")]
        public FieldFilterValue Regex { get; set; }
    }

    public class NotRegexFilterConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value.IsNull())
            {
                writer.WriteNull();
            }
            else
            {
                var regexFilter = (NotRegexFilter) value;
                writer.WriteStartObject();
                    writer.WritePropertyName("not");
                    writer.WriteStartObject();
                        writer.WritePropertyName("regexp");
                        writer.WriteStartObject();
                            writer.WritePropertyName(regexFilter.Field);
                            serializer.Serialize(writer, regexFilter.Regex);
                        writer.WriteEndObject();
                    writer.WriteEndObject();
                writer.WriteEndObject();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (NotRegexFilter);
        }
    }
}

//Så här ser elasticsearch json ut för regex query.
//https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-regexp-query.html
//{
//    "regexp":{
//        "name.first": "s.*y"
//    }
//}