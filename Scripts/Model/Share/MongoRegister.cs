using System;
using System.Collections.Generic;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace ET
{
    public static class MongoRegister
    {
        public static void RegisterStruct<T>() where T : struct
        {
            BsonSerializer.RegisterSerializer(typeof (T), new StructBsonSerialize<T>());
        }

        public static void Init()
        {
            // 清理老的数据
            MethodInfo createSerializerRegistry = typeof (BsonSerializer).GetMethod("CreateSerializerRegistry", BindingFlags.Static | BindingFlags.NonPublic);
            createSerializerRegistry.Invoke(null, Array.Empty<object>());
            MethodInfo registerIdGenerators = typeof (BsonSerializer).GetMethod("RegisterIdGenerators", BindingFlags.Static | BindingFlags.NonPublic);
            registerIdGenerators.Invoke(null, Array.Empty<object>());
            
            
            // 2.30.0 起 ObjectSerializer 默认只放行"已登记"的类型，名义类型是 object 时会抛
            // BsonSerializationException —— LogMsg.Debug 一插值就炸，消息一条都发不出去。这里整体放行。
            ObjectSerializer objectSerializer = new(_ => true);
            BsonSerializer.RegisterSerializer(objectSerializer);

            BsonSerializer.RegisterSerializer(typeof (ComponentsCollection), new BsonComponentsCollectionSerializer());
            BsonSerializer.RegisterSerializer(typeof (ChildrenCollection), new BsonChildrenCollectionSerializer());

            // 自动注册IgnoreExtraElements
            ConventionPack conventionPack = new() { new IgnoreExtraElementsConvention(true) };

            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            // struct 的序列化器注册挪到了 Entry.StartAsync（RegisterStruct 已放开为 public）。
            
            foreach (Type type in CodeTypes.Instance.GetTypes().Values)
            {
                RegisterClass(type);
            }
        }

        private static void RegisterClass(Type type)
        {
            if (!type.IsSubclassOf(typeof (Object)))
            {
                return;
            }

            if (type.IsGenericType)
            {
                return;
            }

            if (BsonClassMap.IsClassMapRegistered(type))
            {
                return;
            }

            BsonClassMap cm = new(type);
            cm.AutoMap();
            cm.SetDiscriminator(type.FullName);
            BsonClassMap.RegisterClassMap(cm);
        }
    }
}