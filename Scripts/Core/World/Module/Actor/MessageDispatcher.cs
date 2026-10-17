using System;
using System.Collections.Generic;

namespace ET
{
    public class MessageDispatcherInfo
    {
        public SceneType SceneType { get; }
        
        public IMHandler IMHandler { get; }

        public MessageDispatcherInfo(SceneType sceneType, IMHandler imHandler)
        {
            this.SceneType = sceneType;
            this.IMHandler = imHandler;
        }
    }
    
    /// <summary>
    /// Actor消息分发组件
    /// </summary>
    [Code]
    public class MessageDispatcher: Singleton<MessageDispatcher>, ISingletonAwake
    {
        private readonly Dictionary<Type, List<MessageDispatcherInfo>> messageHandlers = new();

        public void Awake()
        {
            // 现在只有 MessageHandlerAttribute 一类处理器。
            // ET8.1 原本还有第二段 GetTypes(typeof(MessageLocationHandlerAttribute)) 的注册循环，
            // 随 Location 机制一并删除（该 Attribute 类本身也已删）。两段循环调的是同一个 Register，
            // 唯一差别是 MessageLocationHandler 会先给发送方回一个 MessageResponse 当 ack。
            HashSet<Type> types = CodeTypes.Instance.GetTypes(typeof (MessageHandlerAttribute));
            
            foreach (Type type in types)
            {
                this.Register(type);
            }
        }
        
        private void Register(Type type)
        {
            object obj = Activator.CreateInstance(type);

            IMHandler imHandler = obj as IMHandler;
            if (imHandler == null)
            {
                throw new Exception($"message handler not inherit IMActorHandler abstract class: {obj.GetType().FullName}");
            }
                
            object[] attrs = type.GetCustomAttributes(typeof(MessageHandlerAttribute), true);

            foreach (object attr in attrs)
            {
                MessageHandlerAttribute messageHandlerAttribute = attr as MessageHandlerAttribute;

                Type messageType = imHandler.GetRequestType();

                Type handleResponseType = imHandler.GetResponseType();
                if (handleResponseType != null)
                {
                    Type responseType = OpcodeType.Instance.GetResponseType(messageType);
                    if (handleResponseType != responseType)
                    {
                        throw new Exception($"message handler response type error: {messageType.FullName}");
                    }
                }

                MessageDispatcherInfo messageDispatcherInfo = new(messageHandlerAttribute.SceneType, imHandler);

                this.RegisterHandler(messageType, messageDispatcherInfo);
            }
        }
        
        private void RegisterHandler(Type type, MessageDispatcherInfo handler)
        {
            if (!this.messageHandlers.ContainsKey(type))
            {
                this.messageHandlers.Add(type, new List<MessageDispatcherInfo>());
            }

            this.messageHandlers[type].Add(handler);
        }

        public async ETTask Handle(Entity entity, Address fromAddress, MessageObject message)
        {
            List<MessageDispatcherInfo> list;
            if (!this.messageHandlers.TryGetValue(message.GetType(), out list))
            {
                throw new Exception($"not found message handler: {message} {entity.GetType().FullName}");
            }

            SceneType sceneType = entity.IScene.SceneType;
            foreach (MessageDispatcherInfo actorMessageDispatcherInfo in list)
            {
                if (!actorMessageDispatcherInfo.SceneType.HasSameFlag(sceneType))
                {
                    continue;
                }
                await actorMessageDispatcherInfo.IMHandler.Handle(entity, fromAddress, message);   
            }
        }
    }
}