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
    public class ArgumentAnalyzerTests : CodeFixVerifier
    {

        [TestMethod]
        public void Should_not_trigger_on_empty()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }
        [TestMethod]
        public void Should_not_find_any_issue_with_string_keyword()
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
                var approvals = await context.CallActivityAsync<List<Application>>(""ApplicationsFiltered"", new String(""));
                log.LogInformation($""Approval received. {approvals.Count} applicants approved"");
                return approvals.OrderByDescending(x => x.Score).First();
            }

        [FunctionName(""ApplicationsFiltered"")]
        public static async Task Run(
            [ActivityTrigger] string userName,
            [OrchestrationClient] DurableOrchestrationClient client)
        {
            await client.RaiseEventAsync(approval.InstanceId, ""ApplicationsFiltered"", approval.Applications);
        }
    }
}";

            VerifyCSharpDiagnostic(test);

        }

        [TestMethod]
        public void Should_not_find_any_issue_with_correctly_function_parameter()
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
                var approvals = await context.CallActivityAsync<List<Application>>(""ApplicationsFiltered"", new String(""));
                log.LogInformation($""Approval received. {approvals.Count} applicants approved"");
                return approvals.OrderByDescending(x => x.Score).First();
            }

        [FunctionName(""ApplicationsFiltered"")]
        public static async Task Run(
            [ActivityTrigger] String userName,
            [OrchestrationClient] DurableOrchestrationClient client)
        {
            await client.RaiseEventAsync(approval.InstanceId, ""ApplicationsFiltered"", approval.Applications);
        }
    }
}";
            
            VerifyCSharpDiagnostic(test);
            
        }

        [TestMethod]
        public void Should_find_issue_with_incorrect_function_parameter()
        {
            var test = @"using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;

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
                var approvals = await context.CallActivityAsync<List<Application>>(""ApplicationsFiltered"", Guid.NewGuid());
                log.LogInformation($""Approval received. {approvals.Count} applicants approved"");
                return approvals.OrderByDescending(x => x.Score).First();
            }

        [FunctionName(""ApplicationsFiltered"")]
        public static async Task Run(
            [ActivityTrigger] String userName,
            [OrchestrationClient] DurableOrchestrationClient client)
        {
            await client.RaiseEventAsync(approval.InstanceId, ""ApplicationsFiltered"", approval.Applications);
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = "DurableFunctionsArgumentAnalyzer",
                Message = String.Format("Azure function named '{0}' takes a '{1}' but was given a '{2}'", "ApplicationsFiltered", "System.String", "System.Guid"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 21, 108)
                        }
            };

            VerifyCSharpDiagnostic(test, expected); 
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
