using System;
using System.Collections.Generic;
using Godot;

namespace ET.Client
{
    [EntitySystemOf(typeof(RoleComponent))]
    [FriendOf(typeof(RoleComponent))]
    public static partial class RoleComponentSystem
    {
        private const string RoleDir = "res://Resources/Role";

        private const string MountTag = "__role_part";

        [EntitySystem]
        private static void Awake(this RoleComponent self, RoleParts parts)
        {
            self.Parts = parts;
            Assemble(self);
        }

        [EntitySystem]
        private static void Destroy(this RoleComponent self)
        {
            foreach (Node3D node in self.Mounted.Values)
            {
                if (GodotObject.IsInstanceValid(node))
                {
                    node.GetParent()?.RemoveChild(node);
                    node.QueueFree();
                }
            }
            self.Mounted.Clear();

            if (GodotObject.IsInstanceValid(self.Root))
            {
                self.Root.GetParent()?.RemoveChild(self.Root);
                self.Root.QueueFree();
            }
            self.Root = null;
            self.Skeleton = null;
            self.AnimPlayer = null;
            self.AnimTree = null;
        }

        public static void Assemble(this RoleComponent self)
        {
            if (self.Parts == null || self.Parts.Body <= 0)
            {
                Log.Error($"[角色装配] 部件表没有 Body：{self.Parts}");
                return;
            }

            PackedScene bodyScene = LoadPart(PartIndex.Body, self.Parts.Body);
            if (bodyScene == null)
            {
                return;
            }

            self.Root = bodyScene.Instantiate<Node3D>();
            self.Skeleton = FindFirst<Skeleton3D>(self.Root);
            self.AnimPlayer = FindFirst<AnimationPlayer>(self.Root);
            self.AnimTree = FindFirst<AnimationTree>(self.Root);

            if (self.Skeleton == null && HasMountedParts(self.Parts))
            {
                Log.Error($"[角色装配] Body {self.Parts.Body} 里没有 Skeleton3D，部件挂不上去");
            }

            self.Rebuild();
        }

        public static void Rebuild(this RoleComponent self)
        {
            foreach (int slot in new List<int>(self.Mounted.Keys))
            {
                Unmount(self, slot);
            }

            foreach (KeyValuePair<int, string> kv in PartIndex.SlotMount)
            {
                int id = self.Parts[kv.Key];
                if (id > 0)
                {
                    Mount(self, kv.Key, id);
                }
            }

            ApplyHairColor(self);
        }

