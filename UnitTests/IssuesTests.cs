using NUnit.Framework;

namespace JUST.UnitTests
{
    [TestFixture]
    public class IssuesTests
    {
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

        [Test]
        public void Issue294()
        {
            const string input = "{ \"topLevelItems\": [ { \"name\": \"item1\", \"type\": \"someType\", \"selectable\": false, \"itemArray\": [ { \"property1\": \"item1_value1\", \"property2\": \"item1_value2\" } ] }, { \"name\": \"item2\", \"type\": \"someType\", \"selectable\": true, \"itemArray\": [ { \"property1\": \"item2_value1\", \"property2\": \"item2_value2\" } ] } ]}";
            const string transformer = "{\"filteredItems\": \"#valueof($.topLevelItems[?(@.selectable == true)])\", \"summary\": { \"#loop($..filteredItems)\": { \"name\": \"#currentvalueatpath($.name)\" } }, \"aliasedItems\": \"#valueof($..filteredItems)\" }";

             JUSTContext context = new JUSTContext { EvaluationMode = EvaluationMode.Strict | EvaluationMode.LookInTransformed };
            var result = new JsonTransformer(context).Transform(transformer, input);

            Assert.AreEqual("{\"filteredItems\":{\"name\":\"item2\",\"type\":\"someType\",\"selectable\":true,\"itemArray\":[{\"property1\":\"item2_value1\",\"property2\":\"item2_value2\"}]},\"summary\":{},\"aliasedItems\":{\"name\":\"item2\",\"type\":\"someType\",\"selectable\":true,\"itemArray\":[{\"property1\":\"item2_value1\",\"property2\":\"item2_value2\"}]}}", result);
        }
    }
}