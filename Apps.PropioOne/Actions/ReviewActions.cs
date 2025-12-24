using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;
using Blackbird.Applications.SDK.Extensions.FileManagement.Interfaces;

namespace Apps.PropioOne.Actions
{
    [ActionList("Review")]
    public class ReviewActions(InvocationContext invocationContext, IFileManagementClient fileManagement) : PropioOneInvocable(invocationContext)
    {
    }
}
