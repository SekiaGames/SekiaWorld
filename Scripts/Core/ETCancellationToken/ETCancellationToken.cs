using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ET
{
    public class ETCancellationToken
    {
        private HashSetComponent<Action> actions = HashSetComponent<Action>.Create();

        // 可以带一个数据
        public object Context;

        public void Add(Action callback)
        {
            // 如果action是null，绝对不能添加,要抛异常，说明有协程泄漏
            this.actions.Add(callback);
        }
        
        public void Remove(Action callback)
        {
            this.actions?.Remove(callback);
        }

        public bool IsDispose()
        {
            return this.actions == null;
        }

        public void Cancel()
        {
            if (this.actions == null)
            {
                return;
            }

            this.Invoke();
        }

        private void Invoke()
        {
            using HashSetComponent<Action> runActions = this.actions;
            
            this.actions = null;
            try
            {
                foreach (Action action in runActions)
                {
                    action.Invoke();
                }
            }
            catch (Exception e)
            {
                ETTask.ExceptionHandler.Invoke(e);
            }
        }
    }
}