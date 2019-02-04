using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using DurableFunctionsAnalyzer;

namespace DurableFunctionsAnalyzer.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        [TestMethod]
        public void Should_not_trigger_on_empty()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void Should_not_find_any_issue_with_correctly_named_functions()
        {
            var test = @"using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ExternalInteraction
{
    public static class HireEmployee
    {
        [FunctionName(""HireEmployee"")]
        public static async Task<Application> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
            {
                var applications = context.GetInput<List<Application>>();
                var approvals = await context.CallActivityAsync<List<Application>>(""ApplicationsFiltered"");
                log.LogInformation($""Approval received. {approvals.Count} applicants approved"");
                return approvals.OrderByDescending(x => x.Score).First();
            }

        [FunctionName(""ApplicationsFilteredNicely"")]
        public static async Task Run(
            [QueueTrigger(""approval-queue"")] Approval approval,
            [OrchestrationClient] DurableOrchestrationClient client)
        {
            await client.RaiseEventAsync(approval.InstanceId, ""ApplicationsFiltered"", approval.Applications);
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "DurableFunctionsAnalyzer",
                Message = String.Format("Azure function named '{0}' does not exist. Did you mean 'ApplicationsFilteredNicely'", "ApplicationsFiltered"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 20, 39)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            //        var fixtest = @"
            //using System;
            //using System.Collections.Generic;
            //using System.Linq;
            //using System.Text;
            //using System.Threading.Tasks;
            //using System.Diagnostics;

            //namespace ConsoleApplication1
            //{
            //    class TYPENAME
            //    {   
            //    }
            //}";
            //        VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new DurableFunctionsAnalyzerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NameAnalyzerRegistration();
        }
    }
}
