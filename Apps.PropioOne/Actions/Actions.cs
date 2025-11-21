using Blackbird.Applications.Sdk.Common.Actions;
using Blackbird.Applications.Sdk.Common.Invocation;

namespace Apps.PropioOne.Actions;

[ActionList]
public class Actions(InvocationContext invocationContext) : PropioOneInvocable(invocationContext)
{
    [Action("Action", Description = "Describes the action")]
    public async Task Action()
    {
        
    }
}