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
            const string transformer = "{ \"FullName\": \"#concat(#concat(#concat(#valueof($.Name), ),#concat(#valueof($.MiddleName), )),#valueof($.Surname))\",	\"Contact Information\": { \"Street Name\": \"#substring(#valueof($.ContactInformation),0,#firstindexof(#valueof($.ContactInformation),:))\", \"City\": \"#substring(#valueof($.ContactInformation),#add(#firstindexof(#valueof($.ContactInformation),:),1),#subtract(#subtract(#lastindexof(#valueof($.ContactInformation),:),#firstindexof(#valueof($.ContactInformation),:)),1))\", \"PhoneNumber\": \"#substring(#valueof($.ContactInformation),#add(#lastindexof(#valueof($.ContactInformation),:),1),#subtract(#lastindexof(#valueof($.ContactInformation),),#lastindexof(#valueof($.ContactInformation),:)))\" }, \"Personal Information\": { \"Age\": \"#substring(#valueof($.PersonalInformation),0,#firstindexof(#valueof($.PersonalInformation),:))\", \"Civil Status\": \"#substring(#valueof($.PersonalInformation),#add(#firstindexof(#valueof($.PersonalInformation),:),1),#subtract(#subtract(#lastindexof(#valueof($.PersonalInformation),:),#firstindexof(#valueof($.PersonalInformation),:)),1))\", \"Ethnicity\": \"#substring(#valueof($.PersonalInformation),#add(#lastindexof(#valueof($.PersonalInformation),:),1),#subtract(#lastindexof(#valueof($.PersonalInformation),),#lastindexof(#valueof($.PersonalInformation),:)))\" }}";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"FullName\":\"Kari Inger Nordmann\",\"Contact Information\":{\"Street Name\":\"Karl johans gate\",\"City\":\"Oslo\",\"PhoneNumber\":\"88880000\"},\"Personal Information\":{\"Age\":\"45\",\"Civil Status\":\"Married\",\"Ethnicity\":\"Norwegian\"}}", result);
        }

        [Test]
        public void MultipleArgumentConstantFunctions()
        {
            const string input = "{ \"Name\": \"Kari\", \"Surname\": \"Nordmann\", \"MiddleName\": \"Inger\", \"ContactInformation\": \"Karl johans gate:Oslo:88880000\" , \"PersonalInformation\": \"45:Married:Norwegian\",\"AgeOfMother\": 67,\"AgeOfFather\": 70, \"Empty\": \"\" }";
            const string transformer = "{ \"FullName\": \"#xconcat(#valueof($.Name),#constant_comma(),#valueof($.MiddleName),#constant_comma(),#valueof($.Surname))\", \"AgeOfParents\": \"#xadd(#valueof($.AgeOfMother),#valueof($.AgeOfFather))\", \"TestSomeEmptyString\": \"#ifcondition(#valueof($.Empty),#stringempty(),String is empty,String is not empty)\", \"TestSomeOtherString\": \"#ifcondition(#valueof($.Name),#stringempty(),String is empty,String is not empty)\" }";

            var result = new JsonTransformer().Transform(transformer, input);

            Assert.AreEqual("{\"FullName\":\"Kari,Inger,Nordmann\",\"AgeOfParents\":137,\"TestSomeEmptyString\":\"String is empty\",\"TestSomeOtherString\":\"String is not empty\"}", result);
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
            var transformer = "{ \"result\": \"#applyover({ 'condition': { '#loop($.values)': { 'test': '#ifcondition(#stringcontains(#valueof($.d[0]),#currentvalue()),True,yes,no)' } } }, '#exists($.condition[?(@.test=='yes')])')\" }";
            var context = new JUSTContext
            {
                EvaluationMode = EvaluationMode.Strict
            };
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"result\":true}", result);
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
    }
}