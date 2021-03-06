using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rhino.DivanDB.Json;
using Rhino.DivanDB.Linq;
using Xunit;
using System.Linq;

namespace Rhino.DivanDB.Tests.Linq
{
    public class PerformingQueries
    {
        const string query = @"
var pagesByTitle = 
    from doc in docs
    where doc.type == ""page""
    select new { Key = doc.title, Value = doc.content, Size = (int)doc.size };
";

        [Fact]
        public void Can_query_json()
        {
            var documents = GetDocumentsFromString(@"[
{'type':'page', title: 'hello', content: 'foobar', size: 2},
{'type':'page', title: 'there', content: 'foobar 2', size: 3},
{'type':'revision', size: 4}
]");
            var compiled = new LinqTransformer(query, "docs", typeof(JsonDynamicObject)).Compile();
            var compiledQuery = (AbstractViewGenerator)Activator.CreateInstance(compiled);
            var actual = compiledQuery.Execute(documents)
                .Cast<object>().ToArray();
            var expected = new[]
            {
                "{ Key = hello, Value = foobar, Size = 2 }",
                "{ Key = there, Value = foobar 2, Size = 3 }"
            };

            Assert.Equal(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i].ToString());
            }
        }

        private static IEnumerable<JsonDynamicObject> GetDocumentsFromString(string json)
        {
            var serializer = new JsonSerializer();
            var docs = (JArray)serializer.Deserialize(
                                   new JsonTextReader(
                                       new StringReader(
                                           json)));
            return docs.Select(x => new JsonDynamicObject(x));
        }
    }
}