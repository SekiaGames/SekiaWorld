using Godot;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class MainCameraComponent: Entity, IAwake, IUpdate
    {
        public Camera3D camera;

        public Node3D cameraRoot;

        public bool active;

        public bool following;

        public bool followLogged;

        public Vector3 focusOffset;

        public Vector3 focusViewPort;

        public Vector3 rotation;

        public Quaternion mapRotation;

        public bool rotationFromData;

        public float fieldOfView;

        public float fieldOfViewMin;

        public float fieldOfViewMax;

        public Vector3 followOffset;

        public Vector3 parkedPosition;
    }
}
