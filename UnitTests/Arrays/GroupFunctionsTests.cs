using NUnit.Framework;

namespace JUST.UnitTests.Arrays
{
    [TestFixture]
    public class GroupFunctionsTests
    {
        [Test]
        public void GroupBySingleElement()
        {
            const string transformer = "{ \"Result\": \"#grouparrayby($.Forest,type,all)\" }";
            const string input = "{ \"Forest\": [ { \"type\": \"Mammal\", \"qty\": 1, \"name\": \"Hippo\" }, { \"type\": \"Bird\", \"qty\": 2, \"name\": \"Sparrow\" }, { \"type\": \"Amphibian\", \"qty\": 300, \"name\": \"Lizard\" }, { \"type\": \"Bird\", \"qty\": 3, \"name\": \"Parrot\" }, { \"type\": \"Mammal\", \"qty\": 1, \"name\": \"Elephant\" }, { \"type\": \"Mammal\", \"qty\": 10, \"name\": \"Dog\" } ] }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"Result\":[{\"type\":\"Mammal\",\"all\":[{\"qty\":1,\"name\":\"Hippo\"},{\"qty\":1,\"name\":\"Elephant\"},{\"qty\":10,\"name\":\"Dog\"}]},{\"type\":\"Bird\",\"all\":[{\"qty\":2,\"name\":\"Sparrow\"},{\"qty\":3,\"name\":\"Parrot\"}]},{\"type\":\"Amphibian\",\"all\":[{\"qty\":300,\"name\":\"Lizard\"}]}]}", result);
        }

        [Test]
        public void GroupByMultipleElements()
        {
            const string transformer = "{ \"Result\": \"#grouparrayby($.Vehicle,type:company,all)\" }";
            const string input = "{ \"Vehicle\": [ { \"type\": \"air\", \"company\": \"Boeing\", \"name\": \"airplane\" }, { \"type\": \"air\", \"company\": \"Concorde\", \"name\": \"airplane\" }, { \"type\": \"air\", \"company\": \"Boeing\", \"name\": \"Chopper\" }, { \"type\": \"land\", \"company\": \"GM\", \"name\": \"car\" }, { \"type\": \"sea\", \"company\": \"Viking\", \"name\": \"ship\" }, { \"type\": \"land\", \"company\": \"GM\", \"name\": \"truck\" } ] }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"Result\":[{\"type\":\"air\",\"company\":\"Boeing\",\"all\":[{\"name\":\"airplane\"},{\"name\":\"Chopper\"}]},{\"type\":\"air\",\"company\":\"Concorde\",\"all\":[{\"name\":\"airplane\"}]},{\"type\":\"land\",\"company\":\"GM\",\"all\":[{\"name\":\"car\"},{\"name\":\"truck\"}]},{\"type\":\"sea\",\"company\":\"Viking\",\"all\":[{\"name\":\"ship\"}]}]}", result);
        }

        [Test]
        public void RedefineSplitChar()
        {
            const string transformer = "{ \"Result\": \"#grouparrayby($.Vehicle,type:a|company,all)\" }";
            const string input = "{ \"Vehicle\": [ { \"type:a\": \"air\", \"company\": \"Boeing\", \"name\": \"airplane\" }, { \"type:a\": \"air\", \"company\": \"Concorde\", \"name\": \"airplane\" }, { \"type:a\": \"air\", \"company\": \"Boeing\", \"name\": \"Chopper\" }, { \"type:a\": \"land\", \"company\": \"GM\", \"name\": \"car\" }, { \"type:a\": \"sea\", \"company\": \"Viking\", \"name\": \"ship\" }, { \"type:a\": \"land\", \"company\": \"GM\", \"name\": \"truck\" } ] }";

            var context = new JUSTContext() { SplitGroupChar = '|' };
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"Result\":[{\"type:a\":\"air\",\"company\":\"Boeing\",\"all\":[{\"name\":\"airplane\"},{\"name\":\"Chopper\"}]},{\"type:a\":\"air\",\"company\":\"Concorde\",\"all\":[{\"name\":\"airplane\"}]},{\"type:a\":\"land\",\"company\":\"GM\",\"all\":[{\"name\":\"car\"},{\"name\":\"truck\"}]},{\"type:a\":\"sea\",\"company\":\"Viking\",\"all\":[{\"name\":\"ship\"}]}]}", result);
        }

        [Test]
        public void RedefineSplitCharWithDates()
        {
            const string transformer = "{ \"Result\": \"#grouparrayby($,payrollUnit|fromDate,all)\" }";
            const string input = "[ { \"payrollUnit\": \"euro\", \"name\": \"John\", \"fromDate\": \"2022-01-01T00:00:00\" }, { \"payrollUnit\": \"euro\", \"name\": \"Smith\", \"fromDate\": \"2022-01-01T00:00:00\" }, { \"payrollUnit\": \"usd\", \"name\": \"Lucy\", \"fromDate\": \"2022-01-01T00:00:00\" }, { \"payrollUnit\": \"yen\", \"name\": \"Anne\", \"fromDate\": \"2022-02-01T00:00:00\" }, { \"payrollUnit\": \"usd\", \"name\": \"James\", \"fromDate\": \"2022-12-01T00:00:00\" }, { \"payrollUnit\": \"euro\", \"fromDate\": \"2022-06-01T00:00:00\", \"name\": \"Thelma\" } ]";

            var context = new JUSTContext() { SplitGroupChar = '|' };
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"Result\":[{\"payrollUnit\":\"euro\",\"fromDate\":\"2022-01-01T00:00:00\",\"all\":[{\"name\":\"John\"},{\"name\":\"Smith\"}]},{\"payrollUnit\":\"usd\",\"fromDate\":\"2022-01-01T00:00:00\",\"all\":[{\"name\":\"Lucy\"}]},{\"payrollUnit\":\"yen\",\"fromDate\":\"2022-02-01T00:00:00\",\"all\":[{\"name\":\"Anne\"}]},{\"payrollUnit\":\"usd\",\"fromDate\":\"2022-12-01T00:00:00\",\"all\":[{\"name\":\"James\"}]},{\"payrollUnit\":\"euro\",\"fromDate\":\"2022-06-01T00:00:00\",\"all\":[{\"name\":\"Thelma\"}]}]}", result);
        }
    }
}
