using System.Collections.Generic;

namespace ET
{
    public class LogMsg: Singleton<LogMsg>, ISingletonAwake
    {
        private readonly HashSet<ushort> ignore = new()
        {
            OuterMessage.C2G_Ping, 
            OuterMessage.G2C_Ping, 
            OuterMessage.M2C_Stop,
            OuterMessage.M2C_PathfindingResult,
            OuterMessage.M2C_CreateUnits,
            OuterMessage.M2C_RemoveUnits,
        };

        public void Awake()
        {
        }

        public void Debug(Fiber fiber, object msg)
        {
            ushort opcode = OpcodeType.Instance.GetOpcode(msg.GetType());
            if (this.ignore.Contains(opcode))
            {
                return;
            }
            fiber.Log.Debug(msg.ToString());
        }
    }
}