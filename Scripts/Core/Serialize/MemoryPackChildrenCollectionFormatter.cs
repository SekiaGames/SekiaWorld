#nullable enable
using MemoryPack;
using MemoryPack.Internal;

namespace ET
{
    [Preserve]
    public sealed class MemoryPackChildrenCollectionFormatter: MemoryPackFormatter<ChildrenCollection>
    {
        [Preserve]
        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ChildrenCollection? value)
        {
            if (value == null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            var formatter = writer.GetFormatter<Entity>();

            int count = 0;
            foreach (var kv in value)
            {
                if (kv.Value.IsSerializeWithParent)
                {
                    ++count;
                }
            }

            writer.WriteCollectionHeader(count);
            foreach (var kv in value)
            {
                Entity entity = kv.Value;
                if (entity.IsSerializeWithParent)
                {
                    formatter.Serialize(ref writer, ref entity!);
                }
            }
        }

        [Preserve]
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ChildrenCollection? value)
        {
            if (!reader.TryReadCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            if (value == null)
            {
                value = ChildrenCollection.Create(true);
            }
            else
            {
                value.Clear();
            }

            var formatter = reader.GetFormatter<Entity>();
            for (int i = 0; i < length; i++)
            {
                Entity entity = null!;
                formatter.Deserialize(ref reader, ref entity!);
                entity.IsSerializeWithParent = true;
                value.Add(entity.Id, entity);
            }
        }
    }
}
