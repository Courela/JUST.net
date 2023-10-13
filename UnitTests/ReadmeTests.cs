using JUST.net.Selectables;
using NUnit.Framework;
using System.Globalization;

namespace JUST.UnitTests
{
    [TestFixture, Category("ReadMe")]
    public class ReadmeTests
    {
        [Test]
        public void ValueOf()
        {
            const string transformer = "{\"root\": {\"menu1\": \"#valueof($.menu.popup.menuitem[?(@.value=='New')].onclick)\", \"menu2\": \"#valueof($.menu.popup.menuitem[?(@.value=='Open')].onclick)\"}}";

            var result = new JsonTransformer().Transform(transformer, ExampleInputs.Menu);

            Assert.AreEqual("{\"root\":{\"menu1\":{\"action\":\"CreateNewDoc()\"},\"menu2\":\"OpenDoc()\"}}", result);
        }

        [Test]
        public void IfCondition()
        {
            const string input = "{ \"menu\": { \"id\" : \"github\", \"repository\" : \"JUST\" } }";
            const string transformer = "{ \"ifconditiontesttrue\": \"#ifcondition(#valueof($.menu.id),github,#valueof($.menu.repository),fail)\", \"ifconditiontestfalse\": \"#ifcondition(#valueof($.menu.id),xml,#valueof($.menu.repository),fail)\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"ifconditiontesttrue\":\"JUST\",\"ifconditiontestfalse\":\"fail\"}", result);
        }