        public static void ApplyHairColor(this RoleComponent self)
        {
            int color = self.Parts?.HairColorIndex ?? 0;
            if (color <= 0)
            {
                return;
            }

            if (!self.Mounted.TryGetValue(PartIndex.Hair, out Node3D hair) || !GodotObject.IsInstanceValid(hair))
            {
                return;
            }

            string model = hair.GetChildCount() > 0 ? hair.GetChild(0).Name : hair.Name;
            string path = $"res://Art/Model/Role/Hair/{model}/{model}_{color}.material";
            if (!ResourceLoader.Exists(path))
            {
                Log.Warning($"[角色装配] 没有发色材质 {path}（发型 {self.Parts.Hair}，色号 {color}）");
                return;
            }

            Material mat = ResourceLoader.Load<Material>(path);
            Log.Info($"[角色装配] 发色已应用：{path}");
            foreach (MeshInstance3D mesh in FindAll<MeshInstance3D>(hair))
            {
                if (mesh.Mesh == null)
                {
                    continue;
                }
                for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); ++i)
                {
                    mesh.SetSurfaceOverrideMaterial(i, mat);
                }
            }
        }

        public static void RedressPart(this RoleComponent self, int slot, int id)
        {
            self.Parts[slot] = id;
            Unmount(self, slot);
            if (id > 0)
            {
                Mount(self, slot, id);
            }
        }

        public static void PlayAction(this RoleComponent self, string actionName, bool loop = true)
        {
            if (self.AnimTree == null && self.AnimPlayer == null)
            {
                return;
            }

            if (self.AnimTree != null && GodotObject.IsInstanceValid(self.AnimTree)
                    && self.AnimTree.TreeRoot is AnimationNodeStateMachine sm)
            {
                if (sm.HasNode(actionName))
                {
                    AnimationNodeStateMachinePlayback pb = self.AnimTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
                    if (pb != null)
                    {
                        pb.Travel(actionName);
                        self.CurrentAction = actionName;
                        return;
                    }
                }
            }

            PlayActionByPlayer(self, actionName, loop);
        }

        private static void PlayActionByPlayer(RoleComponent self, string actionName, bool loop)
        {
            if (self.AnimPlayer == null || !GodotObject.IsInstanceValid(self.AnimPlayer))
            {
                Log.Warning($"[角色装配] 没有 AnimationPlayer，播不了动作「{actionName}」");
                return;
            }

            string name = actionName;
            if (!self.AnimPlayer.HasAnimation(name))
            {
                string fallback = null;
                foreach (string lib in self.AnimPlayer.GetAnimationList())
                {
                    if (lib.StartsWith(name, StringComparison.Ordinal))
                    {
                        fallback = lib;
                        break;
                    }
                }
                if (fallback == null)
                {
                    Log.Warning($"[角色装配] 动作库里没有「{actionName}」（Body {self.Parts.Body}）");
                    return;
                }
                name = fallback;
            }

            Animation anim = self.AnimPlayer.GetAnimation(name);
            if (anim != null)
            {
                anim.LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
            }
            self.AnimPlayer.Play(name);
            self.CurrentAction = name;
        }

        public static void AttachTo(this RoleComponent self, Node parent, Vector3 position, Vector3 eulerDegrees)
        {
            if (self.Root == null)
            {
                return;
            }

            if (self.Root.GetParent() != null)
            {
                self.Root.GetParent().RemoveChild(self.Root);
            }
            parent?.AddChild(self.Root);
            self.Root.Position = position;
            self.Root.RotationDegrees = eulerDegrees;
        }

        private static void Mount(RoleComponent self, int slot, int id)
        {
            BoneAttachment3D attach = FindMount(self.Skeleton, PartIndex.SlotMount[slot]);
            if (attach == null)
            {
                Log.Warning($"[角色装配] Body {self.Parts.Body} 上找不到挂点 {PartIndex.SlotMount[slot]}（槽 {slot}）");
                return;
            }

            PackedScene scene = LoadPart(slot, id);
            if (scene == null)
            {
                return;
            }

            Node3D node = scene.Instantiate<Node3D>();
            node.SetMeta(MountTag, slot);

            Node3D target = attach.GetChildCount() > 0 && attach.GetChild(0) is Node3D inner ? inner : attach;
            target.AddChild(node);
            self.Mounted[slot] = node;
        }

        private static void Unmount(RoleComponent self, int slot)
        {
            if (!self.Mounted.TryGetValue(slot, out Node3D node))
            {
                return;
            }

            self.Mounted.Remove(slot);
            if (!GodotObject.IsInstanceValid(node))
            {
                return;
            }
            node.GetParent()?.RemoveChild(node);
            node.QueueFree();
        }

        private static BoneAttachment3D FindMount(Skeleton3D skeleton, string mountName)
        {
            if (skeleton == null || !GodotObject.IsInstanceValid(skeleton))
            {
                return null;
            }

            foreach (Node child in skeleton.GetChildren())
            {
                if (child is BoneAttachment3D attach && attach.Name == mountName)
                {
                    return attach;
                }
            }
            return null;
        }

        private static PackedScene LoadPart(int slot, int id)
        {
            string dir = slot >= 1 && slot <= PartIndex.Count ? PartIndex.SlotDir[slot] : null;
            if (dir == null)
            {
                Log.Warning($"[角色装配] 槽 {slot} 在美术工程里还没有对应目录");
                return null;
            }

            string path = $"{RoleDir}/{dir}/{id}.tscn";
            if (!ResourceLoader.Exists(path))
            {
                Log.Warning($"[角色装配] 部件预制体不存在：{path}");
                return null;
            }

            return ResourceLoader.Load<PackedScene>(path);
        }

        private static IEnumerable<T> FindAll<T>(Node node) where T : class
        {
            if (node is T hit)
            {
                yield return hit;
            }

            foreach (Node child in node.GetChildren())
            {
                foreach (T r in FindAll<T>(child))
                {
                    yield return r;
                }
            }
        }

        private static bool HasMountedParts(RoleParts parts)
        {
            if (parts == null)
            {
                return false;
            }

            foreach (int slot in PartIndex.SlotMount.Keys)
            {
                if (parts[slot] > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static T FindFirst<T>(Node node) where T : class
        {
            if (node is T hit)
            {
                return hit;
            }

            foreach (Node child in node.GetChildren())
            {
                T r = FindFirst<T>(child);
                if (r != null)
                {
                    return r;
                }
            }
            return null;
        }
    }
}
