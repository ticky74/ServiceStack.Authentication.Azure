using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Authentication.Azure.Tests;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: TestCollectionOrderer(XUnitTestCollectionOrderer.TypeName, XUnitTestCollectionOrderer.AssembyName)]
[assembly: TestCaseOrderer(
                XUnitTestCaseOrderer.TypeName,
                    XUnitTestCaseOrderer.AssembyName)]

namespace ServiceStack.Authentication.Azure.Tests
{
    /// <summary>
    /// Used by CustomOrderer
    /// </summary>
    public class OrderAttribute : Attribute
    {
        public int I { get; }

        public OrderAttribute(int i)
        {
            I = i;
        }
    }

    /// <summary>
    /// Custom xUnit test collection orderer that uses the OrderAttribute
    /// </summary>
    public class XUnitTestCollectionOrderer : ITestCollectionOrderer
    {
        public const string TypeName = "ServiceStack.Authentication.Azure.Tests.XUnitTestCollectionOrderer";

        public const string AssembyName = "ServiceStack.Authentication.Azure.Tests";

        public IEnumerable<ITestCollection> OrderTestCollections(
            IEnumerable<ITestCollection> testCollections)
        {
            return testCollections.OrderBy(GetOrder);
        }

        /// <summary>
        /// Test collections are not bound to a specific class, however they
        /// are named by default with the type name as a suffix. We try to
        /// get the class name from the DisplayName and then use reflection to
        /// find the class and OrderAttribute.
        /// </summary>
        private static int GetOrder(
            ITestCollection testCollection)
        {
            var i = testCollection.DisplayName.LastIndexOf(' ');
            if (i <= -1)
                return 0;

            var className = testCollection.DisplayName.Substring(i + 1);
            var type = Type.GetType(className);
            if (type == null)
                return 0;

            var attr = type.GetTypeInfo().GetCustomAttribute<OrderAttribute>();
            return attr?.I ?? 0;
        }
    }

    /// <summary>
    /// Custom xUnit test case orderer that uses the OrderAttribute
    /// </summary>
    public class XUnitTestCaseOrderer : ITestCaseOrderer
    {
        public const string TypeName = "ServiceStack.Authentication.Azure.Tests.XUnitTestCaseOrderer";

        public const string AssembyName = "ServiceStack.Authentication.Azure.Tests";

        public static readonly ConcurrentDictionary<string, ConcurrentQueue<string>>
            QueuedTests = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(
            IEnumerable<TTestCase> testCases)
            where TTestCase : ITestCase
        {
            return testCases.OrderBy(GetOrder);
        }

        private static int GetOrder<TTestCase>(
            TTestCase testCase)
            where TTestCase : ITestCase
        {
            // Enqueue the test name.
            QueuedTests
                .GetOrAdd(
                    testCase.TestMethod.TestClass.Class.Name,
                    key => new ConcurrentQueue<string>())
                .Enqueue(testCase.TestMethod.Method.Name);

            // Order the test based on the attribute.
            var attr = testCase.TestMethod.Method
                .ToRuntimeMethod()
                .GetCustomAttribute<OrderAttribute>();
            return attr?.I ?? 0;
        }
    }
}