        [Test]
        public void StringMathFunctions()
        {
            const string input = "{ \"stringref\": \"thisisandveryunuasualandlongstring\", \"numbers\": [ 1, 2, 3, 4, 5 ] }";
            const string transformer = "{ \"stringresult\": { \"lastindexofand\": \"#lastindexof(#valueof($.stringref),and)\", \"firstindexofand\": \"#firstindexof(#valueof($.stringref),and)\", \"substring\": \"#substring(#valueof($.stringref),9,11)\", \"concat\": \"#concat(#valueof($.menu.id.file),#valueof($.menu.value.Window))\", \"length_string\": \"#length(#valueof($.stringref))\", \"length_array\": \"#length(#valueof($.numbers))\", \"length_path\": \"#length($.numbers)\" }, \"mathresult\": { \"add\": \"#add(#valueof($.numbers[0]),3)\", \"subtract\": \"#subtract(#valueof($.numbers[4]),#valueof($.numbers[0]))\", \"multiply\": \"#multiply(2,#valueof($.numbers[2]))\", \"divide\": \"#divide(9,3)\", \"round\": \"#round(10.005,2)\" } }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"stringresult\":{\"lastindexofand\":21,\"firstindexofand\":6,\"substring\":\"veryunuasua\",\"concat\":null,\"length_string\":34,\"length_array\":5,\"length_path\":5},\"mathresult\":{\"add\":4,\"subtract\":4,\"multiply\":6,\"divide\":3,\"round\":10.01}}", result);
        }

        [Test]
        public void Operators()
        {
            const string input = "{ \"d\": [ \"one\", \"two\", \"three\" ], \"numbers\": [ 1, 2, 3, 4, 5 ] }";
            const string transformer = "{ \"mathresult\": { \"third_element_equals_3\": \"#ifcondition(#mathequals(#valueof($.numbers[2]),3),true,yes,no)\", \"third_element_greaterthan_2\": \"#ifcondition(#mathgreaterthan(#valueof($.numbers[2]),2),true,yes,no)\", \"third_element_lessthan_4\": \"#ifcondition(#mathlessthan(#valueof($.numbers[2]),4),true,yes,no)\", \"third_element_greaterthanorequals_4\": \"#ifcondition(#mathgreaterthanorequalto(#valueof($.numbers[2]),4),true,yes,no)\", \"third_element_lessthanoreuals_2\": \"#ifcondition(#mathlessthanorequalto(#valueof($.numbers[2]),2),true,yes,no)\", \"one_stringequals\": \"#ifcondition(#stringequals(#valueof($.d[0]),one),true,yes,no)\", \"one_stringcontains\": \"#ifcondition(#stringcontains(#valueof($.d[0]),n),true,yes,no)\" } }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"mathresult\":{\"third_element_equals_3\":\"yes\",\"third_element_greaterthan_2\":\"yes\",\"third_element_lessthan_4\":\"yes\",\"third_element_greaterthanorequals_4\":\"no\",\"third_element_lessthanoreuals_2\":\"no\",\"one_stringequals\":\"yes\",\"one_stringcontains\":\"yes\"}}", result);
        }

        [Test]
        public void AggregateFunctions()
        {
            const string input = "{ \"d\": [ \"one\", \"two\", \"three\" ], \"numbers\": [ 1, 2, 3, 4, 5 ] }";
            const string transformer = "{ \"conacted\": \"#concatall(#valueof($.d))\", \"sum\": \"#sum($.numbers)\", \"avg\": \"#average(#valueof($.numbers))\", \"min\": \"#min($.numbers)\", \"max\": \"#max(#valueof($.numbers))\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"conacted\":\"onetwothree\",\"sum\":15,\"avg\":3,\"min\":1,\"max\":5}", result);
        }

        [Test]
        public void AggregateFunctionsMultidimensionalArrays()
        {
            const string input = "{ \"x\": [ { \"v\": { \"a\": \"a1,a2,a3\", \"b\": \"1\", \"c\": \"10\" } }, { \"v\": { \"a\": \"b1,b2\", \"b\": \"2\", \"c\": \"20\" } }, { \"v\": { \"a\": \"c1,c2,c3\", \"b\": \"3\", \"c\": \"30\" } } ] }";
            const string transformer = "{ \"arrayconacted\": \"#concatallatpath(#valueof($.x),$.v.a)\", \"arraysum\": \"#sumatpath(#valueof($.x),$.v.c)\", \"arrayavg\": \"#averageatpath(#valueof($.x),$.v.c)\", \"arraymin\": \"#minatpath(#valueof($.x),$.v.b)\", \"arraymax\": \"#maxatpath(#valueof($.x),$.v.b)\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"arrayconacted\":\"a1,a2,a3b1,b2c1,c2,c3\",\"arraysum\":60,\"arrayavg\":20,\"arraymin\":1,\"arraymax\":3}", result);
        }

        [Test]
        public void TypeConvertions()
        {
            const string input = "{ \"booleans\": { \"affirmative_string\": \"true\", \"negative_string\": \"false\", \"affirmative_int\": 123, \"negative_int\": 0, }, \"strings\": { \"integer\": 123, \"decimal\": 12.34, \"affirmative_boolean\": true, \"negative_boolean\": false }, \"integers\": { \"string\": \"123\", \"decimal\": 1.23, \"affirmative_boolean\": true, \"negative_boolean\": false }, \"decimals\": { \"integer\": 123, \"string\": \"1.23\" }}";
            const string transformer = "{ \"booleans\": { \"affirmative_string\": \"#toboolean(#valueof($.booleans.affirmative_string))\", \"negative_string\":\"#toboolean(#valueof($.booleans.negative_string))\", \"affirmative_int\":\"#toboolean(#valueof($.booleans.affirmative_int))\", \"negative_int\": \"#toboolean(#valueof($.booleans.negative_int))\", }, \"strings\": { \"integer\": \"#tostring(#valueof($.strings.integer))\", \"decimal\":\"#tostring(#valueof($.strings.decimal))\", \"affirmative_boolean\": \"#tostring(#valueof($.strings.affirmative_boolean))\", \"negative_boolean\": \"#tostring(#valueof($.strings.negative_boolean))\" }, \"integers\": { \"string\":\"#tointeger(#valueof($.integers.string))\", \"decimal\": \"#tointeger(#valueof($.integers.decimal))\", \"affirmative_boolean\":\"#tointeger(#valueof($.integers.affirmative_boolean))\", \"negative_boolean\":\"#tointeger(#valueof($.integers.negative_boolean))\" }, \"decimals\": { \"integer\":\"#todecimal(#valueof($.decimals.integer))\", \"string\": \"#todecimal(#valueof($.decimals.string))\" }}";

            var result = new JsonTransformer().Transform(transformer, input);

            var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            Assert.AreEqual($"{{\"booleans\":{{\"affirmative_string\":true,\"negative_string\":false,\"affirmative_int\":true,\"negative_int\":false}},\"strings\":{{\"integer\":\"123\",\"decimal\":\"12{decimalSeparator}34\",\"affirmative_boolean\":\"True\",\"negative_boolean\":\"False\"}},\"integers\":{{\"string\":123,\"decimal\":1,\"affirmative_boolean\":1,\"negative_boolean\":0}},\"decimals\":{{\"integer\":123.0,\"string\":1.23}}}}", result);
        }

        [Test]
        public void BulkFunctions()
        {
            const string transformer = "{ \"#\": [ \"#copy($)\", \"#delete($.tree.branch.bird)\", \"#replace($.tree.branch.extra,#valueof($.tree.ladder))\" ], \"othervalue\" : \"othervalue\" }";

            var result = new JsonTransformer().Transform(transformer, ExampleInputs.Tree);

            Assert.AreEqual("{\"othervalue\":\"othervalue\",\"tree\":{\"branch\":{\"leaf\":\"green\",\"flower\":\"red\",\"extra\":{\"wood\":\"treehouse\"}},\"ladder\":{\"wood\":\"treehouse\"}}}", result);
        }

        [Test]
        public void ArrayLooping()
        {
            const string input = "{ \"tree\": { \"branch\": { \"leaf\": \"green\", \"flower\": \"red\", \"bird\": \"crow\", \"extra\": { \"twig\": \"birdnest\" } }, \"ladder\": { \"wood\": \"treehouse\" } }, \"numbers\": [ 1, 2, 3, 4 ], \"arrayobjects\": [ {\"country\": {\"name\": \"norway\",\"language\": \"norsk\"}}, { \"country\": { \"name\": \"UK\", \"language\": \"english\" } }, { \"country\": { \"name\": \"Sweden\", \"language\": \"swedish\" } }], \"animals\": { \"cat\": { \"legs\": 4, \"sound\": \"meow\" }, \"dog\": { \"legs\": 4, \"sound\": \"woof\" }, \"human\": { \"number_of_legs\": 2, \"sound\": \"@!#$?\" } }, \"spell_numbers\": { \"3\": \"three\", \"2\": \"two\", \"1\": \"one\" } }";
            const string transformer = "{ \"iteration\": { \"#loop($.numbers)\": { \"CurrentValue\": \"#currentvalue()\", \"CurrentIndex\": \"#currentindex()\", \"IsLast\": \"#ifcondition(#currentindex(),#lastindex(),yes,no)\", \"LastValue\": \"#lastvalue()\" } }, \"iteration2\": { \"#loop($.arrayobjects)\": { \"CurrentValue\": \"#currentvalueatpath($.country.name)\", \"CurrentIndex\": \"#currentindex()\", \"IsLast\": \"#ifcondition(#currentindex(),#lastindex(),yes,no)\", \"LastValue\": \"#lastvalueatpath($.country.language)\" } }, \"sounds\": { \"#loop($.animals)\": { \"#eval(#currentproperty())\": \"#currentvalueatpath($..sound)\" } }, \"number_index\": { \"#loop($.spell_numbers)\": { \"#eval(#currentindex())\": \"#currentvalueatpath(#concat($.,#currentproperty()))\" } }, \"othervalue\": \"othervalue\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"iteration\":[{\"CurrentValue\":1,\"CurrentIndex\":0,\"IsLast\":\"no\",\"LastValue\":4},{\"CurrentValue\":2,\"CurrentIndex\":1,\"IsLast\":\"no\",\"LastValue\":4},{\"CurrentValue\":3,\"CurrentIndex\":2,\"IsLast\":\"no\",\"LastValue\":4},{\"CurrentValue\":4,\"CurrentIndex\":3,\"IsLast\":\"yes\",\"LastValue\":4}],\"iteration2\":[{\"CurrentValue\":\"norway\",\"CurrentIndex\":0,\"IsLast\":\"no\",\"LastValue\":\"swedish\"},{\"CurrentValue\":\"UK\",\"CurrentIndex\":1,\"IsLast\":\"no\",\"LastValue\":\"swedish\"},{\"CurrentValue\":\"Sweden\",\"CurrentIndex\":2,\"IsLast\":\"yes\",\"LastValue\":\"swedish\"}],\"sounds\":{\"cat\":\"meow\",\"dog\":\"woof\",\"human\":\"@!#$?\"},\"number_index\":{\"0\":\"three\",\"1\":\"two\",\"2\":\"one\"},\"othervalue\":\"othervalue\"}", result);
        }

        [Test]
        public void NestedArrayLooping()
        {
            const string input = "{ \"NestedLoop\": { \"Organization\": { \"Employee\": [ { \"Name\": \"E2\", \"Surname\": \"S2\", \"Details\": [ { \"Countries\": [ { \"Name\": \"Iceland\", \"Language\": \"Icelandic\" } ], \"Age\": 30 } ] }, { \"Name\": \"E1\", \"Surname\": \"S1\", \"Details\": [ { \"Countries\": [{ \"Name\": \"Denmark\", \"Language\": \"Danish\" }, { \"Name\": \"Greenland\", \"Language\": \"Danish\" } ], \"Age\": 31 } ] } ] } } }";
            const string transformer = "{ \"hello\": { \"#loop($.NestedLoop.Organization.Employee, employees)\": { \"CurrentName\": \"#currentvalueatpath($.Name, employees)\", \"Details\": { \"#loop($.Details)\": { \"Surname\": \"#currentvalueatpath($.Surname, employees)\", \"Age\": \"#currentvalueatpath($.Age)\", \"Country\": { \"#loop($.Countries[0], countries)\": \"#currentvalueatpath($.Name, countries)\" } } } } }";

            var result = new JsonTransformer(new JUSTContext { EvaluationMode = EvaluationMode.Strict }).Transform(transformer, input);

            Assert.AreEqual("{\"hello\":[{\"CurrentName\":\"E2\",\"Details\":[{\"Surname\":\"S2\",\"Age\":30,\"Country\":[\"Iceland\"]}]},{\"CurrentName\":\"E1\",\"Details\":[{\"Surname\":\"S1\",\"Age\":31,\"Country\":[\"Denmark\"]}]}]}", result);
        }

        [Test]
        public void ArrayGrouping()
        {
            const string input = "{ \"Forest\": [ { \"type\": \"Mammal\", \"qty\": 1, \"name\": \"Hippo\" }, { \"type\": \"Bird\", \"qty\": 2, \"name\": \"Sparrow\" }, { \"type\": \"Amphibian\", \"qty\": 300, \"name\": \"Lizard\" }, { \"type\": \"Bird\", \"qty\": 3, \"name\": \"Parrot\" }, { \"type\": \"Mammal\", \"qty\": 1, \"name\": \"Elephant\" }, { \"type\": \"Mammal\", \"qty\": 10, \"name\": \"Dog\" } ] }";
            const string transformer = "{ \"Result\": \"#grouparrayby($.Forest,type,all)\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"Result\":[{\"type\":\"Mammal\",\"all\":[{\"qty\":1,\"name\":\"Hippo\"},{\"qty\":1,\"name\":\"Elephant\"},{\"qty\":10,\"name\":\"Dog\"}]},{\"type\":\"Bird\",\"all\":[{\"qty\":2,\"name\":\"Sparrow\"},{\"qty\":3,\"name\":\"Parrot\"}]},{\"type\":\"Amphibian\",\"all\":[{\"qty\":300,\"name\":\"Lizard\"}]}]}", result);
        }

        [Test]
        public void ComplexNestedFunctions()
        {
            const string input = "{ \"Name\": \"Kari\", \"Surname\": \"Nordmann\", \"MiddleName\": \"Inger\", \"ContactInformation\": \"Karl johans gate:Oslo:88880000\" , \"PersonalInformation\": \"45:Married:Norwegian\"}";
            const string transformer = "{ \"FullName\": \"#concat(#concat(#concat(#valueof($.Name), ),#concat(#valueof($.MiddleName), )),#valueof($.Surname))\",	\"Contact Information\": { \"Street Name\": \"#substring(#valueof($.ContactInformation),0,#firstindexof(#valueof($.ContactInformation),:))\", \"City\": \"#substring(#valueof($.ContactInformation),#add(#firstindexof(#valueof($.ContactInformation),:),1),#subtract(#subtract(#lastindexof(#valueof($.ContactInformation),:),#firstindexof(#valueof($.ContactInformation),:)),1))\", \"PhoneNumber\": \"#substring(#valueof($.ContactInformation),#add(#lastindexof(#valueof($.ContactInformation),:),1),#subtract(#subtract(#length(#valueof($.ContactInformation)),1),#lastindexof(#valueof($.ContactInformation),:)))\" }, \"Personal Information\": { \"Age\": \"#substring(#valueof($.PersonalInformation),0,#firstindexof(#valueof($.PersonalInformation),:))\", \"Civil Status\": \"#substring(#valueof($.PersonalInformation),#add(#firstindexof(#valueof($.PersonalInformation),:),1),#subtract(#subtract(#lastindexof(#valueof($.PersonalInformation),:),#firstindexof(#valueof($.PersonalInformation),:)),1))\", \"Ethnicity\": \"#substring(#valueof($.PersonalInformation),#add(#lastindexof(#valueof($.PersonalInformation),:),1),#subtract(#subtract(#length(#valueof($.PersonalInformation)),1),#lastindexof(#valueof($.PersonalInformation),:)))\" }}";
            //const string transformer = "{ \"FullName\": \"#concat(#concat(#concat(#valueof($.Name), ),#concat(#valueof($.MiddleName), )),#valueof($.Surname))\", \"Contact Information\": { \"Street Name\": \"#substring(#valueof($.ContactInformation),0,#firstindexof(#valueof($.ContactInformation),:))\", \"City\": \"#substring(#valueof($.ContactInformation),#add(#firstindexof(#valueof($.ContactInformation),:),1),#subtract(#subtract(#lastindexof(#valueof($.ContactInformation),:),#firstindexof(#valueof($.ContactInformation),:)),1))\", \"PhoneNumber\": \"#substring(#valueof($.ContactInformation),#add(#lastindexof(#valueof($.ContactInformation),:),1),#subtract(#subtract(#length(#valueof($.ContactInformation)),1),#lastindexof(#valueof($.ContactInformation),:)))\" }, \"Personal Information\": { \"Age\": \"#substring(#valueof($.PersonalInformation),0,#firstindexof(#valueof($.PersonalInformation),:))\", \"Civil Status\": \"#substring(#valueof($.PersonalInformation),#add(#firstindexof(#valueof($.PersonalInformation),:),1),#subtract(#subtract(#lastindexof(#valueof($.PersonalInformation),:),#firstindexof(#valueof($.PersonalInformation),:)),1))\", \"Ethnicity\": \"#substring(#valueof($.PersonalInformation),#add(#lastindexof(#valueof($.PersonalInformation),:),1),#subtract(#subtract(#length(#valueof($.PersonalInformation)),1),#lastindexof(#valueof($.PersonalInformation),:)))\" }}";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"FullName\":\"Kari Inger Nordmann\",\"Contact Information\":{\"Street Name\":\"Karl johans gate\",\"City\":\"Oslo\",\"PhoneNumber\":\"88880000\"},\"Personal Information\":{\"Age\":\"45\",\"Civil Status\":\"Married\",\"Ethnicity\":\"Norwegian\"}}", result);
        }

        [Test]
        public void MultipleArgumentConstantFunctions()
        {
            const string input = "{ \"Name\": \"Kari\", \"Surname\": \"Nordmann\", \"MiddleName\": \"Inger\", \"ContactInformation\": \"Karl johans gate:Oslo:88880000\" , \"PersonalInformation\": \"45:Married:Norwegian\",\"AgeOfMother\": 67,\"AgeOfFather\": 70, \"EmptyString\": \"\", \"EmptyArray\": [] }";
            const string transformer = "{ \"FullName\": \"#xconcat(#valueof($.Name),#constant_comma(),#valueof($.MiddleName),#constant_comma(),#valueof($.Surname))\", \"AgeOfParents\": \"#xadd(#valueof($.AgeOfMother),#valueof($.AgeOfFather))\", \"TestSomeEmptyString\": \"#ifcondition(#valueof($.EmptyString),#stringempty(),String is empty,String is not empty)\", \"TestSomeOtherString\": \"#ifcondition(#valueof($.Name),#stringempty(),String is empty,String is not empty)\", \"TestEmptyArray\": \"#ifcondition(#valueof($.EmptyArray),#arrayempty(),Array is empty,Array is not empty)\", \"ReturnEmptyArray\": \"#ifcondition(#valueof($.Name),Kari,#arrayempty(),Name is not Kari)\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"FullName\":\"Kari,Inger,Nordmann\",\"AgeOfParents\":137,\"TestSomeEmptyString\":\"String is empty\",\"TestSomeOtherString\":\"String is not empty\",\"TestEmptyArray\":\"Array is empty\",\"ReturnEmptyArray\":[]}", result);
        }

        [Test]
        public void CheckForExistance()
        {
            const string transformer = "{ \"BuyDateString\": \"#ifcondition(#exists($.BuyDate),true,#concat(Buy Date : ,#valueof($.BuyDate)),NotExists)\", \"BuyDateString2\": \"#ifcondition(#existsandnotempty($.BuyDate),true,#concat(Buy Date : ,#valueof($.BuyDate)),EmptyOrNotExists)\", \"ExpireDateString\": \"#ifcondition(#exists($.ExpireDate),true,#concat(Expire Date : ,#valueof($.ExpireDate)),NotExists)\", \"ExpireDateString2\": \"#ifcondition(#existsandnotempty($.ExpireDate),true,#concat(Expire Date : ,#valueof($.ExpireDate)),EmptyOrNotExists)\", \"SellDateString\": \"#ifcondition(#exists($.SellDate),true,#concat(Sell Date : ,#valueof($.SellDate)),NotExists)\", \"SellDateString2\": \"#ifcondition(#existsandnotempty($.SellDate),true,#concat(Sell Date : ,#valueof($.SellDate)),EmptyOrNotExists)\" }";
            const string input = "{ \"BuyDate\": \"2017-04-10T11:36:39+03:00\", \"ExpireDate\": \"\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"BuyDateString\":\"Buy Date : 2017-04-10T11:36:39+03:00\",\"BuyDateString2\":\"Buy Date : 2017-04-10T11:36:39+03:00\",\"ExpireDateString\":\"Expire Date : \",\"ExpireDateString2\":\"EmptyOrNotExists\",\"SellDateString\":\"NotExists\",\"SellDateString2\":\"EmptyOrNotExists\"}", result);
        }

        [Test]
        public void ConditionalTransformation()
        {
            const string input = "{ \"Tree\": { \"Branch\": \"leaf\", \"Flower\": \"Rose\" } }";
            string transformer = "{ \"Result\": { \"Header\": \"JsonTransform\", \"#ifgroup(#exists($.Tree.Branch))\": { \"State\": { \"Value1\": \"#valueof($.Tree.Branch)\", \"Value2\": \"#valueof($.Tree.Flower)\" } }, \"Shrubs\": [ \"#ifgroup(#ifcondition(#valueof($.Tree.Flower),Rose,True,False),#valueof($.Tree.Flower))\" ] } }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"Result\":{\"Header\":\"JsonTransform\",\"Shrubs\":[\"Rose\"],\"State\":{\"Value1\":\"leaf\",\"Value2\":\"Rose\"}}}", result);

            transformer = "{ \"Result\": { \"Header\": \"JsonTransform\", \"#ifgroup(#exists($.Tree.Root))\": { \"State\": { \"Value1\": \"#valueof($.Tree.Branch)\", \"Value2\": \"#valueof($.Tree.Flower)\" } }, \"Shrubs\": [ \"#ifgroup(#ifcondition(#valueof($.Tree.Flower),Olive,True,False),#valueof($.Tree.Flower))\" ] } }";

            result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"Result\":{\"Header\":\"JsonTransform\",\"Shrubs\":[]}}", result);
        }

        [Test]
        public void DynamicProperties()
        {
            const string input = "{ \"Tree\": { \"Branch\": \"leaf\", \"Flower\": \"Rose\" } }";
            const string transformer = "{ \"Result\": { \"#eval(#valueof($.Tree.Flower))\": \"x\" } }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"Result\":{\"Rose\":\"x\"}}", result);
        }

        [Test]
        public void ApplyOver()
        {
            var input = "{\"d\": [ \"one\", \"two\", \"three\" ], \"values\": [ \"z\", \"c\", \"n\" ]}";
            var transformer = "{ \"simple_function\": \"#applyover({ 'condition': { '#loop($.values)': { 'test': '#ifcondition(#stringcontains(#valueof($.d[0]),#currentvalue()),True,yes,no)' } } }, '#exists($.condition[?(@.test=='yes')])')\", " +
                                "\"object\": \"#applyover(#xconcat({ 'temp': { '#loop($.values)': { 'index': '#currentindex()', #constant_comma(), 'value': '#currentvalue()' } } }), { 'first_element': '#valueof($.temp[0])' })\", " +
                                "\"array\": \"#applyover(#xconcat({ '#loop($.d)': { 'index': '#currentindex()', #constant_comma(), 'value': '#currentvalue()' } }), { 'last_element': '#valueof($[2])' })\" }";
            var context = new JUSTContext
            {
                EvaluationMode = EvaluationMode.Strict
            };
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"simple_function\":true,\"object\":{\"first_element\":{\"index\":0,\"value\":\"z\"}},\"array\":{\"last_element\":{\"index\":2,\"value\":\"three\"}}}", result);
        }

        [Test]
        public void JmesPath()
        {
            var input = "{\"locations\": [{\"name\": \"Seattle\", \"state\": \"WA\"},{\"name\": \"New York\", \"state\": \"NY\"},{\"name\": \"Bellevue\", \"state\": \"WA\"},{\"name\": \"Olympia\", \"state\": \"WA\"}]}";
            var transformer = "{ \"result\": \"#valueof(locations[?state == 'WA'].name | sort(@) | {WashingtonCities: join(', ', @)})\" }";
            var context = new JUSTContext
            {
                EvaluationMode = EvaluationMode.Strict
            };
            var result = new JsonTransformer<JmesPathSelectable>(context).Transform(transformer, input);

            Assert.AreEqual("{\"result\":{\"WashingtonCities\":\"Bellevue, Olympia, Seattle\"}}", result);
        }

        [Test]
        public void Escape()
        {
            var input = "{ \"arg\": \"some_value\" }";
            var transformer = "{ \"sharp\": \"/#not_a_function\", \"parentheses\": \"#xconcat(func/(',#valueof($.arg),'/))\", \"comma\": \"#xconcat(func/(',#valueof($.arg),'/,'other_value'/))\" }";
            var context = new JUSTContext
            {
                EvaluationMode = EvaluationMode.Strict
            };
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"sharp\":\"#not_a_function\",\"parentheses\":\"func('some_value')\",\"comma\":\"func('some_value','other_value')\"}", result);
        }

        [Test]
        public void ArrayConcatenation()
        {
            var input = "{ \"drugs\": [{ \"code\": \"001\", \"display\": \"Drug1\" },{ \"code\": \"002\", \"display\": \"Drug2\" }],\"pa\": [{ \"code\": \"pa1\", \"display\": \"PA1\" },{ \"code\": \"pa2\", \"display\": \"PA2\" }], \"sa\": [{ \"code\": \"sa1\", \"display\": \"SA1\" },{ \"code\": \"sa2\", \"display\": \"SA2\" }]}";
            var transformer = "{ \"concat\": \"#concat(#valueof($.drugs), #valueof($.pa))\", \"multipleConcat\": \"#concat(#concat(#valueof($.drugs), #valueof($.pa)), #valueof($.sa))\", \"xconcat\": \"#xconcat(#valueof($.drugs), #valueof($.pa), #valueof($.sa))\" }";
            var context = new JUSTContext
            {
                EvaluationMode = EvaluationMode.Strict
            };
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"concat\":[{\"code\":\"001\",\"display\":\"Drug1\"},{\"code\":\"002\",\"display\":\"Drug2\"},{\"code\":\"pa1\",\"display\":\"PA1\"},{\"code\":\"pa2\",\"display\":\"PA2\"}],\"multipleConcat\":[{\"code\":\"001\",\"display\":\"Drug1\"},{\"code\":\"002\",\"display\":\"Drug2\"},{\"code\":\"pa1\",\"display\":\"PA1\"},{\"code\":\"pa2\",\"display\":\"PA2\"},{\"code\":\"sa1\",\"display\":\"SA1\"},{\"code\":\"sa2\",\"display\":\"SA2\"}],\"xconcat\":[{\"code\":\"001\",\"display\":\"Drug1\"},{\"code\":\"002\",\"display\":\"Drug2\"},{\"code\":\"pa1\",\"display\":\"PA1\"},{\"code\":\"pa2\",\"display\":\"PA2\"},{\"code\":\"sa1\",\"display\":\"SA1\"},{\"code\":\"sa2\",\"display\":\"SA2\"}]}", result);
        }

        [Test]
        public void TypeCheck()
        {
            const string input = "{ \"integer\": 0, \"decimal\": 1.23, \"boolean\": true, \"string\": \"abc\", \"array\": [ \"abc\", \"xyz\" ] }";
            const string transformer = "{ \"isNumberTrue1\": \"#isnumber(#valueof($.integer))\", \"isNumberTrue2\": \"#isnumber(#valueof($.decimal))\", \"isNumberFalse\": \"#isnumber(#valueof($.boolean))\", \"isBooleanTrue\": \"#isboolean(#valueof($.boolean))\", \"isBooleanFalse\": \"#isboolean(#valueof($.integer))\", \"isStringTrue\": \"#isstring(#valueof($.string))\", \"isStringFalse\": \"#isstring(#valueof($.array))\", \"isArrayTrue\": \"#isarray(#valueof($.array))\", \"isArrayFalse\": \"#isarray(#valueof($.decimal))\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"isNumberTrue1\":true,\"isNumberTrue2\":true,\"isNumberFalse\":false,\"isBooleanTrue\":true,\"isBooleanFalse\":false,\"isStringTrue\":true,\"isStringFalse\":false,\"isArrayTrue\":true,\"isArrayFalse\":false}", result);
        }

        [Test]
        public void UsePreviousGeneratedProperty()
        {
            const string input = "{ \"number\": 123, \"boolean\": true }";
            const string transformer = "{ \"first\": \"#valueof($.number)\", \"second\": \"#valueof($.first)\", \"third\": \"#add(2,#valueof($.second))\", \"fourth\": \"#valueof($.boolean)\", \"fifth\": \"#valueof($.fourth)\", \"sixth\": \"#ifcondition(#valueof($.fifth),true,value is true,value is false)\" }";

            var result = new JsonTransformer(new JUSTContext { EvaluationMode = EvaluationMode.LookInTransformed | EvaluationMode.Strict }).Transform(transformer, input);

            Assert.AreEqual("{\"first\":123,\"second\":123,\"third\":125,\"fourth\":true,\"fifth\":true,\"sixth\":\"value is true\"}", result);
        }

        [Test]
        public void Issue201()
        {
            var input = "{ \"head\": { \"transaction\": 0, \"signature\": 31705, \"fields\": [{ \"name\": \"Validation_Status\", \"type\": \"xsd:string\", \"process\": \"Smp\", \"settable\": false, \"string_len\": 4 }, { \"name\": \"Sample_Point\", \"type\": \"xsd:string\", \"process\": \"Smp\", \"settable\": false, \"string_len\": 12 }, { \"name\": \"RT\", \"type\": \"xsd:float\", \"units\": \"degC\", \"process\": \"Avg\", \"settable\": false } ] }, \"data\": [{ \"time\": \"1999-02-09T04:10:02\", \"vals\": [\"dummy\", \"TEST_DUMMY\", 99.99] }, { \"time\": \"2022-01-17T15:40:00\", \"vals\": [\"raw\", \"FIAQRT003\", 27.75] } ] }";
            //var transformer = "{ \"channel_data\": { \"#loop($.head.fields,fields)\": { \"#loop($.data,data,root)\": { \"name\": \"#currentvalueatpath($.name,fields)\", \"value\": \"#currentvalueatpath(#xconcat($.vals[,#currentindex(data),]),data)\", \"time\": \"#currentvalueatpath($.time,data)\" } } } } ";
            var transformer = "{ \"channel_data\": { \"#loop($.data,data)\": { \"#loop($.vals)\": { \"name\": \"#valueof(#xconcat($.head.fields[,#currentindex(),].name))\", \"value\": \"#currentvalue()\", \"time\": \"#currentvalueatpath($.time,data)\" } } } }";
            //var transformer = "{ \"channel_data\": { \"#loop($.data,data)\": { " + 
            //    "\"#ifgroup(#exists($.vals[0]))\": { \"name\": \"#valueof($.head.fields[0].name)\", \"value\": \"#currentvalueatpath($.vals[0])\", \"time\": \"#currentvalueatpath($.time)\" }," +
            //    "\"#ifgroup(#exists($.vals[1]))\": { \"name\": \"#valueof($.head.fields[1].name)\", \"value\": \"#currentvalueatpath($.vals[1])\", \"time\": \"#currentvalueatpath($.time)\" }" +
            //    "} } }";
            var context = new JUSTContext
            {
                EvaluationMode = EvaluationMode.Strict
            };
            var result = new JsonTransformer(context).Transform(transformer, input);

            transformer = "{ \"channel_data\": { \"#loop($.channel_data)\": { \"#concat(#currentvalue(), #ifcondition(#mathlessthan(#currentindex(),#subtract(#length(#valueof($.channel_data)),1)),true,#valueof(#xconcat($.channel_data[,#add(#currentindex(),1),])),#arrayempty()))\" } } }";
            result = new JsonTransformer(context).Transform(transformer, result);

            Assert.AreEqual("{\"channel_data\":[{\"name\":\"Validation_Status\",\"value\":\"dummy\",\"time\":\"1999-02-09T04:10:02\"},{\"name\":\"Sample_Point\",\"value\":\"TEST_DUMMY\",\"time\":\"1999-02-09T04:10:02\"},{\"name\":\"RT\",\"value\":99.99,\"time\":\"1999-02-09T04:10:02\"},{\"name\":\"Validation_Status\",\"value\":\"raw\",\"time\":\"2022-01-17T15:40:00\"},{\"name\":\"Sample_Point\",\"value\":\"FIAQRT003\",\"time\":\"2022-01-17T15:40:00\"},{\"name\":\"RT\",\"value\":27.75,\"time\":\"2022-01-17T15:40:00\"}]}", result);
        }

        [Test]
        public void Issue202()
        {
            var input = "[ { \"FirstGroup\": \"abc\", \"SecondGroup\": \"123\", \"ContextValue\": \"test\" }, { \"FirstGroup\": \"abc\", \"SecondGroup\": \"123\", \"ContextValue\": \"test2\" }, { \"FirstGroup\": \"abc:d\", \"SecondGroup\": \"456\", \"ContextValue\": \"test\" }, { \"FirstGroup\": \"abc:d\", \"SecondGroup\": \"456\", \"ContextValue\": \"test2\" }]";
            var transformer = "{  \"test\": \"#grouparrayby($,FirstGroup:SecondGroup,test)\" }";
            var context = new JUSTContext
            {
                EvaluationMode = EvaluationMode.Strict
            };
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"test\":[{\"FirstGroup\":\"abc\",\"SecondGroup\":\"123\",\"test\":[{\"ContextValue\":\"test\"},{\"ContextValue\":\"test2\"}]},{\"FirstGroup\":\"abc:d\",\"SecondGroup\":\"456\",\"test\":[{\"ContextValue\":\"test\"},{\"ContextValue\":\"test2\"}]}]}", result);
        }

        [Test]
        public void Issue206()
        {
            //var input = "{ \"SHIPMENTENTITY\": { \"SHIPMENTID\": \"USMF - 000006\" } }";
            var input = "{ \"SHIPMENTENTITY\": [ { \"SHIPMENTID\": \"USMF - 000003\" }, { \"SHIPMENTID\": \"USMF - 000006\" } ] }";
            var transformer = "{ \"#ifgroup(#exists($.SHIPMENTENTITY.SHIPMENTID))\": { \"reference\": \"#valueof($.SHIPMENTENTITY.SHIPMENTID)\" }, \"#ifgroup(#exists($.SHIPMENTENTITY[0]))\": { \"result\": { \"#loop($.SHIPMENTENTITY)\" : { \"reference\": \"#currentvalueatpath($.SHIPMENTID)\" } } } }";
            //var transformer = "{ \"#ifgroup(#exists($.SHIPMENTENTITY.SHIPMENTID))\": { \"reference\": \"#valueof($.SHIPMENTENTITY.SHIPMENTID)\" }, \"#ifgroup(#exists($.SHIPMENTENTITY[0]))\": { \"result\": { \"#loop($.SHIPMENTENTITY)\" : \"#currentvalue()\" } } }";
            var context = new JUSTContext
            {
                EvaluationMode = EvaluationMode.Strict
            };
            var result = new JsonTransformer(context).Transform(transformer, input);
            //var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("", result);
        }

        [Test]
        public void Issue230()
        {
            const string input = "{ \"Coverages\": [{ \"Id\": \"b2223b13-75d4-42c2-8fcf-1234567890ab\",\"FormNumber\": \"PSXS 01 02 03 04\", \"Notes\": null }," +
                                 " { \"Id\": \"b2223b13-75d4-42c2-8fcf-ba0123456789\",\"FormNumber\": \"PSXS 04 03 02 01\", \"Notes\": null }," +
                                 " { \"Id\": \"b2223b13-75d4-42c2-8fcf-ba0123456789\",\"FormNumber\": \"PCTC 04 03 02 01\", \"Notes\": null }] }";
            const string transformer = "{ \"FormsListCoverages\": { " +
                                       " \"#loop($.Coverages[?(@.FormNumber =~ /^PSXS.*$//)])\": { " +
                                       "   \"FormNumber\": \"#currentvalueatpath($.FormNumber)\", \"FormNotes\": \"#currentvalueatpath($.Notes)\", " +
                                       " } } }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"FormsListCoverages\":[{\"FormNumber\":\"PSXS 01 02 03 04\",\"FormNotes\":null},{\"FormNumber\":\"PSXS 04 03 02 01\",\"FormNotes\":null}]}", result);
        }

        [Test]
        public void Issue173()
        {
            const string input = "[{ \"contract\": \"9402598776\" }, { \"contract\": \"9402598777\"},{ \"contract\": \"9402201620\"}]";
            const string transformer = "[{ \"#loop($)\": { \"accountId\": \"#currentvalueatpath($.contract)\" } }]";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue231()
        {
            const string input = "{ \"allocations\": [{ \"fundId\": 40000037, \"fundName\": \"Conservative (Super)\", \"percentage\": 0.00 }, " +
                " { \"fundId\": 40000038, \"fundName\": \"Balanced (Super)\", \"percentage\": 29.99880004799808 }] }";
            const string transformer = "{ \"#loop(allocations[?(@.percentage != 0)])\": { \"item\": \"#currentvalue()\" } }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("[{\"item\":{\"fundId\":40000038,\"fundName\":\"Balanced (Super)\",\"percentage\":29.99880004799808}}]", result);
        }

        [Test]
        public void Issue232()
        {
            const string input = "{\"Systems\": [{\"Id\": \"SystemId1\",\"Components\": [{\"Id\": \"CompId1\"},{\"Id\": \"CompId2\"}]},{\"Id\": \"SystemId2\",\"Components\": [{\"Id\": \"CompId3\"},{\"Id\": \"CompId4\"}]}]}";
            const string transformer = "{ \"SystemsAndComponets\": \"#applyover({ 'Systems': { '#loop($.Systems)': { 'Id': '#currentvalueatpath($.Id)' } }/, 'Components': { '#loop($.Systems[:].Components[:])': { 'Id': '#currentvalueatpath($.Id)' } },'#concat(#valueof($.Systems),#valueof($.Components))')\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue233()
        {
            const string input = "{\"Array\": [{\"Id\": \"123AAA\"}, {\"Id\": \"123BBB\"}, {\"Id\": \"123AAA\"} ] }";
            const string transformer = "{ \"Array\": { \"#loop($.Array)\": { \"#ifgroup(#mathgreaterthanorequalto(#firstindexof(#currentvalueatpath($.Id),AAA),0))\": { \"Value\": \"#currentvalue()\" } } } }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue234()
        {
            const string input = "{ \"orderItems\": [ { \"id\": \"1\", \"sku\": \"a\" }, { \"id\": \"2\", \"sku\": \"b\" }, { \"id\": \"3\", \"sku\": \"c\" } ], \"affectedItems\": [ 1, 2 ], \"test\": \"abc\" }";
            const string transformer = "{ \"#loop($.affectedItems,affectedItems)\": { \"#loop($.orderItems,orderItems,root)\": { \"sku\": \"#currentvalueatpath($.sku)\", \"id\": \"#currentvalue(affectedItems)\" } }}";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue236()
        {
            const string input = "{ \"createdTimestamp\": \"2022-06-02T15:38:02.783\", \"orderLine\": [ { \"itemId\": \"883849795227\", \"fulfillmentStatus\": \"Canceled\" }, { \"itemId\": \"194998065544\", \"fulfillmentStatus\": \"Canceled\" }, { \"itemId\": \"194998065500\", \"fulfillmentStatus\": \"Nothing\" } ] }";
            //const string transformer = "{ \"productCode\": \"#valueof($.orderLine[?(@.fulfillmentStatus =~ /Canceled//)].itemId)\" }";
            const string transformer = "{ \"productCode\": \"#applyover({ 'arr': '#valueof($.orderLine[?(@.fulfillmentStatus =~ /Canceled/)].itemId)' }, '#valueof($.arr[0])')\" }";

            var result = new JsonTransformer(new JUSTContext { EscapeChar = '|' }).Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue237()
        {
            const string input = "{ \"content_area\": { \"content_area\": [ \"1\" ] }, \"additional_content\": [{ \"Cards_feed\": { \"cards_feed\": [ \"2\"] } }] }";
            //const string transformer = "{ \"content\": [ { \"#loop($.content_area.content_area)\": { \"aaa\": \"bbb\" } }, { \"#loop($.additional_content[0].Cards_feed.cards_feed)\": { \"xxx\": \"yyy\" } } ] }";
            //const string transformer = "{ \"content\": \"#concat(#valueof(#loop($.content_area.content_area): { aaa: bbb }),#valueof(#loop($.additional_content[0].Cards_feed.cards_feed): { xxx: yyy }))\" }";
            //const string transformer = "{ \"content1\": { \"#loop($.content_area.content_area)\": { \"aaa\": \"bbb\" } }, \"content2\": { \"#loop($.additional_content[0].Cards_feed.cards_feed)\": { \"xxx\": \"yyy\" } } }";
            const string transformer = "{ \"content\": \"#applyover({ 'content1': { '#loop/($.content_area.content_area/)': { 'aaa': 'bbb' } }/, 'content2': { '#loop/($.additional_content[0].Cards_feed.cards_feed/)': { 'xxx': 'yyy' } } }, '#concat(#valueof($.content1),#valueof($.content2))')\" }";

            var result = new JsonTransformer(new JUSTContext { EvaluationMode = EvaluationMode.Strict }).Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue238()
        {
            const string input = "{ \"additional_content\": [{ \"Cards_feed\": { \"cards_feed\": [ \"2\"] } }] }";
            const string transformer = "{ \"content\": [ { \"aaa\": \"bbb\" }, { \"#ifgroup(#exists($.additional_content[*].Cards_feed))\": { \"#loop($.additional_content[*].Cards_feed.cards_feed)\": { \"xxx\": \"yyy\" } } } ] }";

            var result = new JsonTransformer(new JUSTContext { EvaluationMode = EvaluationMode.Strict }).Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue240()
        {
            const string input = "{ \"Data\": [ { \"Customer\": { \"Test\":\"TEST\", \"VehicleOwner\": null, \"Claimant\": { \"NameFirst\": \"TEST\", \"NameLast\": \"TEST\", \"Email\": \"test@test.com\", \"Phone\": null, \"Contact\": \"TEST\" }, \"Insured\": { \"NameFirst\": \"TEST\", \"NameLast\": \"TEST\", \"Email\": \"test@test.com\", \"PhoneCell\": \"123456789\", \"Contact\": \"TEST\" } } } ]}";
            //const string transformer = "{ \"test\": { \"#loop($..Customer.*)\": { \"#loop($.Contact)\": \"#currentvalue()\" } } }";
            //const string transformer = "{ \"test\": { \"#loop($..Customer.*)\": \"#currentvalueatpath($.Contact)\" } }";
            //const string transformer = "{ \"test\": \"#valueof($..Customer.*[?/(@ == 'TEST'/)])\" }";
            //const string transformer = "{ \"test\": \"#valueof($..Customer.*)\" }";

            const string transformer = "\"#applyover({ '#loop($..Customer.*)': { '#ifgroup(#exists($.Contact))': { 'Id': null/, 'Name': '#xconcat(#currentvalueatpath($.NameFirst)/,-/,#currentvalueatpath($.NameLast))'/, 'Email': '#currentvalueatpath($.Email)'/, 'Phone': '#currentvalueatpath($.PhoneCell)'/, 'SourceSystemId': 1 } }, '#valueof($[?(@.Id)])')\"";

            var result = new JsonTransformer(new JUSTContext { EvaluationMode = EvaluationMode.Strict }).Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue243()
        {
            const string input = "{ \"paging\": { \"totalItems\": 76, \"totalPages\": 16, \"pageSize\": 5, \"currentPage\": 1, \"maxPageSize\": 250 }, \"data\": [ { \"data\": { \"uniqueId\": \"cz-vehicle-17336428\", \"name\": \"ŠKODA SUPERB NFD7FD7GC* ACDTUAX01 3T\" }, \"meta\": { \"createdAt\": \"2022-07-01T21:16:50.1252598+00:00\", \"hash\": \"bc53b2682f8498abffc49c3203609911da334d6d3ad9f8156a7c0499fc83caf7\", } }, { \"data\": { \"uniqueId\": \"cz-vehicle-17168912\", \"name\": \"HYUNDAI I 30 M6DAZ11 F5P91 PDE\" }, \"meta\": { \"createdAt\": \"2022-07-01T21:16:50.1252598+00:00\", \"hash\": \"6b5a1953d45a35341385575eead2468f115633e507da8f84a538a74b877d0c1a\", } } ]}";
            //const string transformer = "{ \"#\": [ \"#copy($)\", \"#delete($.data..data.name)\" ] }";
            const string transformer = "{ \"paging\": { \"#\": [ \"#copy($.paging)\" ] }, \"data\": { \"#loop($.data)\": { \"data\": { \"#\": [ \"#copy($.data)\", \"#delete($.name)\" ] }, \"meta\": { \"#\": [ \"#copy($.meta)\" ] } } } }";

            var result = new JsonTransformer(new JUSTContext { EvaluationMode = EvaluationMode.Strict }).Transform(transformer, input);

            Assert.AreEqual("{\"paging\":{\"totalItems\":76,\"totalPages\":16,\"pageSize\":5,\"currentPage\":1,\"maxPageSize\":250},\"data\":[{\"data\":{\"uniqueId\":\"cz-vehicle-17336428\"},\"meta\":{\"createdAt\":\"2022-07-01T21:16:50.1252598+00:00\",\"hash\":\"bc53b2682f8498abffc49c3203609911da334d6d3ad9f8156a7c0499fc83caf7\"}},{\"data\":{\"uniqueId\":\"cz-vehicle-17168912\"},\"meta\":{\"createdAt\":\"2022-07-01T21:16:50.1252598+00:00\",\"hash\":\"6b5a1953d45a35341385575eead2468f115633e507da8f84a538a74b877d0c1a\"}}]}", result);
        }

        [Test]
        public void Issue245()
        {
            const string input = "{ \"someObject\": { \"someArray\": [ { \"property1\": \"Some property\", \"user\": { \"userId\": 2 }, \"property2\": \"Some property\" }, { \"property1\": \"Some property 2\", \"user\": { \"userId\": 3 }, \"property2\": \"Some property 3\" }, ] }, \"otherObject\":{ \"otherArray\": [ { \"id\": 2, \"property3\" : \"some value\" }, { \"id\": 3, \"property3\" : \"some value2\" } ] }}";
            const string transformer = "{\"someObject\": {\"someArray\": {\"#loop($.someObject.someArray)\" : {\"property1\": \"#currentvalueatpath($.property1)\",\"user_test\": \"#currentvalueatpath($.user.userId)\",\"user\": \"#valueof(#xconcat($.otherObject.otherArray[?/(@.id==,#currentvalueatpath($.user.userId),/)]))\",\"property2\": \"#currentvalueatpath($.property2)\" }} }}";

            var result = new JsonTransformer(new JUSTContext { EvaluationMode = EvaluationMode.Strict }).Transform(transformer, input);

            Assert.AreEqual("{\"someObject\":{\"someArray\":[{\"property1\":\"Some property\",\"user_test\":2,\"user\":{\"id\":2,\"property3\":\"some value\"},\"property2\":\"Some property\"},{\"property1\":\"Some property 2\",\"user_test\":3,\"user\":{\"id\":3,\"property3\":\"some value2\"},\"property2\":\"Some property 3\"}]}}", result);
        }

        [Test]
        public void Issue248()
        {
            const string input = "{\"FirstName\": \"Ghaiath is the best ever\",\"LastName\": \"\" }";
            const string transformer = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?><Root><FirstName>#valueof($.FirstName)</FirstName><LastName>#valueof($.LastName)</LastName></Root>";

            var result = new DataTransformer(new JUSTContext { EvaluationMode = EvaluationMode.Strict }).Transform(transformer, input);

            Assert.AreEqual("<?xml version=\"1.0\" encoding=\"UTF-8\" ?><Root><FirstName>Ghaiath is the best ever</FirstName><LastName></LastName></Root>", result);
        }

        [Test]
        public void Issue252()
        {
            const string input = "{\"arg\": 1,\"arr\": [{\"id\": 1,\"val\": 100},{\"id\": 2,\"val\": 200}]}";
            const string transformer = "{\"sharp\": \"/#not_a_function\",\"sharp_arg\": \"#xconcat(/#not,_a_function_arg)\"}";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{}", result);
        }

        [Test]
        public void Issue258()
        {
            const string input = "{ \"employees\": [{  \"employee_unique_id\": \"FA6FDECD-DFAD-4C6D-80E8-752643BF4C9E\",  \"clinician_id\": \"178\",  \"salutation\": null,  \"first_name\": \"Valid\",  \"can_drive\": true,  \"locations\": [{   \"location_id\": 23,   \"location_name\": \"test\"  }, {   \"location_id\": 24,   \"location_name\": \"test444\"  }  ] }, {  \"employee_unique_id\": \"GA6FDECD-DFAD-4C6D-80E8-752643BF4C9O\",  \"clinician_id\": \"179\",  \"salutation\": null,  \"first_name\": \"Valid222\",  \"can_drive\": false,  \"locations\": [{   \"location_id\": 23,   \"location_name\": \"test\"  }, {   \"location_id\": 24,   \"location_name\": \"test444\"  }  ] } ]}";
            const string transformer = "{ \"employees\": { \"#loop($.employees,employee)\": { \"employee_unique_id\": \"#currentvalueatpath($.employee_unique_id,employee)\", \"personal\": { \"clinician_id\": \"#currentvalueatpath($.clinician_id,employee)\", \"salutation\": \"#currentvalueatpath($.salutation,employee)\", \"first_name\": \"#currentvalueatpath($.first_name,employee)\", \"can_drive\": \"#isboolean(#currentvalueatpath($.can_drive,employee))\", \"locations\": { \"#loop($.locations)\": { \"location_id\": \"#currentvalueatpath($.location_id)\", \"location_name\": \"#currentvalueatpath($.location_name)\" } } } } }}";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"employees\":[{\"employee_unique_id\":\"FA6FDECD-DFAD-4C6D-80E8-752643BF4C9E\",\"personal\":{\"clinician_id\":\"178\",\"salutation\":\"#currentvalueatpath($.salutation,employee),\",\"first_name\":\"Valid\",\"can_drive\":true,\"locations\":[{\"location_id\":23,\"location_name\":\"test\"},{\"location_id\":24,\"location_name\":\"test444\"}]}},{\"employee_unique_id\":\"GA6FDECD-DFAD-4C6D-80E8-752643BF4C9O\",\"personal\":{\"clinician_id\":\"179\",\"salutation\":\"#currentvalueatpath($.salutation,employee),\",\"first_name\":\"Valid222\",\"can_drive\":true,\"locations\":[{\"location_id\":23,\"location_name\":\"test\"},{\"location_id\":24,\"location_name\":\"test444\"}]}}]}", result);
        }

        [Test]
        public void Issue264()
        {
            const string input = "{\"Customer\":[{ " +
                                    "\"Info1\":{" +
                                        "\"Field1\":\"Value1\",\"Field2\":\"Value2\",\"Field3\":\"Value3\" " +
                                    "}," +
                                    "\"Info2\":{" +
                                        "\"Field4\":\"Value4\",\"Field5\":\"Value5\",\"Field6\":\"Value6\" " +
                                    "}},{\"Info1\":{\"Field1\":\"Value7\",\"Field2\":\"Value8\",\"Field3\":\"Value9\"},\"Info2\":{\"Field4\":\"Value10\",\"Field5\":\"Value11\",\"Field6\":\"Value12\"}}]}";
            //const string transformer = "{ \"GenericCustomer\": { \"#loop($.Customer)\": { \"GenericComponent1\": \"#currentvalueatpath($.Info1.Field1)\", \"GenericComponent2\": \"#currentvalueatpath($.Info1.Field2)\", \"GenericComponent3\": \"#currentvalueatpath($.Info1.Field3)\" } } }";
            /*
            const string transformer = "{\"GenericCustomer1\": {" +
                                            "\"#loop($.Customer)\":{" +
                                                "\"GenericComponent1\":\"#currentvalueatpath($.Info1.Field1)\"," + 
                                                "\"GenericComponent2\":\"#currentvalueatpath($.Info1.Field2)\"," + 
                                                "\"GenericComponent3\":\"#currentvalueatpath($.Info1.Field3)\" "+ 
                                            "}" +
                                            "}," +
                                            "\"GenericCustomer2\": { "+ 
                                                "\"#loop($.Customer)\":{ " +
                                                    "\"GenericComponent1\":\"#currentvalueatpath($.Info2.Field4)\", " +
                                                    "\"GenericComponent2\":\"#currentvalueatpath($.Info2.Field5)\", " +
                                                    "\"GenericComponent3\":\"#currentvalueatpath($.Info2.Field6)\"}}}";
            */
            const string transformer = "{ \"GenericCustomer\": \"#applyover({'GenericCustomer1':{'#loop($.Customer)':{'GenericComponent1':'#currentvalueatpath($.Info1.Field1)'/,'GenericComponent2':'#currentvalueatpath($.Info1.Field2)'/,'GenericComponent3':'#currentvalueatpath($.Info1.Field3)'}}/,'GenericCustomer2':{'#loop($.Customer)':{'GenericComponent1':'#currentvalueatpath($.Info2.Field4)'/,'GenericComponent2':'#currentvalueatpath($.Info2.Field5)'/,'GenericComponent3':'#currentvalueatpath($.Info2.Field6)'}}}, '#concat(#valueof($.GenericCustomer1), #valueof($.GenericCustomer2))')\" }";
            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"GenericCustomer\":[{\"GenericComponent1\":\"Value1\",\"GenericComponent2\":\"Value2\",\"GenericComponent3\":\"Value3\"},{\"GenericComponent1\":\"Value7\",\"GenericComponent2\":\"Value8\",\"GenericComponent3\":\"Value9\"},{\"GenericComponent1\":\"Value4\",\"GenericComponent2\":\"Value5\",\"GenericComponent3\":\"Value6\"},{\"GenericComponent1\":\"Value10\",\"GenericComponent2\":\"Value11\",\"GenericComponent3\":\"Value12\"}]}", result);
        }

        [Test]
        public void Issue269()
        {
            const string input = "{ \"employees\": [{ \"employee_unique_id\": \"FA6FDECD-DFAD-4C6D-80E8-752643BF4C9E\",  \"clinician_id\": \"178\",  \"salutation\": null,  \"first_name\": \"Valid\",  \"can_drive\": true,  \"locations\": [{   \"location_id\": 23,   \"location_name\": \"test\"  }, {   \"location_id\": 24,   \"location_name\": \"test444\"  }  ] }, { \"exists\": \"true\", \"employee_unique_id\": \"GA6FDECD-DFAD-4C6D-80E8-752643BF4C9O\",  \"clinician_id\": \"179\",  \"salutation\": null,  \"first_name\": \"Valid222\",  \"can_drive\": false,  \"locations\": [{   \"location_id\": 23,   \"location_name\": \"test\"  }, {   \"location_id\": 24,   \"location_name\": \"test444\"  }  ] } ]}";
            const string transformer = "{ \"employees\": { \"#loop($.employees)\": { \"#ifgroup(#existsandnotempty($.exists))\": { \"print\": true, \"employee_unique_id\": \"#currentvalueatpath($.employee_unique_id)\" } } } }";

            var result = new JsonTransformer(new JUSTContext { EvaluationMode = EvaluationMode.Strict}).Transform(transformer, input);

            Assert.AreEqual("{\"employees\":[{\"employee_unique_id\":\"FA6FDECD-DFAD-4C6D-80E8-752643BF4C9E\"}]}", result);
        }

        [Test]
        public void Issue272()
        {
            const string input = "{ \"dummy\": 1 }";
            const string transformer = "{ \"result\": \"#applyover(#returnToken(),'#valueof($.fn)')\" }";

            JUSTContext context = new JUSTContext { EvaluationMode = EvaluationMode.Strict};
            context.RegisterCustomFunction(null, "JUST.UnitTests.Token", "ReturnJToken", "returnToken");
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"result\":{\"fn\":1}}", result);
        }

        [Test]
        public void Issue272_array()
        {
            const string input = "{ \"values\": [{ \"OrderNumber__c\": 123 }, { \"OrderNumber__c\": 456 }] }";
            const string transformer = "{ \"result\": \"#applyover(#array(#valueof($.values)),'#valueof($[1].OrderNumber__c)')\" }";

            JUSTContext context = new JUSTContext { EvaluationMode = EvaluationMode.Strict};
            context.RegisterCustomFunction(null, "JUST.UnitTests.Token", "Array", "array");
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"result\":123}", result);
        }

        [Test]
        public void Issue272_loop()
        {
            const string input = "{ \"values\": [{ \"OrderNumber__c\": 123 }, { \"OrderNumber__c\": 456 }] }";
            const string transformer = "{ \"result\": \"#applyover(#array(#valueof($.values)),{ '#loop($)': { 'curr': '#currentvalue()' } })\" }";

            JUSTContext context = new JUSTContext { EvaluationMode = EvaluationMode.Strict};
            context.RegisterCustomFunction(null, "JUST.UnitTests.Token", "Array", "array");
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"result\":[{\"curr\":{\"OrderNumber__c\":123}},{\"curr\":{\"OrderNumber__c\":456}}]}", result);
        }

        [Test]
        public void Issue280()
        {
            const string input = "{ \"Path\": \"Type1\", \"Children\": [ { \"Path\": \"Type1~Cat1\", \"Items\": [ { \"Id\": \"66717101\" }, { \"Id\": \"66717102\" } ], \"Children\": [ { \"Path\": \"Type1~Cat1~Sku1\", \"Items\": [ { \"Id\": \"66717101\" } ], \"Children\": [ { \"Path\": \"Type1~Cat1~Sku2~Model1\", \"Items\": [ { \"Id\": \"66717102\" } ] }, { \"Path\": \"Type1~Cat1~Sku1~Model1\", \"Items\": [ { \"Id\": \"66717101\" }, { \"Id\": \"66717102\" } ] } ] }, { \"Path\": \"Type1~Cat1~Sku2\", \"Items\": [ { \"Id\": \"66717101\" } ], \"Children\": [ { \"Path\": \"Type1~Cat1~Sku2~Model1\", \"Items\": [ { \"Id\": \"66717722\" } ] } ] } ] }, { \"Path\": \"Type1-Cat2\", \"Children\": [] }, { \"Path\": \"Type1-Cat3\", \"Children\": [] } ], \"Items\": [ { \"Id\": \"66717766\" }, { \"Id\": \"6828147\" } ]}";
            const string transformer = "[ \"#valueof($..Id)\", \"#valueof($..Path)\" ]";

            JUSTContext context = new JUSTContext { EvaluationMode = EvaluationMode.Strict};
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("", result);
        }

        [Test]
        public void Issue279()
        {
            const string input = "{ \"open\": \"Open\", \"close\": \"Close\", \"menu\": { \"popup\": { \"menuitem\": [ { \"value\": \"Open\", \"onclick\": \"OpenDoc()\" }, { \"value\": \"Close\", \"onclick\": \"CloseDoc()\" } ], \"submenuitem\": \"CloseSession()\" } } }";
            const string transformer = "{ \"result\": { \"Open\": \"#valueof(#xconcat($.menu.popup.menuitem[?/(@.value == ',#valueof($.open),'/)].onclick))\", \"Close\": \"#valueof(#xconcat($.menu.popup.menuitem[?/(@.value == ',#valueof($.close),'/)].onclick))\" } }";

            JUSTContext context = new JUSTContext { EvaluationMode = EvaluationMode.Strict};
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("", result);
        }

        [Test]
        public void Issue282()
        {
            const string input = "{ \"Questions\": { \"Q1\": { \"Choice1\": \"option 1\", \"Choice2\": \"option 2\" }, \"Q2\": { \"Choice1\": \"option 3\", \"Choice2\": \"option 4\" } }}";
            //const string transformer = "{ \"result\": { \"#loop($.Questions)\": { \"#eval(#xconcat(Q,#currentindex()))\": [ \"#currentvalueatpath(#xconcat($.,#currentproperty()))\" ] } } }";
            const string transformer = "{ \"Questions\": \"#applyover({ 'result': { '#loop($.Questions)': { '#eval(#xconcat(Q,#currentindex()))': [ '#currentvalueatpath(#xconcat($.,#currentproperty()))' ] } } },'#xconcat(#valueof($.result.Q0),#valueof($.result.Q1))')\" }";

             JUSTContext context = new JUSTContext { EvaluationMode = EvaluationMode.Strict};
            // var r1 = new JsonTransformer(context).Transform(transformer, input);

            // const string t2 = "{ \"Questions\": \"#xconcat(#valueof($.result.Q0),#valueof($.result.Q1))\" }";
            //var result = new JsonTransformer(context).Transform(t2, r1);
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("", result);
        }
    }

    public class Token
    {
        public Newtonsoft.Json.Linq.JToken ReturnJToken()
        {
            return Newtonsoft.Json.Linq.JToken.Parse("{ \"fn\": 1 }");
        }

        public Newtonsoft.Json.Linq.JToken Array(object[] values)
        {
            return Newtonsoft.Json.Linq.JArray.FromObject(values);
        }
    }
}