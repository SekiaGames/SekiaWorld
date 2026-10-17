using Godot;

namespace ET.Client
{
    public static class LoginTokenStore
    {
        private const string FilePath = "user://login.json";

        public static void Load(out string email, out string token)
        {
            email = null;
            token = null;

            if (!FileAccess.FileExists(FilePath))
            {
                return;
            }

            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                Log.Warning($"[登录] 免密存档打不开: {FileAccess.GetOpenError()}");
                return;
            }

            Variant parsed = Json.ParseString(file.GetAsText());
            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                return;
            }

            Godot.Collections.Dictionary data = parsed.AsGodotDictionary();
            email = data.ContainsKey("email") ? data["email"].AsString() : null;
            token = data.ContainsKey("token") ? data["token"].AsString() : null;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                email = null;
                token = null;
            }
        }

        public static void Save(string email, string token)
        {
            Godot.Collections.Dictionary data = new()
            {
                { "email", email },
                { "token", token },
            };

            using FileAccess file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                Log.Warning($"[登录] 免密存档写不了: {FileAccess.GetOpenError()}");
                return;
            }

            file.StoreString(Json.Stringify(data));
        }

        public static void Clear()
        {
            if (!FileAccess.FileExists(FilePath))
            {
                return;
            }

            using DirAccess dir = DirAccess.Open("user://");
            dir?.Remove("login.json");
        }
    }
}
