using System;

namespace ET
{
    public class AIHandlerAttribute: BaseAttribute
    {
    }

    [AIHandler]
    public abstract class AAIHandler: HandlerObject
    {
        public abstract int Check(AIComponent aiComponent, AIConfig aiConfig);

        public abstract ETTask Execute(AIComponent aiComponent, AIConfig aiConfig, ETCancellationToken cancellationToken);
    }
}
