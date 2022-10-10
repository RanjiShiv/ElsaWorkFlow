using Elsa.Activities.ControlFlow;
using Elsa.Activities.Email;
using Elsa.Activities.Http;
using Elsa.Builders;
using System.Net;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Primitives;
using Elsa.Activities.Temporal;
using NodaTime;
using Elsa.Services.Models;

namespace POC.Workflows
{
    public class DocumentApprovalWorkflow : IWorkflow
    {
        public string LocalSender => "\"documents@acme.com\"";
        public string LocalSenderv2 => "\"documents_v2@acme.com\"";
        public string path => "/v1/documents";
        public string user => "\"user1@atadata.com\"";

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WithDisplayName("Document Approval Workflow")
                .HttpEndpoint(activity => activity
                    .WithPath(path)
                    .WithMethod(HttpMethod.Post.Method)
                    .WithReadContent())
                .SetVariable("Document", context => context.GetInput<HttpRequestModel>()!.Body)
                .SendEmail(activity => activity
                    .WithSender(LocalSender)
                    .WithRecipient(user)
                    .WithSubject(context => $"Document received from {context.GetVariable<dynamic>("Document")!.Author.Name}")
                    .WithBody(context =>
                    {
                        var document = context.GetVariable<dynamic>("Document")!;
                        var author = document!.Author;
                        return $"Document from {author.Name} received for review.<br>" +
                        $"<a href=\"{context.GenerateSignalUrl("Approve")}\">Approve</a> or " +
                        $"<a href=\"{context.GenerateSignalUrl("Reject")}\">Reject</a> or " +
                        $"<a href=\"{context.GenerateSignalUrl("Remind")}\">Remind</a>";
                    }))
                .WriteHttpResponse(
                    HttpStatusCode.OK,
                    "<h1>Client data has been received and sent for review.</h1><p>If validated successfully, we can continue with the application migration process.</p>",
                    "text/html")
                .Then<Fork>(activity => activity.WithBranches("Approve", "Reject"), fork =>
                {
                    fork
                        .When("Approve")
                        .SignalReceived("Approve")
                        .SendEmail(activity => activity
                            .WithSender(LocalSender)
                            .WithRecipient(context => context.GetVariable<dynamic>("Document")!.Author.Email)
                            .WithSubject(context => $"Document {context.GetVariable<dynamic>("Document")!.Id} Approved!")
                            .WithBody(context => $"APPROVED: Great job {context.GetVariable<dynamic>("Document")!.Author.Name}, " +
                            $"Data  provided is sufficient to move forward."))
                        .ThenNamed("Join")
                        .CancelTimer();

                    fork
                        .When("Reject")
                        .SignalReceived("Reject")
                        .SendEmail(activity => activity
                            .WithSender(LocalSender)
                            .WithRecipient(context => context.GetVariable<dynamic>("Document")!.Author.Email)
                            .WithSubject(context => $"Document {context.GetVariable<dynamic>("Document")!.Id} Rejected")
                            .WithBody(context => $"REJECTED: Nice try {context.GetVariable<dynamic>("Document")!.Author.Name}, " +
                            $"Below are the missing details........"))
                         .Timer(Duration.FromSeconds(10)).WithName("Reminder")
                          .SendEmail(activity => activity
                                  .WithSender(LocalSender)
                                  .WithRecipient(user)
                                  .WithSubject(context => $"{context.GetVariable<dynamic>("Document")!.Author.Name} HAS BEEN UPDATED and " +
                                  $"is waiting for your review AGAIN!")
                                  .WithBody(context =>
                                      $"REMINDER: Don't forget to review the updated document {context.GetVariable<dynamic>("Document")!.Id}." +
                                      $"<br><a href=\"{context.GenerateSignalUrl("Approve")}\">Approve</a> or " +
                                      $"<a href=\"{context.GenerateSignalUrl("Reject")}\">Reject</a>"));
                              //.ThenNamed("Reminder")
                              // .CancelTimer();


                    //fork
                    //.When("Remind")
                    //.Timer(Duration.FromSeconds(10)).WithName("Reminder")
                    // .SendEmail(activity => activity
                    //         .WithSender(LocalSender)
                    //         .WithRecipient(user)
                    //         .WithSubject(context => $"{context.GetVariable<dynamic>("Document")!.Author.Name} HAS BEEN UPDATED and " +
                    //         $"is waiting for your review AGAIN!")
                    //         .WithBody(context =>
                    //             $"REMINDER: Don't forget to review the updated document {context.GetVariable<dynamic>("Document")!.Id}." +
                    //             $"<br><a href=\"{context.GenerateSignalUrl("Approve")}\">Approve</a> or " +
                    //             $"<a href=\"{context.GenerateSignalUrl("Reject")}\">Reject</a>"))
                    //     .ThenNamed("Reminder")
                    //      .CancelTimer()
                })
                //.Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Reminder")
                // .Timer(Duration.FromSeconds(10)).WithName("Reminder")
                //        .SendEmail(activity => activity
                //                .WithSender(LocalSenderv2)
                //                .WithRecipient(user)
                //                .WithSubject(context => $"{context.GetVariable<dynamic>("Document")!.Author.Name} HAS BEEN UPDATED and " +
                //                $"is waiting for your review AGAIN!")
                //                .WithBody(context =>
                //                    $"REMINDER: Don't forget to review the updated document {context.GetVariable<dynamic>("Document")!.Id}." +
                //                    $"<br><a href=\"{context.GenerateSignalUrl("Approve")}\">Approve</a> or " +
                //                    $"<a href=\"{context.GenerateSignalUrl("Reject")}\">Reject</a>"))
                //            .ThenNamed("Reminder")
                //             .CancelTimer()
                .Add<Join>(join => join.WithMode(Join.JoinMode.WaitAny)).WithName("Join")
                .WriteHttpResponse(HttpStatusCode.OK, "Validation complete!", "text/html");


        }
    }
}
